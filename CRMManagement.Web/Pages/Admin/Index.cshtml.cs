using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class IndexModel : PageModel
{
    public sealed record SettingsLink(string Label, string Description, string Icon, string Page, string? Href = null);
    public sealed record SettingsCategory(string Title, string Icon, IReadOnlyList<SettingsLink> Links);

    public IReadOnlyList<SettingsCategory> Categories { get; } = new SettingsCategory[]
    {
        new("Personalization", "fa-paintbrush", new SettingsLink[]
        {
            new("Pipelines", "Stages, probability, and SLA per pipeline.", "fa-diagram-project", "/Admin/Pipelines/Index"),
            new("Tags", "Reusable labels for accounts, contacts, and leads.", "fa-tags", "/Admin/Tags/Index"),
            new("UI Editor", "Tweak DOM for any page (live overrides).", "fa-pen-to-square", Page: "", Href: "/UiCustomization/Editor"),
        }),
        new("Users & Access", "fa-shield-halved", new SettingsLink[]
        {
            new("Users", "Activate users and manage roles.", "fa-users-gear", "/Admin/Users/Index"),
        }),
        new("Automation & AI", "fa-robot", new SettingsLink[]
        {
            new("AI Quality", "Review AI runs, ratings, and feedback.", "fa-wand-magic-sparkles", "/Admin/AiQuality"),
            new("Class Mappings", "Map drawing class labels to products.", "fa-arrows-left-right-to-line", "/Admin/ClassMappings/Index"),
        }),
        new("Data Administration", "fa-database", new SettingsLink[]
        {
            new("Integration Outbox", "Outbound queue to LDataBrain / IMS.", "fa-share-from-square", "/Admin/Outbox"),
        }),
    };

    public void OnGet() { }
}
