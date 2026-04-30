namespace CRMManagement.Domain.Entities;

public class Campaign : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Email";
    public string Status { get; set; } = "Planned";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? BudgetedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public string? Description { get; set; }
    public Guid? OwnerUserId { get; set; }

    public ICollection<CampaignMember> Members { get; set; } = new List<CampaignMember>();
}

public class CampaignMember : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? ContactId { get; set; }
    public string Status { get; set; } = "Sent";
    public DateTime? RespondedAt { get; set; }

    public Campaign? Campaign { get; set; }
}
