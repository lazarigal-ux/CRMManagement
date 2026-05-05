namespace CRMManagement.Domain.Entities;

public class Note : AuditableEntity
{
    public Guid Id { get; set; }
    public string Body { get; set; } = "";
    public string RelatedType { get; set; } = "";
    public Guid RelatedId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public string? Title { get; set; }

    public string? ZohoId { get; set; }
    public DateTime? ZohoCreatedTime { get; set; }
    public DateTime? ZohoModifiedTime { get; set; }
}
