using System.ComponentModel.DataAnnotations.Schema;

namespace CRMManagement.Domain.Entities;

public class Contact : AuditableEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? Department { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
    public bool DoNotContact { get; set; }

    public string? ZohoId { get; set; }
    public DateTime? ZohoCreatedTime { get; set; }
    public DateTime? ZohoModifiedTime { get; set; }

    [NotMapped]
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public Account? Account { get; set; }
}
