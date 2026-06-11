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
    public string? CallType { get; set; }
    public string? CallDurationSeconds { get; set; }
    public string? ActivityType { get; set; }
    public string? EventTitle { get; set; }

    public string? ZohoId { get; set; }
    public DateTime? ZohoCreatedTime { get; set; }
    public DateTime? ZohoModifiedTime { get; set; }
}
