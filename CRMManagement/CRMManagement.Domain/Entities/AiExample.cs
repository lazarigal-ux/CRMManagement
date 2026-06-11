namespace CRMManagement.Domain.Entities;

public class AiExample : AuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>Category like "count-detectors", "remove-hatching", "clean-drawing"</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Searchable tags: {"ספירה","גלאים","detector","count"}</summary>
    public string[] Tags { get; set; } = [];

    /// <summary>The original user instruction</summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>Which AI provider produced this result</summary>
    public string? Provider { get; set; }

    /// <summary>Before image stored as base64 (PNG)</summary>
    public string? BeforeImageBase64 { get; set; }

    /// <summary>After image stored as base64 (PNG) — null for analysis-only results</summary>
    public string? AfterImageBase64 { get; set; }

    /// <summary>AI result as JSON (e.g. {"count":12,"locations":[...]} or operations array)</summary>
    public string? ResultJson { get; set; }

    /// <summary>Plain-text AI answer (for analysis/counting results)</summary>
    public string? ResultText { get; set; }

    /// <summary>User rating: -1 bad, 0 neutral, 1 good</summary>
    public short Rating { get; set; }
}
