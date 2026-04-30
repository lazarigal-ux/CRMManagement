namespace CRMManagement.Domain.Entities;

public class Opportunity : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid? AccountId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid PipelineId { get; set; }
    public Guid StageId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? CloseDate { get; set; }
    public int Probability { get; set; }
    public string Status { get; set; } = "Open";
    public string? LeadSource { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? Description { get; set; }
    public string? NextStep { get; set; }

    public Account? Account { get; set; }
    public Pipeline? Pipeline { get; set; }
    public PipelineStage? Stage { get; set; }
}
