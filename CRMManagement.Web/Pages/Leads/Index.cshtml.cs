using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Leads;

public sealed class IndexModel : PageModel
{
    private const string EntityType = "Lead";

    private readonly ILeadService _svc;
    private readonly ISavedViewService _views;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(ILeadService svc, ISavedViewService views, ICurrentUserService currentUser)
    {
        _svc = svc;
        _views = views;
        _currentUser = currentUser;
    }

    public IReadOnlyList<LeadListItemDto> Items { get; private set; } = Array.Empty<LeadListItemDto>();
    public IReadOnlyList<SavedViewDto> SavedViews { get; private set; } = Array.Empty<SavedViewDto>();

    [FromQuery] public string? Status { get; set; }
    [FromQuery] public string? Rating { get; set; }
    [FromQuery] public Guid? OwnerUserId { get; set; }
    [FromQuery] public string? Search { get; set; }
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
        IEnumerable<LeadListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(x => string.Equals(x.Status, Status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Rating)) q = q.Where(x => string.Equals(x.Rating, Rating, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            q = q.Where(x => (x.FirstName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase)
                          || (x.LastName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase)
                          || (x.Company ?? "").Contains(s, StringComparison.OrdinalIgnoreCase)
                          || (x.Email ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
        }
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
            if (Status is null && root.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
                Status = st.GetString();
            if (Rating is null && root.TryGetProperty("rating", out var r) && r.ValueKind == JsonValueKind.String)
                Rating = r.GetString();
        }
        catch (JsonException)
        {
            // Saved view has malformed JSON; ignore filters and continue with current page state.
        }
    }
}
