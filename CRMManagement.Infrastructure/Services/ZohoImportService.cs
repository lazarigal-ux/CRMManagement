using System.Text;
using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using CRMManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

public sealed class ZohoImportService : IZohoImportService
{
    private const int PageSize = 200;
    private const int MaxErrorsRecorded = 50;
    private static readonly JsonSerializerOptions ErrorSerializerOpts = new() { WriteIndented = false };

    private readonly AppDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ZohoImportService> _logger;
    private readonly IZohoImportStatusService _status;

    public ZohoImportService(
        AppDbContext db,
        IServiceScopeFactory scopeFactory,
        ILogger<ZohoImportService> logger,
        IZohoImportStatusService status)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _status = status;
    }

    public async Task<Guid> StartImportAsync(ZohoImportRequest request, CancellationToken ct)
    {
        var job = await CreateJobRowAsync(request, ct);
        _status.Start(job.Id, null);

        // Fire-and-forget on a background Task with its own scope.
        _ = Task.Run(() => RunImportSafelyAsync(job.Id, request));

        return job.Id;
    }

    public async Task<ZohoImportJobDto> RunImportAndWaitAsync(ZohoImportRequest request, CancellationToken ct)
    {
        var job = await CreateJobRowAsync(request, ct);
        _status.Start(job.Id, null);

        // Block until the import completes. Internal token is None so a client disconnect
        // does not abort a partially-applied import; matches the Jira run-and-wait pattern.
        await RunImportSafelyAsync(job.Id, request);

        var dto = await GetJobAsync(job.Id, ct);
        return dto ?? throw new InvalidOperationException("Zoho import job row vanished after run.");
    }

    private async Task<ZohoImportJob> CreateJobRowAsync(ZohoImportRequest request, CancellationToken ct)
    {
        var modules = string.Join(",",
            new[]
            {
                request.Leads      ? "leads"      : null,
                request.Contacts   ? "contacts"   : null,
                request.Accounts   ? "accounts"   : null,
                request.Deals      ? "deals"      : null,
                request.Products   ? "products"   : null,
                request.Quotes     ? "quotes"     : null,
                request.Activities ? "activities" : null,
                request.Campaigns  ? "campaigns"  : null,
                request.Tickets    ? "tickets"    : null,
                request.Invoices   ? "invoices"   : null,
                request.Orders     ? "orders"     : null,
                request.Notes      ? "notes"      : null,
                request.Vendors        ? "vendors"        : null,
                request.PurchaseOrders ? "purchaseorders" : null,
                request.Solutions      ? "solutions"      : null,
            }.Where(s => s is not null)!);

        if (string.IsNullOrEmpty(modules))
            throw new ArgumentException("Select at least one module to import.");

        var job = new ZohoImportJob
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            Status = "Running",
            Modules = modules,
        };
        _db.ZohoImportJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<ZohoImportJobDto?> GetLatestJobAsync(CancellationToken ct)
    {
        var row = await _db.ZohoImportJobs.AsNoTracking()
            .OrderByDescending(j => j.StartedAt)
            .FirstOrDefaultAsync(ct);
        return row is null ? null : ToDto(row);
    }

    public async Task<ZohoImportJobDto?> GetJobAsync(Guid jobId, CancellationToken ct)
    {
        var row = await _db.ZohoImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        return row is null ? null : ToDto(row);
    }

    private async Task RunImportSafelyAsync(Guid jobId, ZohoImportRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var reader = sp.GetRequiredService<IZohoCrmReader>();
        var connections = sp.GetRequiredService<IZohoConnectionService>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var ct = CancellationToken.None;
        var errors = new List<string>();
        var job = await db.ZohoImportJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            _status.Fail($"Job {jobId} not found.");
            return;
        }

        // Total = number of enabled modules; used by the global progress banner to compute %.
        var totalModules = CountEnabledModules(request);
        _status.SetTotal(totalModules);
        var doneModules = 0;

        try
        {
            _status.SetPhase("Resolving connection and pipeline");

            // Pre-resolve owner email → user id once (small cache).
            var ownerCache = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);

            // Cache of Zoho custom-field metadata per module + lazily-resolved local CustomField rows.
            var cfCtx = new CustomFieldContext();

            // Default pipeline + stages — used for opportunities/deals.
            var pipeline = await db.Pipelines.AsNoTracking()
                .OrderByDescending(p => p.IsDefault).ThenBy(p => p.SortOrder).FirstOrDefaultAsync(ct);
            var stages = pipeline is null
                ? new List<PipelineStage>()
                : await db.PipelineStages.AsNoTracking()
                    .Where(s => s.PipelineId == pipeline.Id)
                    .OrderBy(s => s.SortOrder).ToListAsync(ct);

            async Task RunModuleAsync(string name, Func<Task> action)
            {
                _status.SetCurrentModule(name);
                _status.SetPhase($"Importing {name}");
                await action();
                _status.ReportProgress(++doneModules, $"Imported {name}");
            }

            // Order matters — Accounts/Contacts/Products/Deals/Quotes must precede things that reference them.
            if (request.Accounts)
                await RunModuleAsync("Accounts",
                    () => ImportAccountsAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Products)
                await RunModuleAsync("Products",
                    () => ImportProductsAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Contacts)
                await RunModuleAsync("Contacts",
                    () => ImportContactsAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Deals)
            {
                if (pipeline is null || stages.Count == 0)
                {
                    errors.Add("Skipped Deals: no Pipeline / Stages configured.");
                    _status.ReportProgress(++doneModules, "Skipped Deals (no pipeline configured)");
                }
                else
                {
                    await RunModuleAsync("Deals",
                        () => ImportDealsAsync(db, reader, userManager, ownerCache, pipeline, stages, cfCtx, job, errors, ct));
                }
            }

            if (request.Leads)
                await RunModuleAsync("Leads",
                    () => ImportLeadsAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Quotes)
                await RunModuleAsync("Quotes",
                    () => ImportQuotesAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Orders)
                await RunModuleAsync("Orders",
                    () => ImportOrdersAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Invoices)
                await RunModuleAsync("Invoices",
                    () => ImportInvoicesAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Activities)
                await RunModuleAsync("Activities",
                    () => ImportActivitiesAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Campaigns)
                await RunModuleAsync("Campaigns",
                    () => ImportCampaignsAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Tickets)
                await RunModuleAsync("Tickets",
                    () => ImportTicketsAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Notes)
                await RunModuleAsync("Notes",
                    () => ImportNotesAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            // Vendors must precede PurchaseOrders so PO→Vendor lookup resolves.
            if (request.Vendors)
                await RunModuleAsync("Vendors",
                    () => ImportVendorsAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.PurchaseOrders)
                await RunModuleAsync("PurchaseOrders",
                    () => ImportPurchaseOrdersAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            if (request.Solutions)
                await RunModuleAsync("Solutions",
                    () => ImportSolutionsAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct));

            foreach (var ownerEmail in ownerCache
                         .Where(kvp => kvp.Value is null)
                         .Select(kvp => kvp.Key)
                         .OrderBy(email => email, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Warning: Zoho owner email '{ownerEmail}' does not match any local CRM user. Imported records for this owner remain unassigned.");
            }

            job.Status = "Succeeded";
            job.CompletedAt = DateTime.UtcNow;
            if (errors.Count > 0)
            {
                job.ErrorsJson = JsonSerializer.Serialize(errors.Take(MaxErrorsRecorded), ErrorSerializerOpts);
                job.Message = $"Completed with {errors.Count} non-fatal error(s).";
            }
            else
            {
                job.Message = "Completed.";
            }
            await db.SaveChangesAsync(ct);

            try { await connections.MarkImportedAsync(DateTime.UtcNow, ct); } catch { /* non-fatal */ }

            _status.SetCurrentModule(null);
            _status.Complete(job.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zoho import job {JobId} failed.", jobId);
            errors.Add($"Fatal: {ex.Message}");
            job.Status = "Failed";
            job.CompletedAt = DateTime.UtcNow;
            job.Message = ex.Message;
            job.ErrorsJson = JsonSerializer.Serialize(errors.Take(MaxErrorsRecorded), ErrorSerializerOpts);
            try { await db.SaveChangesAsync(ct); } catch { /* swallow — already failing */ }

            _status.Fail(ex.Message);
        }
    }

    private static int CountEnabledModules(ZohoImportRequest r)
    {
        var n = 0;
        if (r.Accounts) n++;
        if (r.Products) n++;
        if (r.Contacts) n++;
        if (r.Deals) n++;
        if (r.Leads) n++;
        if (r.Quotes) n++;
        if (r.Orders) n++;
        if (r.Invoices) n++;
        if (r.Activities) n++;
        if (r.Campaigns) n++;
        if (r.Tickets) n++;
        if (r.Notes) n++;
        if (r.Vendors) n++;
        if (r.PurchaseOrders) n++;
        if (r.Solutions) n++;
        return n;
    }

    // ─── Accounts ─────────────────────────────────────────────────────────────
    private async Task ImportAccountsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        // Buffer parent links to resolve in a second pass after every account is in the DB.
        var parentLinks = new List<(string ChildZohoId, string ParentZohoId)>();

        const string Module = "Accounts";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoAccountDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoAccountDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoAccountDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Accounts page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Accounts.Where(a => a.ZohoId != null && ids.Contains(a.ZohoId!))
                .ToDictionaryAsync(a => a.ZohoId!, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    var billing  = ComposeAddress(z.BillingStreet,  z.BillingCity,  z.BillingState,  z.BillingCode,  z.BillingCountry);
                    var shipping = ComposeAddress(z.ShippingStreet, z.ShippingCity, z.ShippingState, z.ShippingCode, z.ShippingCountry);

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Name             = NotEmpty(z.AccountName, row.Name);
                        row.Industry         = z.Industry        ?? row.Industry;
                        row.Website          = z.Website         ?? row.Website;
                        row.Phone            = z.Phone           ?? row.Phone;
                        row.Description      = z.Description     ?? row.Description;
                        row.AccountType      = z.AccountType     ?? row.AccountType;
                        row.AnnualRevenue    = z.AnnualRevenue   ?? row.AnnualRevenue;
                        row.EmployeeCount    = z.Employees       ?? row.EmployeeCount;
                        row.BillingAddress   = billing           ?? row.BillingAddress;
                        row.ShippingAddress  = shipping          ?? row.ShippingAddress;
                        row.OwnerUserId      = ownerId           ?? row.OwnerUserId;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.AccountsUpdated++;
                    }
                    else
                    {
                        var newRow = new Account
                        {
                            Id               = Guid.NewGuid(),
                            Name             = NotEmpty(z.AccountName, "(unnamed)"),
                            Industry         = z.Industry,
                            Website          = z.Website,
                            Phone            = z.Phone,
                            Description      = z.Description,
                            AccountType      = z.AccountType,
                            AnnualRevenue    = z.AnnualRevenue,
                            EmployeeCount    = z.Employees,
                            BillingAddress   = billing,
                            ShippingAddress  = shipping,
                            OwnerUserId      = ownerId,
                            IsActive         = true,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Accounts.Add(newRow);
                        entityId = newRow.Id;
                        job.AccountsInserted++;
                    }

                    if (!string.IsNullOrEmpty(z.ParentAccount?.Id))
                        parentLinks.Add((z.Id, z.ParentAccount!.Id!));

                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Account", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Account {z.Id}: {ex.Message}"); job.AccountsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }

        // Second pass: resolve parent-account references now that all rows exist.
        if (parentLinks.Count > 0)
        {
            var allZohoIds = parentLinks.SelectMany(p => new[] { p.ChildZohoId, p.ParentZohoId }).Distinct().ToArray();
            var idMap = await db.Accounts
                .Where(a => a.ZohoId != null && allZohoIds.Contains(a.ZohoId!))
                .Select(a => new { a.Id, a.ZohoId })
                .ToDictionaryAsync(a => a.ZohoId!, a => a.Id, ct);

            foreach (var (childZ, parentZ) in parentLinks)
            {
                if (!idMap.TryGetValue(childZ,  out var childId))  continue;
                if (!idMap.TryGetValue(parentZ, out var parentId)) continue;
                var child = await db.Accounts.FirstOrDefaultAsync(a => a.Id == childId, ct);
                if (child is null) continue;
                child.ParentAccountId = parentId;
            }
            await SaveBatchAsync(db, job, errors, ct);
        }
    }

    // ─── Contacts ─────────────────────────────────────────────────────────────
    private async Task ImportContactsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Contacts";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoContactDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoContactDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoContactDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Contacts page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Contacts.Where(c => c.ZohoId != null && ids.Contains(c.ZohoId!))
                .ToDictionaryAsync(c => c.ZohoId!, ct);

            // Map AccountName.id -> our local AccountId.
            var accountZohoIds = batch.Items
                .Select(t => t.Dto.AccountName?.Id)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToArray();
            var accountMap = await db.Accounts.Where(a => a.ZohoId != null && accountZohoIds.Contains(a.ZohoId!))
                .Select(a => new { a.Id, a.ZohoId })
                .ToDictionaryAsync(a => a.ZohoId!, a => a.Id, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    Guid? accountId = z.AccountName?.Id is string acctZohoId && accountMap.TryGetValue(acctZohoId, out var aid)
                        ? aid : null;
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    var address = ComposeAddress(z.MailingStreet, z.MailingCity, z.MailingState, z.MailingZip, z.MailingCountry);
                    var doNotContact = (z.EmailOptOut ?? false) || (z.DoNotCall ?? false);

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.FirstName        = NotEmpty(z.FirstName, row.FirstName);
                        row.LastName         = NotEmpty(z.LastName,  row.LastName);
                        row.Title            = z.Title       ?? row.Title;
                        row.Email            = z.Email       ?? row.Email;
                        row.Phone            = z.Phone       ?? row.Phone;
                        row.Mobile           = z.Mobile      ?? row.Mobile;
                        row.Department       = z.Department  ?? row.Department;
                        row.Description      = z.Description ?? row.Description;
                        row.Address          = address       ?? row.Address;
                        row.AccountId        = accountId     ?? row.AccountId;
                        row.OwnerUserId      = ownerId       ?? row.OwnerUserId;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        // Booleans: only overwrite when Zoho explicitly returned a value.
                        if (z.EmailOptOut.HasValue || z.DoNotCall.HasValue)
                            row.DoNotContact = doNotContact;
                        entityId = row.Id;
                        job.ContactsUpdated++;
                    }
                    else
                    {
                        var newRow = new Contact
                        {
                            Id               = Guid.NewGuid(),
                            FirstName        = NotEmpty(z.FirstName, "(unknown)"),
                            LastName         = NotEmpty(z.LastName,  "(unknown)"),
                            Title            = z.Title,
                            Email            = z.Email,
                            Phone            = z.Phone,
                            Mobile           = z.Mobile,
                            Department       = z.Department,
                            Description      = z.Description,
                            Address          = address,
                            AccountId        = accountId,
                            OwnerUserId      = ownerId,
                            DoNotContact     = doNotContact,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Contacts.Add(newRow);
                        entityId = newRow.Id;
                        job.ContactsInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Contact", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Contact {z.Id}: {ex.Message}"); job.ContactsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Deals (→ our Opportunities) ──────────────────────────────────────────
    private async Task ImportDealsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, Pipeline pipeline, List<PipelineStage> stages,
        CustomFieldContext cfCtx, ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        var stageByName = stages.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
        var defaultStage = stages[0];

        const string Module = "Deals";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoDealDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoDealDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoDealDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Deals page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Opportunities.Where(o => o.ZohoId != null && ids.Contains(o.ZohoId!))
                .ToDictionaryAsync(o => o.ZohoId!, ct);

            var accountZohoIds = batch.Items
                .Select(t => t.Dto.AccountName?.Id)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToArray();
            var accountMap = await db.Accounts.Where(a => a.ZohoId != null && accountZohoIds.Contains(a.ZohoId!))
                .Select(a => new { a.Id, a.ZohoId })
                .ToDictionaryAsync(a => a.ZohoId!, a => a.Id, ct);

            var contactZohoIds = batch.Items
                .Select(t => t.Dto.ContactName?.Id)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToArray();
            var contactMap = await db.Contacts.Where(c => c.ZohoId != null && contactZohoIds.Contains(c.ZohoId!))
                .Select(c => new { c.Id, c.ZohoId })
                .ToDictionaryAsync(c => c.ZohoId!, c => c.Id, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    Guid? accountId = z.AccountName?.Id is string acctZohoId && accountMap.TryGetValue(acctZohoId, out var aid)
                        ? aid : null;
                    Guid? contactId = z.ContactName?.Id is string cZohoId && contactMap.TryGetValue(cZohoId, out var cid)
                        ? cid : null;
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    PipelineStage stage;
                    if (z.Stage is not null && stageByName.TryGetValue(z.Stage, out var matchedStage))
                    {
                        stage = matchedStage;
                    }
                    else
                    {
                        stage = defaultStage;
                        if (!string.IsNullOrEmpty(z.Stage))
                            errors.Add($"Deal {z.Id}: stage '{z.Stage}' not found in pipeline; using default '{defaultStage.Name}'.");
                    }
                    var status = stage.IsWon ? "Won" : stage.IsLost ? "Lost" : "Open";
                    // Zoho Probability is per-record; fall back to the stage's default if unset.
                    var probability = z.Probability ?? stage.Probability;

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Name             = NotEmpty(z.DealName, row.Name);
                        row.AccountId        = accountId ?? row.AccountId;
                        row.ContactId        = contactId ?? row.ContactId;
                        row.Amount           = z.Amount ?? row.Amount;
                        row.CloseDate        = z.ClosingDate?.UtcDateTime ?? row.CloseDate;
                        row.PipelineId       = pipeline.Id;
                        row.StageId          = stage.Id;
                        row.Probability      = probability;
                        row.Status           = status;
                        row.LeadSource       = z.LeadSource  ?? row.LeadSource;
                        row.Description      = z.Description ?? row.Description;
                        row.NextStep         = z.NextStep    ?? row.NextStep;
                        row.Type             = z.Type        ?? row.Type;
                        row.OwnerUserId      = ownerId       ?? row.OwnerUserId;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.DealsUpdated++;
                    }
                    else
                    {
                        var newRow = new Opportunity
                        {
                            Id               = Guid.NewGuid(),
                            Name             = NotEmpty(z.DealName, "(unnamed deal)"),
                            AccountId        = accountId,
                            ContactId        = contactId,
                            PipelineId       = pipeline.Id,
                            StageId          = stage.Id,
                            Amount           = z.Amount ?? 0m,
                            Currency         = "USD",
                            CloseDate        = z.ClosingDate?.UtcDateTime,
                            Probability      = probability,
                            Status           = status,
                            LeadSource       = z.LeadSource,
                            Description      = z.Description,
                            NextStep         = z.NextStep,
                            Type             = z.Type,
                            OwnerUserId      = ownerId,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Opportunities.Add(newRow);
                        entityId = newRow.Id;
                        job.DealsInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Opportunity", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Deal {z.Id}: {ex.Message}"); job.DealsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Leads ────────────────────────────────────────────────────────────────
    private async Task ImportLeadsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Leads";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoLeadDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoLeadDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoLeadDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Leads page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Leads.Where(l => l.ZohoId != null && ids.Contains(l.ZohoId!))
                .ToDictionaryAsync(l => l.ZohoId!, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.FirstName        = NotEmpty(z.FirstName, row.FirstName);
                        row.LastName         = NotEmpty(z.LastName,  row.LastName);
                        row.Title            = z.Title         ?? row.Title;
                        row.Email            = z.Email         ?? row.Email;
                        row.Phone            = z.Phone         ?? row.Phone;
                        row.Mobile           = z.Mobile        ?? row.Mobile;
                        row.Company          = z.Company       ?? row.Company;
                        row.Industry         = z.Industry      ?? row.Industry;
                        row.Website          = z.Website       ?? row.Website;
                        row.Description      = z.Description   ?? row.Description;
                        row.Rating           = z.Rating        ?? row.Rating;
                        row.Source           = z.LeadSource    ?? row.Source;
                        row.Status           = NotEmpty(z.LeadStatus, row.Status);
                        row.Street           = z.Street        ?? row.Street;
                        row.City             = z.City          ?? row.City;
                        row.State            = z.State         ?? row.State;
                        row.ZipCode          = z.ZipCode       ?? row.ZipCode;
                        row.Country          = z.Country       ?? row.Country;
                        row.OwnerUserId      = ownerId         ?? row.OwnerUserId;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.LeadsUpdated++;
                    }
                    else
                    {
                        var newRow = new Lead
                        {
                            Id               = Guid.NewGuid(),
                            FirstName        = NotEmpty(z.FirstName, "(unknown)"),
                            LastName         = NotEmpty(z.LastName,  "(unknown)"),
                            Title            = z.Title,
                            Company          = z.Company,
                            Industry         = z.Industry,
                            Website          = z.Website,
                            Email            = z.Email,
                            Phone            = z.Phone,
                            Mobile           = z.Mobile,
                            Description      = z.Description,
                            Rating           = z.Rating,
                            Status           = NotEmpty(z.LeadStatus, "New"),
                            Source           = NotEmpty(z.LeadSource, "Zoho"),
                            Street           = z.Street,
                            City             = z.City,
                            State            = z.State,
                            ZipCode          = z.ZipCode,
                            Country          = z.Country,
                            OwnerUserId      = ownerId,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Leads.Add(newRow);
                        entityId = newRow.Id;
                        job.LeadsInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Lead", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Lead {z.Id}: {ex.Message}"); job.LeadsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Products ─────────────────────────────────────────────────────────────
    private async Task ImportProductsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Products";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoProductDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoProductDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoProductDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Products page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Products.Where(p => p.ZohoId != null && ids.Contains(p.ZohoId!))
                .ToDictionaryAsync(p => p.ZohoId!, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    // Zoho's Product_Code maps to our Sku. If missing, fall back to the Zoho ID
                    // so the unique-Sku constraint always has something distinct to store.
                    var sku = NotEmpty(z.ProductCode, $"ZOHO-{z.Id}");

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Name             = NotEmpty(z.ProductName, row.Name);
                        row.Sku              = NotEmpty(z.ProductCode, row.Sku);
                        row.Description      = z.Description ?? row.Description;
                        row.Family           = z.Category    ?? row.Family;
                        row.Unit             = z.Unit        ?? row.Unit;
                        row.StandardPrice    = z.UnitPrice ?? row.StandardPrice;
                        row.IsActive         = z.IsActive  ?? row.IsActive;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.ProductsUpdated++;
                    }
                    else
                    {
                        var newRow = new Product
                        {
                            Id               = Guid.NewGuid(),
                            Sku              = sku,
                            Name             = NotEmpty(z.ProductName, "(unnamed)"),
                            Description      = z.Description,
                            Family           = z.Category,
                            Unit             = z.Unit,
                            StandardPrice    = z.UnitPrice ?? 0m,
                            IsActive         = z.IsActive ?? true,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Products.Add(newRow);
                        entityId = newRow.Id;
                        job.ProductsInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Product", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Product {z.Id}: {ex.Message}"); job.ProductsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Quotes ───────────────────────────────────────────────────────────────
    private async Task ImportQuotesAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Quotes";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoQuoteDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoQuoteDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoQuoteDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Quotes page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Quotes.Include(q => q.Lines)
                .Where(q => q.ZohoId != null && ids.Contains(q.ZohoId!))
                .ToDictionaryAsync(q => q.ZohoId!, ct);

            var (accountMap, contactMap, dealMap, productMap) = await ResolveLookupsAsync(db,
                accountZohoIds: batch.Items.Select(t => t.Dto.AccountName?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                contactZohoIds: batch.Items.Select(t => t.Dto.ContactName?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                dealZohoIds:    batch.Items.Select(t => t.Dto.DealName?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                productZohoIds: batch.Items.SelectMany(t => t.Dto.Lines ?? Enumerable.Empty<ZohoQuoteLineDto>())
                                   .Select(l => l.Product?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    Guid? accountId = MapId(z.AccountName?.Id, accountMap);
                    Guid? contactId = MapId(z.ContactName?.Id, contactMap);
                    Guid? dealId    = MapId(z.DealName?.Id,    dealMap);

                    Quote quote;
                    bool isNew = !existing.TryGetValue(z.Id, out var row);
                    if (isNew)
                    {
                        quote = new Quote
                        {
                            Id            = Guid.NewGuid(),
                            QuoteNumber   = NotEmpty(z.Id, $"Q-{Guid.NewGuid():N}"[..12]),
                            ZohoId        = z.Id,
                        };
                        db.Quotes.Add(quote);
                        job.QuotesInserted++;
                    }
                    else
                    {
                        quote = row!;
                        job.QuotesUpdated++;
                    }

                    quote.Name             = NotEmpty(z.Subject, quote.Name);
                    quote.Status           = NotEmpty(z.Stage, NotEmpty(quote.Status, "Draft"));
                    quote.AccountId        = accountId ?? quote.AccountId;
                    quote.ContactId        = contactId ?? quote.ContactId;
                    quote.OpportunityId    = dealId    ?? quote.OpportunityId;
                    quote.ExpiresAt        = z.ValidTill?.UtcDateTime ?? quote.ExpiresAt;
                    quote.Subtotal         = z.Subtotal   ?? quote.Subtotal;
                    quote.Discount         = z.Discount   ?? quote.Discount;
                    quote.Tax              = z.Tax        ?? quote.Tax;
                    quote.Total            = z.GrandTotal ?? quote.Total;
                    quote.Notes            = z.Description ?? quote.Notes;
                    quote.OwnerUserId      = ownerId      ?? quote.OwnerUserId;
                    quote.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? quote.ZohoCreatedTime;
                    quote.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? quote.ZohoModifiedTime;

                    SyncLines(
                        quote.Lines, z.Lines, productMap,
                        lineZohoIdOnDest: l => l.ZohoId,
                        srcZohoId: s => s.Id,
                        srcProductZohoId: s => s.Product?.Id,
                        newLine: () => new QuoteLine { Id = Guid.NewGuid() },
                        updateLine: (line, src, productId, sortOrder) =>
                        {
                            line.QuoteId     = quote.Id;
                            line.ProductId   = productId;
                            line.Description = src.Description;
                            line.Quantity    = src.Quantity ?? 0m;
                            line.UnitPrice   = src.ListPrice ?? 0m;
                            line.Discount    = src.Discount ?? 0m;
                            line.LineTotal   = src.NetTotal ?? src.Total ?? 0m;
                            line.SortOrder   = sortOrder;
                            line.ZohoId      = src.Id;
                        });
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Quote", quote.Id, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Quote {z.Id}: {ex.Message}"); job.QuotesErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Sales Orders (→ our Orders) ──────────────────────────────────────────
    private async Task ImportOrdersAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Sales_Orders";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoSalesOrderDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoSalesOrderDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoSalesOrderDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Sales Orders page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Orders.Include(o => o.Lines)
                .Where(o => o.ZohoId != null && ids.Contains(o.ZohoId!))
                .ToDictionaryAsync(o => o.ZohoId!, ct);

            var (accountMap, _, dealMap, productMap) = await ResolveLookupsAsync(db,
                accountZohoIds: batch.Items.Select(t => t.Dto.AccountName?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                contactZohoIds: Array.Empty<string?>(),
                dealZohoIds:    batch.Items.Select(t => t.Dto.DealName?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                productZohoIds: batch.Items.SelectMany(t => t.Dto.Lines ?? Enumerable.Empty<ZohoSalesOrderLineDto>())
                                   .Select(l => l.Product?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                ct);

            // Quote refs: resolve separately since they're not in the standard 4-tuple.
            var quoteZohoIds = batch.Items.Select(t => t.Dto.QuoteName?.Id).Where(NotEmptyStr).Distinct().ToArray();
            var quoteMap = await db.Quotes
                .Where(q => q.ZohoId != null && quoteZohoIds.Contains(q.ZohoId!))
                .Select(q => new { q.Id, q.ZohoId })
                .ToDictionaryAsync(q => q.ZohoId!, q => q.Id, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    Guid? accountId = MapId(z.AccountName?.Id, accountMap);
                    Guid? dealId    = MapId(z.DealName?.Id,    dealMap);
                    Guid? quoteId   = MapId(z.QuoteName?.Id,   quoteMap);
                    var billing  = ComposeAddress(z.BillingStreet,  z.BillingCity,  z.BillingState,  z.BillingCode,  z.BillingCountry);
                    var shipping = ComposeAddress(z.ShippingStreet, z.ShippingCity, z.ShippingState, z.ShippingCode, z.ShippingCountry);

                    // Order.AccountId is non-nullable. Skip rows that have no resolvable account.
                    if (accountId is null && !existing.ContainsKey(z.Id))
                    {
                        errors.Add($"Sales Order {z.Id}: skipped — no matching local Account for Zoho Account '{z.AccountName?.Id}'.");
                        job.OrdersSkipped++;
                        continue;
                    }

                    Order order;
                    bool isNew = !existing.TryGetValue(z.Id, out var row);
                    if (isNew)
                    {
                        order = new Order
                        {
                            Id          = Guid.NewGuid(),
                            OrderNumber = NotEmpty(z.OrderNumber, $"SO-{Guid.NewGuid():N}"[..14]),
                            AccountId   = accountId!.Value,
                            OrderDate   = z.CreatedTime?.UtcDateTime ?? DateTime.UtcNow,
                            ZohoId      = z.Id,
                        };
                        db.Orders.Add(order);
                        job.OrdersInserted++;
                    }
                    else
                    {
                        order = row!;
                        if (accountId is Guid ai) order.AccountId = ai;
                        job.OrdersUpdated++;
                    }

                    order.Subject          = z.Subject ?? order.Subject;
                    order.Status           = NotEmpty(z.Status, NotEmpty(order.Status, "Draft"));
                    order.OpportunityId    = dealId  ?? order.OpportunityId;
                    order.QuoteId          = quoteId ?? order.QuoteId;
                    order.Subtotal         = z.Subtotal   ?? order.Subtotal;
                    order.Discount         = z.Discount   ?? order.Discount;
                    order.Tax              = z.Tax        ?? order.Tax;
                    order.Total            = z.GrandTotal ?? order.Total;
                    order.Notes            = z.Description ?? order.Notes;
                    order.BillingAddress   = billing  ?? order.BillingAddress;
                    order.ShippingAddress  = shipping ?? order.ShippingAddress;
                    order.OwnerUserId      = ownerId      ?? order.OwnerUserId;
                    order.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? order.ZohoCreatedTime;
                    order.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? order.ZohoModifiedTime;

                    SyncLines(
                        order.Lines, z.Lines, productMap,
                        lineZohoIdOnDest: l => l.ZohoId,
                        srcZohoId: s => s.Id,
                        srcProductZohoId: s => s.Product?.Id,
                        newLine: () => new OrderLine { Id = Guid.NewGuid() },
                        updateLine: (line, src, productId, sortOrder) =>
                        {
                            line.OrderId     = order.Id;
                            line.ProductId   = productId;
                            line.Description = src.Description;
                            line.Quantity    = src.Quantity ?? 0m;
                            line.UnitPrice   = src.ListPrice ?? 0m;
                            line.Discount    = src.Discount ?? 0m;
                            line.LineTotal   = src.NetTotal ?? 0m;
                            line.SortOrder   = sortOrder;
                            line.ZohoId      = src.Id;
                        });
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Order", order.Id, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Sales Order {z.Id}: {ex.Message}"); job.OrdersErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Invoices ─────────────────────────────────────────────────────────────
    private async Task ImportInvoicesAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Invoices";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoInvoiceDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoInvoiceDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoInvoiceDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Invoices page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Invoices.Include(i => i.Lines)
                .Where(i => i.ZohoId != null && ids.Contains(i.ZohoId!))
                .ToDictionaryAsync(i => i.ZohoId!, ct);

            var (accountMap, _, _, productMap) = await ResolveLookupsAsync(db,
                accountZohoIds: batch.Items.Select(t => t.Dto.AccountName?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                contactZohoIds: Array.Empty<string?>(),
                dealZohoIds:    Array.Empty<string?>(),
                productZohoIds: batch.Items.SelectMany(t => t.Dto.Lines ?? Enumerable.Empty<ZohoInvoiceLineDto>())
                                   .Select(l => l.Product?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                ct);

            var orderZohoIds = batch.Items.Select(t => t.Dto.SalesOrder?.Id).Where(NotEmptyStr).Distinct().ToArray();
            var orderMap = await db.Orders
                .Where(o => o.ZohoId != null && orderZohoIds.Contains(o.ZohoId!))
                .Select(o => new { o.Id, o.ZohoId })
                .ToDictionaryAsync(o => o.ZohoId!, o => o.Id, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    Guid? accountId = MapId(z.AccountName?.Id, accountMap);
                    Guid? orderId   = MapId(z.SalesOrder?.Id,  orderMap);
                    var billing  = ComposeAddress(z.BillingStreet,  z.BillingCity,  z.BillingState,  z.BillingCode,  z.BillingCountry);
                    var shipping = ComposeAddress(z.ShippingStreet, z.ShippingCity, z.ShippingState, z.ShippingCode, z.ShippingCountry);

                    if (accountId is null && !existing.ContainsKey(z.Id))
                    {
                        errors.Add($"Invoice {z.Id}: skipped — no matching local Account.");
                        job.InvoicesSkipped++;
                        continue;
                    }

                    Invoice invoice;
                    bool isNew = !existing.TryGetValue(z.Id, out var row);
                    if (isNew)
                    {
                        invoice = new Invoice
                        {
                            Id            = Guid.NewGuid(),
                            InvoiceNumber = NotEmpty(z.InvoiceNumber, $"INV-{Guid.NewGuid():N}"[..14]),
                            AccountId     = accountId!.Value,
                            IssueDate     = z.InvoiceDate?.UtcDateTime ?? DateTime.UtcNow,
                            ZohoId        = z.Id,
                        };
                        db.Invoices.Add(invoice);
                        job.InvoicesInserted++;
                    }
                    else
                    {
                        invoice = row!;
                        if (accountId is Guid ai) invoice.AccountId = ai;
                        job.InvoicesUpdated++;
                    }

                    invoice.Subject          = z.Subject ?? invoice.Subject;
                    invoice.Status           = NotEmpty(z.Status, NotEmpty(invoice.Status, "Draft"));
                    invoice.OrderId          = orderId ?? invoice.OrderId;
                    invoice.IssueDate        = z.InvoiceDate?.UtcDateTime ?? invoice.IssueDate;
                    invoice.DueDate          = z.DueDate?.UtcDateTime ?? invoice.DueDate;
                    invoice.Subtotal         = z.Subtotal   ?? invoice.Subtotal;
                    invoice.Tax              = z.Tax        ?? invoice.Tax;
                    invoice.Total            = z.GrandTotal ?? invoice.Total;
                    if (z.Balance is decimal bal && z.GrandTotal is decimal tot)
                        invoice.AmountPaid = Math.Max(0m, tot - bal);
                    invoice.Notes            = z.Description ?? invoice.Notes;
                    invoice.BillingAddress   = billing  ?? invoice.BillingAddress;
                    invoice.ShippingAddress  = shipping ?? invoice.ShippingAddress;
                    invoice.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? invoice.ZohoCreatedTime;
                    invoice.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? invoice.ZohoModifiedTime;

                    SyncLines(
                        invoice.Lines, z.Lines, productMap,
                        lineZohoIdOnDest: l => l.ZohoId,
                        srcZohoId: s => s.Id,
                        srcProductZohoId: s => s.Product?.Id,
                        newLine: () => new InvoiceLine { Id = Guid.NewGuid() },
                        updateLine: (line, src, productId, sortOrder) =>
                        {
                            line.InvoiceId   = invoice.Id;
                            line.ProductId   = productId;
                            line.Description = src.Description;
                            line.Quantity    = src.Quantity ?? 0m;
                            line.UnitPrice   = src.ListPrice ?? 0m;
                            line.LineTotal   = src.NetTotal ?? 0m;
                            line.SortOrder   = sortOrder;
                            line.ZohoId      = src.Id;
                        });
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Invoice", invoice.Id, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Invoice {z.Id}: {ex.Message}"); job.InvoicesErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Activities (Tasks + Calls + Events) ──────────────────────────────────
    private async Task ImportActivitiesAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        await ImportActivityKindAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct, "Tasks",  "Task");
        await ImportActivityKindAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct, "Calls",  "Call");
        await ImportActivityKindAsync(db, reader, userManager, ownerCache, cfCtx, job, errors, ct, "Events", "Event");
    }

    private async Task ImportActivityKindAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct,
        string moduleLabel, string activityType)
    {
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, moduleLabel, ct);
        var fieldsParam = ComposeFieldsParam(ZohoActivityDto.Fields, cfCtx.CustomApiNamesByModule[moduleLabel]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoActivityDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoActivityDto>(moduleLabel, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"{moduleLabel} page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Activities.Where(a => a.ZohoId != null && ids.Contains(a.ZohoId!))
                .ToDictionaryAsync(a => a.ZohoId!, ct);

            // Resolve What_Id (Account/Deal) and Who_Id (Contact/Lead) → local Ids.
            var whatIds = batch.Items.Select(t => t.Dto.WhatId?.Id).Where(NotEmptyStr).Distinct().ToArray();
            var whoIds  = batch.Items.Select(t => t.Dto.WhoId?.Id).Where(NotEmptyStr).Distinct().ToArray();

            var accountMap = await db.Accounts.Where(a => a.ZohoId != null && whatIds.Contains(a.ZohoId!))
                .Select(a => new { a.Id, a.ZohoId }).ToDictionaryAsync(a => a.ZohoId!, a => a.Id, ct);
            var dealMap = await db.Opportunities.Where(o => o.ZohoId != null && whatIds.Contains(o.ZohoId!))
                .Select(o => new { o.Id, o.ZohoId }).ToDictionaryAsync(o => o.ZohoId!, o => o.Id, ct);
            var contactMap = await db.Contacts.Where(c => c.ZohoId != null && whoIds.Contains(c.ZohoId!))
                .Select(c => new { c.Id, c.ZohoId }).ToDictionaryAsync(c => c.ZohoId!, c => c.Id, ct);
            var leadMap = await db.Leads.Where(l => l.ZohoId != null && whoIds.Contains(l.ZohoId!))
                .Select(l => new { l.Id, l.ZohoId }).ToDictionaryAsync(l => l.ZohoId!, l => l.Id, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    var (relatedType, relatedId) = ResolveActivityRelation(z, accountMap, dealMap, contactMap, leadMap);

                    var subject = NotEmpty(z.EventTitle, NotEmpty(z.Subject, "(no subject)"));
                    var status  = z.Status ?? (activityType == "Event" ? "Planned" : "Open");
                    // Zoho Calls expose Call_Start_Time instead of Due_Date / Start_DateTime, so fold it in for both.
                    var callStart = z.CallStartTime?.UtcDateTime;
                    var startAt = z.StartDateTime?.UtcDateTime ?? (activityType == "Call" ? callStart : null);
                    var endAt   = z.EndDateTime?.UtcDateTime;
                    var dueDate = z.DueDate?.UtcDateTime ?? (activityType == "Call" ? callStart : null);
                    var completedAt = string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase)
                                      ? (DateTime?)(z.ModifiedTime?.UtcDateTime ?? DateTime.UtcNow)
                                      : null;

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Type             = activityType;
                        row.Subject          = subject;
                        row.Description      = z.Description ?? row.Description;
                        row.Status           = status;
                        row.Priority         = z.Priority    ?? row.Priority;
                        row.StartAt          = startAt       ?? row.StartAt;
                        row.EndAt            = endAt         ?? row.EndAt;
                        row.DueDate          = dueDate       ?? row.DueDate;
                        row.Location         = z.Venue       ?? row.Location;
                        row.OwnerUserId      = ownerId       ?? row.OwnerUserId;
                        row.RelatedType      = relatedType   ?? row.RelatedType;
                        row.RelatedId        = relatedId     ?? row.RelatedId;
                        row.CompletedAt      = completedAt   ?? row.CompletedAt;
                        row.CallType         = z.CallType    ?? row.CallType;
                        row.CallDurationSeconds = z.CallDuration ?? row.CallDurationSeconds;
                        row.ActivityType     = z.ActivityType ?? row.ActivityType;
                        row.EventTitle       = z.EventTitle  ?? row.EventTitle;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.ActivitiesUpdated++;
                    }
                    else
                    {
                        var newRow = new Activity
                        {
                            Id               = Guid.NewGuid(),
                            Type             = activityType,
                            Subject          = subject,
                            Description      = z.Description,
                            Status           = status,
                            Priority         = z.Priority,
                            StartAt          = startAt,
                            EndAt            = endAt,
                            DueDate          = dueDate,
                            Location         = z.Venue,
                            OwnerUserId      = ownerId,
                            RelatedType      = relatedType,
                            RelatedId        = relatedId,
                            CompletedAt      = completedAt,
                            CallType         = z.CallType,
                            CallDurationSeconds = z.CallDuration,
                            ActivityType     = z.ActivityType,
                            EventTitle       = z.EventTitle,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Activities.Add(newRow);
                        entityId = newRow.Id;
                        job.ActivitiesInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, moduleLabel, "Activity", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"{moduleLabel} {z.Id}: {ex.Message}"); job.ActivitiesErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    private static (string? Type, Guid? Id) ResolveActivityRelation(

        ZohoActivityDto z,
        Dictionary<string, Guid> accountMap,
        Dictionary<string, Guid> dealMap,
        Dictionary<string, Guid> contactMap,
        Dictionary<string, Guid> leadMap)
    {
        if (z.WhatId?.Id is string whatId)
        {
            if (dealMap.TryGetValue(whatId, out var dId))    return ("Opportunity", dId);
            if (accountMap.TryGetValue(whatId, out var aId)) return ("Account", aId);
        }
        if (z.WhoId?.Id is string whoId)
        {
            if (contactMap.TryGetValue(whoId, out var cId)) return ("Contact", cId);
            if (leadMap.TryGetValue(whoId, out var lId))    return ("Lead", lId);
        }
        return (null, null);
    }

    // ─── Campaigns ────────────────────────────────────────────────────────────
    private async Task ImportCampaignsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Campaigns";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoCampaignDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoCampaignDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoCampaignDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Campaigns page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Campaigns.Where(c => c.ZohoId != null && ids.Contains(c.ZohoId!))
                .ToDictionaryAsync(c => c.ZohoId!, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Name             = NotEmpty(z.Name, row.Name);
                        row.Type             = NotEmpty(z.Type, row.Type);
                        row.Status           = NotEmpty(z.Status, row.Status);
                        row.StartDate        = z.StartDate?.UtcDateTime ?? row.StartDate;
                        row.EndDate          = z.EndDate?.UtcDateTime   ?? row.EndDate;
                        row.Description      = z.Description     ?? row.Description;
                        row.BudgetedCost     = z.BudgetedCost    ?? row.BudgetedCost;
                        row.ActualCost       = z.ActualCost      ?? row.ActualCost;
                        row.ExpectedRevenue  = z.ExpectedRevenue ?? row.ExpectedRevenue;
                        row.NumSent          = z.NumSent         ?? row.NumSent;
                        row.OwnerUserId      = ownerId           ?? row.OwnerUserId;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.CampaignsUpdated++;
                    }
                    else
                    {
                        var newRow = new Campaign
                        {
                            Id               = Guid.NewGuid(),
                            Name             = NotEmpty(z.Name, "(unnamed)"),
                            Type             = NotEmpty(z.Type, "Email"),
                            Status           = NotEmpty(z.Status, "Planned"),
                            StartDate        = z.StartDate?.UtcDateTime,
                            EndDate          = z.EndDate?.UtcDateTime,
                            Description      = z.Description,
                            BudgetedCost     = z.BudgetedCost,
                            ActualCost       = z.ActualCost,
                            ExpectedRevenue  = z.ExpectedRevenue,
                            NumSent          = z.NumSent,
                            OwnerUserId      = ownerId,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Campaigns.Add(newRow);
                        entityId = newRow.Id;
                        job.CampaignsInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Campaign", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Campaign {z.Id}: {ex.Message}"); job.CampaignsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Cases (→ our Tickets) ────────────────────────────────────────────────
    private async Task ImportTicketsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Cases";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoCaseDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoCaseDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoCaseDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Cases page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Tickets.Where(t => t.ZohoId != null && ids.Contains(t.ZohoId!))
                .ToDictionaryAsync(t => t.ZohoId!, ct);

            var (accountMap, contactMap, _, _) = await ResolveLookupsAsync(db,
                accountZohoIds: batch.Items.Select(t => t.Dto.AccountName?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                contactZohoIds: batch.Items.Select(t => t.Dto.ContactName?.Id).Where(NotEmptyStr).Distinct().ToArray(),
                dealZohoIds: Array.Empty<string?>(), productZohoIds: Array.Empty<string?>(), ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId   = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    Guid? accountId = MapId(z.AccountName?.Id, accountMap);
                    Guid? contactId = MapId(z.ContactName?.Id, contactMap);

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Subject          = NotEmpty(z.Subject, row.Subject);
                        row.Description      = z.Description ?? row.Description;
                        row.AccountId        = accountId ?? row.AccountId;
                        row.ContactId        = contactId ?? row.ContactId;
                        row.Status           = NotEmpty(z.Status,   row.Status);
                        row.Priority         = NotEmpty(z.Priority, row.Priority);
                        row.Type             = NotEmpty(z.Type,     row.Type);
                        row.Channel          = NotEmpty(z.CaseOrigin, row.Channel);
                        row.ReportedBy       = z.ReportedBy ?? row.ReportedBy;
                        row.OwnerUserId      = ownerId ?? row.OwnerUserId;
                        row.ClosedAt         = z.ClosedTime?.UtcDateTime ?? row.ClosedAt;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.TicketsUpdated++;
                    }
                    else
                    {
                        var newRow = new Ticket
                        {
                            Id               = Guid.NewGuid(),
                            TicketNumber     = NotEmpty(z.CaseNumber, $"TK-{Guid.NewGuid():N}"[..14]),
                            Subject          = NotEmpty(z.Subject, "(no subject)"),
                            Description      = z.Description,
                            AccountId        = accountId,
                            ContactId        = contactId,
                            Status           = NotEmpty(z.Status,   "New"),
                            Priority         = NotEmpty(z.Priority, "Normal"),
                            Type             = NotEmpty(z.Type,     "Question"),
                            Channel          = NotEmpty(z.CaseOrigin, "Web"),
                            ReportedBy       = z.ReportedBy,
                            OwnerUserId      = ownerId,
                            OpenedAt         = z.CreatedTime?.UtcDateTime ?? DateTime.UtcNow,
                            ClosedAt         = z.ClosedTime?.UtcDateTime,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Tickets.Add(newRow);
                        entityId = newRow.Id;
                        job.TicketsInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Ticket", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Case {z.Id}: {ex.Message}"); job.TicketsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Notes ────────────────────────────────────────────────────────────────
    private async Task ImportNotesAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Notes";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoNoteDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoNoteDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoNoteDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Notes page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Notes.Where(n => n.ZohoId != null && ids.Contains(n.ZohoId!))
                .ToDictionaryAsync(n => n.ZohoId!, ct);

            var parentZohoIds = batch.Items.Select(t => t.Dto.ParentId?.Id).Where(NotEmptyStr).Distinct().ToArray();
            // Notes can attach to any module — resolve against all four primary entity types.
            var accountMap = await db.Accounts.Where(a => a.ZohoId != null && parentZohoIds.Contains(a.ZohoId!))
                .Select(a => new { a.Id, a.ZohoId }).ToDictionaryAsync(a => a.ZohoId!, a => a.Id, ct);
            var contactMap = await db.Contacts.Where(c => c.ZohoId != null && parentZohoIds.Contains(c.ZohoId!))
                .Select(c => new { c.Id, c.ZohoId }).ToDictionaryAsync(c => c.ZohoId!, c => c.Id, ct);
            var leadMap = await db.Leads.Where(l => l.ZohoId != null && parentZohoIds.Contains(l.ZohoId!))
                .Select(l => new { l.Id, l.ZohoId }).ToDictionaryAsync(l => l.ZohoId!, l => l.Id, ct);
            var dealMap = await db.Opportunities.Where(o => o.ZohoId != null && parentZohoIds.Contains(o.ZohoId!))
                .Select(o => new { o.Id, o.ZohoId }).ToDictionaryAsync(o => o.ZohoId!, o => o.Id, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var (relatedType, relatedId) = ResolveNoteParent(z, accountMap, contactMap, leadMap, dealMap);
                    if (relatedId is null)
                    {
                        if (!existing.ContainsKey(z.Id))
                        {
                            errors.Add($"Note {z.Id}: skipped — parent record '{z.ParentId?.Id}' ({z.SeModule}) not found locally.");
                            job.NotesSkipped++;
                            continue;
                        }
                    }

                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    var body = NotEmpty(z.Content, NotEmpty(z.Title, "(empty)"));

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Title            = z.Title       ?? row.Title;
                        row.Body             = body;
                        if (relatedType is not null) row.RelatedType = relatedType;
                        if (relatedId is Guid rid)   row.RelatedId   = rid;
                        row.AuthorUserId     = ownerId       ?? row.AuthorUserId;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.NotesUpdated++;
                    }
                    else
                    {
                        var newRow = new Note
                        {
                            Id               = Guid.NewGuid(),
                            Title            = z.Title,
                            Body             = body,
                            RelatedType      = relatedType!,
                            RelatedId        = relatedId!.Value,
                            AuthorUserId     = ownerId,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Notes.Add(newRow);
                        entityId = newRow.Id;
                        job.NotesInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Note", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Note {z.Id}: {ex.Message}"); job.NotesErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    private static (string? Type, Guid? Id) ResolveNoteParent(
        ZohoNoteDto z,
        Dictionary<string, Guid> accountMap,
        Dictionary<string, Guid> contactMap,
        Dictionary<string, Guid> leadMap,
        Dictionary<string, Guid> dealMap)
    {
        if (z.ParentId?.Id is not string parentId) return (null, null);

        // Prefer the module hint from $se_module, but fall back to whichever map matches.
        var module = z.SeModule;
        if (string.Equals(module, "Accounts", StringComparison.OrdinalIgnoreCase) && accountMap.TryGetValue(parentId, out var a1)) return ("Account", a1);
        if (string.Equals(module, "Contacts", StringComparison.OrdinalIgnoreCase) && contactMap.TryGetValue(parentId, out var c1)) return ("Contact", c1);
        if (string.Equals(module, "Leads",    StringComparison.OrdinalIgnoreCase) && leadMap.TryGetValue(parentId, out var l1))    return ("Lead",    l1);
        if (string.Equals(module, "Deals",    StringComparison.OrdinalIgnoreCase) && dealMap.TryGetValue(parentId, out var d1))    return ("Opportunity", d1);

        if (accountMap.TryGetValue(parentId, out var a2)) return ("Account", a2);
        if (contactMap.TryGetValue(parentId, out var c2)) return ("Contact", c2);
        if (leadMap.TryGetValue(parentId, out var l2))    return ("Lead", l2);
        if (dealMap.TryGetValue(parentId, out var d2))    return ("Opportunity", d2);
        return (null, null);
    }

    // ─── Vendors ──────────────────────────────────────────────────────────────
    private async Task ImportVendorsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Vendors";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoVendorDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoVendorDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoVendorDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Vendors page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Vendors.Where(v => v.ZohoId != null && ids.Contains(v.ZohoId!))
                .ToDictionaryAsync(v => v.ZohoId!, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Name             = NotEmpty(z.Name, row.Name);
                        row.Email            = z.Email       ?? row.Email;
                        row.Phone            = z.Phone       ?? row.Phone;
                        row.Website          = z.Website     ?? row.Website;
                        row.Description      = z.Description ?? row.Description;
                        row.Category         = z.Category    ?? row.Category;
                        row.GlAccount        = z.GlAccount   ?? row.GlAccount;
                        row.Street           = z.Street      ?? row.Street;
                        row.City             = z.City        ?? row.City;
                        row.State            = z.State       ?? row.State;
                        row.ZipCode          = z.ZipCode     ?? row.ZipCode;
                        row.Country          = z.Country     ?? row.Country;
                        row.OwnerUserId      = ownerId       ?? row.OwnerUserId;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.VendorsUpdated++;
                    }
                    else
                    {
                        var newRow = new Vendor
                        {
                            Id               = Guid.NewGuid(),
                            Name             = NotEmpty(z.Name, "(unnamed)"),
                            Email            = z.Email,
                            Phone            = z.Phone,
                            Website          = z.Website,
                            Description      = z.Description,
                            Category         = z.Category,
                            GlAccount        = z.GlAccount,
                            Street           = z.Street,
                            City             = z.City,
                            State            = z.State,
                            ZipCode          = z.ZipCode,
                            Country          = z.Country,
                            OwnerUserId      = ownerId,
                            IsActive         = true,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Vendors.Add(newRow);
                        entityId = newRow.Id;
                        job.VendorsInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Vendor", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Vendor {z.Id}: {ex.Message}"); job.VendorsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Purchase Orders ──────────────────────────────────────────────────────
    private async Task ImportPurchaseOrdersAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Purchase_Orders";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoPurchaseOrderDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoPurchaseOrderDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoPurchaseOrderDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Purchase Orders page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.PurchaseOrders.Include(p => p.Lines)
                .Where(p => p.ZohoId != null && ids.Contains(p.ZohoId!))
                .ToDictionaryAsync(p => p.ZohoId!, ct);

            // Resolve vendor + product lookups.
            var vendorZohoIds = batch.Items.Select(t => t.Dto.VendorName?.Id).Where(NotEmptyStr).Distinct().ToArray();
            var vendorMap = vendorZohoIds.Length == 0 ? new Dictionary<string, Guid>()
                : await db.Vendors.Where(v => v.ZohoId != null && vendorZohoIds.Contains(v.ZohoId!))
                    .Select(v => new { v.Id, v.ZohoId })
                    .ToDictionaryAsync(v => v.ZohoId!, v => v.Id, ct);

            var productZohoIds = batch.Items.SelectMany(t => t.Dto.Lines ?? Enumerable.Empty<ZohoPurchaseOrderLineDto>())
                .Select(l => l.Product?.Id).Where(NotEmptyStr).Distinct().ToArray();
            var productMap = productZohoIds.Length == 0 ? new Dictionary<string, Guid>()
                : await db.Products.Where(p => p.ZohoId != null && productZohoIds.Contains(p.ZohoId!))
                    .Select(p => new { p.Id, p.ZohoId })
                    .ToDictionaryAsync(p => p.ZohoId!, p => p.Id, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    Guid? vendorId = MapId(z.VendorName?.Id, vendorMap);
                    var billing  = ComposeAddress(z.BillingStreet,  z.BillingCity,  z.BillingState,  z.BillingCode,  z.BillingCountry);
                    var shipping = ComposeAddress(z.ShippingStreet, z.ShippingCity, z.ShippingState, z.ShippingCode, z.ShippingCountry);

                    PurchaseOrder po;
                    bool isNew = !existing.TryGetValue(z.Id, out var row);
                    if (isNew)
                    {
                        po = new PurchaseOrder
                        {
                            Id       = Guid.NewGuid(),
                            PoNumber = NotEmpty(z.PoNumber, $"PO-{Guid.NewGuid():N}"[..14]),
                            ZohoId   = z.Id,
                        };
                        db.PurchaseOrders.Add(po);
                        job.PurchaseOrdersInserted++;
                    }
                    else
                    {
                        po = row!;
                        job.PurchaseOrdersUpdated++;
                    }

                    po.Subject            = NotEmpty(z.Subject, NotEmpty(po.Subject, "(no subject)"));
                    po.RequisitionNo      = z.RequisitionNo ?? po.RequisitionNo;
                    po.Status             = NotEmpty(z.Status, NotEmpty(po.Status, "Draft"));
                    po.VendorId           = vendorId ?? po.VendorId;
                    po.PoDate             = z.PoDate?.UtcDateTime ?? po.PoDate;
                    po.DueDate            = z.DueDate?.UtcDateTime ?? po.DueDate;
                    po.CarrierName        = z.Carrier      ?? po.CarrierName;
                    po.Subtotal           = z.Subtotal     ?? po.Subtotal;
                    po.Discount           = z.Discount     ?? po.Discount;
                    po.Tax                = z.Tax          ?? po.Tax;
                    po.AdjustmentAmount   = z.Adjustment   ?? po.AdjustmentAmount;
                    po.Total              = z.GrandTotal   ?? po.Total;
                    po.Description        = z.Description  ?? po.Description;
                    po.TermsAndConditions = z.Terms        ?? po.TermsAndConditions;
                    po.BillingAddress     = billing        ?? po.BillingAddress;
                    po.ShippingAddress    = shipping       ?? po.ShippingAddress;
                    po.OwnerUserId        = ownerId        ?? po.OwnerUserId;
                    po.ZohoCreatedTime    = z.CreatedTime?.UtcDateTime  ?? po.ZohoCreatedTime;
                    po.ZohoModifiedTime   = z.ModifiedTime?.UtcDateTime ?? po.ZohoModifiedTime;

                    SyncLines(
                        po.Lines, z.Lines, productMap,
                        lineZohoIdOnDest: l => l.ZohoId,
                        srcZohoId: s => s.Id,
                        srcProductZohoId: s => s.Product?.Id,
                        newLine: () => new PurchaseOrderLine { Id = Guid.NewGuid() },
                        updateLine: (line, src, productId, sortOrder) =>
                        {
                            line.PurchaseOrderId = po.Id;
                            line.ProductId       = productId;
                            line.Description     = src.Description;
                            line.Quantity        = src.Quantity ?? 0m;
                            line.UnitPrice       = src.ListPrice ?? 0m;
                            line.Discount        = src.Discount ?? 0m;
                            line.LineTotal       = src.NetTotal ?? 0m;
                            line.SortOrder       = sortOrder;
                            line.ZohoId          = src.Id;
                        });
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "PurchaseOrder", po.Id, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Purchase Order {z.Id}: {ex.Message}"); job.PurchaseOrdersErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Solutions ────────────────────────────────────────────────────────────
    private async Task ImportSolutionsAsync(
        AppDbContext db, IZohoCrmReader reader, UserManager<ApplicationUser> userManager,
        Dictionary<string, Guid?> ownerCache, CustomFieldContext cfCtx,
        ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        const string Module = "Solutions";
        await EnsureCustomFieldsLoadedAsync(reader, cfCtx, Module, ct);
        var fieldsParam = ComposeFieldsParam(ZohoSolutionDto.Fields, cfCtx.CustomApiNamesByModule[Module]);

        for (int page = 1; ; page++)
        {
            ZohoRawPage<ZohoSolutionDto> batch;
            try { batch = await reader.ListWithRawAsync<ZohoSolutionDto>(Module, page, PageSize, fieldsParam, ct); }
            catch (Exception ex) { errors.Add($"Solutions page {page}: {ex.Message}"); break; }

            if (batch.Items.Count == 0) break;

            var ids = batch.Items.Select(t => t.Dto.Id).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var existing = await db.Solutions.Where(s => s.ZohoId != null && ids.Contains(s.ZohoId!))
                .ToDictionaryAsync(s => s.ZohoId!, ct);

            var productZohoIds = batch.Items.Select(t => t.Dto.ProductName?.Id).Where(NotEmptyStr).Distinct().ToArray();
            var productMap = productZohoIds.Length == 0 ? new Dictionary<string, Guid>()
                : await db.Products.Where(p => p.ZohoId != null && productZohoIds.Contains(p.ZohoId!))
                    .Select(p => new { p.Id, p.ZohoId })
                    .ToDictionaryAsync(p => p.ZohoId!, p => p.Id, ct);

            foreach (var (z, raw) in batch.Items)
            {
                try
                {
                    var ownerId   = await ResolveOwnerIdAsync(z.Owner?.Email, userManager, ownerCache);
                    Guid? productId = MapId(z.ProductName?.Id, productMap);

                    Guid entityId;
                    if (existing.TryGetValue(z.Id, out var row))
                    {
                        row.Title            = NotEmpty(z.Title, row.Title);
                        row.Question         = z.Question  ?? row.Question;
                        row.Answer           = z.Answer    ?? row.Answer;
                        row.Category         = z.Category  ?? row.Category;
                        row.Status           = NotEmpty(z.Status, row.Status);
                        row.ProductId        = productId   ?? row.ProductId;
                        row.Published        = z.Published ?? row.Published;
                        row.Comments         = z.Comments  ?? row.Comments;
                        row.OwnerUserId      = ownerId     ?? row.OwnerUserId;
                        row.ZohoCreatedTime  = z.CreatedTime?.UtcDateTime  ?? row.ZohoCreatedTime;
                        row.ZohoModifiedTime = z.ModifiedTime?.UtcDateTime ?? row.ZohoModifiedTime;
                        entityId = row.Id;
                        job.SolutionsUpdated++;
                    }
                    else
                    {
                        var newRow = new Solution
                        {
                            Id               = Guid.NewGuid(),
                            SolutionNumber   = NotEmpty(z.SolutionNumber, $"SOL-{Guid.NewGuid():N}"[..14]),
                            Title            = NotEmpty(z.Title, "(no title)"),
                            Question         = z.Question,
                            Answer           = z.Answer,
                            Category         = z.Category,
                            Status           = NotEmpty(z.Status, "Draft"),
                            ProductId        = productId,
                            Published        = z.Published ?? false,
                            Comments         = z.Comments,
                            OwnerUserId      = ownerId,
                            ZohoId           = z.Id,
                            ZohoCreatedTime  = z.CreatedTime?.UtcDateTime,
                            ZohoModifiedTime = z.ModifiedTime?.UtcDateTime,
                        };
                        db.Solutions.Add(newRow);
                        entityId = newRow.Id;
                        job.SolutionsInserted++;
                    }
                    await CaptureCustomFieldsAsync(db, cfCtx, Module, "Solution", entityId, raw, ct);
                }
                catch (Exception ex) { errors.Add($"Solution {z.Id}: {ex.Message}"); job.SolutionsErrored++; }
            }

            await SaveBatchAsync(db, job, errors, ct);
            if (!batch.Info.MoreRecords) break;
        }
    }

    // ─── Custom field capture ────────────────────────────────────────────────

    /// <summary>
    /// Per-import cache of resolved CustomField rows keyed by (EntityType, api_name).
    /// Built lazily as records are imported to avoid extra queries when Zoho returns no custom fields.
    /// </summary>
    private sealed class CustomFieldContext
    {
        public Dictionary<string, HashSet<string>> CustomApiNamesByModule { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, Dictionary<string, string?>> CustomLabelsByModule { get; } = new(StringComparer.Ordinal);
        public Dictionary<(string EntityType, string Name), Guid> CustomFieldIdCache { get; } = new();
    }

    private async Task EnsureCustomFieldsLoadedAsync(
        IZohoCrmReader reader, CustomFieldContext ctx, string module, CancellationToken ct)
    {
        if (ctx.CustomApiNamesByModule.ContainsKey(module)) return;

        IReadOnlyList<ZohoFieldMetadataDto> meta;
        try
        {
            meta = await reader.ListFieldsAsync(module, ct);
        }
        catch
        {
            // Metadata fetch failures are non-fatal — just skip custom field discovery.
            ctx.CustomApiNamesByModule[module] = new HashSet<string>(StringComparer.Ordinal);
            ctx.CustomLabelsByModule[module] = new Dictionary<string, string?>(StringComparer.Ordinal);
            return;
        }

        var customNames = new HashSet<string>(StringComparer.Ordinal);
        var labels = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var f in meta)
        {
            if (string.IsNullOrEmpty(f.ApiName)) continue;
            if (!f.CustomField) continue;
            customNames.Add(f.ApiName);
            labels[f.ApiName] = f.FieldLabel;
        }
        ctx.CustomApiNamesByModule[module] = customNames;
        ctx.CustomLabelsByModule[module] = labels;
    }

    /// <summary>Returns a fields= string combining the DTO's standard fields with any
    /// custom fields discovered for the module.</summary>
    private static string ComposeFieldsParam(string standardFields, HashSet<string> customApiNames)
    {
        if (customApiNames.Count == 0) return standardFields;
        return standardFields + "," + string.Join(",", customApiNames);
    }

    /// <summary>Persist any custom-field values present in the raw Zoho JSON for a record.
    /// Creates CustomField rows on demand keyed by EntityType + api_name.</summary>
    private async Task CaptureCustomFieldsAsync(
        AppDbContext db, CustomFieldContext ctx, string module, string entityType, Guid entityId,
        System.Text.Json.JsonElement raw, CancellationToken ct)
    {
        if (!ctx.CustomApiNamesByModule.TryGetValue(module, out var apiNames) || apiNames.Count == 0) return;
        var labels = ctx.CustomLabelsByModule.TryGetValue(module, out var l) ? l : new Dictionary<string, string?>();

        foreach (var apiName in apiNames)
        {
            if (!raw.TryGetProperty(apiName, out var valueEl)) continue;
            if (valueEl.ValueKind == System.Text.Json.JsonValueKind.Null
                || valueEl.ValueKind == System.Text.Json.JsonValueKind.Undefined) continue;

            var valueText = valueEl.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => valueEl.GetString(),
                System.Text.Json.JsonValueKind.Number => valueEl.GetRawText(),
                System.Text.Json.JsonValueKind.True   => "true",
                System.Text.Json.JsonValueKind.False  => "false",
                _ => valueEl.GetRawText(),
            };
            if (string.IsNullOrWhiteSpace(valueText)) continue;

            var key = (entityType, apiName);
            if (!ctx.CustomFieldIdCache.TryGetValue(key, out var cfId))
            {
                var cf = await db.CustomFields
                    .FirstOrDefaultAsync(c => c.EntityType == entityType && c.Name == apiName, ct);
                if (cf is null)
                {
                    cf = new CustomField
                    {
                        Id = Guid.NewGuid(),
                        EntityType = entityType,
                        Name = apiName,
                        Label = labels.TryGetValue(apiName, out var lab) && !string.IsNullOrWhiteSpace(lab) ? lab! : apiName,
                        DataType = "Text",
                    };
                    db.CustomFields.Add(cf);
                    await db.SaveChangesAsync(ct);
                }
                cfId = cf.Id;
                ctx.CustomFieldIdCache[key] = cfId;
            }

            var existing = await db.CustomFieldValues
                .FirstOrDefaultAsync(v => v.CustomFieldId == cfId && v.EntityId == entityId, ct);
            if (existing is null)
            {
                db.CustomFieldValues.Add(new CustomFieldValue
                {
                    Id = Guid.NewGuid(),
                    CustomFieldId = cfId,
                    EntityId = entityId,
                    ValueText = valueText,
                });
            }
            else
            {
                existing.ValueText = valueText;
            }
        }
    }

    // ─── Shared lookup helpers ────────────────────────────────────────────────

    private static bool NotEmptyStr(string? s) => !string.IsNullOrEmpty(s);

    private static Guid? MapId(string? zohoId, Dictionary<string, Guid> map) =>
        zohoId is not null && map.TryGetValue(zohoId, out var id) ? id : null;

    private static async Task<(
        Dictionary<string, Guid> Accounts,
        Dictionary<string, Guid> Contacts,
        Dictionary<string, Guid> Deals,
        Dictionary<string, Guid> Products)> ResolveLookupsAsync(
        AppDbContext db,
        string?[] accountZohoIds,
        string?[] contactZohoIds,
        string?[] dealZohoIds,
        string?[] productZohoIds,
        CancellationToken ct)
    {
        var accountIds = accountZohoIds.Where(NotEmptyStr).ToArray();
        var contactIds = contactZohoIds.Where(NotEmptyStr).ToArray();
        var dealIds    = dealZohoIds.Where(NotEmptyStr).ToArray();
        var productIds = productZohoIds.Where(NotEmptyStr).ToArray();

        var accounts = accountIds.Length == 0 ? new Dictionary<string, Guid>()
            : await db.Accounts.Where(a => a.ZohoId != null && accountIds.Contains(a.ZohoId!))
                .Select(a => new { a.Id, a.ZohoId })
                .ToDictionaryAsync(a => a.ZohoId!, a => a.Id, ct);

        var contacts = contactIds.Length == 0 ? new Dictionary<string, Guid>()
            : await db.Contacts.Where(c => c.ZohoId != null && contactIds.Contains(c.ZohoId!))
                .Select(c => new { c.Id, c.ZohoId })
                .ToDictionaryAsync(c => c.ZohoId!, c => c.Id, ct);

        var deals = dealIds.Length == 0 ? new Dictionary<string, Guid>()
            : await db.Opportunities.Where(o => o.ZohoId != null && dealIds.Contains(o.ZohoId!))
                .Select(o => new { o.Id, o.ZohoId })
                .ToDictionaryAsync(o => o.ZohoId!, o => o.Id, ct);

        var products = productIds.Length == 0 ? new Dictionary<string, Guid>()
            : await db.Products.Where(p => p.ZohoId != null && productIds.Contains(p.ZohoId!))
                .Select(p => new { p.Id, p.ZohoId })
                .ToDictionaryAsync(p => p.ZohoId!, p => p.Id, ct);

        return (accounts, contacts, deals, products);
    }

    /// <summary>
    /// Reconciles a child collection against an incoming list of Zoho line items.
    /// Lines are matched by Zoho line id (via <paramref name="lineZohoIdOnDest"/> / <paramref name="srcZohoId"/>);
    /// missing lines are removed; new lines are created via <paramref name="newLine"/>;
    /// every kept line is then refreshed via <paramref name="updateLine"/>.
    /// </summary>
    private static void SyncLines<TLine, TSrc>(
        ICollection<TLine> dest,
        List<TSrc>? src,
        Dictionary<string, Guid> productMap,
        Func<TLine, string?> lineZohoIdOnDest,
        Func<TSrc, string?> srcZohoId,
        Func<TSrc, string?> srcProductZohoId,
        Func<TLine> newLine,
        Action<TLine, TSrc, Guid?, int> updateLine)
        where TLine : class
        where TSrc : class
    {
        if (src is null || src.Count == 0)
        {
            dest.Clear();
            return;
        }

        var byZohoId = new Dictionary<string, TLine>(StringComparer.Ordinal);
        foreach (var line in dest)
        {
            var zid = lineZohoIdOnDest(line);
            if (!string.IsNullOrEmpty(zid)) byZohoId[zid!] = line;
        }

        var keep = new HashSet<TLine>();
        int sortOrder = 0;
        foreach (var srcLine in src)
        {
            var zid = srcZohoId(srcLine);
            TLine? line = !string.IsNullOrEmpty(zid) && byZohoId.TryGetValue(zid!, out var existing) ? existing : null;
            if (line is null)
            {
                line = newLine();
                dest.Add(line);
            }

            var productZohoId = srcProductZohoId(srcLine);
            Guid? productId = productZohoId is not null && productMap.TryGetValue(productZohoId, out var pid) ? pid : null;

            updateLine(line, srcLine, productId, sortOrder++);
            keep.Add(line);
        }

        var toRemove = dest.Where(l => !keep.Contains(l)).ToList();
        foreach (var l in toRemove) dest.Remove(l);
    }

    private static async Task<Guid?> ResolveOwnerIdAsync(
        string? email, UserManager<ApplicationUser> userManager, Dictionary<string, Guid?> cache)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        if (cache.TryGetValue(email, out var cached)) return cached;
        var user = await userManager.FindByEmailAsync(email);
        var id = user?.Id;
        cache[email] = id;
        return id;
    }

    private static async Task SaveBatchAsync(AppDbContext db, ZohoImportJob job, List<string> errors, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (Exception ex) { errors.Add($"DB save: {ex.Message}"); }
    }

    private static string NotEmpty(string? candidate, string fallback) =>
        string.IsNullOrWhiteSpace(candidate) ? fallback : candidate!;

    /// <summary>
    /// Joins Zoho's split address parts (Street / City / State / Zip / Country) into a single
    /// multi-line string that fits the local entities' single-string Address columns.
    /// Returns null when every part is blank.
    /// </summary>
    private static string? ComposeAddress(string? street, string? city, string? state, string? zip, string? country)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(street)) sb.AppendLine(street.Trim());

        var cityState = string.Join(", ",
            new[] { city, state }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
        var line2 = string.Join(" ",
            new[] { cityState, zip?.Trim() }.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (!string.IsNullOrWhiteSpace(line2)) sb.AppendLine(line2);

        if (!string.IsNullOrWhiteSpace(country)) sb.AppendLine(country.Trim());

        var result = sb.ToString().TrimEnd();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static ZohoImportJobDto ToDto(ZohoImportJob j) => new(
        j.Id, j.StartedAt, j.CompletedAt, j.Status, j.Modules,
        j.LeadsInserted,      j.LeadsUpdated,      j.LeadsSkipped,      j.LeadsErrored,
        j.ContactsInserted,   j.ContactsUpdated,   j.ContactsSkipped,   j.ContactsErrored,
        j.AccountsInserted,   j.AccountsUpdated,   j.AccountsSkipped,   j.AccountsErrored,
        j.DealsInserted,      j.DealsUpdated,      j.DealsSkipped,      j.DealsErrored,
        j.ProductsInserted,   j.ProductsUpdated,   j.ProductsSkipped,   j.ProductsErrored,
        j.QuotesInserted,     j.QuotesUpdated,     j.QuotesSkipped,     j.QuotesErrored,
        j.ActivitiesInserted, j.ActivitiesUpdated, j.ActivitiesSkipped, j.ActivitiesErrored,
        j.CampaignsInserted,  j.CampaignsUpdated,  j.CampaignsSkipped,  j.CampaignsErrored,
        j.TicketsInserted,    j.TicketsUpdated,    j.TicketsSkipped,    j.TicketsErrored,
        j.InvoicesInserted,   j.InvoicesUpdated,   j.InvoicesSkipped,   j.InvoicesErrored,
        j.OrdersInserted,     j.OrdersUpdated,     j.OrdersSkipped,     j.OrdersErrored,
        j.NotesInserted,      j.NotesUpdated,      j.NotesSkipped,      j.NotesErrored,
        j.VendorsInserted,        j.VendorsUpdated,        j.VendorsSkipped,        j.VendorsErrored,
        j.PurchaseOrdersInserted, j.PurchaseOrdersUpdated, j.PurchaseOrdersSkipped, j.PurchaseOrdersErrored,
        j.SolutionsInserted,      j.SolutionsUpdated,      j.SolutionsSkipped,      j.SolutionsErrored,
        j.ErrorsJson, j.Message);
}
