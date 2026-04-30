namespace CRMManagement.Domain.Entities;

public class Invoice : AuditableEntity
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public Guid AccountId { get; set; }
    public Guid? OrderId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Notes { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}

public class InvoiceLine : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }

    public Invoice? Invoice { get; set; }
}
