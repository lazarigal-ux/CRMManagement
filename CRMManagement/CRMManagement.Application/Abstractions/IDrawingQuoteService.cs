using CRMManagement.Application.DTOs;

namespace CRMManagement.Application.Abstractions;

/// <summary>
/// Phase 4 — turns an uploaded drawing into a priced quote draft.
///
/// 1. <see cref="AnalyzeAsync"/> — sends the image to the AI gateway, parses the
///    structured count, looks up Product mappings, persists a DrawingAnalysis row.
/// 2. <see cref="GetAsync"/> — returns the analysis state for the preview UI.
/// 3. <see cref="CreateQuoteAsync"/> — accepts the preview (with optional per-line
///    overrides) and creates a draft Quote with line items.
/// </summary>
public interface IDrawingQuoteService
{
    Task<DrawingAnalysisDto> AnalyzeAsync(DrawingAnalyzeRequest request, CancellationToken ct);
    Task<DrawingAnalysisDto?> GetAsync(Guid analysisId, CancellationToken ct);
    Task<Guid> CreateQuoteAsync(CreateQuoteFromAnalysisRequest request, CancellationToken ct);
}

public interface IClassProductMappingService
{
    Task<IReadOnlyList<ClassProductMappingDto>> ListAsync(CancellationToken ct);
    Task<Guid> UpsertAsync(ClassProductMappingUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
