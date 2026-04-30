namespace CRMManagement.Domain.Entities;

public class Activity : AuditableEntity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "Task";
    public string Subject { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Priority { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? RelatedType { get; set; }
    public Guid? RelatedId { get; set; }
    public string? Location { get; set; }
    public DateTime? CompletedAt { get; set; }
}
