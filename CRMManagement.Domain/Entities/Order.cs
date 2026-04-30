namespace CRMManagement.Domain.Entities;

public class Order : AuditableEntity
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public Guid AccountId { get; set; }
    public Guid? OpportunityId { get; set; }
    public Guid? QuoteId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime OrderDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Notes { get; set; }
    public Guid? OwnerUserId { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}

public class OrderLine : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }

    public Order? Order { get; set; }
}
