namespace CRMManagement.Domain.Entities;

public class SavedView : AuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>"Contact", "Lead", "Opportunity", etc.</summary>
    public string EntityType { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Owning user. Null = shared/system view.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>"list" | "card" | "kanban".</summary>
    public string ViewMode { get; set; } = "list";

    /// <summary>JSON-encoded filter state (e.g., {"status":"New","search":"foo"}).</summary>
    public string FiltersJson { get; set; } = "{}";

    /// <summary>JSON-encoded column visibility/order, optional.</summary>
    public string? ColumnsJson { get; set; }

    public bool IsShared { get; set; }
    public bool IsDefault { get; set; }
}
