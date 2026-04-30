namespace CRMManagement.Domain.Entities;

public class Ticket : AuditableEntity
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? Description { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? ContactId { get; set; }
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Normal";
    public string Type { get; set; } = "Question";
    public string Channel { get; set; } = "Web";
    public Guid? OwnerUserId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}

public class TicketComment : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string Body { get; set; } = "";
    public bool IsInternal { get; set; }
    public Guid? AuthorUserId { get; set; }

    public Ticket? Ticket { get; set; }
}
