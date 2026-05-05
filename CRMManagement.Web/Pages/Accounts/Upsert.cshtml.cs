using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Accounts;

public sealed class UpsertModel : PageModel
{
    private readonly IAccountService _svc;
    public UpsertModel(IAccountService svc) => _svc = svc;

    [BindProperty] public AccountUpsertDto Input { get; set; } =
        new(null, "", null, null, null, null, null, null, null, null, null, null, null, null, true);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new AccountUpsertDto(d.Id, d.Name, d.LegalName, d.Industry, d.Website, d.Phone, d.Email, d.BillingAddress, d.ShippingAddress, d.AnnualRevenue, d.EmployeeCount, d.OwnerUserId, d.ParentAccountId, d.Description, d.IsActive);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Accounts/Details", new { id });
    }
}
