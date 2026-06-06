using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Web.Pages.Customers;

public class UpsertModel : PageModel
{
    private readonly AppDbContext _db;

    public UpsertModel(AppDbContext db)
    {
        _db = db;
    }

    public sealed class CustomerInputModel
    {
        public string ExternalNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CustomerType { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? Status { get; set; } = "פעיל";
        public DateOnly? OpenedOn { get; set; }
        public DateOnly? StatusUpdatedOn { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? CompanyNumber { get; set; }
        public string? VatFileNumber { get; set; }
        public string? PaymentTermsCode { get; set; }
        public string? PaymentTermsName { get; set; }
    }

    [BindProperty]
    public CustomerInputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public bool IsEdit => Id.HasValue;

    public sealed record PaymentTermOption(string Code, string Name);

    public List<PaymentTermOption> PaymentTermOptions { get; private set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        await LoadPaymentTermsAsync(cancellationToken);

        if (Id.HasValue)
        {
            var entity = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == Id, cancellationToken);
            if (entity is null) return NotFound();
            Input = new CustomerInputModel
            {
                ExternalNumber = entity.ExternalNumber,
                Name = entity.Name,
                CustomerType = entity.CustomerType,
                City = entity.City,
                Address = entity.Address,
                Status = entity.Status,
                OpenedOn = entity.OpenedOn,
                StatusUpdatedOn = entity.StatusUpdatedOn,
                Phone = entity.Phone,
                Fax = entity.Fax,
                Email = entity.Email,
                CompanyNumber = entity.CompanyNumber,
                VatFileNumber = entity.VatFileNumber,
                PaymentTermsCode = entity.PaymentTermsCode,
                PaymentTermsName = entity.PaymentTermsName
            };
            EnsureCurrentPaymentTermOption();
        }
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        await LoadPaymentTermsAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(Input.ExternalNumber))
            ModelState.AddModelError("Input.ExternalNumber", "Customer number is required.");
        if (string.IsNullOrWhiteSpace(Input.Name))
            ModelState.AddModelError("Input.Name", "Customer name is required.");

        EnsureCurrentPaymentTermOption();

        if (!ModelState.IsValid) return Page();

        try
        {
            Customer entity;
            var now = DateTime.UtcNow;
            if (Id.HasValue)
            {
                var found = await _db.Customers.FirstOrDefaultAsync(c => c.Id == Id, cancellationToken);
                if (found is null) return NotFound();
                entity = found;
            }
            else
            {
                entity = new Customer { Id = Guid.NewGuid(), CreatedAt = now };
                _db.Customers.Add(entity);
            }

            entity.ExternalNumber = Input.ExternalNumber.Trim();
            entity.Name = Input.Name.Trim();
            entity.CustomerType = Trim(Input.CustomerType);
            entity.City = Trim(Input.City);
            entity.Address = Trim(Input.Address);
            entity.Status = Trim(Input.Status);
            entity.OpenedOn = Input.OpenedOn;
            entity.StatusUpdatedOn = Input.StatusUpdatedOn;
            entity.Phone = Trim(Input.Phone);
            entity.Fax = Trim(Input.Fax);
            entity.Email = Trim(Input.Email);
            entity.CompanyNumber = Trim(Input.CompanyNumber);
            entity.VatFileNumber = Trim(Input.VatFileNumber);
            entity.PaymentTermsCode = Trim(Input.PaymentTermsCode);
            entity.PaymentTermsName = Trim(Input.PaymentTermsName);
            entity.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            ModelState.AddModelError("Input.ExternalNumber", "Customer number must be unique.");
            return Page();
        }

        return RedirectToPage("/Customers/Index");
    }

    private async Task LoadPaymentTermsAsync(CancellationToken cancellationToken)
    {
        var customerTerms = await _db.Customers.AsNoTracking()
            .Where(c => c.PaymentTermsCode != null || c.PaymentTermsName != null)
            .Select(c => new { c.PaymentTermsCode, c.PaymentTermsName })
            .ToListAsync(cancellationToken);

        PaymentTermOptions = customerTerms
            .Select(x => new PaymentTermOption(Trim(x.PaymentTermsCode) ?? string.Empty, Trim(x.PaymentTermsName) ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x.Code) || !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => (x.Code.Trim() + "" + x.Name.Trim()).ToUpperInvariant())
            .Select(g => g.First())
            .OrderBy(x => string.IsNullOrWhiteSpace(x.Code) ? "~" : x.Code)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private void EnsureCurrentPaymentTermOption()
    {
        var code = Trim(Input.PaymentTermsCode) ?? string.Empty;
        var name = Trim(Input.PaymentTermsName) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name)) return;
        if (PaymentTermOptions.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))) return;
        PaymentTermOptions.Add(new PaymentTermOption(code, name));
        PaymentTermOptions = PaymentTermOptions
            .OrderBy(x => string.IsNullOrWhiteSpace(x.Code) ? "~" : x.Code)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
