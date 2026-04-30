namespace CRMManagement.Domain.Entities;

public class Note : AuditableEntity
{
    public Guid Id { get; set; }
    public string Body { get; set; } = "";
    public string RelatedType { get; set; } = "";
    public Guid RelatedId { get; set; }
    public Guid? AuthorUserId { get; set; }
}
