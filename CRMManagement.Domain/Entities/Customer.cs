using System.ComponentModel.DataAnnotations;

namespace CRMManagement.Domain.Entities;

/// <summary>
/// Service customer directory, relocated from the Service module's
/// <c>service.svc_customers</c> table into the CRM‑owned <c>crm.crm_customers</c>
/// table. CRM now owns this data; the Service module reads it read‑only for
/// contract/request references. Column shape must stay 1:1 with the moved
/// table (EF maps to it; it is not created by an EF migration on prod).
/// </summary>
public sealed class Customer
{
    public Guid Id { get; set; }

    public Guid? MainProjectId { get; set; }

    [Required]
    [MaxLength(20)]
    public string ExternalNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? CustomerType { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(40)]
    public string? Status { get; set; }

    public DateOnly? OpenedOn { get; set; }
    public DateOnly? StatusUpdatedOn { get; set; }

    [MaxLength(60)]
    public string? Phone { get; set; }

    [MaxLength(60)]
    public string? Fax { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? CompanyNumber { get; set; }

    [MaxLength(40)]
    public string? VatFileNumber { get; set; }

    [MaxLength(20)]
    public string? PaymentTermsCode { get; set; }

    [MaxLength(60)]
    public string? PaymentTermsName { get; set; }

    // Matches the moved table's only audit columns (svc_customers had just these two).
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
