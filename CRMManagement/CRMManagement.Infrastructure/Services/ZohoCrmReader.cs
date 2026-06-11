using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

public sealed class ZohoCrmReader : IZohoCrmReader
{
    public const string HttpClientName = "ZohoApi";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _http;
    private readonly IZohoTokenProvider _tokens;
    private readonly IZohoConnectionService _connections;
    private readonly ILogger<ZohoCrmReader> _logger;

    public ZohoCrmReader(
        IHttpClientFactory http,
        IZohoTokenProvider tokens,
        IZohoConnectionService connections,
        ILogger<ZohoCrmReader> logger)
    {
        _http = http;
        _tokens = tokens;
        _connections = connections;
        _logger = logger;
    }

    public Task<ZohoPage<ZohoLeadDto>>    ListLeadsAsync   (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoLeadDto>("Leads",       page, perPage, fields, ct);
    public Task<ZohoLeadDto?>             GetLeadAsync     (string id, CancellationToken ct) => GetByIdAsync<ZohoLeadDto>("Leads",        id, ct);

    public Task<ZohoPage<ZohoContactDto>> ListContactsAsync(int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoContactDto>("Contacts", page, perPage, fields, ct);
    public Task<ZohoContactDto?>          GetContactAsync  (string id, CancellationToken ct) => GetByIdAsync<ZohoContactDto>("Contacts",  id, ct);

    public Task<ZohoPage<ZohoAccountDto>> ListAccountsAsync(int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoAccountDto>("Accounts", page, perPage, fields, ct);
    public Task<ZohoAccountDto?>          GetAccountAsync  (string id, CancellationToken ct) => GetByIdAsync<ZohoAccountDto>("Accounts",  id, ct);

    public Task<ZohoPage<ZohoDealDto>>    ListDealsAsync   (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoDealDto>("Deals",       page, perPage, fields, ct);
    public Task<ZohoDealDto?>             GetDealAsync     (string id, CancellationToken ct) => GetByIdAsync<ZohoDealDto>("Deals",        id, ct);

    public Task<ZohoPage<ZohoProductDto>> ListProductsAsync(int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoProductDto>("Products", page, perPage, fields, ct);
    public Task<ZohoProductDto?>          GetProductAsync  (string id, CancellationToken ct) => GetByIdAsync<ZohoProductDto>("Products",  id, ct);

    public Task<ZohoPage<ZohoQuoteDto>>   ListQuotesAsync  (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoQuoteDto>("Quotes",     page, perPage, fields, ct);
    public Task<ZohoQuoteDto?>            GetQuoteAsync    (string id, CancellationToken ct) => GetByIdAsync<ZohoQuoteDto>("Quotes",      id, ct);

    // Tasks/Calls/Events are sub-modules of Activities in Zoho CRM v6.
    public Task<ZohoPage<ZohoActivityDto>> ListTasksAsync (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoActivityDto>("Tasks",  page, perPage, fields, ct);
    public Task<ZohoPage<ZohoActivityDto>> ListCallsAsync (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoActivityDto>("Calls",  page, perPage, fields, ct);
    public Task<ZohoPage<ZohoActivityDto>> ListEventsAsync(int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoActivityDto>("Events", page, perPage, fields, ct);

    public Task<ZohoPage<ZohoCampaignDto>>  ListCampaignsAsync  (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoCampaignDto>("Campaigns",       page, perPage, fields, ct);
    public Task<ZohoCampaignDto?>           GetCampaignAsync    (string id, CancellationToken ct) => GetByIdAsync<ZohoCampaignDto>("Campaigns",        id, ct);

    public Task<ZohoPage<ZohoCaseDto>>      ListCasesAsync      (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoCaseDto>("Cases",                page, perPage, fields, ct);
    public Task<ZohoCaseDto?>               GetCaseAsync        (string id, CancellationToken ct) => GetByIdAsync<ZohoCaseDto>("Cases",                 id, ct);

    public Task<ZohoPage<ZohoInvoiceDto>>   ListInvoicesAsync   (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoInvoiceDto>("Invoices",          page, perPage, fields, ct);
    public Task<ZohoInvoiceDto?>            GetInvoiceAsync     (string id, CancellationToken ct) => GetByIdAsync<ZohoInvoiceDto>("Invoices",           id, ct);

    public Task<ZohoPage<ZohoSalesOrderDto>> ListSalesOrdersAsync(int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoSalesOrderDto>("Sales_Orders", page, perPage, fields, ct);
    public Task<ZohoSalesOrderDto?>         GetSalesOrderAsync  (string id, CancellationToken ct) => GetByIdAsync<ZohoSalesOrderDto>("Sales_Orders",     id, ct);

    public Task<ZohoPage<ZohoNoteDto>>      ListNotesAsync      (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoNoteDto>("Notes",                page, perPage, fields, ct);
    public Task<ZohoNoteDto?>               GetNoteAsync        (string id, CancellationToken ct) => GetByIdAsync<ZohoNoteDto>("Notes",                 id, ct);

    public Task<ZohoPage<ZohoVendorDto>>        ListVendorsAsync       (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoVendorDto>("Vendors",               page, perPage, fields, ct);
    public Task<ZohoVendorDto?>                 GetVendorAsync         (string id, CancellationToken ct) => GetByIdAsync<ZohoVendorDto>("Vendors",                id, ct);

    public Task<ZohoPage<ZohoPurchaseOrderDto>> ListPurchaseOrdersAsync(int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoPurchaseOrderDto>("Purchase_Orders", page, perPage, fields, ct);
    public Task<ZohoPurchaseOrderDto?>          GetPurchaseOrderAsync  (string id, CancellationToken ct) => GetByIdAsync<ZohoPurchaseOrderDto>("Purchase_Orders",  id, ct);

    public Task<ZohoPage<ZohoSolutionDto>>      ListSolutionsAsync     (int page, int perPage, string? fields, CancellationToken ct) => ListAsync<ZohoSolutionDto>("Solutions",            page, perPage, fields, ct);
    public Task<ZohoSolutionDto?>               GetSolutionAsync       (string id, CancellationToken ct) => GetByIdAsync<ZohoSolutionDto>("Solutions",             id, ct);

    public async Task<IReadOnlyList<ZohoFieldMetadataDto>> ListFieldsAsync(string module, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(module)) return Array.Empty<ZohoFieldMetadataDto>();
        var path = $"crm/v6/settings/fields?module={Uri.EscapeDataString(module)}";
        var raw = await SendAsync(HttpMethod.Get, path, ct);
        if (raw is null) return Array.Empty<ZohoFieldMetadataDto>();
        var parsed = JsonSerializer.Deserialize<ZohoFieldsResponse>(raw, JsonOpts);
        return parsed?.Fields ?? Array.Empty<ZohoFieldMetadataDto>();
    }

    public async Task<ZohoRawPage<T>> ListWithRawAsync<T>(string module, int page, int perPage, string? fields, CancellationToken ct) where T : class
    {
        page = page < 1 ? 1 : page;
        perPage = perPage < 1 ? 50 : Math.Min(perPage, 200);

        var qs = $"page={page}&per_page={perPage}";
        if (!string.IsNullOrWhiteSpace(fields))
            qs += $"&fields={Uri.EscapeDataString(fields)}";

        var path = $"crm/v6/{module}?{qs}";
        var raw = await SendAsync(HttpMethod.Get, path, ct);
        if (raw is null)
            return new ZohoRawPage<T> { Info = new ZohoPageInfo { Page = page, PerPage = perPage } };

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var items = new List<(T, JsonElement)>();
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var elem in data.EnumerateArray())
            {
                var dto = elem.Deserialize<T>(JsonOpts);
                if (dto is null) continue;
                // Clone so the JsonElement remains valid after the JsonDocument is disposed.
                items.Add((dto, elem.Clone()));
            }
        }
        var info = root.TryGetProperty("info", out var infoEl)
            ? infoEl.Deserialize<ZohoPageInfo>(JsonOpts) ?? new ZohoPageInfo()
            : new ZohoPageInfo();

        return new ZohoRawPage<T> { Items = items, Info = info };
    }

    public async Task<ZohoHealthDto> HealthAsync(CancellationToken ct)
    {
        var conn = await _connections.GetAsync(ct);
        if (conn is null) return new ZohoHealthDto(false, "com", false, "Not configured");
        if (!conn.HasRefreshToken) return new ZohoHealthDto(false, conn.Region, false, "Zoho not connected (no refresh token).");

        try
        {
            await _tokens.GetAccessTokenAsync(false, ct);
            return new ZohoHealthDto(true, conn.Region, true, null);
        }
        catch (Exception ex)
        {
            return new ZohoHealthDto(true, conn.Region, false, Truncate(ex.Message, 500));
        }
    }

    private async Task<ZohoPage<T>> ListAsync<T>(string module, int page, int perPage, string? fields, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        perPage = perPage < 1 ? 50 : Math.Min(perPage, 200);

        var qs = $"page={page}&per_page={perPage}";
        if (!string.IsNullOrWhiteSpace(fields))
            qs += $"&fields={Uri.EscapeDataString(fields)}";

        var path = $"crm/v6/{module}?{qs}";
        var raw = await SendAsync(HttpMethod.Get, path, ct);
        if (raw is null)
        {
            // 204 No Content from Zoho when there are no records on this page.
            return new ZohoPage<T>
            {
                Data = Array.Empty<T>(),
                Info = new ZohoPageInfo { Page = page, PerPage = perPage, Count = 0, MoreRecords = false },
            };
        }

        var parsed = JsonSerializer.Deserialize<ZohoPage<T>>(raw, JsonOpts);
        return parsed ?? new ZohoPage<T>
        {
            Data = Array.Empty<T>(),
            Info = new ZohoPageInfo { Page = page, PerPage = perPage, Count = 0, MoreRecords = false },
        };
    }

    private async Task<T?> GetByIdAsync<T>(string module, string id, CancellationToken ct) where T : class
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var path = $"crm/v6/{module}/{Uri.EscapeDataString(id)}";
        var raw = await SendAsync(HttpMethod.Get, path, ct);
        if (raw is null)
            return null;

        // Zoho returns { "data": [ { ... } ] } even for single-record fetches.
        var page = JsonSerializer.Deserialize<ZohoPage<T>>(raw, JsonOpts);
        return page?.Data.FirstOrDefault();
    }

    /// <summary>Returns the raw response body, or null on 204/404. Throws on non-2xx.</summary>
    private async Task<string?> SendAsync(HttpMethod method, string path, CancellationToken ct)
    {
        var conn = await _connections.GetAsync(ct)
            ?? throw new InvalidOperationException("Zoho not configured");
        if (!conn.HasRefreshToken)
            throw new InvalidOperationException("Zoho not connected (no refresh token).");

        var absolute = new Uri($"https://www.zohoapis.{conn.Region}/{path}");

        var token = await _tokens.GetAccessTokenAsync(false, ct);
        var response = await SendWithTokenAsync(method, absolute, token, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            token = await _tokens.GetAccessTokenAsync(true, ct);
            response = await SendWithTokenAsync(method, absolute, token, ct);
        }

        try
        {
            if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
                return null;

            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta
                    ?? (response.Headers.RetryAfter?.Date is DateTimeOffset d
                        ? d - DateTimeOffset.UtcNow
                        : null);
                throw new ZohoRateLimitException(retryAfter, Truncate(body, 1000));
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Zoho API {Method} {Path} returned {Status}: {Body}",
                    method, path, (int)response.StatusCode, Truncate(body, 1000));
                throw new ZohoApiException((int)response.StatusCode, Truncate(body, 2000));
            }

            return body;
        }
        finally
        {
            response.Dispose();
        }
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(HttpMethod method, Uri absoluteUri, string token, CancellationToken ct)
    {
        var client = _http.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(method, absoluteUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", token);
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) || s!.Length <= max ? s ?? "" : s[..max];
}

public class ZohoApiException : Exception
{
    public int Status { get; }
    public string Body { get; }

    public ZohoApiException(int status, string body)
        : base($"Zoho API error {status}: {Trim(body)}")
    {
        Status = status;
        Body = body;
    }

    private static string Trim(string s) => s.Length <= 200 ? s : s[..200];
}

public sealed class ZohoRateLimitException : ZohoApiException
{
    public TimeSpan? RetryAfter { get; }

    public ZohoRateLimitException(TimeSpan? retryAfter, string body)
        : base(429, body)
    {
        RetryAfter = retryAfter;
    }
}
