namespace CRMManagement.Domain.Entities;

public class Account : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? LegalName { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public int? EmployeeCount { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? ParentAccountId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
