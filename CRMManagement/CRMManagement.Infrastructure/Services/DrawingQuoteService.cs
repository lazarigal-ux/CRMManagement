using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

public sealed class DrawingQuoteService : IDrawingQuoteService
{
    private const string SystemPrompt = """
        You are a CAD/architectural drawing analyzer. Look at the provided drawing and
        identify discrete devices/symbols. Group them by type and return ONLY valid JSON
        in this EXACT shape (no prose, no markdown, no code fences):

        {
          "items": [
            { "label": "<short device name, lowercase, singular>", "count": <integer>, "notes": "<optional clarification>" }
          ]
        }

        Rules:
        - Use short, generic, lowercase labels (e.g. "smoke detector", "sprinkler", "exit sign", "fire extinguisher", "speaker", "junction box").
        - Do NOT include door swings, walls, windows, or text labels — only countable installable devices.
        - If you cannot identify any devices, return {"items": []}.
        - Counts must be integers. No ranges, no estimates as text.
        """;

    private readonly AppDbContext _db;
    private readonly ICrmAiClient _ai;
    private readonly ILogger<DrawingQuoteService> _logger;

    public DrawingQuoteService(AppDbContext db, ICrmAiClient ai, ILogger<DrawingQuoteService> logger)
    {
        _db = db;
        _ai = ai;
        _logger = logger;
    }

    public async Task<DrawingAnalysisDto> AnalyzeAsync(DrawingAnalyzeRequest request, CancellationToken ct)
    {
        var analysis = new DrawingAnalysis
        {
            Id = Guid.NewGuid(),
            OpportunityId = request.OpportunityId,
            AccountId = request.AccountId,
            SourceFileName = request.SourceFileName,
            MediaType = request.MediaType,
            Instruction = request.Instruction,
            Status = "analyzing",
        };
        _db.DrawingAnalyses.Add(analysis);
        await _db.SaveChangesAsync(ct);

        var userPrompt = string.IsNullOrWhiteSpace(request.Instruction)
            ? "Identify and count all installable devices in this drawing. Return JSON only."
            : $"Identify and count all installable devices in this drawing. Hint from the rep: {request.Instruction}\nReturn JSON only.";

        var aiResult = await _ai.CallAsync(
            new AiCallRequest(
                SystemPrompt: SystemPrompt,
                UserPrompt: userPrompt,
                Mode: "drawing-quote.count",
                Provider: null,
                MaxTokens: 1500,
                ImageBase64: request.ImageBase64,
                ImageMediaType: request.MediaType),
            ct);

        analysis.AiLogId = aiResult.InteractionLogId == Guid.Empty ? null : aiResult.InteractionLogId;

        if (!aiResult.Success)
        {
            analysis.Status = "failed";
            analysis.ErrorMessage = aiResult.ErrorMessage ?? "AI call failed.";
            await _db.SaveChangesAsync(ct);
            return await ProjectAsync(analysis, ct);
        }

        var rawJson = ExtractJson(aiResult.Text);
        if (rawJson is null)
        {
            analysis.Status = "failed";
            analysis.ErrorMessage = "AI did not return parseable JSON.";
            analysis.ItemsJson = aiResult.Text;
            await _db.SaveChangesAsync(ct);
            return await ProjectAsync(analysis, ct);
        }

        analysis.ItemsJson = rawJson;
        analysis.Status = "ready";
        await _db.SaveChangesAsync(ct);
        return await ProjectAsync(analysis, ct);
    }

    public async Task<DrawingAnalysisDto?> GetAsync(Guid analysisId, CancellationToken ct)
    {
        var analysis = await _db.DrawingAnalyses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == analysisId, ct);
        return analysis is null ? null : await ProjectAsync(analysis, ct);
    }

    public async Task<Guid> CreateQuoteAsync(CreateQuoteFromAnalysisRequest request, CancellationToken ct)
    {
        var analysis = await _db.DrawingAnalyses.FirstOrDefaultAsync(a => a.Id == request.AnalysisId, ct)
            ?? throw new InvalidOperationException("Analysis not found.");
        if (analysis.Status != "ready")
            throw new InvalidOperationException($"Analysis is not ready (status='{analysis.Status}').");

        var parsed = ParseItems(analysis.ItemsJson);
        var mappings = await LoadMappingsForLabelsAsync(parsed.Select(p => p.Label).Distinct().ToList(), ct);

        Guid? accountId = analysis.AccountId;
        if (analysis.OpportunityId is Guid oid)
        {
            var opp = await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oid, ct);
            accountId ??= opp?.AccountId;
        }

        var overrides = (request.Overrides ?? Array.Empty<AnalyzedItemOverrideDto>())
            .ToDictionary(o => o.Label.ToLowerInvariant(), o => o);

        // Phase 4 learning loop: if the rep picked a product for a label that wasn't mapped,
        // remember that decision so future drawings auto-map it.
        await LearnNewMappingsAsync(parsed, overrides, mappings, ct);

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            QuoteNumber = $"Q-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
            Name = string.IsNullOrWhiteSpace(request.QuoteName) ? "Drawing-generated quote" : request.QuoteName,
            AccountId = accountId,
            OpportunityId = analysis.OpportunityId,
            Status = "Draft",
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
            Notes = request.Notes,
            Discount = request.Discount ?? 0m,
            Tax = request.Tax ?? 0m,
        };

        decimal subtotal = 0m;
        int sort = 0;
        foreach (var item in parsed)
        {
            var key = item.Label.ToLowerInvariant();
            overrides.TryGetValue(key, out var ov);
            mappings.TryGetValue(key, out var map);

            var productId = ov?.ProductId ?? map?.ProductId;
            decimal? unitPrice = ov?.UnitPriceOverride ?? map?.Product?.StandardPrice;
            int qty = (ov?.CountOverride ?? item.Count);
            if (map?.Multiplier is decimal m && m > 0) qty = (int)Math.Ceiling(qty * (double)m);
            if (qty <= 0) continue;

            var line = new QuoteLine
            {
                Id = Guid.NewGuid(),
                QuoteId = quote.Id,
                ProductId = productId,
                Description = (productId is null)
                    ? $"[unmapped] {item.Label}{(string.IsNullOrWhiteSpace(item.Notes) ? "" : " — " + item.Notes)}"
                    : (map?.Product?.Name ?? item.Label),
                Quantity = qty,
                UnitPrice = unitPrice ?? 0m,
                Discount = 0m,
                LineTotal = qty * (unitPrice ?? 0m),
                SortOrder = sort++,
            };
            subtotal += line.LineTotal;
            quote.Lines.Add(line);
        }

        quote.Subtotal = subtotal;
        quote.Total = subtotal - quote.Discount + quote.Tax;

        _db.Quotes.Add(quote);
        analysis.QuoteId = quote.Id;
        analysis.Status = "quoted";
        await _db.SaveChangesAsync(ct);
        return quote.Id;
    }

    private async Task LearnNewMappingsAsync(
        IReadOnlyList<ParsedItem> parsed,
        Dictionary<string, AnalyzedItemOverrideDto> overrides,
        Dictionary<string, ClassProductMapping> existing,
        CancellationToken ct)
    {
        var newMappings = new List<ClassProductMapping>();
        var seenLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in parsed)
        {
            var label = item.Label.ToLowerInvariant();
            if (existing.ContainsKey(label)) continue;          // already mapped
            if (!seenLabels.Add(label)) continue;               // dedup
            if (!overrides.TryGetValue(label, out var ov)) continue;
            if (ov.ProductId is null || ov.ProductId == Guid.Empty) continue;

            newMappings.Add(new ClassProductMapping
            {
                Id = Guid.NewGuid(),
                Label = label,
                ProductId = ov.ProductId.Value,
                Multiplier = 1.0m,
                Notes = "Learned from drawing-quote review.",
                IsActive = true,
            });
        }
        if (newMappings.Count == 0) return;

        // The Label index is unique — guard against a race where the same label was learned
        // moments ago (e.g. another quote being created at the same time).
        var labels = newMappings.Select(m => m.Label).ToArray();
        var alreadyThere = await _db.ClassProductMappings
            .AsNoTracking()
            .Where(m => labels.Contains(m.Label.ToLower()))
            .Select(m => m.Label.ToLowerInvariant())
            .ToListAsync(ct);
        var alreadySet = new HashSet<string>(alreadyThere, StringComparer.OrdinalIgnoreCase);
        var toInsert = newMappings.Where(m => !alreadySet.Contains(m.Label)).ToList();
        if (toInsert.Count == 0) return;

        _db.ClassProductMappings.AddRange(toInsert);
        // Saved later as part of the quote-creation transaction.
    }

    // ── helpers ───────────────────────────────────────────────────────────────────

    private async Task<DrawingAnalysisDto> ProjectAsync(DrawingAnalysis analysis, CancellationToken ct)
    {
        var parsed = ParseItems(analysis.ItemsJson);
        var mappings = await LoadMappingsForLabelsAsync(parsed.Select(p => p.Label).Distinct().ToList(), ct);

        var items = parsed.Select(p =>
        {
            var key = p.Label.ToLowerInvariant();
            mappings.TryGetValue(key, out var map);
            return new AnalyzedItemDto(
                p.Label,
                p.Count,
                p.Notes,
                map?.ProductId,
                map?.Product?.Name,
                map?.Product?.Sku,
                map?.Product?.StandardPrice,
                map?.Multiplier,
                map is not null);
        }).ToList();

        return new DrawingAnalysisDto(
            analysis.Id,
            analysis.OpportunityId,
            analysis.AccountId,
            analysis.Status,
            analysis.SourceFileName,
            analysis.Instruction,
            analysis.CreatedAt,
            analysis.AiLogId,
            analysis.QuoteId,
            analysis.ErrorMessage,
            items);
    }

    private async Task<Dictionary<string, ClassProductMapping>> LoadMappingsForLabelsAsync(IList<string> labels, CancellationToken ct)
    {
        if (labels.Count == 0) return new(StringComparer.OrdinalIgnoreCase);
        var keys = labels.Select(l => l.ToLowerInvariant()).ToArray();
        var rows = await _db.ClassProductMappings
            .AsNoTracking()
            .Include(m => m.Product)
            .Where(m => m.IsActive && keys.Contains(m.Label.ToLower()))
            .ToListAsync(ct);
        return rows.ToDictionary(m => m.Label.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
    }

    private record ParsedItem(string Label, int Count, string? Notes);

    private static IReadOnlyList<ParsedItem> ParseItems(string? itemsJson)
    {
        if (string.IsNullOrWhiteSpace(itemsJson)) return Array.Empty<ParsedItem>();
        try
        {
            using var doc = JsonDocument.Parse(itemsJson);
            if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return Array.Empty<ParsedItem>();
            var list = new List<ParsedItem>();
            foreach (var el in items.EnumerateArray())
            {
                var label = el.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "";
                var count = el.TryGetProperty("count", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
                var notes = el.TryGetProperty("notes", out var n) ? n.GetString() : null;
                if (string.IsNullOrWhiteSpace(label) || count <= 0) continue;
                list.Add(new ParsedItem(label.Trim().ToLowerInvariant(), count, notes));
            }
            // Collapse duplicates by label.
            return list
                .GroupBy(i => i.Label)
                .Select(g => new ParsedItem(g.Key, g.Sum(x => x.Count), g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Notes))?.Notes))
                .ToList();
        }
        catch
        {
            return Array.Empty<ParsedItem>();
        }
    }

    /// <summary>
    /// The model is told to return JSON only, but reality bites. Strip ```json fences,
    /// pull the first {...} block if there's chatter around it.
    /// </summary>
    private static string? ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
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

        var first = t.IndexOf('{');
        var last  = t.LastIndexOf('}');
        if (first >= 0 && last > first) return t.Substring(first, last - first + 1);
        return null;
    }
}

public sealed class ClassProductMappingService : IClassProductMappingService
{
    private readonly AppDbContext _db;
    public ClassProductMappingService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ClassProductMappingDto>> ListAsync(CancellationToken ct)
    {
        return await _db.ClassProductMappings
            .AsNoTracking()
            .Include(m => m.Product)
            .OrderBy(m => m.Label)
            .Select(m => new ClassProductMappingDto(
                m.Id, m.Label, m.ProductId,
                m.Product!.Name, m.Product!.Sku,
                m.Multiplier, m.Notes, m.IsActive))
            .ToListAsync(ct);
    }

    public async Task<Guid> UpsertAsync(ClassProductMappingUpsertDto dto, CancellationToken ct)
    {
        ClassProductMapping e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.ClassProductMappings.FirstOrDefaultAsync(m => m.Id == id, ct)
                ?? throw new InvalidOperationException("Mapping not found.");
        }
        else
        {
            e = new ClassProductMapping { Id = Guid.NewGuid() };
            _db.ClassProductMappings.Add(e);
        }
        e.Label = dto.Label.Trim().ToLowerInvariant();
        e.ProductId = dto.ProductId;
        e.Multiplier = dto.Multiplier <= 0 ? 1m : dto.Multiplier;
        e.Notes = dto.Notes;
        e.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.ClassProductMappings.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (e is null) return;
        _db.ClassProductMappings.Remove(e);
        await _db.SaveChangesAsync(ct);
    }
}
