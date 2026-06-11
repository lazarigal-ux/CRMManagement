using CRMManagement.Application.DTOs;

namespace CRMManagement.Application.Abstractions;

/// <summary>
/// Phase 6 — public e-sign portal for quotes.
/// </summary>
public interface IQuotePortalService
{
    /// <summary>Issue a signature token if the quote doesn't already have one. Returns the active token.</summary>
    Task<Guid?> EnsureSignatureTokenAsync(Guid quoteId, CancellationToken ct);

    /// <summary>Read-only public projection by token. Returns null if token unknown or quote expired.</summary>
    Task<PublicQuoteDto?> GetByTokenAsync(Guid token, CancellationToken ct);

    /// <summary>Records acceptance. Idempotent — if already accepted, returns the prior timestamp.</summary>
    Task<QuoteAcceptResult> AcceptAsync(Guid token, QuoteAcceptRequest request, string? ip, CancellationToken ct);
}
