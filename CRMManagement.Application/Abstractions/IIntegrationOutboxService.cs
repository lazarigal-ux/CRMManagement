namespace CRMManagement.Application.Abstractions;

public sealed record OutboxRow(
    Guid Id,
    string Target,
    string Status,
    int Attempts,
    DateTime? LastAttemptAt,
    string? LastError,
    DateTime? SentAt,
    string? RelatedType,
    Guid? RelatedId,
    DateTime CreatedAt);

/// <summary>
/// Phase 7 (partial) — durable outbox for outbound integration messages.
/// Producers call <see cref="EnqueueAsync"/>; a hosted drainer handles delivery.
/// </summary>
public interface IIntegrationOutboxService
{
    Task<Guid> EnqueueAsync(string target, object payload, string? relatedType = null, Guid? relatedId = null, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxRow>> ListAsync(int limit, string? statusFilter, CancellationToken ct);
    Task<int> RetryAsync(Guid id, CancellationToken ct);
    Task<int> RetryAllFailedAsync(CancellationToken ct);
}
