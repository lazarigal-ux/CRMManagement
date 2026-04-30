namespace CRMManagement.Domain.Entities;

public class Lead : AuditableEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Company { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = "New";
    public string? Rating { get; set; }
    public int Score { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public Guid? ConvertedAccountId { get; set; }
    public Guid? ConvertedContactId { get; set; }
    public Guid? ConvertedOpportunityId { get; set; }
    public DateTime? ConvertedAt { get; set; }
}
