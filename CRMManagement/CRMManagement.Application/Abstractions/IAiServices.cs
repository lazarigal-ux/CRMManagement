using CRMManagement.Application.DTOs;

namespace CRMManagement.Application.Abstractions;

/// <summary>
/// Wraps the LDataBrain AiGateway. Every call is logged to AiInteractionLog so
/// we can build a feedback loop on top of the same data the existing endpoints use.
/// </summary>
public interface ICrmAiClient
{
    Task<AiCallResult> CallAsync(AiCallRequest request, CancellationToken ct);
    Task RecordFeedbackAsync(Guid interactionLogId, short feedback, CancellationToken ct);
}

/// <summary>
/// Loads grounded context for an entity (Opportunity / Lead / Contact / Account)
/// — used by the AI Assistant to produce summaries, drafts, and next-step suggestions.
/// </summary>
public interface ICrmRagService
{
    Task<EntityContextDto?> LoadContextAsync(AiEntityKind kind, Guid id, CancellationToken ct);
}

/// <summary>
/// Outbound calls to LDataBrain (the parent app). Today: ingest comms + send WhatsApp.
/// </summary>
public interface ILDataBrainBridge
{
    Task<bool> SendWhatsAppAsync(string toPhone, string body, CancellationToken ct);
    Task<IReadOnlyList<IngestCommunicationDto>> FetchRecentCommunicationsAsync(DateTime since, CancellationToken ct);
}

public interface ICommunicationsService
{
    Task<IReadOnlyList<CommunicationRecordDto>> ListForEntityAsync(AiEntityKind kind, Guid id, int limit, CancellationToken ct);
    Task<Guid> IngestAsync(IngestCommunicationDto dto, CancellationToken ct);
}

public interface ITimelineService
{
    Task<IReadOnlyList<TimelineItemDto>> GetForEntityAsync(AiEntityKind kind, Guid id, int limit, CancellationToken ct);
}

public interface IWhatsAppLeadService
{
    Task<WhatsAppLeadIngestionResult> IngestAsync(WhatsAppLeadIngestionDto dto, CancellationToken ct);
}
