using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Web.Pages.Customers;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true, Name = "scope")]
    public string Scope { get; set; } = "all"; // all | active | inactive

    public int TotalCount { get; private set; }
    public int ServiceCount { get; private set; }
    public int ActiveCount { get; private set; }
    public IReadOnlyList<Customer> Items { get; private set; } = Array.Empty<Customer>();

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();

        if (string.Equals(Scope, "active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.Status == "פעיל");
        }
        else if (string.Equals(Scope, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.Status == "לא פעיל");
        }

        TotalCount   = await query.CountAsync(cancellationToken);
        ServiceCount = await _db.Customers.AsNoTracking()
            .CountAsync(c => c.CustomerType != null && c.CustomerType.Contains("שרות"), cancellationToken);
        ActiveCount  = await _db.Customers.AsNoTracking()
            .CountAsync(c => c.Status == "פעיל", cancellationToken);

        Items = await query
            .OrderBy(c => c.ExternalNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, string? scope, CancellationToken cancellationToken)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is not null)
        {
            _db.Customers.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
        return RedirectToPage("./Index", new { scope = string.IsNullOrEmpty(scope) ? "all" : scope });
    }
}
