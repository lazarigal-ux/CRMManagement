namespace CRMManagement.Domain.Entities;

public class Team : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class TeamMember : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public string? Role { get; set; }
}

public class Territory : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
}
