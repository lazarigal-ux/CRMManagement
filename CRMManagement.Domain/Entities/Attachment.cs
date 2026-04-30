namespace CRMManagement.Domain.Entities;

public class Attachment : AuditableEntity
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string StoragePath { get; set; } = "";
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string RelatedType { get; set; } = "";
    public Guid RelatedId { get; set; }
    public Guid? UploadedByUserId { get; set; }
}
