using System.Net.Http.Json;
using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

public sealed class IntegrationOutboxService : IIntegrationOutboxService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly AppDbContext _db;
    public IntegrationOutboxService(AppDbContext db) => _db = db;

    public async Task<Guid> EnqueueAsync(string target, object payload, string? relatedType = null, Guid? relatedId = null, CancellationToken ct = default)
    {
        var entry = new IntegrationOutboxEntry
        {
            Id = Guid.NewGuid(),
            Target = target,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOpts),
            Status = "pending",
            RelatedType = relatedType,
            RelatedId = relatedId,
        };
        _db.IntegrationOutbox.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<OutboxRow>> ListAsync(int limit, string? statusFilter, CancellationToken ct)
    {
        var q = _db.IntegrationOutbox.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(statusFilter))
            q = q.Where(x => x.Status == statusFilter);
        return await q.OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .Select(x => new OutboxRow(
                x.Id, x.Target, x.Status, x.Attempts,
                x.LastAttemptAt, x.LastError, x.SentAt,
                x.RelatedType, x.RelatedId, x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<int> RetryAsync(Guid id, CancellationToken ct)
    {
        var entry = await _db.IntegrationOutbox.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null) return 0;
        if (entry.Status == "sent") return 0;
        entry.Status = "pending";
        entry.LastError = null;
        await _db.SaveChangesAsync(ct);
        return 1;
    }

    public async Task<int> RetryAllFailedAsync(CancellationToken ct)
    {
        var failed = await _db.IntegrationOutbox.Where(x => x.Status == "failed").ToListAsync(ct);
        foreach (var f in failed)
        {
            f.Status = "pending";
            f.LastError = null;
        }
        await _db.SaveChangesAsync(ct);
        return failed.Count;
    }
}

/// <summary>
/// Drains pending outbox entries by POSTing to the URL configured for each Target.
/// Lookup table lives in <c>Integrations:Targets:<targetName></c> (BaseUrl + Path + ApiKey).
///
/// Backoff: attempts 1-3 fast (every drain tick); 4-9 mark "failed" — admin can retry.
/// Delivery is at-least-once; the consumer is responsible for idempotency.
/// </summary>
public sealed class IntegrationOutboxDrainer : BackgroundService
{
    private const int MaxAttempts = 9;

    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<IntegrationOutboxDrainer> _logger;

    public IntegrationOutboxDrainer(IServiceProvider sp, IConfiguration cfg, ILogger<IntegrationOutboxDrainer> logger)
    {
        _sp = sp;
        _cfg = cfg;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollSec = _cfg.GetValue<int?>("Integrations:DrainSeconds") ?? 30;
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Outbox drain tick failed."); }

            try { await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(pollSec, 10, 600)), stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var pending = await db.IntegrationOutbox
            .Where(x => x.Status == "pending")
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(ct);
        if (pending.Count == 0) return;

        foreach (var entry in pending)
        {
            var section = _cfg.GetSection($"Integrations:Targets:{entry.Target}");
            var baseUrl = section["BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                // Target not configured yet (e.g. IMS isn't online). Leave as pending — try again later.
                _logger.LogDebug("Outbox: target '{Target}' has no BaseUrl configured — skipping.", entry.Target);
                continue;
            }
            var path = section["Path"] ?? "";
            var apiKey = section["ApiKey"];

            entry.Attempts++;
            entry.LastAttemptAt = DateTime.UtcNow;

            try
            {
                var client = http.CreateClient();
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(20);
                if (!string.IsNullOrWhiteSpace(apiKey))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Internal-Api-Key", apiKey);

                using var content = new StringContent(entry.PayloadJson, System.Text.Encoding.UTF8, "application/json");
                using var resp = await client.PostAsync(path, content, ct);
                if (resp.IsSuccessStatusCode)
                {
                    entry.Status = "sent";
                    entry.SentAt = DateTime.UtcNow;
                    entry.LastError = null;
                }
                else
                {
                    var raw = await resp.Content.ReadAsStringAsync(ct);
                    entry.LastError = $"HTTP {(int)resp.StatusCode}: {Truncate(raw, 1500)}";
                    if (entry.Attempts >= MaxAttempts) entry.Status = "failed";
                }
            }
            catch (Exception ex)
            {
                entry.LastError = Truncate(ex.Message, 1500);
                if (entry.Attempts >= MaxAttempts) entry.Status = "failed";
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];
}
