using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public sealed class IndexModel : PageModel
{
    private readonly IUserAdminService _svc;
    public IndexModel(IUserAdminService svc) => _svc = svc;

    public IReadOnlyList<UserAdminListItemDto> Items { get; private set; } = Array.Empty<UserAdminListItemDto>();

    public async Task OnGetAsync(CancellationToken ct) => Items = await _svc.GetUsersAsync(ct);

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid userId, bool isActive, CancellationToken ct)
    {
        await _svc.SetUserActiveAsync(userId, isActive, ct);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetRolesAsync(Guid userId, string roles, CancellationToken ct)
    {
        var list = (roles ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        await _svc.SetUserRolesAsync(userId, list, ct);
        return RedirectToPage();
    }
}
