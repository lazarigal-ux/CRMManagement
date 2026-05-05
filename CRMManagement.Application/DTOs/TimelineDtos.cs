namespace CRMManagement.Application.DTOs;

public enum TimelineItemKind { Activity, Note, Email, WhatsApp, Other }

public sealed record TimelineItemDto(
    Guid Id,
    TimelineItemKind Kind,
    DateTime At,
    string? Direction,
    string? Title,
    string Body,
    string? Author,
    string? Reference);
