namespace CRMManagement.Domain.Entities;

public class EmailTemplate : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Language { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
}
