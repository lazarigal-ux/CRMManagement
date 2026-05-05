using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderListItemDto>> ListAsync(CancellationToken ct) =>
        await _db.Orders.AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderListItemDto(
                o.Id, o.OrderNumber, o.Subject, o.AccountId, o.Status,
                o.Total, o.Currency, o.OrderDate)
            {
                Subtotal         = o.Subtotal,
                Discount         = o.Discount,
                Tax              = o.Tax,
                Notes            = o.Notes,
                ZohoCreatedTime  = o.ZohoCreatedTime,
                ZohoModifiedTime = o.ZohoModifiedTime,
                AccountName      = _db.Accounts.Where(a => a.Id == o.AccountId).Select(a => a.Name).FirstOrDefault(),
                OpportunityName  = o.OpportunityId == null
                    ? null
                    : _db.Opportunities.Where(x => x.Id == o.OpportunityId).Select(x => x.Name).FirstOrDefault(),
                QuoteNumber      = o.QuoteId == null
                    ? null
                    : _db.Quotes.Where(q => q.Id == o.QuoteId).Select(q => q.QuoteNumber).FirstOrDefault(),
                OwnerName        = o.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == o.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);

    public async Task<OrderDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var o = await _db.Orders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (o is null) return null;
        var lines = o.Lines.OrderBy(l => l.SortOrder)
            .Select(l => new OrderLineDto(l.Id, l.OrderId, l.ProductId, l.Description,
                l.Quantity, l.UnitPrice, l.Discount, l.LineTotal, l.SortOrder))
            .ToList();
        return new OrderDetailDto(
            o.Id, o.OrderNumber, o.Subject, o.AccountId, o.OpportunityId, o.QuoteId, o.Status,
            o.OrderDate, o.Subtotal, o.Discount, o.Tax, o.Total, o.Currency,
            o.Notes, o.OwnerUserId, o.BillingAddress, o.ShippingAddress, lines);
    }
}
