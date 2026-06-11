using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.PurchaseOrders;

public sealed class UpsertModel : PageModel
{
    private readonly IPurchaseOrderService _svc;
    public UpsertModel(IPurchaseOrderService svc) => _svc = svc;

    [BindProperty] public PurchaseOrderUpsertDto Input { get; set; } =
        new(null, "", "", null, null, "Draft", null, null, null, 0m, 0m, 0m, 0m, 0m, "USD", null, null, null, null, null);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new PurchaseOrderUpsertDto(d.Id, d.PoNumber, d.Subject, d.RequisitionNo, d.VendorId, d.Status, d.PoDate, d.DueDate,
                d.CarrierName, d.Subtotal, d.Discount, d.Tax, d.AdjustmentAmount, d.Total, d.Currency,
                d.Description, d.TermsAndConditions, d.BillingAddress, d.ShippingAddress, d.OwnerUserId);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Input.PoNumber))
            ModelState.AddModelError("Input.PoNumber", "PO Number is required.");
        if (string.IsNullOrWhiteSpace(Input.Subject))
            ModelState.AddModelError("Input.Subject", "Subject is required.");
        if (!ModelState.IsValid) return Page();

        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/PurchaseOrders/Details", new { id });
    }
}
