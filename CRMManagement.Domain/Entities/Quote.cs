namespace CRMManagement.Domain.Entities;

public class Quote : AuditableEntity
{
    public Guid Id { get; set; }
    public string QuoteNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public Guid? AccountId { get; set; }
    public Guid? OpportunityId { get; set; }
    public Guid? ContactId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? ExpiresAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Notes { get; set; }
    public Guid? OwnerUserId { get; set; }

    public ICollection<QuoteLine> Lines { get; set; } = new List<QuoteLine>();
}

public class QuoteLine : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }

    public Quote? Quote { get; set; }
}
