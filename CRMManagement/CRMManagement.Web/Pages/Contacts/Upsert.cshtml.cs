using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Contacts;

public sealed class UpsertModel : PageModel
{
    private readonly IContactService _svc;
    public UpsertModel(IContactService svc) => _svc = svc;

    [BindProperty] public ContactUpsertDto Input { get; set; } =
        new(null, "", "", null, null, null, null, null, null, null, null, null, false, false);

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Input = new ContactUpsertDto(d.Id, d.FirstName, d.LastName, d.Title, d.Email, d.Phone, d.Mobile, d.AccountId, d.OwnerUserId, d.Department, d.Address, d.Description, d.IsPrimary, d.DoNotContact);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var id = await _svc.UpsertAsync(Input, ct);
        return RedirectToPage("/Contacts/Details", new { id });
    }
}
