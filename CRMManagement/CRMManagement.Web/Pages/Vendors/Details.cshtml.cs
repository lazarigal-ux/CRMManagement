using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Vendors;

public sealed class DetailsModel : PageModel
{
    private readonly IVendorService _svc;
    public DetailsModel(IVendorService svc) => _svc = svc;

    public VendorDetailDto? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        return Item is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return RedirectToPage("/Vendors/Index");
    }
}
