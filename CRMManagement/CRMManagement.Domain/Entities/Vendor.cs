namespace CRMManagement.Domain.Entities;

public class Vendor : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? GlAccount { get; set; }
    public Guid? OwnerUserId { get; set; }
    public bool IsActive { get; set; } = true;

    public string? ZohoId { get; set; }
    public DateTime? ZohoCreatedTime { get; set; }
    public DateTime? ZohoModifiedTime { get; set; }
}
