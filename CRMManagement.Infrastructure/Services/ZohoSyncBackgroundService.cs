using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

/// <summary>
/// Periodically pulls every supported Zoho module into the local DB so that all CRM
/// tabs (Leads, Accounts, Contacts, Opportunities, Activities, Quotes, Products,
/// Campaigns, Tickets, Vendors, Purchase Orders, Solutions) and the dashboard
/// reflect the live Zoho org. The actual import work is delegated to
/// <see cref="IZohoImportService"/> so this service stays a thin scheduler.
///
/// Configuration:
///   Zoho:SyncEnabled (bool, default true)  — master switch
///   Zoho:SyncMinutes (int,  default 15)    — interval between syncs
///   Zoho:SyncStartupDelaySeconds (int, default 30) — delay before first run
/// Disabled at runtime when no Zoho connection is configured (the import would
/// throw "Zoho not configured" anyway).
/// </summary>
public sealed class ZohoSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<ZohoSyncBackgroundService> _logger;

    public ZohoSyncBackgroundService(
        IServiceProvider sp,
        IConfiguration cfg,
        ILogger<ZohoSyncBackgroundService> logger)
    {
        _sp = sp;
        _cfg = cfg;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _cfg.GetValue<bool?>("Zoho:SyncEnabled") ?? true;
        if (!enabled)
        {
            _logger.LogInformation("ZohoSync disabled via Zoho:SyncEnabled=false.");
            return;
        }

        var minutes = _cfg.GetValue<int?>("Zoho:SyncMinutes") ?? 15;
        var startupDelaySec = _cfg.GetValue<int?>("Zoho:SyncStartupDelaySeconds") ?? 30;
        var interval = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 24 * 60));

        try { await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(startupDelaySec, 0, 600)), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var connections = scope.ServiceProvider.GetRequiredService<IZohoConnectionService>();

            var conn = await connections.GetAsync(ct);
            if (conn is null || !conn.HasRefreshToken)
            {
                _logger.LogDebug("ZohoSync skipped: Zoho not connected.");
                return;
            }

            var importer = scope.ServiceProvider.GetRequiredService<IZohoImportService>();

            // Sync every supported module so all tabs stay in sync with Zoho.
            var request = new ZohoImportRequest(
                Leads:          true,
                Contacts:       true,
                Accounts:       true,
                Deals:          true,
                Products:       true,
                Quotes:         true,
                Activities:     true,
                Campaigns:      true,
                Tickets:        true,
                Invoices:       true,
                Orders:         true,
                Notes:          true,
                Vendors:        true,
                PurchaseOrders: true,
                Solutions:      true);

            var job = await importer.RunImportAndWaitAsync(request, ct);
            _logger.LogInformation(
                "ZohoSync completed job {JobId} status={Status} modules={Modules}.",
                job.Id, job.Status, job.Modules);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Host is shutting down — let the loop exit cleanly.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ZohoSync iteration failed; will retry on next interval.");
        }
    }
}
