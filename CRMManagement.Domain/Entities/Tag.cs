namespace CRMManagement.Domain.Entities;

public class Tag : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Color { get; set; }
    public string Scope { get; set; } = "All";
}

public class AccountTag : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TargetId { get; set; }
    public Guid TagId { get; set; }
}

public class ContactTag : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TargetId { get; set; }
    public Guid TagId { get; set; }
}

public class LeadTag : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TargetId { get; set; }
    public Guid TagId { get; set; }
}
