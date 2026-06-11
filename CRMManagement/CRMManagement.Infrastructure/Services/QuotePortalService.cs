using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class QuotePortalService : IQuotePortalService
{
    private readonly AppDbContext _db;
    public QuotePortalService(AppDbContext db) => _db = db;

    public async Task<Guid?> EnsureSignatureTokenAsync(Guid quoteId, CancellationToken ct)
    {
        var quote = await _db.Quotes.FirstOrDefaultAsync(q => q.Id == quoteId, ct);
        if (quote is null) return null;
        if (quote.SignatureToken is null)
        {
            quote.SignatureToken = Guid.NewGuid();
            // Move to a "Sent" status if still in Draft so the deal owner sees state advance.
            if (string.Equals(quote.Status, "Draft", StringComparison.OrdinalIgnoreCase))
                quote.Status = "Sent";
            await _db.SaveChangesAsync(ct);
        }
        return quote.SignatureToken;
    }

    public async Task<PublicQuoteDto?> GetByTokenAsync(Guid token, CancellationToken ct)
    {
        var quote = await _db.Quotes
            .AsNoTracking()
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.SignatureToken == token, ct);
        if (quote is null) return null;
        if (quote.ExpiresAt is DateTime exp && exp < DateTime.UtcNow) return null;

        string? accountName = null;
        if (quote.AccountId is Guid accId)
        {
            accountName = await _db.Accounts
                .AsNoTracking()
                .Where(a => a.Id == accId)
                .Select(a => a.Name)
                .FirstOrDefaultAsync(ct);
        }

        var lines = quote.Lines
            .OrderBy(l => l.SortOrder)
            .Select(l => new PublicQuoteLineDto(
                l.Description ?? "(line)",
                l.Quantity, l.UnitPrice, l.LineTotal, l.SortOrder))
            .ToList();

        return new PublicQuoteDto(
            quote.Id, quote.QuoteNumber, quote.Name, quote.Status, quote.Currency,
            quote.Subtotal, quote.Discount, quote.Tax, quote.Total,
            quote.ExpiresAt, quote.Notes, accountName,
            quote.AcceptedAt, quote.AcceptedByName, lines);
    }

    public async Task<QuoteAcceptResult> AcceptAsync(Guid token, QuoteAcceptRequest request, string? ip, CancellationToken ct)
    {
        var quote = await _db.Quotes.FirstOrDefaultAsync(q => q.SignatureToken == token, ct);
        if (quote is null) return new QuoteAcceptResult(false, "Quote not found.", null);
        if (quote.ExpiresAt is DateTime exp && exp < DateTime.UtcNow)
            return new QuoteAcceptResult(false, "This quote has expired.", null);

        if (string.IsNullOrWhiteSpace(request.AcceptedByName))
            return new QuoteAcceptResult(false, "Name is required.", null);

        // Idempotency: if already accepted, just return the recorded timestamp.
        if (quote.AcceptedAt is not null)
            return new QuoteAcceptResult(true, null, quote.AcceptedAt);

        quote.AcceptedAt = DateTime.UtcNow;
        quote.AcceptedByName = request.AcceptedByName.Trim();
        quote.AcceptedByEmail = string.IsNullOrWhiteSpace(request.AcceptedByEmail) ? null : request.AcceptedByEmail.Trim();
        quote.AcceptedFromIp = string.IsNullOrWhiteSpace(ip) ? null : ip;
        quote.SignatureSvg = string.IsNullOrWhiteSpace(request.SignatureSvg) ? null : request.SignatureSvg;
        quote.Status = "Accepted";

        await _db.SaveChangesAsync(ct);
        return new QuoteAcceptResult(true, null, quote.AcceptedAt);
    }
}
