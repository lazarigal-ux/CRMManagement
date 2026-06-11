namespace CRMManagement.Domain.Entities;

public class Pipeline : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }

    public ICollection<PipelineStage> Stages { get; set; } = new List<PipelineStage>();
}

public class PipelineStage : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid PipelineId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public int Probability { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }

    /// <summary>SLA in hours. If an opportunity sits in this stage longer, it's "stale".</summary>
    public int? SlaHours { get; set; }

    public Pipeline? Pipeline { get; set; }
}
