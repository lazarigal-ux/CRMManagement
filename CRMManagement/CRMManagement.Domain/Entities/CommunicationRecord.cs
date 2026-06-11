namespace CRMManagement.Domain.Entities;

/// <summary>
/// Cross-channel communication event ingested from LDataBrain (email, WhatsApp, ...).
/// Joined to a Contact / Account / Opportunity by phone or email at ingestion time.
/// </summary>
public class CommunicationRecord : AuditableEntity
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = "";
    public string Direction { get; set; } = "in";
    public DateTime OccurredAt { get; set; }

    public string? FromAddress { get; set; }
    public string? ToAddress { get; set; }

    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? RawJson { get; set; }

    public Guid? ContactId { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? OpportunityId { get; set; }
    public Guid? LeadId { get; set; }

    public string? ExternalId { get; set; }
}
