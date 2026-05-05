using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    public InvoiceService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<InvoiceListItemDto>> ListAsync(CancellationToken ct) =>
        await _db.Invoices.AsNoTracking()
            .OrderByDescending(i => i.IssueDate)
            .Select(i => new InvoiceListItemDto(
                i.Id, i.InvoiceNumber, i.Subject, i.AccountId, i.Status,
                i.Total, i.AmountPaid, i.Currency, i.IssueDate, i.DueDate)
            {
                PaidAt           = i.PaidAt,
                Subtotal         = i.Subtotal,
                Tax              = i.Tax,
                Notes            = i.Notes,
                ZohoCreatedTime  = i.ZohoCreatedTime,
                ZohoModifiedTime = i.ZohoModifiedTime,
                AccountName      = _db.Accounts.Where(a => a.Id == i.AccountId).Select(a => a.Name).FirstOrDefault(),
                OrderNumber      = i.OrderId == null
                    ? null
                    : _db.Orders.Where(o => o.Id == i.OrderId).Select(o => o.OrderNumber).FirstOrDefault(),
            })
            .ToListAsync(ct);

    public async Task<InvoiceDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var i = await _db.Invoices.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i is null) return null;
        var lines = i.Lines.OrderBy(l => l.SortOrder)
            .Select(l => new InvoiceLineDto(l.Id, l.InvoiceId, l.ProductId, l.Description,
                l.Quantity, l.UnitPrice, l.LineTotal, l.SortOrder))
            .ToList();
        return new InvoiceDetailDto(
            i.Id, i.InvoiceNumber, i.Subject, i.AccountId, i.OrderId, i.Status,
            i.IssueDate, i.DueDate, i.PaidAt, i.Subtotal, i.Tax, i.Total, i.AmountPaid,
            i.Currency, i.Notes, i.BillingAddress, i.ShippingAddress, lines);
    }
}
