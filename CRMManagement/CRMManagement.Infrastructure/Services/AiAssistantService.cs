using System.Text;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

public interface IAiAssistantService
{
    Task<AiAssistantResponse> RunAsync(AiAssistantRequest request, CancellationToken ct);
}

public sealed class AiAssistantService : IAiAssistantService
{
    private readonly ICrmRagService _rag;
    private readonly ICrmAiClient _ai;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(ICrmRagService rag, ICrmAiClient ai, ILogger<AiAssistantService> logger)
    {
        _rag = rag;
        _ai = ai;
        _logger = logger;
    }

    public async Task<AiAssistantResponse> RunAsync(AiAssistantRequest request, CancellationToken ct)
    {
        var ctx = await _rag.LoadContextAsync(request.EntityKind, request.EntityId, ct);
        if (ctx is null)
        {
            return new AiAssistantResponse(Guid.Empty, "Entity not found.", null, "n/a", null);
        }

        var (system, user, mode) = BuildPrompts(ctx, request);

        var result = await _ai.CallAsync(
            new AiCallRequest(system, user, mode, Provider: null, MaxTokens: 800),
            ct);

        if (!result.Success)
        {
            _logger.LogInformation("AI assistant call failed for {Kind} {Id} ({Mode}): {Err}",
                request.EntityKind, request.EntityId, mode, result.ErrorMessage);
        }

        // For email-draft mode, try to split out a "Subject:" first line.
        string? subject = null;
        var text = result.Text;
        if (request.Action == AiAssistantAction.DraftFollowUpEmail && !string.IsNullOrWhiteSpace(text))
        {
            (subject, text) = SplitSubjectAndBody(text);
        }

        return new AiAssistantResponse(
            result.InteractionLogId,
            text ?? "",
            subject,
            result.Provider,
            result.TotalMs);
    }

    private static (string system, string user, string mode) BuildPrompts(
        EntityContextDto ctx, AiAssistantRequest req)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Snapshot:");
        sb.AppendLine(ctx.Snapshot);

        if (ctx.RecentActivities.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Recent activities:");
            foreach (var a in ctx.RecentActivities) sb.AppendLine($"- {a}");
        }
        if (ctx.RecentCommunications.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Recent communications:");
            foreach (var c in ctx.RecentCommunications) sb.AppendLine($"- {c}");
        }
        if (ctx.RecentNotes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Recent notes:");
            foreach (var n in ctx.RecentNotes) sb.AppendLine($"- {n}");
        }
        if (!string.IsNullOrWhiteSpace(req.UserHint))
        {
            sb.AppendLine();
            sb.AppendLine($"Extra context from the user: {req.UserHint}");
        }

        var contextBlock = sb.ToString();

        return req.Action switch
        {
            AiAssistantAction.Summarize => (
                """
                You are a concise CRM assistant. Summarize the deal/contact in 4-6 short bullet points:
                what's open, what's blocking, the most recent meaningful interaction, and risk signals.
                Do NOT invent facts beyond the provided context.
                """,
                contextBlock,
                "deal-assistant.summarize"
            ),

            AiAssistantAction.DraftFollowUpEmail => (
                """
                You draft polished, short follow-up emails for a B2B sales rep. Tone: professional, warm, action-oriented.
                Output format EXACTLY:
                Subject: <one-line subject>
                <one blank line>
                <email body, 80-160 words, no subject line, no signature placeholder beyond "Best,">
                Do NOT invent facts beyond the provided context.
                """,
                contextBlock,
                "deal-assistant.draft-email"
            ),

            AiAssistantAction.DraftFollowUpWhatsApp => (
                """
                You draft short WhatsApp follow-ups for a B2B sales rep. Keep it under 60 words.
                One short paragraph. Friendly but professional. No subject line.
                Do NOT invent facts beyond the provided context.
                """,
                contextBlock,
                "deal-assistant.draft-whatsapp"
            ),

            AiAssistantAction.NextBestAction => (
                """
                You are a sales-coach assistant. Recommend the single highest-leverage next action this rep should take,
                in 1-3 sentences, with a clear due-by suggestion (today / this week / next 14 days).
                Do NOT invent facts beyond the provided context.
                """,
                contextBlock,
                "deal-assistant.next-action"
            ),

            _ => (
                "You are a CRM assistant.",
                contextBlock,
                "deal-assistant.unknown"
            ),
        };
    }

    private static (string? subject, string body) SplitSubjectAndBody(string text)
    {
        var lines = text.Split('\n');
        if (lines.Length > 0 && lines[0].StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
        {
            var subj = lines[0]["Subject:".Length..].Trim();
            var body = string.Join("\n", lines.Skip(1)).TrimStart('\r', '\n');
            return (subj, body);
        }
        return (null, text);
    }
}
