using CRMManagement.Web.UiCustomization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CRMManagement.Web.Pages.UiCustomization;

public sealed class EditorModel : PageModel
{
    private readonly IUiLayoutClient _client;
    private readonly ILogger<EditorModel> _logger;
    private readonly IHostEnvironment _env;

    public EditorModel(IUiLayoutClient client, ILogger<EditorModel> logger, IHostEnvironment env)
    {
        _client = client;
        _logger = logger;
        _env = env;
    }

    [BindProperty(SupportsGet = true)]
    public string PageKey { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string FeatureKey { get; set; } = "dom_overrides";

    [BindProperty(SupportsGet = true, Name = "path")]
    public string? ReturnPath { get; set; }

    public string InitialOverridesJson { get; private set; } = "[]";

    /// <summary>
    /// Mirrors UiLayoutMiddleware normalisation: trim slashes, default "root".
    /// </summary>
    private string DerivePageKey()
    {
        var pk = (PageKey ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(pk))
            return pk;

        // Fall back to ReturnPath (same logic as the middleware).
        var rp = (ReturnPath ?? "").Split('?')[0].Trim('/');
        return string.IsNullOrWhiteSpace(rp) ? "root" : rp;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        try
        {
            var pageKey = DerivePageKey();
            PageKey = pageKey;   // so the Razor view serialises the correct value

            var featureKey = (FeatureKey ?? "").Trim();
            if (string.IsNullOrWhiteSpace(featureKey)) featureKey = "dom_overrides";

            var existing = await _client.GetLayoutAsync(pageKey, featureKey, ct);
            InitialOverridesJson = ExtractOverridesJson(existing);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UIEditor] OnGet failed to load initial overrides.");
            InitialOverridesJson = "[]";
        }
    }

    public sealed class SaveRequest
    {
        public string? PageKey { get; set; }
        public string? FeatureKey { get; set; }
        public string? ReturnPath { get; set; }
        public JsonElement Dom { get; set; }
    }

    public sealed class ResetRequest
    {
        public string? PageKey { get; set; }
        public string? ReturnPath { get; set; }
    }

    public sealed class ResetSelectedRequest
    {
        public string? PageKey { get; set; }
        public string? FeatureKey { get; set; }
        public string? ReturnPath { get; set; }
        public string? Selector { get; set; }
    }

    private static string ExtractOverridesJson(JsonElement? layout)
    {
        try
        {
            if (!layout.HasValue) return "[]";
            if (layout.Value.ValueKind != JsonValueKind.Object) return "[]";

            var root = (JsonNode.Parse(layout.Value.GetRawText()) as JsonObject) ?? new JsonObject();
            var domNode = root.TryGetPropertyValue("dom", out var domProp) ? domProp : root;

            if (domNode is not JsonObject domObj) return "[]";
            if (!domObj.TryGetPropertyValue("overrides", out var ovNode)) return "[]";
            if (ovNode is not JsonArray ovArr) return "[]";

            return ovArr.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch
        {
            return "[]";
        }
    }

    private static JsonArray ExtractOverridesArray(JsonElement? layout)
    {
        try
        {
            var json = ExtractOverridesJson(layout);
            return (JsonNode.Parse(json) as JsonArray) ?? new JsonArray();
        }
        catch
        {
            return new JsonArray();
        }
    }

    public async Task<IActionResult> OnPostSaveAsync([FromBody] SaveRequest req, CancellationToken ct)
    {
        try
        {
            if (req is null)
                return BadRequest("Missing body.");

            if (string.IsNullOrWhiteSpace(req.PageKey) || string.IsNullOrWhiteSpace(req.FeatureKey))
                return BadRequest("Missing pageKey/featureKey.");

            if (req.Dom.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return BadRequest("Missing dom payload.");

            if (req.Dom.ValueKind != JsonValueKind.Object)
                return BadRequest("Dom must be a JSON object.");

            var pageKey = req.PageKey.Trim();
            var featureKey = req.FeatureKey.Trim();

            _logger.LogInformation("[UIEditor] Save request. pageKey={PageKey} featureKey={FeatureKey}", pageKey, featureKey);

            var existing = await _client.GetLayoutAsync(pageKey, featureKey, ct);
            var mergedDom = MergeDom(existing, req.Dom);

            var layoutJson = JsonSerializer.SerializeToElement(
                new { dom = mergedDom },
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            );

            await _client.SaveLayoutAsync(pageKey, featureKey, layoutJson, ct);

            _logger.LogInformation("[UIEditor] Save ok. pageKey={PageKey} featureKey={FeatureKey}", pageKey, featureKey);
            return new JsonResult(new { ok = true, returnPath = req.ReturnPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UIEditor] Save failed.");
            var detail = _env.IsDevelopment() ? ex.Message : "Save failed (server error).";
            return StatusCode(500, new { ok = false, error = detail });
        }
    }

    private static JsonObject MergeDom(JsonElement? existingLayout, JsonElement incomingDom)
    {
        JsonObject existingDomObj = new();

        if (existingLayout.HasValue && existingLayout.Value.ValueKind == JsonValueKind.Object)
        {
            var root = existingLayout.Value;
            if (root.TryGetProperty("dom", out var domProp) && domProp.ValueKind == JsonValueKind.Object)
                existingDomObj = (JsonNode.Parse(domProp.GetRawText()) as JsonObject) ?? new JsonObject();
            else
                existingDomObj = (JsonNode.Parse(root.GetRawText()) as JsonObject) ?? new JsonObject();
        }

        var incomingDomObj = (JsonNode.Parse(incomingDom.GetRawText()) as JsonObject) ?? new JsonObject();

        var mergedOverrides = new Dictionary<string, JsonObject>(StringComparer.Ordinal);

        void IngestOverrides(JsonObject domObj)
        {
            if (!domObj.TryGetPropertyValue("overrides", out var ovNode)) return;
            if (ovNode is not JsonArray arr) return;

            foreach (var n in arr)
            {
                if (n is not JsonObject o) continue;
                var selector = o.TryGetPropertyValue("selector", out var sNode) ? (sNode?.ToString() ?? "") : "";
                selector = selector.Trim();
                if (string.IsNullOrWhiteSpace(selector)) continue;
                mergedOverrides[selector] = (JsonNode.Parse(o.ToJsonString()) as JsonObject) ?? o;
            }
        }

        IngestOverrides(existingDomObj);
        IngestOverrides(incomingDomObj);

        var finalDom = (JsonNode.Parse(existingDomObj.ToJsonString()) as JsonObject) ?? new JsonObject();

        foreach (var kv in incomingDomObj)
        {
            if (string.Equals(kv.Key, "overrides", StringComparison.OrdinalIgnoreCase))
                continue;
            finalDom[kv.Key] = kv.Value is null ? null : JsonNode.Parse(kv.Value.ToJsonString());
        }

        var finalOverrides = new JsonArray();
        foreach (var ov in mergedOverrides.Values)
            finalOverrides.Add(JsonNode.Parse(ov.ToJsonString()));

        finalDom["overrides"] = finalOverrides;
        return finalDom;
    }

    public async Task<IActionResult> OnPostResetAsync([FromBody] ResetRequest? req, CancellationToken ct)
    {
        try
        {
            var pageKey = (req?.PageKey ?? PageKey ?? "").Trim();
            if (string.IsNullOrWhiteSpace(pageKey)) pageKey = "root";

            _logger.LogInformation("[UIEditor] Reset called. pageKey={PageKey}", pageKey);

            await _client.ResetLayoutAsync(pageKey, "page_theme", ct);
            await _client.ResetLayoutAsync(pageKey, "dom_overrides", ct);

            return new JsonResult(new { ok = true, pageKey, returnPath = req?.ReturnPath ?? ReturnPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UIEditor] Reset failed.");
            var detail = _env.IsDevelopment() ? ex.Message : "Reset failed (server error).";
            return StatusCode(500, new { ok = false, error = detail });
        }
    }

    public async Task<IActionResult> OnPostResetSelectedAsync([FromBody] ResetSelectedRequest? req, CancellationToken ct)
    {
        try
        {
            var pageKey = (req?.PageKey ?? PageKey ?? "").Trim();
            if (string.IsNullOrWhiteSpace(pageKey)) pageKey = "root";

            var featureKey = (req?.FeatureKey ?? FeatureKey ?? "").Trim();
            if (string.IsNullOrWhiteSpace(featureKey)) featureKey = "dom_overrides";

            var selector = (req?.Selector ?? "").Trim();
            if (string.IsNullOrWhiteSpace(selector))
                return BadRequest(new { ok = false, error = "Missing selector." });

            _logger.LogInformation("[UIEditor] ResetSelected. pageKey={PageKey} selector={Selector}", pageKey, selector);

            var existing = await _client.GetLayoutAsync(pageKey, featureKey, ct);
            var overrides = ExtractOverridesArray(existing);

            var removed = 0;
            for (var i = overrides.Count - 1; i >= 0; i--)
            {
                if (overrides[i] is not JsonObject o) continue;
                var sel = o.TryGetPropertyValue("selector", out var sNode) ? (sNode?.ToString() ?? "") : "";
                if (!string.Equals(sel?.Trim(), selector, StringComparison.Ordinal)) continue;
                overrides.RemoveAt(i);
                removed++;
            }

            if (removed == 0)
                return new JsonResult(new { ok = true, removed = 0 });

            if (overrides.Count == 0)
            {
                await _client.ResetLayoutAsync(pageKey, featureKey, ct);
                return new JsonResult(new { ok = true, removed, deleted = true });
            }

            var layoutJson = JsonSerializer.SerializeToElement(
                new { dom = new { overrides } },
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            );

            await _client.SaveLayoutAsync(pageKey, featureKey, layoutJson, ct);
            return new JsonResult(new { ok = true, removed, deleted = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UIEditor] ResetSelected failed.");
            var detail = _env.IsDevelopment() ? ex.Message : "Reset selected failed (server error).";
            return StatusCode(500, new { ok = false, error = detail });
        }
    }
}
