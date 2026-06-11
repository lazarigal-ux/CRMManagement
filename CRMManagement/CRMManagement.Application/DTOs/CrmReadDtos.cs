namespace CRMManagement.Application.DTOs;

public sealed record CrmListResult<T>(int Page, int PerPage, int Count, bool HasMore, IReadOnlyList<T> Items);

public sealed record ZohoLeadListItemDto(
    Guid Id,
    string? ZohoId,
    string FirstName,
    string LastName,
    string? Company,
    string? Email,
    string? Phone,
    string? Status,
    string? Source,
    Guid? OwnerUserId,
    DateTime? ZohoModifiedTime);

public sealed record ZohoContactListItemDto(
    Guid Id,
    string? ZohoId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Mobile,
    Guid? AccountId,
    Guid? OwnerUserId,
    DateTime? ZohoModifiedTime);

public sealed record ZohoAccountListItemDto(
    Guid Id,
    string? ZohoId,
    string Name,
    string? Industry,
    string? Website,
    string? Phone,
    string? AccountType,
    Guid? OwnerUserId,
    DateTime? ZohoModifiedTime);

public sealed record ZohoOpportunityListItemDto(
    Guid Id,
    string? ZohoId,
    string Name,
    Guid? AccountId,
    Guid? ContactId,
    decimal Amount,
    string Currency,
    DateTime? CloseDate,
    int Probability,
    string Status,
    string? LeadSource,
    Guid? OwnerUserId,
    DateTime? ZohoModifiedTime);
