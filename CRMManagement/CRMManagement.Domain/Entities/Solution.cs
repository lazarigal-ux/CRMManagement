namespace CRMManagement.Domain.Entities;

public class Solution : AuditableEntity
{
    public Guid Id { get; set; }
    public string SolutionNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid? ProductId { get; set; }
    public bool Published { get; set; }
    public string? Comments { get; set; }
    public Guid? OwnerUserId { get; set; }

    public string? ZohoId { get; set; }
    public DateTime? ZohoCreatedTime { get; set; }
    public DateTime? ZohoModifiedTime { get; set; }
}
