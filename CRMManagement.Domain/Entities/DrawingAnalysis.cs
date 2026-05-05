namespace CRMManagement.Domain.Entities;

/// <summary>
/// One AI-driven count of a drawing/PDF/SVG. Owns the structured items JSON
/// and links back to the AiInteractionLog row that produced it.
/// Once accepted, a Quote is generated and QuoteId is stamped.
/// </summary>
public class DrawingAnalysis : AuditableEntity
{
    public Guid Id { get; set; }

    public Guid? OpportunityId { get; set; }
    public Guid? AccountId { get; set; }

    public string SourceFileName { get; set; } = "";
    public string MediaType { get; set; } = "image/png";

    /// <summary>Optional plain-language hint from the user, e.g. "count fire alarm devices".</summary>
    public string? Instruction { get; set; }

    /// <summary>"pending" | "analyzing" | "ready" | "failed" | "quoted"</summary>
    public string Status { get; set; } = "pending";

    /// <summary>JSON: { "items": [ { "label": "...", "count": 12, "notes": "..." } ] }</summary>
    public string? ItemsJson { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>FK to crm_ai_interaction_log row that produced this analysis.</summary>
    public Guid? AiLogId { get; set; }

    /// <summary>Stamped once a quote is generated from this analysis.</summary>
    public Guid? QuoteId { get; set; }
}
