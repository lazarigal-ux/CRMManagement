using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin.Tags;

[Authorize(Roles = "Admin")]
public sealed class IndexModel : PageModel
{
    private readonly ITagService _svc;
    public IndexModel(ITagService svc) => _svc = svc;

    public IReadOnlyList<TagDto> Items { get; private set; } = Array.Empty<TagDto>();

    public async Task OnGetAsync(CancellationToken ct) => Items = await _svc.ListAsync(ct);
}
