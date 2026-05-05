using CRMManagement.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

/// <summary>
/// Background poller: fetches recent emails + WhatsApp from LDataBrain on a schedule
/// and ingests them into crm_communications, joining to Contact/Account by phone or email.
///
/// Disabled when LDataBrain:BaseUrl is not configured. Polls every
/// <c>LDataBrain:PollSeconds</c> (default 60) and reaches back
/// <c>LDataBrain:LookbackHours</c> on first run (default 24).
/// </summary>
public sealed class CommsIngestionService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<CommsIngestionService> _logger;

    public CommsIngestionService(IServiceProvider sp, IConfiguration cfg, ILogger<CommsIngestionService> logger)
    {
        _sp = sp;
        _cfg = cfg;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_cfg["LDataBrain:BaseUrl"]))
        {
            _logger.LogInformation("CommsIngestionService disabled: LDataBrain:BaseUrl not configured.");
            return;
        }

        var pollSec = _cfg.GetValue<int?>("LDataBrain:PollSeconds") ?? 60;
        var lookbackHours = _cfg.GetValue<int?>("LDataBrain:LookbackHours") ?? 24;
        var since = DateTime.UtcNow.AddHours(-Math.Abs(lookbackHours));

        // Wait for the app to finish startup before the first poll.
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var newWatermark = await PollOnceAsync(since, stoppingToken);
                if (newWatermark > since) since = newWatermark;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CommsIngestion poll failed.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(pollSec, 15, 600)), stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task<DateTime> PollOnceAsync(DateTime since, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var bridge  = scope.ServiceProvider.GetRequiredService<ILDataBrainBridge>();
        var comms   = scope.ServiceProvider.GetRequiredService<ICommunicationsService>();
        var db      = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();

        var batch = await bridge.FetchRecentCommunicationsAsync(since, ct);
        if (batch.Count == 0) return since;

        // De-duplicate against (provider, externalId) already stored.
        var externalIds = batch.Where(b => !string.IsNullOrWhiteSpace(b.ExternalId))
                               .Select(b => b.ExternalId!).Distinct().ToList();
        var existing = externalIds.Count == 0
            ? new HashSet<string>()
            : (await db.Communications
                .AsNoTracking()
                .Where(c => c.ExternalId != null && externalIds.Contains(c.ExternalId))
                .Select(c => c.Provider + ":" + c.ExternalId)
                .ToListAsync(ct))
                .ToHashSet();

        var max = since;
        foreach (var item in batch)
        {
            var key = item.Provider + ":" + item.ExternalId;
            if (!string.IsNullOrEmpty(item.ExternalId) && existing.Contains(key)) continue;

            try
            {
                await comms.IngestAsync(item, ct);
                if (item.OccurredAt > max) max = item.OccurredAt;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Comms ingest skipped one item ({Provider}/{Id}).", item.Provider, item.ExternalId);
            }
        }

        _logger.LogInformation("CommsIngestion: ingested {Count} new comms (watermark advanced to {Wm:O}).", batch.Count, max);
        return max;
    }
}
