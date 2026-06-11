using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Solutions;

public sealed class IndexModel : PageModel
{
    private readonly ISolutionService _svc;
    public IndexModel(ISolutionService svc) => _svc = svc;

    public IReadOnlyList<SolutionListItemDto> Items { get; private set; } = Array.Empty<SolutionListItemDto>();

    [FromQuery] public string? Search { get; set; }
    [FromQuery] public bool? PublishedOnly { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _svc.ListAsync(ct);
        IEnumerable<SolutionListItemDto> q = all;
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(s =>
                s.Title.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || s.SolutionNumber.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || (s.Category?.Contains(Search, StringComparison.OrdinalIgnoreCase) ?? false));
        if (PublishedOnly == true) q = q.Where(s => s.Published);
        Items = q.ToList();
    }
}
