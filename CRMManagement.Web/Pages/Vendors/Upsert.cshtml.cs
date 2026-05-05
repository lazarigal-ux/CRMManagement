using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Vendors;

public sealed class UpsertModel : PageModel
{
    private readonly IVendorService _svc;
    public UpsertModel(IVendorService svc) => _svc = svc;

    [BindProperty] public VendorUpsertDto Input { get; set; } =
        new(null, "", null, null, null, null, null, null, null, null, null, null, null, null, true);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new VendorUpsertDto(d.Id, d.Name, d.Category, d.Email, d.Phone, d.Website, d.Description,
                d.Street, d.City, d.State, d.ZipCode, d.Country, d.GlAccount, d.OwnerUserId, d.IsActive);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
            ModelState.AddModelError("Input.Name", "Name is required.");
        if (!ModelState.IsValid) return Page();

        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Vendors/Details", new { id });
    }
}
