using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

/// <summary>
/// Phase 8 — handles inbound WhatsApp messages from LDataBrain.
///
/// 1. If the phone matches an existing Contact → log a comm against that contact, no Lead created.
/// 2. Else if a Lead with that phone exists → re-score and append to its history.
/// 3. Else → create a new Lead, score it, set Rating (Hot/Warm/Cold) and Status.
///
/// Best-effort: if the AI gateway is unreachable, the Lead is still created with score 0
/// and Status = "NeedsReview" so reps see it in their queue.
/// </summary>
public sealed class WhatsAppLeadService : IWhatsAppLeadService
{
    private const string SystemPrompt = """
        You score inbound B2B leads from short messages. Return ONLY valid JSON, no prose, no fences.
        Schema: {"score": <int 0..100>, "rating": "Hot|Warm|Cold", "intent": "<one short phrase>", "reason": "<brief>"}

        Scoring rubric:
        - 80-100 ("Hot"): explicit purchase intent, urgency, budget signals, specific request, mentions a project/timeline.
        - 50-79  ("Warm"): exploratory questions about products/services, comparing options, no urgency yet.
        - 0-49   ("Cold"): vague greeting, support question for an existing customer, off-topic, spam, or unclear.

        If the message is in a language other than English, score it normally — do not lower the score for language.
        """;

    private readonly AppDbContext _db;
    private readonly ICrmAiClient _ai;
    private readonly ILogger<WhatsAppLeadService> _logger;

    public WhatsAppLeadService(AppDbContext db, ICrmAiClient ai, ILogger<WhatsAppLeadService> logger)
    {
        _db = db;
        _ai = ai;
        _logger = logger;
    }

    public async Task<WhatsAppLeadIngestionResult> IngestAsync(WhatsAppLeadIngestionDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.SenderPhone) || string.IsNullOrWhiteSpace(dto.Body))
        {
            return new WhatsAppLeadIngestionResult(null, null, false, null, null, "Missing phone or body.");
        }

        var phone = NormalizePhone(dto.SenderPhone);

        // 1. Existing Contact? → log against contact, do not create lead.
        var contact = await _db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Phone == phone || c.Mobile == phone, ct);
        if (contact is not null)
        {
            await SaveCommAsync(contactId: contact.Id, accountId: contact.AccountId, dto: dto, ct: ct);
            return new WhatsAppLeadIngestionResult(null, contact.Id, false, null, null, "Matched existing contact.");
        }

        // 2/3. Existing Lead?
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Phone == phone, ct);
        var createdNew = lead is null;

        if (lead is null)
        {
            var (first, last) = SplitName(dto.SenderName);
            lead = new Lead
            {
                Id = Guid.NewGuid(),
                FirstName = string.IsNullOrWhiteSpace(first) ? "WhatsApp" : first,
                LastName  = string.IsNullOrWhiteSpace(last)  ? phone        : last,
                Phone = phone,
                Source = "WhatsApp",
                Status = "New",
                Description = dto.Body,
            };
            _db.Leads.Add(lead);
        }
        else
        {
            // Append the latest message body to the lead description so the rep has chat context.
            lead.Description = string.IsNullOrWhiteSpace(lead.Description)
                ? dto.Body
                : $"{lead.Description}\n\n[{dto.OccurredAt:yyyy-MM-dd HH:mm}] {dto.Body}";
        }
        await _db.SaveChangesAsync(ct);

        await SaveCommAsync(leadId: lead.Id, dto: dto, ct: ct);

        // Score the most recent message (or accumulated description for re-scoring).
        var (score, rating, reason) = await ScoreAsync(lead.Description ?? dto.Body, ct);

        if (score >= 0)
        {
            lead.Score = score;
            lead.Rating = rating;
            // Promote Hot leads in the default rep view.
            if (string.Equals(rating, "Hot", StringComparison.OrdinalIgnoreCase) && lead.Status == "New")
                lead.Status = "Working";
        }
        else if (createdNew)
        {
            lead.Status = "NeedsReview";
        }
        await _db.SaveChangesAsync(ct);

        return new WhatsAppLeadIngestionResult(
            LeadId: lead.Id,
            ContactId: null,
            CreatedNewLead: createdNew,
            Score: score >= 0 ? score : null,
            Rating: rating,
            Reason: reason);
    }

    // ── helpers ───────────────────────────────────────────────────────────────────

    private async Task SaveCommAsync(
        Guid? contactId = null, Guid? accountId = null, Guid? leadId = null,
        WhatsAppLeadIngestionDto dto = default!, CancellationToken ct = default)
    {
        var comm = new CommunicationRecord
        {
            Id = Guid.NewGuid(),
            Provider = "whatsapp",
            Direction = "in",
            OccurredAt = dto.OccurredAt == default ? DateTime.UtcNow : dto.OccurredAt,
            FromAddress = NormalizePhone(dto.SenderPhone),
            Subject = null,
            Body = dto.Body,
            ExternalId = dto.ExternalId,
            ContactId = contactId,
            AccountId = accountId,
            LeadId = leadId,
        };

        // De-dup on (provider, externalId).
        if (!string.IsNullOrEmpty(comm.ExternalId))
        {
            var exists = await _db.Communications
                .AsNoTracking()
                .AnyAsync(c => c.Provider == "whatsapp" && c.ExternalId == comm.ExternalId, ct);
            if (exists) return;
        }
        _db.Communications.Add(comm);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<(int score, string? rating, string? reason)> ScoreAsync(string body, CancellationToken ct)
    {
        var result = await _ai.CallAsync(
            new AiCallRequest(
                SystemPrompt: SystemPrompt,
                UserPrompt: $"Inbound WhatsApp message:\n---\n{body}\n---",
                Mode: "lead-score",
                Provider: null,
                MaxTokens: 200),
            ct);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Text))
        {
            _logger.LogWarning("Lead-score AI call failed: {Err}", result.ErrorMessage);
            return (-1, null, "AI scoring unavailable.");
        }

        var json = ExtractJson(result.Text) ?? result.Text;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            int score = 0;
            if (root.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number)
                score = Math.Clamp(s.GetInt32(), 0, 100);
            var rating = root.TryGetProperty("rating", out var r) ? r.GetString() : null;
            var reason = root.TryGetProperty("reason", out var rs) ? rs.GetString() : null;
            return (score, rating, reason);
        }
        catch
        {
            return (-1, null, "Could not parse score JSON.");
        }
    }

    private static string? ExtractJson(string raw)
    {
        var t = raw.Trim();
        if (t.StartsWith("```"))
        {
            var nl = t.IndexOf('\n');
            if (nl > 0) t = t[(nl + 1)..];
            var fenceEnd = t.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd > 0) t = t[..fenceEnd];
            t = t.Trim();
        }
        if (t.StartsWith("{") && t.EndsWith("}")) return t;
        var first = t.IndexOf('{'); var last = t.LastIndexOf('}');
        return first >= 0 && last > first ? t.Substring(first, last - first + 1) : null;
    }

    /// <summary>Strip whitespace and the WhatsApp <c>@c.us</c> / <c>@lid</c> suffix WAHA sometimes leaves.</summary>
    private static string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return phone;
        var p = phone.Trim();
        var atIdx = p.IndexOf('@');
        if (atIdx > 0) p = p[..atIdx];
        return p;
    }

    private static (string first, string last) SplitName(string? full)
    {
        if (string.IsNullOrWhiteSpace(full)) return ("", "");
        var parts = full.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => ("", ""),
            1 => (parts[0], ""),
            _ => (parts[0], parts[1]),
        };
    }
}
