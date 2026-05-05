namespace CRMManagement.Domain.Entities;

public class CustomField : AuditableEntity
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = "";
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string DataType { get; set; } = "Text";
    public string? OptionsJson { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

public class CustomFieldValue : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid CustomFieldId { get; set; }
    public Guid EntityId { get; set; }
    public string? ValueText { get; set; }
}
