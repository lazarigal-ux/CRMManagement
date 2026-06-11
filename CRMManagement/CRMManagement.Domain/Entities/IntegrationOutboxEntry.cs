namespace CRMManagement.Domain.Entities;

/// <summary>
/// Durable queue for outbound integration messages (CRM → IMS, CRM → 3rd-party, ...).
/// A drainer hosted service POSTs each pending entry to its target and updates status.
/// Survives restarts and target downtime — messages just stay <see cref="Status"/> = "pending".
/// </summary>
public class IntegrationOutboxEntry : AuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>Logical target name, e.g. "ims.deal-won". Used to route to a configured base URL + path.</summary>
    public string Target { get; set; } = "";

    /// <summary>JSON body POSTed to the target.</summary>
    public string PayloadJson { get; set; } = "";

    /// <summary>"pending" | "sent" | "failed"</summary>
    public string Status { get; set; } = "pending";

    public int Attempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }

    /// <summary>Optional correlation back to the entity that produced this message.</summary>
    public string? RelatedType { get; set; }
    public Guid? RelatedId { get; set; }
}
