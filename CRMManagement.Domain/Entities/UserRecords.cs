namespace CRMManagement.Domain.Entities;

public class UserRecentRecord : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public DateTime LastVisitedAt { get; set; }
}

public class UserStarredRecord : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public DateTime StarredAt { get; set; }
}
