namespace CRMManagement.Application.DTOs;

public sealed record PublicQuoteLineDto(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int SortOrder);

public sealed record PublicQuoteDto(
    Guid Id,
    string QuoteNumber,
    string Name,
    string Status,
    string Currency,
    decimal Subtotal,
    decimal Discount,
    decimal Tax,
    decimal Total,
    DateTime? ExpiresAt,
    string? Notes,
    string? AccountName,
    DateTime? AcceptedAt,
    string? AcceptedByName,
    IReadOnlyList<PublicQuoteLineDto> Lines);

public sealed record QuoteAcceptRequest(
    string AcceptedByName,
    string? AcceptedByEmail,
    string? SignatureSvg);

public sealed record QuoteAcceptResult(
    bool Success,
    string? ErrorMessage,
    DateTime? AcceptedAt);
