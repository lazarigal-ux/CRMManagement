namespace CRMManagement.Domain.Entities;

/// <summary>
/// Maps an AI-detected class label (e.g. "smoke detector", "sprinkler") to a Product
/// in the catalog so a counted drawing can be turned into priced quote line items.
/// </summary>
public class ClassProductMapping : AuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>Lowercased label as the AI emits it (matched case-insensitively).</summary>
    public string Label { get; set; } = "";

    public Guid ProductId { get; set; }

    /// <summary>Per-instance multiplier (e.g. 1.1 = 10% extra for cabling waste).</summary>
    public decimal Multiplier { get; set; } = 1.0m;

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public Product? Product { get; set; }
}
