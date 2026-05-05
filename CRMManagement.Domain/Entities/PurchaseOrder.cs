namespace CRMManagement.Domain.Entities;

public class PurchaseOrder : AuditableEntity
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? RequisitionNo { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? RequisitionedById { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? PoDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? CarrierName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Description { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }
    public Guid? OwnerUserId { get; set; }

    public string? ZohoId { get; set; }
    public DateTime? ZohoCreatedTime { get; set; }
    public DateTime? ZohoModifiedTime { get; set; }

    public Vendor? Vendor { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}

public class PurchaseOrderLine : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }

    public string? ZohoId { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
}
