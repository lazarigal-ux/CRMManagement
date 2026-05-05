namespace CRMManagement.Application.DTOs;

public sealed record DrawingAnalyzeRequest(
    Guid? OpportunityId,
    Guid? AccountId,
    string SourceFileName,
    string MediaType,
    string ImageBase64,
    string? Instruction);

public sealed record AnalyzedItemDto(
    string Label,
    int Count,
    string? Notes,
    Guid? MappedProductId,
    string? MappedProductName,
    string? MappedProductSku,
    decimal? UnitPrice,
    decimal? Multiplier,
    bool IsMapped);

public sealed record DrawingAnalysisDto(
    Guid Id,
    Guid? OpportunityId,
    Guid? AccountId,
    string Status,
    string SourceFileName,
    string? Instruction,
    DateTime CreatedAt,
    Guid? AiLogId,
    Guid? QuoteId,
    string? ErrorMessage,
    IReadOnlyList<AnalyzedItemDto> Items);

public sealed record CreateQuoteFromAnalysisRequest(
    Guid AnalysisId,
    string QuoteName,
    string Currency,
    decimal? Discount,
    decimal? Tax,
    string? Notes,
    IReadOnlyList<AnalyzedItemOverrideDto>? Overrides);

public sealed record AnalyzedItemOverrideDto(
    string Label,
    Guid? ProductId,
    decimal? UnitPriceOverride,
    int? CountOverride);

public sealed record ClassProductMappingDto(
    Guid Id,
    string Label,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    decimal Multiplier,
    string? Notes,
    bool IsActive);

public sealed record ClassProductMappingUpsertDto(
    Guid? Id,
    string Label,
    Guid ProductId,
    decimal Multiplier,
    string? Notes,
    bool IsActive);
