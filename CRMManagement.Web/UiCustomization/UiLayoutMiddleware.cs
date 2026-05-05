namespace CRMManagement.Web.UiCustomization;

/// <summary>
/// Intercepts HTML GET requests and fetches stored DOM overrides,
/// placing them in HttpContext.Items for the layout to inject as window.__UI_DOM__.
/// </summary>
public sealed class UiLayoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UiLayoutMiddleware> _log;

    public UiLayoutMiddleware(RequestDelegate next, ILogger<UiLayoutMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task Invoke(HttpContext ctx, IUiLayoutClient client)
    {
        // Skip static assets and non-HTML requests
        var path = ctx.Request.Path.Value ?? "";
        if (path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        if (!HttpMethods.IsGet(ctx.Request.Method))
        {
            await _next(ctx);
            return;
        }

        var accept = ctx.Request.Headers.Accept.ToString();
        if (!string.IsNullOrWhiteSpace(accept) &&
            accept.IndexOf("text/html", StringComparison.OrdinalIgnoreCase) < 0 &&
            accept.IndexOf("application/xhtml", StringComparison.OrdinalIgnoreCase) < 0)
        {
            await _next(ctx);
            return;
        }

        // Normalize page key
        var pageKey = path.Trim('/');
        if (string.IsNullOrWhiteSpace(pageKey)) pageKey = "root";

        ctx.Items["ui.page_key"] = pageKey;

        try
        {
            var dom = await client.GetLayoutAsync(pageKey, "dom_overrides", ctx.RequestAborted);
            if (dom.HasValue)
            {
                ctx.Items["ui.dom_json"] = dom.Value.GetRawText();
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[UiLayout] Fetch failed for pageKey={PageKey}", pageKey);
        }

        await _next(ctx);
    }
}
