using System.Net.Http.Json;
using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

/// <summary>
/// Typed HTTP client for the parent LDataBrain app. Used to fetch unified
/// communications (email + WhatsApp) and to send WhatsApp on the user's behalf.
///
/// Auth uses a shared API key sent as the X-Internal-Api-Key header (matches
/// LDataBrain's existing convention for runtime/agent endpoints).
/// </summary>
public sealed class LDataBrainBridge : ILDataBrainBridge
{
    public const string HttpClientName = "LDataBrain";

    private readonly IHttpClientFactory _http;
    private readonly ILogger<LDataBrainBridge> _logger;

    public LDataBrainBridge(IHttpClientFactory http, ILogger<LDataBrainBridge> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> SendWhatsAppAsync(string toPhone, string body, CancellationToken ct)
    {
        try
        {
            var client = _http.CreateClient(HttpClientName);
            if (client.BaseAddress is null)
            {
                _logger.LogWarning("LDataBrain BaseUrl not configured — skipping WhatsApp send.");
                return false;
            }

            using var resp = await client.PostAsJsonAsync(
                "api/integration/whatsapp/send",
                new { toPhone, body },
                ct);

            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("LDataBrain WhatsApp send failed: {Status} {Body}", (int)resp.StatusCode, raw);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LDataBrain WhatsApp send threw.");
            return false;
        }
    }

    public async Task<IReadOnlyList<IngestCommunicationDto>> FetchRecentCommunicationsAsync(DateTime since, CancellationToken ct)
    {
        try
        {
            var client = _http.CreateClient(HttpClientName);
            if (client.BaseAddress is null) return Array.Empty<IngestCommunicationDto>();

            using var resp = await client.GetAsync(
                $"api/integration/comms/recent?since={Uri.EscapeDataString(since.ToString("O"))}",
                ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogDebug("LDataBrain comms fetch returned {Status}", (int)resp.StatusCode);
                return Array.Empty<IngestCommunicationDto>();
            }

            var list = await resp.Content.ReadFromJsonAsync<List<IngestCommunicationDto>>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
            return list ?? new List<IngestCommunicationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LDataBrain comms fetch threw — returning empty.");
            return Array.Empty<IngestCommunicationDto>();
        }
    }
}
