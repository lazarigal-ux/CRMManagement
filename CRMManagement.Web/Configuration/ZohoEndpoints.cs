using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Infrastructure.Data;
using CRMManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Web.Configuration;

/// <summary>
/// Read-only API for the imported Zoho data. Endpoints query the local Postgres mirror
/// (populated by ZohoImportService) — they do NOT call Zoho live, so they are not subject
/// to Zoho API rate limits and remain available even when the OAuth token is missing.
/// Mirrors the pattern used by WorkManagement/Jira: import → local DB → Razor Pages + API.
/// </summary>
public static class ZohoEndpoints
{
    private static readonly HashSet<string> SupportedZohoFieldModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "Leads",
        "Contacts",
        "Accounts",
        "Deals",
        "Products",
        "Quotes",
        "Tasks",
        "Calls",
        "Events",
        "Campaigns",
        "Cases",
        "Invoices",
        "Sales_Orders",
        "Notes",
        "Vendors",
        "Purchase_Orders",
        "Solutions",
    };

    public static IEndpointRouteBuilder MapZohoApi(this IEndpointRouteBuilder app)
    {
        // Anonymous like WorkManagement/Jira's read paths: once data is in the local Postgres mirror,
        // it's just data — no Zoho login (or any login) required to view it. The import-trigger
        // endpoints in Program.cs and the OAuth admin pages remain protected.
        var group = app.MapGroup("/api/zoho");

        // Live health-check still uses the Zoho reader so callers can verify the OAuth connection works.
        // Stays authenticated because it makes a live outbound call (avoid being a Zoho-ping proxy for randoms).
        group.MapGet("/health", async (IZohoCrmReader r, CancellationToken ct) =>
            Results.Ok(await r.HealthAsync(ct))).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,SalesManager" });

        group.MapGet("/test-connection", async (
            IZohoConnectionService connections,
            IZohoTokenProvider tokens,
            IZohoCrmReader reader,
            CancellationToken ct) =>
        {
            var checkedAtUtc = DateTime.UtcNow;
            var connection = await connections.GetAsync(ct);
            if (connection is null)
            {
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: false,
                    ConnectionExists: false,
                    Configured: false,
                    Connected: false,
                    TokenAcquired: false,
                    CrmApiReachable: false,
                    Region: "com",
                    CheckedAtUtc: checkedAtUtc,
                    Message: "Zoho connection is not configured.",
                    Error: null,
                    StatusCode: null,
                    RetryAfterSeconds: null));
            }

            var configured = !string.IsNullOrWhiteSpace(connection.ClientId) && connection.HasClientSecret;
            if (!configured)
            {
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: false,
                    ConnectionExists: true,
                    Configured: false,
                    Connected: false,
                    TokenAcquired: false,
                    CrmApiReachable: false,
                    Region: connection.Region,
                    CheckedAtUtc: checkedAtUtc,
                    Message: "Zoho app credentials are incomplete.",
                    Error: null,
                    StatusCode: null,
                    RetryAfterSeconds: null));
            }

            if (!connection.HasRefreshToken)
            {
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: false,
                    ConnectionExists: true,
                    Configured: true,
                    Connected: false,
                    TokenAcquired: false,
                    CrmApiReachable: false,
                    Region: connection.Region,
                    CheckedAtUtc: checkedAtUtc,
                    Message: "Zoho is not connected because no refresh token is stored.",
                    Error: null,
                    StatusCode: null,
                    RetryAfterSeconds: null));
            }

            try
            {
                _ = await tokens.GetAccessTokenAsync(false, ct);
            }
            catch (InvalidOperationException)
            {
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: false,
                    ConnectionExists: true,
                    Configured: true,
                    Connected: true,
                    TokenAcquired: false,
                    CrmApiReachable: false,
                    Region: connection.Region,
                    CheckedAtUtc: checkedAtUtc,
                    Message: "Zoho access token could not be acquired.",
                    Error: "Token acquisition failed.",
                    StatusCode: null,
                    RetryAfterSeconds: null));
            }
            catch (HttpRequestException)
            {
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: false,
                    ConnectionExists: true,
                    Configured: true,
                    Connected: true,
                    TokenAcquired: false,
                    CrmApiReachable: false,
                    Region: connection.Region,
                    CheckedAtUtc: checkedAtUtc,
                    Message: "Zoho access token could not be acquired.",
                    Error: "Token acquisition failed.",
                    StatusCode: null,
                    RetryAfterSeconds: null));
            }

            try
            {
                var fields = await reader.ListFieldsAsync("Leads", ct);
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: true,
                    ConnectionExists: true,
                    Configured: true,
                    Connected: true,
                    TokenAcquired: true,
                    CrmApiReachable: true,
                    Region: connection.Region,
                    CheckedAtUtc: checkedAtUtc,
                    Message: $"Zoho CRM API is reachable. Leads field metadata returned {fields.Count} fields.",
                    Error: null,
                    StatusCode: null,
                    RetryAfterSeconds: null));
            }
            catch (ZohoRateLimitException ex)
            {
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: false,
                    ConnectionExists: true,
                    Configured: true,
                    Connected: true,
                    TokenAcquired: true,
                    CrmApiReachable: false,
                    Region: connection.Region,
                    CheckedAtUtc: checkedAtUtc,
                    Message: "Zoho CRM API is rate limiting requests.",
                    Error: "Rate limited by Zoho CRM API.",
                    StatusCode: StatusCodes.Status429TooManyRequests,
                    RetryAfterSeconds: ToRetryAfterSeconds(ex.RetryAfter)));
            }
            catch (ZohoApiException ex)
            {
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: false,
                    ConnectionExists: true,
                    Configured: true,
                    Connected: true,
                    TokenAcquired: true,
                    CrmApiReachable: false,
                    Region: connection.Region,
                    CheckedAtUtc: checkedAtUtc,
                    Message: "Zoho CRM API returned an error.",
                    Error: $"Zoho CRM API returned HTTP {ex.Status}.",
                    StatusCode: ex.Status,
                    RetryAfterSeconds: null));
            }
            catch (InvalidOperationException)
            {
                return Results.Ok(new ZohoConnectionTestDto(
                    Ok: false,
                    ConnectionExists: true,
                    Configured: true,
                    Connected: true,
                    TokenAcquired: true,
                    CrmApiReachable: false,
                    Region: connection.Region,
                    CheckedAtUtc: checkedAtUtc,
                    Message: "Zoho access token was acquired, but the live CRM API test failed before a successful response.",
                    Error: "Live CRM API test failed.",
                    StatusCode: null,
                    RetryAfterSeconds: null));
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,SalesManager" });

        group.MapGet("/fields/{module}", async (string module, IZohoCrmReader reader, CancellationToken ct) =>
        {
            var normalizedModule = module.Trim();
            if (!SupportedZohoFieldModules.Contains(normalizedModule))
            {
                return Results.BadRequest(new
                {
                    ok = false,
                    message = "Unknown or unsupported Zoho module.",
                    module
                });
            }

            try
            {
                var fields = await reader.ListFieldsAsync(normalizedModule, ct);
                return Results.Ok(new
                {
                    module = normalizedModule,
                    fields
                });
            }
            catch (ZohoRateLimitException ex)
            {
                return ZohoFieldsProblem(
                    normalizedModule,
                    "Zoho CRM API is rate limiting requests.",
                    StatusCodes.Status429TooManyRequests,
                    ToRetryAfterSeconds(ex.RetryAfter));
            }
            catch (ZohoApiException ex)
            {
                return ZohoFieldsProblem(
                    normalizedModule,
                    "Zoho CRM API returned an error while reading field metadata.",
                    ex.Status);
            }
            catch (InvalidOperationException)
            {
                return ZohoFieldsProblem(
                    normalizedModule,
                    "Zoho field metadata could not be read because the Zoho connection is not ready.",
                    StatusCodes.Status503ServiceUnavailable);
            }
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,SalesManager" });

        // Latest import job summary — tells clients how stale the local mirror is.
        group.MapGet("/sync-status", async (IZohoImportService imports, CancellationToken ct) =>
        {
            var dto = await imports.GetLatestJobAsync(ct);
            return dto is null ? Results.NoContent() : Results.Ok(dto);
        });

        group.MapGet("/leads", async (int? page, int? per_page, AppDbContext db, CancellationToken ct) =>
        {
            var (p, pp) = NormalizePaging(page, per_page);
            var q = db.Leads.AsNoTracking().OrderByDescending(l => l.ZohoModifiedTime).ThenByDescending(l => l.Id);
            var total = await q.CountAsync(ct);
            var items = await q.Skip((p - 1) * pp).Take(pp)
                .Select(l => new ZohoLeadListItemDto(
                    l.Id, l.ZohoId, l.FirstName, l.LastName, l.Company, l.Email, l.Phone,
                    l.Status, l.Source, l.OwnerUserId, l.ZohoModifiedTime))
                .ToListAsync(ct);
            return Results.Ok(new CrmListResult<ZohoLeadListItemDto>(p, pp, total, p * pp < total, items));
        });

        group.MapGet("/leads/{id}", async (string id, AppDbContext db, CancellationToken ct) =>
        {
            var hasLocal = Guid.TryParse(id, out var local);
            var dto = await db.Leads.AsNoTracking()
                .Where(l => (hasLocal && l.Id == local) || l.ZohoId == id)
                .Select(l => new ZohoLeadListItemDto(
                    l.Id, l.ZohoId, l.FirstName, l.LastName, l.Company, l.Email, l.Phone,
                    l.Status, l.Source, l.OwnerUserId, l.ZohoModifiedTime))
                .FirstOrDefaultAsync(ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapGet("/contacts", async (int? page, int? per_page, AppDbContext db, CancellationToken ct) =>
        {
            var (p, pp) = NormalizePaging(page, per_page);
            var q = db.Contacts.AsNoTracking().OrderByDescending(c => c.ZohoModifiedTime).ThenByDescending(c => c.Id);
            var total = await q.CountAsync(ct);
            var items = await q.Skip((p - 1) * pp).Take(pp)
                .Select(c => new ZohoContactListItemDto(
                    c.Id, c.ZohoId, c.FirstName, c.LastName, c.Email, c.Phone, c.Mobile,
                    c.AccountId, c.OwnerUserId, c.ZohoModifiedTime))
                .ToListAsync(ct);
            return Results.Ok(new CrmListResult<ZohoContactListItemDto>(p, pp, total, p * pp < total, items));
        });

        group.MapGet("/contacts/{id}", async (string id, AppDbContext db, CancellationToken ct) =>
        {
            var hasLocal = Guid.TryParse(id, out var local);
            var dto = await db.Contacts.AsNoTracking()
                .Where(c => (hasLocal && c.Id == local) || c.ZohoId == id)
                .Select(c => new ZohoContactListItemDto(
                    c.Id, c.ZohoId, c.FirstName, c.LastName, c.Email, c.Phone, c.Mobile,
                    c.AccountId, c.OwnerUserId, c.ZohoModifiedTime))
                .FirstOrDefaultAsync(ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapGet("/accounts", async (int? page, int? per_page, AppDbContext db, CancellationToken ct) =>
        {
            var (p, pp) = NormalizePaging(page, per_page);
            var q = db.Accounts.AsNoTracking().OrderByDescending(a => a.ZohoModifiedTime).ThenByDescending(a => a.Id);
            var total = await q.CountAsync(ct);
            var items = await q.Skip((p - 1) * pp).Take(pp)
                .Select(a => new ZohoAccountListItemDto(
                    a.Id, a.ZohoId, a.Name, a.Industry, a.Website, a.Phone,
                    a.AccountType, a.OwnerUserId, a.ZohoModifiedTime))
                .ToListAsync(ct);
            return Results.Ok(new CrmListResult<ZohoAccountListItemDto>(p, pp, total, p * pp < total, items));
        });

        group.MapGet("/accounts/{id}", async (string id, AppDbContext db, CancellationToken ct) =>
        {
            var hasLocal = Guid.TryParse(id, out var local);
            var dto = await db.Accounts.AsNoTracking()
                .Where(a => (hasLocal && a.Id == local) || a.ZohoId == id)
                .Select(a => new ZohoAccountListItemDto(
                    a.Id, a.ZohoId, a.Name, a.Industry, a.Website, a.Phone,
                    a.AccountType, a.OwnerUserId, a.ZohoModifiedTime))
                .FirstOrDefaultAsync(ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // Zoho Deals are imported into the local Opportunities table (see ZohoImportService.ImportDealsAsync).
        group.MapGet("/deals", async (int? page, int? per_page, AppDbContext db, CancellationToken ct) =>
        {
            var (p, pp) = NormalizePaging(page, per_page);
            var q = db.Opportunities.AsNoTracking().OrderByDescending(o => o.ZohoModifiedTime).ThenByDescending(o => o.Id);
            var total = await q.CountAsync(ct);
            var items = await q.Skip((p - 1) * pp).Take(pp)
                .Select(o => new ZohoOpportunityListItemDto(
                    o.Id, o.ZohoId, o.Name, o.AccountId, o.ContactId, o.Amount, o.Currency,
                    o.CloseDate, o.Probability, o.Status, o.LeadSource, o.OwnerUserId, o.ZohoModifiedTime))
                .ToListAsync(ct);
            return Results.Ok(new CrmListResult<ZohoOpportunityListItemDto>(p, pp, total, p * pp < total, items));
        });

        group.MapGet("/deals/{id}", async (string id, AppDbContext db, CancellationToken ct) =>
        {
            var hasLocal = Guid.TryParse(id, out var local);
            var dto = await db.Opportunities.AsNoTracking()
                .Where(o => (hasLocal && o.Id == local) || o.ZohoId == id)
                .Select(o => new ZohoOpportunityListItemDto(
                    o.Id, o.ZohoId, o.Name, o.AccountId, o.ContactId, o.Amount, o.Currency,
                    o.CloseDate, o.Probability, o.Status, o.LeadSource, o.OwnerUserId, o.ZohoModifiedTime))
                .FirstOrDefaultAsync(ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        return app;
    }

    /// <summary>
    /// Live phase status of the running import — mirrors WorkManagement's /api/jira-import/status.
    /// AllowAnonymous so a global progress banner can poll it from any page (including pre-auth ones).
    /// </summary>
    public static IEndpointRouteBuilder MapZohoImportApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/zoho-import/status", (IZohoImportStatusService status) =>
            Results.Json(status.GetStatus())).AllowAnonymous();
        return app;
    }

    private static (int Page, int PerPage) NormalizePaging(int? page, int? perPage)
        => (Math.Max(1, page ?? 1), Math.Clamp(perPage ?? 50, 1, 200));

    private static int? ToRetryAfterSeconds(TimeSpan? retryAfter) =>
        retryAfter is null ? null : Math.Max(0, (int)Math.Ceiling(retryAfter.Value.TotalSeconds));

    private static IResult ZohoFieldsProblem(string module, string message, int statusCode, int? retryAfterSeconds = null) =>
        Results.Json(new
        {
            ok = false,
            module,
            message,
            statusCode,
            retryAfterSeconds
        }, statusCode: statusCode);
}
