using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Contacts;

public sealed class IndexModel : PageModel
{
    private const string EntityType = "Contact";

    private readonly IContactService _svc;
    private readonly ISavedViewService _views;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(IContactService svc, ISavedViewService views, ICurrentUserService currentUser)
    {
        _svc = svc;
        _views = views;
        _currentUser = currentUser;
    }

    public IReadOnlyList<ContactListItemDto> Items { get; private set; } = Array.Empty<ContactListItemDto>();
    public IReadOnlyList<SavedViewDto> SavedViews { get; private set; } = Array.Empty<SavedViewDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public Guid? AccountId { get; set; }
    [FromQuery(Name = "view")] public Guid? ViewId { get; set; }
    [FromQuery(Name = "mode")] public string? Mode { get; set; }

    public string ViewMode => string.Equals(Mode, "card", StringComparison.OrdinalIgnoreCase) ? "card" : "list";

    public async Task OnGetAsync(CancellationToken ct)
    {
        var userId = await _currentUser.GetCurrentUserIdAsync(User, ct);
        SavedViews = await _views.ListAsync(EntityType, userId, ct);

        if (ViewId is { } id && SavedViews.FirstOrDefault(v => v.Id == id) is { } view)
        {
            ApplySavedView(view);
        }

        var all = await _svc.ListAsync(ct);
        IEnumerable<ContactListItemDto> q = all;
        if (AccountId is { } aid) q = q.Where(c => c.AccountId == aid);
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(c => (c.FirstName + " " + c.LastName).Contains(Search, StringComparison.OrdinalIgnoreCase)
                          || (c.Email ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }

    public async Task<IActionResult> OnPostSaveViewAsync(string Name, string ViewMode, string FiltersJson, bool IsShared, bool IsDefault, CancellationToken ct)
    {
        var userId = await _currentUser.GetCurrentUserIdAsync(User, ct);
        var dto = new SavedViewUpsertDto(
            Id: null,
            EntityType: EntityType,
            Name: string.IsNullOrWhiteSpace(Name) ? "Untitled view" : Name.Trim(),
            OwnerUserId: IsShared ? null : userId,
            ViewMode: string.IsNullOrEmpty(ViewMode) ? "list" : ViewMode,
            FiltersJson: string.IsNullOrEmpty(FiltersJson) ? "{}" : FiltersJson,
            ColumnsJson: null,
            IsShared: IsShared,
            IsDefault: IsDefault);
        var id = await _views.UpsertAsync(dto, ct);
        return RedirectToPage(new { view = id, mode = ViewMode });
    }

    public async Task<IActionResult> OnPostDeleteViewAsync(Guid id, CancellationToken ct)
    {
        await _views.DeleteAsync(id, ct);
        return RedirectToPage();
    }

    private void ApplySavedView(SavedViewDto view)
    {
        if (string.IsNullOrEmpty(Mode)) Mode = view.ViewMode;
        if (string.IsNullOrWhiteSpace(view.FiltersJson)) return;
        try
        {
            using var doc = JsonDocument.Parse(view.FiltersJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;

            if (Search is null && root.TryGetProperty("search", out var s) && s.ValueKind == JsonValueKind.String)
                Search = s.GetString();
            if (AccountId is null && root.TryGetProperty("accountId", out var a) && a.ValueKind == JsonValueKind.String && Guid.TryParse(a.GetString(), out var gid))
                AccountId = gid;
        }
        catch (JsonException)
        {
            // Saved view has malformed JSON; ignore filters and continue with current page state.
        }
    }
}
