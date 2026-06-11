using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Accounts;

public sealed class DetailsModel : PageModel
{
    private readonly IAccountService _svc;
    private readonly IContactService _contacts;
    private readonly IOpportunityService _opps;
    public DetailsModel(IAccountService svc, IContactService contacts, IOpportunityService opps)
    { _svc = svc; _contacts = contacts; _opps = opps; }

    public AccountDetailDto? Item { get; private set; }
    public IReadOnlyList<ContactListItemDto> Contacts { get; private set; } = Array.Empty<ContactListItemDto>();
    public IReadOnlyList<OpportunityListItemDto> Opportunities { get; private set; } = Array.Empty<OpportunityListItemDto>();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Item = await _svc.GetAsync(id, ct);
        if (Item is null) return NotFound();
        Contacts = (await _contacts.ListAsync(ct)).Where(c => c.AccountId == id).ToList();
        Opportunities = (await _opps.ListAsync(ct)).Where(o => o.AccountId == id).ToList();
        return Page();
    }
}
