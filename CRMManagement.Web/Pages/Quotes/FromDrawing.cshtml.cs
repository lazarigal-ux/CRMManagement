using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Web.Pages.Quotes;

[Authorize]
public sealed class FromDrawingModel : PageModel
{
    private readonly Infrastructure.Data.AppDbContext _db;
    public FromDrawingModel(Infrastructure.Data.AppDbContext db) => _db = db;

    public Guid? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public Guid? AccountId { get; set; }

    public async Task OnGetAsync(Guid? opportunityId, CancellationToken ct)
    {
        OpportunityId = opportunityId;
        if (opportunityId is Guid id)
        {
            var opp = await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
            OpportunityName = opp?.Name;
            AccountId = opp?.AccountId;
        }
    }
}
