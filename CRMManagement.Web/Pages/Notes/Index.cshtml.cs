using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Notes;

public sealed class IndexModel : PageModel
{
    private readonly INoteService _svc;
    public IndexModel(INoteService svc) => _svc = svc;

    public IReadOnlyList<NoteDto> Items { get; private set; } = Array.Empty<NoteDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public string? RelatedType { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAllAsync(ct);
        IEnumerable<NoteDto> q = all;
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(n =>
                (n.Title?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false)
                || n.Body.Contains(Search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(RelatedType))
            q = q.Where(n => string.Equals(n.RelatedType, RelatedType, StringComparison.OrdinalIgnoreCase));
        Items = q.ToList();
    }
}
