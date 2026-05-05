using Npgsql;
using CRMManagement.Infrastructure;
using CRMManagement.Infrastructure.Data;
using CRMManagement.Web.Configuration;
using CRMManagement.Web.Configuration.Auth;
using CRMManagement.Web.UiCustomization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CRMManagement.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Best-effort: if DB password env vars are missing (common when starting from VS), load them from the installer-style .env.
// Does not override already-set environment variables (e.g., run-crmmanagement-dev.ps1).
try
{
    var contentRoot = builder.Environment.ContentRootPath;
    var envPathFromEnv = Environment.GetEnvironmentVariable("WORKMANAGEMENT_ENV_FILE")
                         ?? Environment.GetEnvironmentVariable("DOTENV_PATH");
    var envPath = !string.IsNullOrWhiteSpace(envPathFromEnv)
        ? envPathFromEnv!
        : Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "YAMLFile", ".env"));

    DotEnvLoader.LoadIfMissing(envPath,
        "APP_DB_NAME",
        "APP_DB_USER",
        "APP_DB_PASSWORD",
        "DB_HOST",
        "DB_PORT",
        "DB_NAME",
        "DB_USER",
        "DB_PASSWORD",
        "CORE_POSTGRES_HOST",
        "CORE_POSTGRES_DB",
        "CORE_POSTGRES_USER",
        "CORE_POSTGRES_PASSWORD");
}
catch
{
    // Best-effort: log so the user knows the .env file wasn't loaded.
    Console.WriteLine("[CRMManagement] .env file load skipped (file not found or parse error).");
}

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Auth");
    options.Conventions.AllowAnonymousToFolder("/Portal");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToPage("/Privacy");
});
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration);

// DataProtection — used to encrypt/decrypt Zoho refresh tokens at rest.
// Persist keys to App_Data so they survive restarts and aren't re-generated on each boot.
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")))
    .SetApplicationName("CRMManagement");

// UI Editor: local-file-based layout storage
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IUiLayoutClient, LocalFileUiLayoutClient>();

builder.Services
    .AddOptions<OidcAuthOptions>()
    .Bind(builder.Configuration.GetSection(OidcAuthOptions.SectionName))
    ;

builder.Services
    .AddOptions<GitHubCodeOptions>()
    .Bind(builder.Configuration.GetSection(GitHubCodeOptions.SectionName))
    ;

var oidc = builder.Configuration.GetSection(OidcAuthOptions.SectionName).Get<OidcAuthOptions>() ?? new OidcAuthOptions();
var oidcAuthorityOk = Uri.TryCreate(oidc.Authority, UriKind.Absolute, out var oidcAuthorityUri)
                      && (string.Equals(oidcAuthorityUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(oidcAuthorityUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

if (oidc.Enabled && oidcAuthorityOk && !string.IsNullOrWhiteSpace(oidc.ClientId))
{
    builder.Services.AddAuthentication()
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = oidcAuthorityUri!.ToString().TrimEnd('/');
            options.ClientId = oidc.ClientId;
            options.ClientSecret = string.IsNullOrWhiteSpace(oidc.ClientSecret) ? null : oidc.ClientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = true;

            // Identity external sign-in flow uses the external cookie.
            options.SignInScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme;

            options.GetClaimsFromUserInfoEndpoint = true;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");

            // Common Keycloak claim mapping.
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = oidc.NameClaimType,
                RoleClaimType = oidc.RoleClaimType
            };

            // Keep it flexible: allow http metadata in dev if the user chooses.
            options.RequireHttpsMetadata = oidc.RequireHttpsMetadata;
        });
}

var github = builder.Configuration.GetSection(GitHubCodeOptions.SectionName).Get<GitHubCodeOptions>() ?? new GitHubCodeOptions();
if (github.Enabled && !string.IsNullOrWhiteSpace(github.ClientId) && !string.IsNullOrWhiteSpace(github.ClientSecret))
{
    builder.Services.AddAuthentication()
        .AddOAuth("github-code", options =>
        {
            options.ClientId = github.ClientId;
            options.ClientSecret = github.ClientSecret;
            options.CallbackPath = "/signin-github-code";

            options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
            options.TokenEndpoint = "https://github.com/login/oauth/access_token";
            options.UserInformationEndpoint = "https://api.github.com/user";

            options.Scope.Clear();
            options.Scope.Add("read:user");
            options.Scope.Add("repo");

            options.SaveTokens = true;

            // Avoid "Correlation failed" in local dev on http://localhost.
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

            // Store the OAuth ticket in the external cookie, so Razor Pages can read tokens after the redirect.
            options.SignInScheme = IdentityConstants.ExternalScheme;

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async context =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                    request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CRMManagement", "1.0"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                    using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                    response.EnsureSuccessStatusCode();

                    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));

                    var root = payload.RootElement;
                    if (context.Identity != null)
                    {
                        if (root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
                        {
                            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, idEl.GetInt64().ToString()));
                        }

                        if (root.TryGetProperty("login", out var loginEl))
                        {
                            var login = loginEl.GetString();
                            if (!string.IsNullOrWhiteSpace(login))
                            {
                                context.Identity.AddClaim(new Claim("urn:github:login", login));
                            }
                        }

                        if (root.TryGetProperty("name", out var nameEl))
                        {
                            var name = nameEl.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                context.Identity.AddClaim(new Claim(ClaimTypes.Name, name));
                            }
                        }
                    }
                }
            };
        });
}

var app = builder.Build();

// PathBase support (for /crm behind nginx reverse proxy).
var rawPathBase = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE");
var normalizedPathBase = string.IsNullOrWhiteSpace(rawPathBase)
    ? null
    : (rawPathBase.StartsWith('/') ? rawPathBase : "/" + rawPathBase).TrimEnd('/');
if (!string.IsNullOrEmpty(normalizedPathBase) && normalizedPathBase != "/")
{
    app.UsePathBase(normalizedPathBase);
    app.Use((ctx, next) =>
    {
        ctx.Request.PathBase = normalizedPathBase;
        return next();
    });
}

// If the DB is unavailable in Development (common when Postgres isn't started yet),
// show a friendly 503 page instead of throwing unhandled exceptions on first DB access.
var isDbAvailable = true;

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var urlsSetting = builder.Configuration["ASPNETCORE_URLS"]
                  ?? builder.WebHost.GetSetting("urls");

var hasHttpsListener = !string.IsNullOrWhiteSpace(urlsSetting)
    && urlsSetting.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

if (hasHttpsListener)
{
    app.UseHttpsRedirection();
}

// Security headers
var allowedFrameAncestors = builder.Configuration
    .GetSection("CRMManagement:AllowedFrameAncestors")
    .Get<string[]>() ?? Array.Empty<string>();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=()";

    var req = ctx.Request;
    var isEmbedRequest =
        IsTruthy(req.Query["embed"]) ||
        IsTruthy(req.Query["iframe"]) ||
        req.Cookies["crm_embed"] == "1" ||
        string.Equals(req.Headers["Sec-Fetch-Dest"].ToString(), "iframe", StringComparison.OrdinalIgnoreCase);

    if (isEmbedRequest && allowedFrameAncestors.Length > 0)
    {
        // CSP frame-ancestors supersedes X-Frame-Options in modern browsers, but
        // Firefox still honors XFO when both are present — so we drop XFO here.
        ctx.Response.Headers["Content-Security-Policy"] =
            "frame-ancestors 'self' " + string.Join(' ', allowedFrameAncestors);
    }
    else
    {
        ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    }

    await next();
});

app.UseStaticFiles();

// UI Editor middleware: loads saved DOM overrides for each page
app.UseMiddleware<UiLayoutMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.Use(async (ctx, next) =>
{
    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        await next();
        return;
    }

    if (!HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method))
    {
        await next();
        return;
    }

    var isEmbedRequest = IsTruthy(ctx.Request.Query["embed"]) || IsTruthy(ctx.Request.Query["iframe"]);
    if (!isEmbedRequest)
    {
        await next();
        return;
    }

    var embedToken = ctx.Request.Query["embedToken"].ToString();
    var embedSecret = builder.Configuration["CRMManagement:EmbedSecret"];
    if (string.IsNullOrWhiteSpace(embedToken) || string.IsNullOrWhiteSpace(embedSecret))
    {
        await next();
        return;
    }

    // Try the new JSON-payload token format first (carries user identity),
    // then fall back to the legacy {expiresAt}.{signature} format.
    string? embedUserName = null;
    string? sourceUserId  = null;

    if (TryValidateEmbedTokenV2(embedToken, embedSecret, out var tokenUserName, out var tokenSourceId))
    {
        embedUserName = tokenUserName;
        sourceUserId  = tokenSourceId;
    }
    else if (TryValidateEmbedTokenLegacy(embedToken, embedSecret))
    {
        // Legacy token – no user identity; fall back to config.
        embedUserName = builder.Configuration["CRMManagement:EmbedUserName"];
    }
    else
    {
        await next();
        return;
    }

    if (string.IsNullOrWhiteSpace(embedUserName))
    {
        embedUserName = builder.Configuration["CRMManagement:EmbedUserName"];
        if (string.IsNullOrWhiteSpace(embedUserName))
            embedUserName = "admin";
    }

    var userManager = ctx.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
    var signInManager = ctx.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
    var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("CRMManagementEmbedAuth");

    var user = await userManager.Users
        .FirstOrDefaultAsync(x => x.UserName == embedUserName && x.IsActive, ctx.RequestAborted);

    // Auto-provision user from the main site if not found locally.
    if (user == null && !string.IsNullOrWhiteSpace(embedUserName))
    {
        user = new ApplicationUser
        {
            UserName  = embedUserName,
            Email     = $"{embedUserName}@ldatabrain.local",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "User");
            logger.LogInformation(
                "Auto-provisioned CRMManagement user '{UserName}' (source ID {SourceId}) from embed token.",
                embedUserName, sourceUserId ?? "n/a");
        }
        else
        {
            logger.LogWarning(
                "Failed to auto-provision user '{UserName}': {Errors}",
                embedUserName,
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            user = null;
        }
    }

    if (user == null)
    {
        logger.LogWarning("Embedded CRMManagement sign-in skipped because user '{UserName}' was not found or inactive.", embedUserName);
        await next();
        return;
    }

    await signInManager.SignInAsync(user, isPersistent: false);
    ctx.User = await signInManager.CreateUserPrincipalAsync(user);

    ctx.Response.Cookies.Append("crm_embed", "1", new CookieOptions
    {
        HttpOnly = false,
        SameSite = SameSiteMode.None,
        Secure = ctx.Request.IsHttps,
        MaxAge = TimeSpan.FromHours(12)
    });

    if (ctx.Request.Query.ContainsKey("embedToken"))
    {
        var cleanedQuery = QueryString.Create(
            ctx.Request.Query
                .Where(kvp => !string.Equals(kvp.Key, "embedToken", StringComparison.OrdinalIgnoreCase))
                .SelectMany(kvp => kvp.Value, (kvp, value) => new KeyValuePair<string, string?>(kvp.Key, value)));

        var cleanedUrl = $"{ctx.Request.PathBase}{ctx.Request.Path}{cleanedQuery}";
        ctx.Response.Redirect(cleanedUrl);
        return;
    }

    await next();
});
app.UseAuthorization();

app.MapRazorPages();
app.MapHealthChecks("/healthz");

// ── Zoho CRM read-only API (reads local DB mirror) + import status banner ──
app.MapZohoApi();
app.MapZohoImportApi();

var internalZohoImportGate = new SemaphoreSlim(1, 1);

// Internal endpoint for host-side AI agents. Triggers a Zoho import and blocks until the local
// CRM mirror tables have been refreshed. Mirrors WorkManagement's /api/internal/jira-import/run-and-wait.
// Auth: X-Agent-Key matched against (in order) CRMMANAGEMENT_INTERNAL_API_KEY env / CRMManagement:InternalApiKey config /
// AGENT_API_KEY env / AGENT_API_KEY config.
app.MapPost("/api/internal/zoho-import/run-and-wait", async (
    HttpContext ctx,
    CRMManagement.Application.Abstractions.IZohoImportService imports,
    CRMManagement.Application.Abstractions.IZohoConnectionService connections,
    Microsoft.Extensions.Configuration.IConfiguration cfg,
    Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) =>
{
    var expected = new[]
    {
        Environment.GetEnvironmentVariable("CRMMANAGEMENT_INTERNAL_API_KEY"),
        cfg["CRMManagement:InternalApiKey"],
        Environment.GetEnvironmentVariable("AGENT_API_KEY"),
        cfg["AGENT_API_KEY"]
    }.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    if (string.IsNullOrWhiteSpace(expected))
    {
        return Results.Problem("Internal Zoho import key is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var provided = ctx.Request.Headers["X-Agent-Key"].ToString();
    if (!string.Equals(provided, expected, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    if (!await internalZohoImportGate.WaitAsync(0, ctx.RequestAborted))
    {
        return Results.Conflict(new { ok = false, message = "A Zoho import is already running." });
    }

    try
    {
        // Default = all modules enabled. Caller can override by posting { "modules": ["leads","contacts",...] }.
        var enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "leads","contacts","accounts","deals","products","quotes","activities","campaigns",
            "tickets","invoices","orders","notes","vendors","purchaseorders","solutions"
        };

        if ((ctx.Request.ContentLength ?? 0) > 0)
        {
            try
            {
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body, cancellationToken: ctx.RequestAborted);
                if (doc.RootElement.TryGetProperty("modules", out var modsEl) && modsEl.ValueKind == JsonValueKind.Array)
                {
                    enabled.Clear();
                    foreach (var m in modsEl.EnumerateArray())
                    {
                        if (m.ValueKind == JsonValueKind.String)
                        {
                            var v = m.GetString();
                            if (!string.IsNullOrWhiteSpace(v)) enabled.Add(v);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                return Results.BadRequest(new { ok = false, message = "Invalid JSON body." });
            }
        }

        // Verify the OAuth connection is configured before kicking off — otherwise the import
        // would fail in its first reader call. Surface as 503 so callers can distinguish from auth/conflict.
        var conn = await connections.GetAsync(ctx.RequestAborted);
        if (conn is null || !conn.HasRefreshToken)
        {
            return Results.Problem("Zoho is not connected (no refresh token).", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var request = new CRMManagement.Application.DTOs.ZohoImportRequest(
            Leads:          enabled.Contains("leads"),
            Contacts:       enabled.Contains("contacts"),
            Accounts:       enabled.Contains("accounts"),
            Deals:          enabled.Contains("deals"),
            Products:       enabled.Contains("products"),
            Quotes:         enabled.Contains("quotes"),
            Activities:     enabled.Contains("activities"),
            Campaigns:      enabled.Contains("campaigns"),
            Tickets:        enabled.Contains("tickets"),
            Invoices:       enabled.Contains("invoices"),
            Orders:         enabled.Contains("orders"),
            Notes:          enabled.Contains("notes"),
            Vendors:        enabled.Contains("vendors"),
            PurchaseOrders: enabled.Contains("purchaseorders"),
            Solutions:      enabled.Contains("solutions"));

        var logger = loggerFactory.CreateLogger("InternalZohoImport");
        try
        {
            var dto = await imports.RunImportAndWaitAsync(request, ctx.RequestAborted);
            return Results.Ok(new { ok = true, jobId = dto.Id, status = dto.Status, message = dto.Message, job = dto });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { ok = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal Zoho import failed.");
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
    finally
    {
        internalZohoImportGate.Release();
    }
}).AllowAnonymous().DisableAntiforgery();

// ── AI Assistant API (Phase 1) ──
app.MapPost("/api/ai/assistant", async (
    AiAssistantApiRequest body,
    CRMManagement.Infrastructure.Services.IAiAssistantService assistant,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<CRMManagement.Application.DTOs.AiEntityKind>(body.EntityKind, ignoreCase: true, out var kind))
        return Results.BadRequest(new { error = $"Unknown entityKind '{body.EntityKind}'." });
    if (!Enum.TryParse<CRMManagement.Application.DTOs.AiAssistantAction>(body.Action, ignoreCase: true, out var action))
        return Results.BadRequest(new { error = $"Unknown action '{body.Action}'." });

    var resp = await assistant.RunAsync(
        new CRMManagement.Application.DTOs.AiAssistantRequest(kind, body.EntityId, action, body.UserHint, body.Channel),
        ct);
    return Results.Ok(resp);
}).RequireAuthorization();

app.MapPost("/api/ai/assistant/{logId:guid}/feedback", async (
    Guid logId,
    int rating,
    CRMManagement.Application.Abstractions.ICrmAiClient ai,
    CancellationToken ct) =>
{
    var clamped = (short)Math.Clamp(rating, -1, 2);
    await ai.RecordFeedbackAsync(logId, clamped, ct);
    return Results.Ok(new { logId, feedback = clamped });
}).RequireAuthorization();

// ── Inbound WhatsApp → Lead (Phase 8) ──
app.MapPost("/api/integration/lead-from-whatsapp", async (
    CRMManagement.Application.DTOs.WhatsAppLeadIngestionDto body,
    CRMManagement.Application.Abstractions.IWhatsAppLeadService svc,
    HttpContext ctx,
    Microsoft.Extensions.Configuration.IConfiguration cfg,
    CancellationToken ct) =>
{
    var expected = cfg["LDataBrain:InboundApiKey"]
                   ?? Environment.GetEnvironmentVariable("LDATABRAIN_INBOUND_API_KEY");
    if (string.IsNullOrWhiteSpace(expected))
        return Results.Problem("LDataBrain:InboundApiKey not configured.", statusCode: 503);
    var provided = ctx.Request.Headers["X-Internal-Api-Key"].ToString();
    if (!string.Equals(provided, expected, StringComparison.Ordinal))
        return Results.Unauthorized();

    var result = await svc.IngestAsync(body, ct);
    return Results.Ok(result);
}).AllowAnonymous().DisableAntiforgery();

// ── Quote portal API (Phase 6) ──
app.MapPost("/api/quotes/{quoteId:guid}/portal-token", async (
    Guid quoteId,
    CRMManagement.Application.Abstractions.IQuotePortalService portal,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var token = await portal.EnsureSignatureTokenAsync(quoteId, ct);
    if (token is null) return Results.NotFound();
    var url = $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}/Portal/Quotes/{token}";
    return Results.Ok(new { token, url });
}).RequireAuthorization();

app.MapPost("/api/portal/quotes/{token:guid}/accept", async (
    Guid token,
    CRMManagement.Application.DTOs.QuoteAcceptRequest body,
    CRMManagement.Application.Abstractions.IQuotePortalService portal,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var ip = ctx.Connection.RemoteIpAddress?.ToString();
    var result = await portal.AcceptAsync(token, body, ip, ct);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}).AllowAnonymous().DisableAntiforgery();

// Lightweight product list for picker UIs (used by drawing-quote preview).
app.MapGet("/api/products", async (
    CRMManagement.Application.Abstractions.IProductService svc,
    CancellationToken ct) =>
{
    return Results.Ok(await svc.ListAsync(ct));
}).RequireAuthorization();

// ── Drawing → Quote API (Phase 4) ──
app.MapPost("/api/drawings/analyze", async (
    CRMManagement.Application.DTOs.DrawingAnalyzeRequest body,
    CRMManagement.Application.Abstractions.IDrawingQuoteService svc,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(body.ImageBase64))
        return Results.BadRequest(new { error = "imageBase64 is required." });
    var dto = await svc.AnalyzeAsync(body, ct);
    return Results.Ok(dto);
}).RequireAuthorization()
  .DisableAntiforgery();

app.MapGet("/api/drawings/{id:guid}", async (
    Guid id,
    CRMManagement.Application.Abstractions.IDrawingQuoteService svc,
    CancellationToken ct) =>
{
    var dto = await svc.GetAsync(id, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
}).RequireAuthorization();

app.MapPost("/api/drawings/create-quote", async (
    CRMManagement.Application.DTOs.CreateQuoteFromAnalysisRequest body,
    CRMManagement.Application.Abstractions.IDrawingQuoteService svc,
    CancellationToken ct) =>
{
    try
    {
        var quoteId = await svc.CreateQuoteAsync(body, ct);
        return Results.Ok(new { quoteId });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/class-mappings", async (
    CRMManagement.Application.Abstractions.IClassProductMappingService svc,
    CancellationToken ct) =>
{
    return Results.Ok(await svc.ListAsync(ct));
}).RequireAuthorization();

app.MapPost("/api/class-mappings", async (
    CRMManagement.Application.DTOs.ClassProductMappingUpsertDto body,
    CRMManagement.Application.Abstractions.IClassProductMappingService svc,
    CancellationToken ct) =>
{
    try
    {
        var id = await svc.UpsertAsync(body, ct);
        return Results.Ok(new { id });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapDelete("/api/class-mappings/{id:guid}", async (
    Guid id,
    CRMManagement.Application.Abstractions.IClassProductMappingService svc,
    CancellationToken ct) =>
{
    await svc.DeleteAsync(id, ct);
    return Results.NoContent();
}).RequireAuthorization();

// ── Timeline API (Phase 2) ──
app.MapGet("/api/timeline", async (
    string entityKind,
    Guid entityId,
    int? limit,
    CRMManagement.Application.Abstractions.ITimelineService timeline,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<CRMManagement.Application.DTOs.AiEntityKind>(entityKind, ignoreCase: true, out var kind))
        return Results.BadRequest(new { error = $"Unknown entityKind '{entityKind}'." });
    var items = await timeline.GetForEntityAsync(kind, entityId, limit ?? 50, ct);
    return Results.Ok(items);
}).RequireAuthorization();

// ── Communications API (Phase 0/2) ──
app.MapGet("/api/communications", async (
    string entityKind,
    Guid entityId,
    int? limit,
    CRMManagement.Application.Abstractions.ICommunicationsService comms,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<CRMManagement.Application.DTOs.AiEntityKind>(entityKind, ignoreCase: true, out var kind))
        return Results.BadRequest(new { error = $"Unknown entityKind '{entityKind}'." });
    var list = await comms.ListForEntityAsync(kind, entityId, limit ?? 50, ct);
    return Results.Ok(list);
}).RequireAuthorization();

app.MapPost("/api/communications/ingest", async (
    CRMManagement.Application.DTOs.IngestCommunicationDto body,
    CRMManagement.Application.Abstractions.ICommunicationsService comms,
    HttpContext ctx,
    Microsoft.Extensions.Configuration.IConfiguration cfg,
    CancellationToken ct) =>
{
    // Accept either an authenticated user OR a shared internal API key from LDataBrain.
    var expected = cfg["LDataBrain:InboundApiKey"] ?? Environment.GetEnvironmentVariable("LDATABRAIN_INBOUND_API_KEY");
    var provided = ctx.Request.Headers["X-Internal-Api-Key"].ToString();
    var isAuthed = ctx.User?.Identity?.IsAuthenticated == true;
    var isApiKey = !string.IsNullOrWhiteSpace(expected) && string.Equals(provided, expected, StringComparison.Ordinal);
    if (!isAuthed && !isApiKey) return Results.Unauthorized();

    var id = await comms.IngestAsync(body, ct);
    return Results.Ok(new { id });
}).AllowAnonymous();

// ── AI Examples API ──
app.MapGet("/api/ai-examples/match", async (string? instruction, string? category, bool? includeThumbnails,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var wantThumbs = includeThumbnails ?? false;
    var query = db.AiExamples.Where(e => e.Rating >= 0);
    if (!string.IsNullOrWhiteSpace(category))
        query = query.Where(e => e.Category == category);

    // Tag-based matching: extract keywords from instruction
    if (!string.IsNullOrWhiteSpace(instruction))
    {
        var words = instruction.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2).ToArray();
        if (words.Length > 0)
            query = query.Where(e => e.Tags.Any(t => words.Contains(t.ToLower())));
    }

    var results = await query
        .OrderByDescending(e => e.Rating)
        .ThenByDescending(e => e.CreatedAt)
        .Take(5)
        .Select(e => new
        {
            e.Id, e.Category, e.Tags, e.Instruction, e.Provider,
            e.ResultText, e.ResultJson, e.Rating, e.CreatedAt,
            HasBeforeImage = e.BeforeImageBase64 != null,
            HasAfterImage = e.AfterImageBase64 != null,
            BeforeImageBase64 = wantThumbs ? e.BeforeImageBase64 : null,
            AfterImageBase64 = wantThumbs ? e.AfterImageBase64 : null
        })
        .ToListAsync();

    // Also check file-based examples
    var fileExamples = new List<object>();
    var examplesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "AiExamples");
    if (!Directory.Exists(examplesDir))
        examplesDir = Path.Combine(Directory.GetCurrentDirectory(), "AiExamples");
    if (Directory.Exists(examplesDir))
    {
        foreach (var dir in Directory.GetDirectories(examplesDir))
        {
            var metaPath = Path.Combine(dir, "meta.json");
            if (!File.Exists(metaPath)) continue;
            try
            {
                var meta = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                    await File.ReadAllTextAsync(metaPath));
                var tags = meta.TryGetProperty("tags", out var tagsEl)
                    ? tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").ToArray()
                    : Array.Empty<string>();
                var cat = meta.TryGetProperty("category", out var catEl) ? catEl.GetString() : Path.GetFileName(dir);

                bool matches = false;
                if (!string.IsNullOrWhiteSpace(category) && cat == category) matches = true;
                if (!string.IsNullOrWhiteSpace(instruction))
                {
                    var lower = instruction.ToLower();
                    if (tags.Any(t => lower.Contains(t.ToLower()))) matches = true;
                }
                if (!matches && string.IsNullOrWhiteSpace(instruction) && string.IsNullOrWhiteSpace(category))
                    matches = true;

                if (matches)
                {
                    fileExamples.Add(new
                    {
                        Source = "file",
                        Category = cat,
                        Tags = tags,
                        SystemPrompt = meta.TryGetProperty("systemPrompt", out var sp) ? sp.GetString() : null,
                        ExampleInstruction = meta.TryGetProperty("exampleInstruction", out var ei) ? ei.GetString() : null,
                        ExampleResult = meta.TryGetProperty("exampleResult", out var er) ? er.GetString() : null,
                        Description = meta.TryGetProperty("description", out var desc) ? desc.GetString() : null
                    });
                }
            }
            catch { /* skip malformed meta.json */ }
        }
    }

    return Results.Ok(new { dbExamples = results, fileExamples });
});

// ── List all AI examples with optional thumbnails ──
app.MapGet("/api/ai-examples", async (string? category, int? limit, bool? includeThumbnails,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var wantThumbs = includeThumbnails ?? false;
    var query = db.AiExamples.AsQueryable();
    if (!string.IsNullOrWhiteSpace(category))
        query = query.Where(e => e.Category == category);

    var results = await query
        .OrderByDescending(e => e.CreatedAt)
        .Take(limit ?? 50)
        .Select(e => new
        {
            e.Id, e.Category, e.Tags, e.Instruction, e.Provider,
            e.ResultText, e.ResultJson, e.Rating, e.CreatedAt,
            HasBeforeImage = e.BeforeImageBase64 != null,
            HasAfterImage = e.AfterImageBase64 != null,
            BeforeImageBase64 = wantThumbs ? e.BeforeImageBase64 : null,
            AfterImageBase64 = wantThumbs ? e.AfterImageBase64 : null
        })
        .ToListAsync();

    return Results.Ok(new { total = results.Count, examples = results });
});

app.MapPost("/api/ai-examples", async (CRMManagement.Infrastructure.Data.AppDbContext db,
    AiExampleSaveRequest body) =>
{
    var example = new CRMManagement.Domain.Entities.AiExample
    {
        Id = Guid.NewGuid(),
        Category = body.Category ?? "general",
        Tags = body.Tags ?? [],
        Instruction = body.Instruction ?? "",
        Provider = body.Provider,
        BeforeImageBase64 = body.BeforeImageBase64,
        AfterImageBase64 = body.AfterImageBase64,
        ResultJson = body.ResultJson,
        ResultText = body.ResultText,
        Rating = body.Rating
    };
    db.AiExamples.Add(example);
    await db.SaveChangesAsync();
    return Results.Created($"/api/ai-examples/{example.Id}", new { example.Id });
});

app.MapPost("/api/ai-examples/{id}/rate", async (Guid id, short rating,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var example = await db.AiExamples.FindAsync(id);
    if (example is null) return Results.NotFound();
    example.Rating = rating;
    await db.SaveChangesAsync();
    return Results.Ok(new { example.Id, example.Rating });
});

app.MapDelete("/api/ai-examples/{id}", async (Guid id,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var example = await db.AiExamples.FindAsync(id);
    if (example is null) return Results.NotFound();
    db.AiExamples.Remove(example);
    await db.SaveChangesAsync();
    return Results.Ok(new { deleted = true, id });
});

app.MapPost("/api/ai-examples/{id}/toggle-rating", async (Guid id,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var example = await db.AiExamples.FindAsync(id);
    if (example is null) return Results.NotFound();
    // Cycle: +1 → -1 → 0 → +1
    example.Rating = example.Rating > 0 ? (short)-1 : example.Rating < 0 ? (short)0 : (short)1;
    await db.SaveChangesAsync();
    return Results.Ok(new { example.Id, example.Rating });
});

app.MapGet("/api/ai-examples/{id}/image/{type}", async (Guid id, string type,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var example = await db.AiExamples.FindAsync(id);
    if (example is null) return Results.NotFound();
    var b64 = type == "after" ? example.AfterImageBase64 : example.BeforeImageBase64;
    if (string.IsNullOrWhiteSpace(b64)) return Results.NotFound();
    return Results.Text(b64, "text/plain");
});

// ── AI Interaction Log API ──
app.MapPost("/api/ai-log", async (CRMManagement.Infrastructure.Data.AppDbContext db,
    AiLogRequest body) =>
{
    var log = new CRMManagement.Domain.Entities.AiInteractionLog
    {
        Id = Guid.NewGuid(),
        SessionId = body.SessionId ?? Guid.NewGuid(),
        StepNumber = body.StepNumber,
        IsFinalStep = body.IsFinalStep,
        Instruction = body.Instruction ?? "",
        Provider = body.Provider ?? "",
        Mode = body.Mode ?? "",
        Success = body.Success,
        ErrorMessage = body.ErrorMessage,
        ResultText = body.ResultText,
        ResultJson = body.ResultJson,
        BeforeImageBase64 = body.BeforeImageBase64,
        AfterImageBase64 = body.AfterImageBase64,
        SourceFileName = body.SourceFileName,
        SourceDpi = body.SourceDpi,
        TotalMs = body.TotalMs,
        NetMs = body.NetMs,
        ExamplesUsed = body.ExamplesUsed
    };
    db.AiInteractionLogs.Add(log);
    await db.SaveChangesAsync();
    return Results.Ok(new { log.Id, log.SessionId });
});

app.MapPost("/api/ai-log/{id}/feedback", async (Guid id, short feedback,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var log = await db.AiInteractionLogs.FindAsync(id);
    if (log is null) return Results.NotFound();
    log.Feedback = feedback;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/api/ai-log", async (int? limit, string? provider,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var query = db.AiInteractionLogs.AsQueryable();
    if (!string.IsNullOrWhiteSpace(provider))
        query = query.Where(l => l.Provider == provider);
    var results = await query
        .OrderByDescending(l => l.CreatedAt)
        .Take(limit ?? 50)
        .Select(l => new
        {
            l.Id, l.SessionId, l.StepNumber, l.IsFinalStep,
            l.Instruction, l.Provider, l.Mode, l.Success,
            l.ErrorMessage, l.ResultText, l.TotalMs, l.NetMs,
            l.ExamplesUsed, l.Feedback, l.CreatedAt,
            l.SourceFileName, l.SourceDpi,
            HasBeforeImage = l.BeforeImageBase64 != null,
            HasAfterImage = l.AfterImageBase64 != null
        })
        .ToListAsync();
    return Results.Ok(results);
});

// Mark a session as complete (set IsFinalStep on last step, feedback=2 "perfect")
app.MapPost("/api/ai-log/session/{sessionId}/done", async (Guid sessionId,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var steps = await db.AiInteractionLogs
        .Where(l => l.SessionId == sessionId)
        .OrderByDescending(l => l.StepNumber)
        .ToListAsync();
    if (!steps.Any()) return Results.NotFound();

    // Mark the last step as final
    steps[0].IsFinalStep = true;
    steps[0].Feedback = 2; // perfect
    await db.SaveChangesAsync();
    return Results.Ok(new { sessionId, totalSteps = steps.Count, finalStepId = steps[0].Id });
});

// Get full session chain (all steps in order)
app.MapGet("/api/ai-log/session/{sessionId}", async (Guid sessionId,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var steps = await db.AiInteractionLogs
        .Where(l => l.SessionId == sessionId)
        .OrderBy(l => l.StepNumber)
        .Select(l => new
        {
            l.Id, l.StepNumber, l.IsFinalStep, l.Instruction,
            l.Provider, l.Mode, l.Success, l.ResultText, l.ResultJson,
            l.TotalMs, l.Feedback, l.CreatedAt,
            HasBeforeImage = l.BeforeImageBase64 != null,
            HasAfterImage = l.AfterImageBase64 != null
        })
        .ToListAsync();
    return Results.Ok(steps);
});

// ── Training data export for YOLO / Ollama ──
// Returns completed sessions with all steps + images for model training
app.MapGet("/api/ai-log/export/training", async (int? limit, string? minFeedback,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var minFb = short.TryParse(minFeedback, out var mf) ? mf : (short)1;

    // Find sessions that have a final step with good feedback
    var goodSessions = await db.AiInteractionLogs
        .Where(l => l.IsFinalStep && l.Feedback >= minFb)
        .OrderByDescending(l => l.CreatedAt)
        .Take(limit ?? 100)
        .Select(l => l.SessionId)
        .Distinct()
        .ToListAsync();

    var trainingData = new List<object>();
    foreach (var sid in goodSessions)
    {
        var steps = await db.AiInteractionLogs
            .Where(l => l.SessionId == sid)
            .OrderBy(l => l.StepNumber)
            .Select(l => new
            {
                l.StepNumber, l.Instruction, l.Provider, l.Mode,
                l.ResultText, l.ResultJson, l.Feedback, l.IsFinalStep,
                l.SourceFileName, l.SourceDpi,
                BeforeImage = l.BeforeImageBase64,
                AfterImage = l.AfterImageBase64
            })
            .ToListAsync();

        trainingData.Add(new
        {
            SessionId = sid,
            TotalSteps = steps.Count,
            Steps = steps
        });
    }
    return Results.Ok(new { count = trainingData.Count, sessions = trainingData });
});

// ── YOLO-format export for object detection training ──
// Returns images + annotations from counting sessions ready for YOLO training
app.MapGet("/api/ai-log/export/yolo", async (int? limit, string? minFeedback, bool? imagesInline,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var minFb = short.TryParse(minFeedback, out var mf) ? mf : (short)1;
    var includeImages = imagesInline ?? false;

    // Query object-count interactions with good feedback
    var countLogs = await db.AiInteractionLogs
        .Where(l => (l.Mode == "object-count" || l.Mode == "object-count-rescan") && l.Feedback >= minFb)
        .OrderByDescending(l => l.CreatedAt)
        .Take(limit ?? 200)
        .Select(l => new
        {
            l.Id, l.CreatedAt, l.SessionId, l.Mode, l.Instruction,
            l.ResultJson, l.Feedback, l.SourceFileName,
            BeforeImage = includeImages ? l.BeforeImageBase64 : null,
            AfterImage = includeImages ? l.AfterImageBase64 : null,
            HasBeforeImage = l.BeforeImageBase64 != null,
            HasAfterImage = l.AfterImageBase64 != null
        })
        .ToListAsync();

    // Also include examples from AiExamples table
    var countExamples = await db.AiExamples
        .Where(e => e.Category == "count-objects" && e.Rating >= minFb)
        .OrderByDescending(e => e.Id)
        .Take(limit ?? 200)
        .Select(e => new
        {
            e.Id, e.Category, e.Tags, e.Instruction, e.ResultJson, e.Rating,
            BeforeImage = includeImages ? e.BeforeImageBase64 : null,
            AfterImage = includeImages ? e.AfterImageBase64 : null,
            HasBeforeImage = e.BeforeImageBase64 != null,
            HasAfterImage = e.AfterImageBase64 != null
        })
        .ToListAsync();

    // Parse YOLO annotations from ResultJson
    var yoloDatasets = new List<object>();
    foreach (var log in countLogs)
    {
        if (string.IsNullOrEmpty(log.ResultJson)) continue;
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(log.ResultJson);
            var root = doc.RootElement;
            var annotations = new List<string>();

            if (root.TryGetProperty("annotations", out var annArr) && annArr.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var ann in annArr.EnumerateArray())
                {
                    var classId = ann.TryGetProperty("classId", out var cid) ? cid.GetInt32() : 0;
                    var cx = ann.TryGetProperty("cx", out var cxVal) ? cxVal.GetDouble() : 0;
                    var cy = ann.TryGetProperty("cy", out var cyVal) ? cyVal.GetDouble() : 0;
                    var w = ann.TryGetProperty("w", out var wVal) ? wVal.GetDouble() : 0;
                    var h = ann.TryGetProperty("h", out var hVal) ? hVal.GetDouble() : 0;
                    annotations.Add($"{classId} {cx:F6} {cy:F6} {w:F6} {h:F6}");
                }
            }
            else if (root.TryGetProperty("yoloTxt", out var yoloTxt))
            {
                annotations.AddRange(yoloTxt.GetString()?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? []);
            }

            if (annotations.Count == 0) continue;

            var imgW = root.TryGetProperty("imageWidth", out var iw) ? iw.GetInt32() : 0;
            var imgH = root.TryGetProperty("imageHeight", out var ih) ? ih.GetInt32() : 0;
            var tplW = root.TryGetProperty("templateWidth", out var tw) ? tw.GetInt32() : 0;
            var tplH = root.TryGetProperty("templateHeight", out var th) ? th.GetInt32() : 0;

            yoloDatasets.Add(new
            {
                Source = "interaction-log",
                LogId = log.Id,
                SessionId = (Guid?)log.SessionId,
                log.SourceFileName,
                log.Feedback,
                CreatedAt = (DateTime?)log.CreatedAt,
                ImageSize = new { Width = imgW, Height = imgH },
                TemplateSize = new { Width = tplW, Height = tplH },
                AnnotationCount = annotations.Count,
                YoloAnnotations = string.Join("\n", annotations),
                ImageEndpoint = $"/api/ai-log/export/yolo/image/{log.Id}",
                TemplateEndpoint = $"/api/ai-log/export/yolo/template/{log.Id}",
                BeforeImage = log.BeforeImage
            });
        }
        catch { /* skip malformed JSON */ }
    }

    // Also process examples
    foreach (var ex in countExamples)
    {
        if (string.IsNullOrEmpty(ex.ResultJson)) continue;
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(ex.ResultJson);
            var root = doc.RootElement;
            var annotations = new List<string>();

            if (root.TryGetProperty("annotations", out var annArr) && annArr.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var ann in annArr.EnumerateArray())
                {
                    var classId = ann.TryGetProperty("classId", out var cid) ? cid.GetInt32() : 0;
                    var cx = ann.TryGetProperty("cx", out var cxVal) ? cxVal.GetDouble() : 0;
                    var cy = ann.TryGetProperty("cy", out var cyVal) ? cyVal.GetDouble() : 0;
                    var w = ann.TryGetProperty("w", out var wVal) ? wVal.GetDouble() : 0;
                    var h = ann.TryGetProperty("h", out var hVal) ? hVal.GetDouble() : 0;
                    annotations.Add($"{classId} {cx:F6} {cy:F6} {w:F6} {h:F6}");
                }
            }
            else if (root.TryGetProperty("yoloTxt", out var yoloTxt))
            {
                annotations.AddRange(yoloTxt.GetString()?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? []);
            }

            if (annotations.Count == 0) continue;

            var imgW = root.TryGetProperty("imageWidth", out var iw) ? iw.GetInt32() : 0;
            var imgH = root.TryGetProperty("imageHeight", out var ih) ? ih.GetInt32() : 0;

            yoloDatasets.Add(new
            {
                Source = "ai-example",
                LogId = ex.Id,
                SessionId = (Guid?)null,
                SourceFileName = (string?)null,
                Feedback = (short)ex.Rating,
                CreatedAt = (DateTime?)null,
                ImageSize = new { Width = imgW, Height = imgH },
                TemplateSize = new { Width = 0, Height = 0 },
                AnnotationCount = annotations.Count,
                YoloAnnotations = string.Join("\n", annotations),
                ImageEndpoint = $"/api/ai-examples/{ex.Id}/image/before",
                TemplateEndpoint = $"/api/ai-examples/{ex.Id}/image/after",
                BeforeImage = ex.BeforeImage
            });
        }
        catch { }
    }

    return Results.Ok(new
    {
        format = "yolo-v8",
        description = "Object detection training data. Each entry has YOLO-format annotations (class cx cy w h, normalized 0-1) and endpoint URLs for images.",
        totalDatasets = yoloDatasets.Count,
        totalAnnotations = yoloDatasets.Sum(d => ((dynamic)d).AnnotationCount),
        datasets = yoloDatasets
    });
});

// Serve YOLO training images from interaction logs
app.MapGet("/api/ai-log/export/yolo/image/{id:guid}", async (Guid id,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var log = await db.AiInteractionLogs
        .Where(l => l.Id == id)
        .Select(l => new { l.BeforeImageBase64 })
        .FirstOrDefaultAsync();
    if (log?.BeforeImageBase64 == null) return Results.NotFound();
    var base64 = log.BeforeImageBase64;
    if (base64.StartsWith("data:")) base64 = base64[(base64.IndexOf(',') + 1)..];
    return Results.Bytes(Convert.FromBase64String(base64), "image/jpeg");
});

app.MapGet("/api/ai-log/export/yolo/template/{id:guid}", async (Guid id,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var log = await db.AiInteractionLogs
        .Where(l => l.Id == id)
        .Select(l => new { l.AfterImageBase64 })
        .FirstOrDefaultAsync();
    if (log?.AfterImageBase64 == null) return Results.NotFound();
    var base64 = log.AfterImageBase64;
    if (base64.StartsWith("data:")) base64 = base64[(base64.IndexOf(',') + 1)..];
    return Results.Bytes(Convert.FromBase64String(base64), "image/jpeg");
});

// ── Training data export for SVG cleaning / wall operations ──
// Returns before/after images + operation details for Ollama/YOLO segmentation training
app.MapGet("/api/ai-log/export/clean-training", async (int? limit, string? mode, bool? imagesInline,
    CRMManagement.Infrastructure.Data.AppDbContext db) =>
{
    var includeImages = imagesInline ?? false;
    var modeFilter = mode; // "svg-clean", "wall-clean", or null for all

    // Query from interaction logs
    var query = db.AiInteractionLogs
        .Where(l => l.Mode == "svg-clean" || l.Mode == "wall-clean");
    if (!string.IsNullOrEmpty(modeFilter))
        query = query.Where(l => l.Mode == modeFilter);

    var logs = await query
        .OrderByDescending(l => l.CreatedAt)
        .Take(limit ?? 200)
        .Select(l => new
        {
            l.Id, l.CreatedAt, l.SessionId, l.Mode, l.Instruction,
            l.ResultText, l.ResultJson, l.Feedback, l.SourceFileName,
            BeforeImage = includeImages ? l.BeforeImageBase64 : null,
            AfterImage = includeImages ? l.AfterImageBase64 : null,
            HasBeforeImage = l.BeforeImageBase64 != null,
            HasAfterImage = l.AfterImageBase64 != null
        })
        .ToListAsync();

    // Also query examples with clean-drawing category
    var examples = await db.AiExamples
        .Where(e => e.Category == "clean-drawing")
        .OrderByDescending(e => e.Id)
        .Take(limit ?? 200)
        .Select(e => new
        {
            e.Id, e.Category, e.Tags, e.Instruction, e.ResultText, e.ResultJson, e.Rating,
            BeforeImage = includeImages ? e.BeforeImageBase64 : null,
            AfterImage = includeImages ? e.AfterImageBase64 : null,
            HasBeforeImage = e.BeforeImageBase64 != null,
            HasAfterImage = e.AfterImageBase64 != null
        })
        .ToListAsync();

    var datasets = new List<object>();

    foreach (var log in logs)
    {
        object? parsedOps = null;
        try
        {
            if (!string.IsNullOrEmpty(log.ResultJson))
                parsedOps = System.Text.Json.JsonSerializer.Deserialize<object>(log.ResultJson);
        }
        catch { }

        datasets.Add(new
        {
            Source = "interaction-log",
            log.Id,
            log.SessionId,
            log.Mode,
            log.Instruction,
            log.ResultText,
            OperationDetails = parsedOps,
            log.Feedback,
            log.CreatedAt,
            log.SourceFileName,
            log.HasBeforeImage,
            log.HasAfterImage,
            BeforeImageEndpoint = $"/api/ai-log/export/yolo/image/{log.Id}",
            AfterImageEndpoint = $"/api/ai-log/export/yolo/template/{log.Id}",
            BeforeImage = log.BeforeImage,
            AfterImage = log.AfterImage
        });
    }

    foreach (var ex in examples)
    {
        object? parsedOps = null;
        try
        {
            if (!string.IsNullOrEmpty(ex.ResultJson))
                parsedOps = System.Text.Json.JsonSerializer.Deserialize<object>(ex.ResultJson);
        }
        catch { }

        datasets.Add(new
        {
            Source = "ai-example",
            Id = ex.Id,
            SessionId = (Guid?)null,
            Mode = "clean-drawing",
            ex.Instruction,
            ex.ResultText,
            OperationDetails = parsedOps,
            Feedback = (short)ex.Rating,
            CreatedAt = (DateTime?)null,
            SourceFileName = (string?)null,
            HasBeforeImage = ex.HasBeforeImage,
            HasAfterImage = ex.HasAfterImage,
            BeforeImageEndpoint = $"/api/ai-examples/{ex.Id}/image/before",
            AfterImageEndpoint = $"/api/ai-examples/{ex.Id}/image/after",
            BeforeImage = ex.BeforeImage,
            AfterImage = ex.AfterImage
        });
    }

    return Results.Ok(new
    {
        description = "SVG cleaning / wall operations training data. Each entry has before/after images showing the cleaning transformation, plus operation details JSON.",
        totalDatasets = datasets.Count,
        fromLogs = logs.Count,
        fromExamples = examples.Count,
        datasets
    });
});

// Run migrations + seed at startup.
// Default behavior:
// - Development: auto-migrate ON (dev convenience)
// - Non-Development: auto-migrate OFF unless explicitly enabled
var autoMigrate = builder.Configuration.GetValue<bool?>("Database:AutoMigrate")
                 ?? app.Environment.IsDevelopment();
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync(CancellationToken.None);
    }
    catch (PostgresException ex) when (string.Equals(ex.SqlState, "28P01", StringComparison.Ordinal))
    {
        // ConnectionStrings:Default may be empty when we build the connection string from env vars
        // inside Infrastructure. The resolved (masked) connection string is printed by Infrastructure
        // at startup as: "[CRMManagement] Using connection string: ...".
        logger.LogCritical(ex,
            "PostgreSQL authentication failed (28P01). Fix the password and verify the resolved connection string printed above. " +
            "Set DB_PASSWORD / APP_DB_PASSWORD or ConnectionStrings__Default.");
        throw;
    }
    catch (NpgsqlException ex) when (IsDbConnectionUnavailable(ex))
    {
        isDbAvailable = false;
        logger.LogCritical(ex,
            "Cannot connect to PostgreSQL (connection refused/unavailable). " +
            "Start Postgres on localhost:5432 (or update ConnectionStrings:Default / DB_HOST+DB_PORT), then restart. " +
            "Tip: run scripts/run-crmmanagement-dev.ps1 to load .env and verify DB connectivity before starting the app.");

        // In Development, don't crash the entire app just because Postgres isn't up.
        // The UI can still start, and pages will surface DB errors as they are accessed.
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database initialization failed. Check ConnectionStrings:Default and PostgreSQL availability.");
        throw;
    }
}

if (app.Environment.IsDevelopment() && !isDbAvailable)
{
    app.Run(async ctx =>
    {
        // Always allow health checks.
        if (ctx.Request.Path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await ctx.Response.WriteAsync("db_unavailable");
            return;
        }

        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        ctx.Response.ContentType = "text/html; charset=utf-8";

        var host = Environment.GetEnvironmentVariable("DB_HOST")
                         ?? Environment.GetEnvironmentVariable("APP_DB_HOST")
                         ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT")
                         ?? Environment.GetEnvironmentVariable("POSTGRES_PORT")
                         ?? "5432";

        await ctx.Response.WriteAsync($$"""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width,initial-scale=1" />
    <title>Database unavailable</title>
    <style>
        body { font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif; margin: 0; background: #0b1220; color: #e5e7eb; }
        .wrap { max-width: 880px; margin: 48px auto; padding: 0 16px; }
        .card { background: rgba(15,23,42,.85); border: 1px solid rgba(148,163,184,.35); border-radius: 16px; padding: 20px; }
        h1 { margin: 0 0 8px; font-size: 22px; }
        p { margin: 8px 0; color: #cbd5e1; line-height: 1.5; }
        code, pre { background: rgba(2,6,23,.6); border: 1px solid rgba(148,163,184,.25); border-radius: 10px; padding: 2px 6px; color: #e2e8f0; }
        pre { padding: 12px; overflow: auto; }
        .muted { color: #94a3b8; font-size: 13px; }
    </style>
</head>
<body>
    <div class="wrap">
        <div class="card">
            <h1>PostgreSQL is not running / not reachable</h1>
            <p>This app needs Postgres to sign in and load pages. Right now it cannot connect to <code>{{host}}:{{port}}</code>.</p>
            <p>Start your local Postgres (or Docker container) and refresh this page.</p>
            <p class="muted">Tip: use the dev helper script (it loads your .env and checks TCP connectivity):</p>
            <pre>powershell -ExecutionPolicy Bypass -File scripts/run-crmmanagement-dev.ps1</pre>
        </div>
    </div>
</body>
</html>
""");
    });
}

static bool IsDbConnectionUnavailable(NpgsqlException ex)
{
    // Common local-dev case: no Postgres listening on localhost:5432.
    // Npgsql wraps the underlying SocketException.
    if (ex.InnerException is System.Net.Sockets.SocketException)
        return true;

    // Best-effort fallback based on message content.
    var msg = ex.Message ?? string.Empty;
    return msg.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase)
           || msg.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
           || msg.Contains("actively refused", StringComparison.OrdinalIgnoreCase);
}

static bool IsTruthy(string? value)
    => string.Equals((value ?? string.Empty).Trim(), "1", StringComparison.OrdinalIgnoreCase)
       || string.Equals((value ?? string.Empty).Trim(), "true", StringComparison.OrdinalIgnoreCase)
       || string.Equals((value ?? string.Empty).Trim(), "yes", StringComparison.OrdinalIgnoreCase);

// ── Embed token V2: JSON-payload format ("{base64url-json}.{signature}") ──
// Payload carries user identity from the main site.

static bool TryValidateEmbedTokenV2(string token, string secret, out string? userName, out string? sourceUserId)
{
    userName = null;
    sourceUserId = null;
    if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secret))
        return false;

    var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 2) return false;

    // If the first part parses as a plain number, this is a legacy token – bail out.
    if (long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out _))
        return false;

    // Validate HMAC-SHA256 over the payload portion.
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var expectedSig = WebEncoders.Base64UrlEncode(
        hmac.ComputeHash(Encoding.UTF8.GetBytes($"crm-embed:{parts[0]}")));

    var expectedBytes = Encoding.UTF8.GetBytes(expectedSig);
    var providedBytes = Encoding.UTF8.GetBytes(parts[1]);
    if (expectedBytes.Length != providedBytes.Length
        || !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        return false;

    // Decode & parse JSON.
    byte[] jsonBytes;
    try { jsonBytes = WebEncoders.Base64UrlDecode(parts[0]); }
    catch { return false; }

    JsonElement root;
    try { root = JsonDocument.Parse(jsonBytes).RootElement; }
    catch { return false; }

    // Verify expiry.
    if (!root.TryGetProperty("exp", out var expEl) || expEl.ValueKind != JsonValueKind.Number)
        return false;

    var expiresAtUnix = expEl.GetInt64();
    if (DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix) <= DateTimeOffset.UtcNow)
        return false;

    userName     = root.TryGetProperty("un",  out var unEl)  ? unEl.GetString()  : null;
    sourceUserId = root.TryGetProperty("sid", out var sidEl) ? sidEl.GetString() : null;
    return true;
}

// ── Legacy token format: "{expiresAt}.{signature}" ──
static bool TryValidateEmbedTokenLegacy(string token, string secret)
{
    if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secret))
        return false;

    var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 2)
        return false;

    if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAtUnix))
        return false;

    DateTimeOffset expiresAt;
    try { expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix); }
    catch { return false; }

    if (expiresAt <= DateTimeOffset.UtcNow)
        return false;

    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var expectedSignature = WebEncoders.Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes($"crm-embed:{parts[0]}")));

    var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
    var providedBytes = Encoding.UTF8.GetBytes(parts[1]);

    return expectedBytes.Length == providedBytes.Length
           && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
}

app.Run();

// Expose Program for integration testing (WebApplicationFactory).
public partial class Program { }

public record AiLogRequest(
    string? Instruction,
    string? Provider,
    string? Mode,
    bool Success = true,
    string? ErrorMessage = null,
    string? ResultText = null,
    string? ResultJson = null,
    int? TotalMs = null,
    int? NetMs = null,
    int ExamplesUsed = 0,
    Guid? SessionId = null,
    int StepNumber = 1,
    bool IsFinalStep = false,
    string? BeforeImageBase64 = null,
    string? AfterImageBase64 = null,
    string? SourceFileName = null,
    int? SourceDpi = null);

public record AiAssistantApiRequest(
    string EntityKind,
    Guid EntityId,
    string Action,
    string? UserHint = null,
    string? Channel = null);

public record AiExampleSaveRequest(
    string? Category,
    string[]? Tags,
    string? Instruction,
    string? Provider,
    string? BeforeImageBase64,
    string? AfterImageBase64,
    string? ResultJson,
    string? ResultText,
    short Rating = 0);
