using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class QuoteService : IQuoteService
{
    private readonly AppDbContext _db;
    public QuoteService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<QuoteListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Quotes.AsNoTracking()
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuoteListItemDto(q.Id, q.QuoteNumber, q.Name, q.AccountId, q.Status, q.Total, q.Currency, q.ExpiresAt)
            {
                Subtotal         = q.Subtotal,
                Discount         = q.Discount,
                Tax              = q.Tax,
                Notes            = q.Notes,
                AcceptedAt       = q.AcceptedAt,
                AcceptedByName   = q.AcceptedByName,
                ZohoCreatedTime  = q.ZohoCreatedTime,
                ZohoModifiedTime = q.ZohoModifiedTime,
                AccountName      = q.AccountId == null
                    ? null
                    : _db.Accounts.Where(a => a.Id == q.AccountId).Select(a => a.Name).FirstOrDefault(),
                OpportunityName  = q.OpportunityId == null
                    ? null
                    : _db.Opportunities.Where(o => o.Id == q.OpportunityId).Select(o => o.Name).FirstOrDefault(),
                ContactName      = q.ContactId == null
                    ? null
                    : _db.Contacts.Where(c => c.Id == q.ContactId).Select(c => c.FirstName + " " + c.LastName).FirstOrDefault(),
                OwnerName        = q.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == q.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);
    }

    public async Task<QuoteDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Quotes.AsNoTracking()
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id, ct);
        if (x == null) return null;
        var lines = x.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new QuoteLineDto(l.Id, l.QuoteId, l.ProductId, l.Description, l.Quantity, l.UnitPrice, l.Discount, l.LineTotal, l.SortOrder))
            .ToList();
        return new QuoteDetailDto(x.Id, x.QuoteNumber, x.Name, x.AccountId, x.OpportunityId, x.ContactId, x.Status, x.ExpiresAt, x.Subtotal, x.Discount, x.Tax, x.Total, x.Currency, x.Notes, x.OwnerUserId, lines);
    }

    public async Task<Guid> UpsertAsync(QuoteUpsertDto dto, CancellationToken ct)
    {
        Quote e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Quotes.FirstOrDefaultAsync(q => q.Id == id, ct)
                ?? throw new InvalidOperationException("Quote not found");
        }
        else
        {
            e = new Quote { Id = Guid.NewGuid() };
            _db.Quotes.Add(e);
        }
        e.QuoteNumber = dto.QuoteNumber;
        e.Name = dto.Name;
        e.AccountId = dto.AccountId;
        e.OpportunityId = dto.OpportunityId;
        e.ContactId = dto.ContactId;
        e.Status = dto.Status;
        e.ExpiresAt = dto.ExpiresAt;
        e.Subtotal = dto.Subtotal;
        e.Discount = dto.Discount;
        e.Tax = dto.Tax;
        e.Total = dto.Total;
        e.Currency = dto.Currency;
        e.Notes = dto.Notes;
        e.OwnerUserId = dto.OwnerUserId;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Quotes.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (e != null)
        {
            _db.Quotes.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AddQuoteLineAsync(Guid quoteId, QuoteLineUpsertDto line, CancellationToken ct)
    {
        var quote = await _db.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId, ct)
            ?? throw new InvalidOperationException("Quote not found");
        var lineTotal = (line.Quantity * line.UnitPrice) - line.Discount;
        _db.QuoteLines.Add(new QuoteLine
        {
            Id = Guid.NewGuid(),
            QuoteId = quote.Id,
            ProductId = line.ProductId,
            Description = line.Description,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            Discount = line.Discount,
            LineTotal = lineTotal,
            SortOrder = line.SortOrder
        });
        quote.Subtotal += lineTotal;
        quote.Total = quote.Subtotal - quote.Discount + quote.Tax;
        await _db.SaveChangesAsync(ct);
    }
}
