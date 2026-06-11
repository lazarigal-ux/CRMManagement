namespace CRMManagement.Domain.Entities;

public class AiInteractionLog
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    // ── Session chain: groups multiple steps toward a final result ──

    /// <summary>Unique session ID — all steps in one editing sequence share this</summary>
    public Guid SessionId { get; set; }

    /// <summary>Step number within the session (1, 2, 3...)</summary>
    public int StepNumber { get; set; }

    /// <summary>Is this the final step the user marked as "done"?</summary>
    public bool IsFinalStep { get; set; }

    // ── Instruction & provider ──

    /// <summary>What the user asked: "ספור גלאים", "remove red lines", etc.</summary>
    public string Instruction { get; set; } = string.Empty;

    /// <summary>AI provider used: openai, claude, gemini, ollama, local</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Execution mode: ai-vision, ai-image-edit, direct, selection-count, analysis</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>Was the request successful?</summary>
    public bool Success { get; set; }

    /// <summary>Error message if failed</summary>
    public string? ErrorMessage { get; set; }

    // ── AI result ──

    /// <summary>AI text response (for analysis) or summary</summary>
    public string? ResultText { get; set; }

    /// <summary>AI JSON response (operations, structured data)</summary>
    public string? ResultJson { get; set; }

    // ── Images for YOLO / Ollama training ──

    /// <summary>Image BEFORE this step was applied (base64 PNG, downscaled)</summary>
    public string? BeforeImageBase64 { get; set; }

    /// <summary>Image AFTER this step was applied (base64 PNG, downscaled)</summary>
    public string? AfterImageBase64 { get; set; }

    /// <summary>Source PDF file name</summary>
    public string? SourceFileName { get; set; }

    /// <summary>DPI used for PDF rasterization</summary>
    public int? SourceDpi { get; set; }

    // ── Timing ──

    /// <summary>Total processing time in ms</summary>
    public int? TotalMs { get; set; }

    /// <summary>Network time in ms</summary>
    public int? NetMs { get; set; }

    // ── Learning context ──

    /// <summary>How many examples were injected as few-shot context</summary>
    public int ExamplesUsed { get; set; }

    /// <summary>User feedback: -1 bad, 0 none, 1 good, 2 perfect (final)</summary>
    public short Feedback { get; set; }

    /// <summary>Linked to AiExample if saved as example</summary>
    public Guid? AiExampleId { get; set; }
}
