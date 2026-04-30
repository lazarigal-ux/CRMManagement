namespace CRMManagement.Domain.Entities;

public class Product : AuditableEntity
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Family { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal StandardPrice { get; set; }
    public decimal? Cost { get; set; }
    public string? Unit { get; set; }
}

public class PriceBook : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string Currency { get; set; } = "USD";
}

public class PriceBookEntry : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid PriceBookId { get; set; }
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
