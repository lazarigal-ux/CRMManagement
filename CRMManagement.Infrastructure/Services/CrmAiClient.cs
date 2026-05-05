using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

/// <summary>
/// Talks to the LDataBrain AiGateway. The gateway accepts an Anthropic-style
/// messages payload and returns a unified shape; the exact endpoint is configurable
/// via AiGateway:CompletionsPath (defaults to "v1/messages").
/// </summary>
public sealed class CrmAiClient : ICrmAiClient
{
    public const string HttpClientName = "AiGateway";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _http;
    private readonly AppDbContext _db;
    private readonly ILogger<CrmAiClient> _logger;
    private readonly string _completionsPath;
    private readonly string _defaultProvider;
    private readonly string _defaultModel;

    public CrmAiClient(
        IHttpClientFactory http,
        AppDbContext db,
        ILogger<CrmAiClient> logger,
        Microsoft.Extensions.Configuration.IConfiguration cfg)
    {
        _http = http;
        _db = db;
        _logger = logger;
        _completionsPath = cfg["AiGateway:CompletionsPath"] ?? "v1/messages";
        _defaultProvider = cfg["AiGateway:Provider"] ?? "claude";
        _defaultModel = cfg["AiGateway:Model"] ?? "claude-sonnet-4-6";
    }

    public async Task<AiCallResult> CallAsync(AiCallRequest request, CancellationToken ct)
    {
        var sessionId = Guid.NewGuid();
        var log = new AiInteractionLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            SessionId = sessionId,
            StepNumber = 1,
            Instruction = request.UserPrompt,
            Provider = request.Provider ?? _defaultProvider,
            Mode = request.Mode,
            Success = false,
            ExamplesUsed = 0,
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var client = _http.CreateClient(HttpClientName);
            if (client.BaseAddress is null)
            {
                log.Success = false;
                log.ErrorMessage = "AiGateway:BaseUrl is not configured.";
                await PersistLogAsync(log, ct);
                return new AiCallResult(log.Id, false, "", log.Provider, null, log.ErrorMessage);
            }

            object userContent = !string.IsNullOrWhiteSpace(request.ImageBase64)
                ? new object[]
                {
                    new
                    {
                        type = "image",
                        source = new
                        {
                            type = "base64",
                            media_type = request.ImageMediaType ?? "image/png",
                            data = request.ImageBase64,
                        }
                    },
                    new { type = "text", text = request.UserPrompt },
                }
                : (object)request.UserPrompt;

            var payload = new
            {
                model = _defaultModel,
                max_tokens = request.MaxTokens ?? 1024,
                system = request.SystemPrompt,
                messages = new object[]
                {
                    new { role = "user", content = userContent }
                },
            };

            using var response = await client.PostAsJsonAsync(_completionsPath, payload, JsonOpts, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            sw.Stop();

            log.NetMs = (int)sw.ElapsedMilliseconds;
            log.TotalMs = (int)sw.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                log.ErrorMessage = $"AiGateway returned {(int)response.StatusCode}: {Truncate(raw, 1500)}";
                await PersistLogAsync(log, ct);
                return new AiCallResult(log.Id, false, "", log.Provider, log.TotalMs, log.ErrorMessage);
            }

            var text = ExtractText(raw);
            log.Success = true;
            log.ResultText = text;
            log.ResultJson = Truncate(raw, 8000);
            await PersistLogAsync(log, ct);

            return new AiCallResult(log.Id, true, text, log.Provider, log.TotalMs, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.NetMs = (int)sw.ElapsedMilliseconds;
            log.TotalMs = (int)sw.ElapsedMilliseconds;
            log.ErrorMessage = Truncate(ex.Message, 1500);
            log.Success = false;
            try { await PersistLogAsync(log, ct); } catch { /* don't mask the original */ }
            _logger.LogWarning(ex, "AiGateway call failed for mode {Mode}", request.Mode);
            return new AiCallResult(log.Id, false, "", log.Provider, log.TotalMs, ex.Message);
        }
    }

    public async Task RecordFeedbackAsync(Guid interactionLogId, short feedback, CancellationToken ct)
    {
        var log = await _db.AiInteractionLogs.FirstOrDefaultAsync(l => l.Id == interactionLogId, ct);
        if (log is null) return;
        log.Feedback = feedback;
        await _db.SaveChangesAsync(ct);
    }

    private async Task PersistLogAsync(AiInteractionLog log, CancellationToken ct)
    {
        _db.AiInteractionLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Extract the assistant text from an Anthropic Messages-style response.
    /// Falls back to OpenAI-style <c>choices[0].message.content</c> if shape differs.
    /// </summary>
    private static string ExtractText(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            // Anthropic: { "content": [ { "type": "text", "text": "..." } ] }
            if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                        return t.GetString() ?? "";
                }
            }

            // OpenAI: { "choices": [ { "message": { "content": "..." } } ] }
            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in choices.EnumerateArray())
                {
                    if (c.TryGetProperty("message", out var msg)
                        && msg.TryGetProperty("content", out var mc)
                        && mc.ValueKind == JsonValueKind.String)
                        return mc.GetString() ?? "";
                }
            }

            // Plain "text" or "output_text" fallback.
            if (root.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                return textProp.GetString() ?? "";

            if (root.TryGetProperty("output_text", out var outProp) && outProp.ValueKind == JsonValueKind.String)
                return outProp.GetString() ?? "";
        }
        catch
        {
            // fall through
        }
        return rawJson;
    }

    private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];
}
