using CRMManagement.Application.DTOs;

namespace CRMManagement.Application.Abstractions;

public interface IZohoTokenProvider
{
    Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken ct);
}

public interface IZohoCrmReader
{
    Task<ZohoPage<ZohoLeadDto>>       ListLeadsAsync      (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoLeadDto?>                GetLeadAsync        (string id, CancellationToken ct);

    Task<ZohoPage<ZohoContactDto>>    ListContactsAsync   (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoContactDto?>             GetContactAsync     (string id, CancellationToken ct);

    Task<ZohoPage<ZohoAccountDto>>    ListAccountsAsync   (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoAccountDto?>             GetAccountAsync     (string id, CancellationToken ct);

    Task<ZohoPage<ZohoDealDto>>       ListDealsAsync      (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoDealDto?>                GetDealAsync        (string id, CancellationToken ct);

    Task<ZohoPage<ZohoProductDto>>    ListProductsAsync   (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoProductDto?>             GetProductAsync     (string id, CancellationToken ct);

    Task<ZohoPage<ZohoQuoteDto>>      ListQuotesAsync     (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoQuoteDto?>               GetQuoteAsync       (string id, CancellationToken ct);

    Task<ZohoPage<ZohoActivityDto>>   ListTasksAsync      (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoPage<ZohoActivityDto>>   ListCallsAsync      (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoPage<ZohoActivityDto>>   ListEventsAsync     (int page, int perPage, string? fields, CancellationToken ct);

    Task<ZohoPage<ZohoCampaignDto>>   ListCampaignsAsync  (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoCampaignDto?>            GetCampaignAsync    (string id, CancellationToken ct);

    Task<ZohoPage<ZohoCaseDto>>       ListCasesAsync      (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoCaseDto?>                GetCaseAsync        (string id, CancellationToken ct);

    Task<ZohoPage<ZohoInvoiceDto>>    ListInvoicesAsync   (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoInvoiceDto?>             GetInvoiceAsync     (string id, CancellationToken ct);

    Task<ZohoPage<ZohoSalesOrderDto>> ListSalesOrdersAsync(int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoSalesOrderDto?>          GetSalesOrderAsync  (string id, CancellationToken ct);

    Task<ZohoPage<ZohoNoteDto>>       ListNotesAsync      (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoNoteDto?>                GetNoteAsync        (string id, CancellationToken ct);

    Task<ZohoPage<ZohoVendorDto>>        ListVendorsAsync       (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoVendorDto?>                 GetVendorAsync         (string id, CancellationToken ct);

    Task<ZohoPage<ZohoPurchaseOrderDto>> ListPurchaseOrdersAsync(int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoPurchaseOrderDto?>          GetPurchaseOrderAsync  (string id, CancellationToken ct);

    Task<ZohoPage<ZohoSolutionDto>>      ListSolutionsAsync     (int page, int perPage, string? fields, CancellationToken ct);
    Task<ZohoSolutionDto?>               GetSolutionAsync       (string id, CancellationToken ct);

    /// <summary>Returns the field metadata for a Zoho module (e.g. "Leads", "Contacts").
    /// Used to discover custom field API names so the importer can request and persist them.</summary>
    Task<IReadOnlyList<ZohoFieldMetadataDto>> ListFieldsAsync(string module, CancellationToken ct);

    /// <summary>Lists records returning both the typed DTO and the raw JSON object so callers
    /// can extract custom-field values not bound to standard DTO properties.</summary>
    Task<ZohoRawPage<T>> ListWithRawAsync<T>(string module, int page, int perPage, string? fields, CancellationToken ct) where T : class;

    Task<ZohoHealthDto>               HealthAsync         (CancellationToken ct);
}
