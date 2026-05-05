namespace CRMManagement.Application.DTOs;

/// <summary>The kind of entity the assistant is operating on.</summary>
public enum AiEntityKind
{
    Opportunity,
    Lead,
    Contact,
    Account,
}

/// <summary>What the user wants the assistant to produce.</summary>
public enum AiAssistantAction
{
    Summarize,
    DraftFollowUpEmail,
    DraftFollowUpWhatsApp,
    NextBestAction,
}

public sealed record AiAssistantRequest(
    AiEntityKind EntityKind,
    Guid EntityId,
    AiAssistantAction Action,
    string? UserHint,
    string? Channel);

public sealed record AiAssistantResponse(
    Guid InteractionLogId,
    string Text,
    string? Subject,
    string Provider,
    int? TotalMs);

public sealed record AiCallRequest(
    string SystemPrompt,
    string UserPrompt,
    string Mode,
    string? Provider = null,
    int? MaxTokens = null,
    string? ImageBase64 = null,
    string? ImageMediaType = null);

public sealed record AiCallResult(
    Guid InteractionLogId,
    bool Success,
    string Text,
    string Provider,
    int? TotalMs,
    string? ErrorMessage);

public sealed record EntityContextDto(
    AiEntityKind Kind,
    Guid Id,
    string DisplayName,
    string Snapshot,
    IReadOnlyList<string> RecentActivities,
    IReadOnlyList<string> RecentCommunications,
    IReadOnlyList<string> RecentNotes);

public sealed record CommunicationRecordDto(
    Guid Id,
    string Provider,
    string Direction,
    DateTime OccurredAt,
    string? FromAddress,
    string? ToAddress,
    string? Subject,
    string? Body,
    Guid? ContactId,
    Guid? AccountId,
    Guid? OpportunityId,
    Guid? LeadId);

public sealed record IngestCommunicationDto(
    string Provider,
    string Direction,
    DateTime OccurredAt,
    string? FromAddress,
    string? ToAddress,
    string? Subject,
    string? Body,
    string? ExternalId);

public sealed record WhatsAppLeadIngestionDto(
    string SenderPhone,
    string? SenderName,
    string Body,
    DateTime OccurredAt,
    string? ExternalId);

public sealed record WhatsAppLeadIngestionResult(
    Guid? LeadId,
    Guid? ContactId,
    bool CreatedNewLead,
    int? Score,
    string? Rating,
    string? Reason);
