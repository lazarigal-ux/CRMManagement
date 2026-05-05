namespace CRMManagement.Domain.Entities;

public class ZohoConnection : AuditableEntity
{
    public Guid Id { get; set; }

    public int? CompanyId { get; set; }

    public string Region { get; set; } = "com";
    public string ClientId { get; set; } = "";
    public string ClientSecretProtected { get; set; } = "";
    public string? RefreshTokenProtected { get; set; }

    public string? AccountOwnerEmail { get; set; }
    public string? AccountOwnerName { get; set; }

    public DateTime? ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public DateTime? LastImportAt { get; set; }

    public string Status { get; set; } = "Pending";

    public string? LastError { get; set; }
}
