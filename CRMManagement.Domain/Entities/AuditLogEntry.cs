namespace CRMManagement.Domain.Entities;

public class AuditLogEntry : AuditableEntity
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Action { get; set; } = "";
    public string? ChangesJson { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime At { get; set; }
}
