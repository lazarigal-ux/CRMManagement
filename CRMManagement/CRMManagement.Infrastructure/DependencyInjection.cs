using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Security.Claims;
using System.IO;
using CRMManagement.Application.Abstractions;
using CRMManagement.Infrastructure.Data;
using CRMManagement.Infrastructure.Identity;
using CRMManagement.Infrastructure.Repositories;
using CRMManagement.Infrastructure.Services;

namespace CRMManagement.Infrastructure;

public static class DependencyInjection
{
    private static bool _connectionStringLogged;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);
        LogConnectionStringOnce(connectionString);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("crm___EFMigrationsHistory", "crm");
            });
        });

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Auth/Login";
            options.AccessDeniedPath = "/Auth/Denied";
            options.Cookie.Name = "CRMManagement.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.None;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(12);

            options.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = async context =>
                {
                    try
                    {
                        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!Guid.TryParse(userIdValue, out var userId))
                        {
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var user = await userManager.FindByIdAsync(userId.ToString());
                        if (user is { IsActive: false })
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                        }
                    }
                    catch
                    {
                        // no-op
                    }
                }
            };
        });

        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        services.AddScoped<DbInitializer>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ITokenStore, EfTokenStore>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IOpportunityService, OpportunityService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPipelineService, PipelineService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ISavedViewService, SavedViewService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ISolutionService, SolutionService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomFieldQueryService, CustomFieldQueryService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICompanyContext, CookieCompanyContext>();

        // Phase 0: AI client + RAG + LDataBrain bridge.
        services.AddScoped<ICrmAiClient, CrmAiClient>();
        services.AddScoped<ICrmRagService, CrmRagService>();
        services.AddScoped<ICommunicationsService, CommunicationsService>();
        services.AddScoped<ILDataBrainBridge, LDataBrainBridge>();

        // Phase 1: AI Assistant orchestrator.
        services.AddScoped<IAiAssistantService, AiAssistantService>();

        // Phase 2: timeline + background poller for LDataBrain comms.
        services.AddScoped<ITimelineService, TimelineService>();
        services.AddHostedService<CommsIngestionService>();

        // Phase 4: drawing → quote.
        services.AddScoped<IDrawingQuoteService, DrawingQuoteService>();
        services.AddScoped<IClassProductMappingService, ClassProductMappingService>();

        // Phase 6: public quote acceptance portal.
        services.AddScoped<IQuotePortalService, QuotePortalService>();

        // Phase 8: inbound WhatsApp → Lead with AI scoring.
        services.AddScoped<IWhatsAppLeadService, WhatsAppLeadService>();

        // Phase 7 (partial): durable integration outbox + drainer.
        services.AddScoped<IIntegrationOutboxService, IntegrationOutboxService>();
        services.AddHostedService<IntegrationOutboxDrainer>();

        var aiBaseUrl = configuration["AiGateway:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(aiBaseUrl))
        {
            services.AddHttpClient(CrmAiClient.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(aiBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(120);

                var apiKey = configuration["AiGateway:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", apiKey);
                }
            });
        }
        else
        {
            // Register an empty client so resolution doesn't fail; CrmAiClient short-circuits on null BaseAddress.
            services.AddHttpClient(CrmAiClient.HttpClientName);
        }

        var lDataBrainBaseUrl = configuration["LDataBrain:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(lDataBrainBaseUrl))
        {
            services.AddHttpClient(LDataBrainBridge.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(lDataBrainBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(20);

                var apiKey = configuration["LDataBrain:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Internal-Api-Key", apiKey);
                }
            });
        }
        else
        {
            services.AddHttpClient(LDataBrainBridge.HttpClientName);
        }

        // Zoho CRM read-only integration. Region is read per-request from the persisted ZohoConnection
        // row, so the HttpClients here have no BaseAddress and the services build absolute URLs each call.
        services.AddScoped<IZohoConnectionService, ZohoConnectionService>();
        services.AddScoped<IZohoImportService, ZohoImportService>();
        services.AddSingleton<IZohoImportStatusService, ZohoImportStatusService>();
        services.AddSingleton<IZohoTokenProvider, ZohoTokenProvider>();
        services.AddScoped<IZohoCrmReader, ZohoCrmReader>();

        services.AddHttpClient(ZohoTokenProvider.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient(ZohoCrmReader.HttpClientName,    c => c.Timeout = TimeSpan.FromSeconds(30));

        // Periodic full sync from Zoho into the local DB so every CRM tab + the dashboard
        // mirrors the connected Zoho org. Skips itself when no connection is configured.
        services.AddHostedService<ZohoSyncBackgroundService>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var fromConfig = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(fromConfig) && !fromConfig.Contains("__SET_ME__", StringComparison.Ordinal))
        {
            return fromConfig;
        }

        var host =
            Environment.GetEnvironmentVariable("DB_HOST")
            ?? Environment.GetEnvironmentVariable("APP_DB_HOST")
            ?? Environment.GetEnvironmentVariable("CORE_POSTGRES_HOST")
            ?? Environment.GetEnvironmentVariable("POSTGRES_HOST")
            ?? Environment.GetEnvironmentVariable("POSTGRES_SERVER")
            ?? Environment.GetEnvironmentVariable("POSTGRES_IP")
            ?? Environment.GetEnvironmentVariable("HOST_IP")
            ?? "localhost";

        if (!IsRunningInContainer() && string.Equals(host, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            host = "localhost";
        }

        var portS =
            Environment.GetEnvironmentVariable("DB_PORT")
            ?? Environment.GetEnvironmentVariable("POSTGRES_PORT")
            ?? "5432";

        var db =
            Environment.GetEnvironmentVariable("DB_NAME")
            ?? Environment.GetEnvironmentVariable("APP_DB_NAME")
            ?? Environment.GetEnvironmentVariable("CORE_POSTGRES_DB")
            ?? Environment.GetEnvironmentVariable("POSTGRES_DB")
            ?? "ldatabrain";

        var user =
            Environment.GetEnvironmentVariable("DB_USER")
            ?? Environment.GetEnvironmentVariable("APP_DB_USER")
            ?? Environment.GetEnvironmentVariable("CORE_POSTGRES_USER")
            ?? Environment.GetEnvironmentVariable("POSTGRES_USER")
            ?? "postgres";

        var pass =
            Environment.GetEnvironmentVariable("DB_PASSWORD")
            ?? Environment.GetEnvironmentVariable("APP_DB_PASSWORD");

        if (string.IsNullOrWhiteSpace(pass))
        {
            var coreUser =
                Environment.GetEnvironmentVariable("CORE_POSTGRES_USER")
                ?? Environment.GetEnvironmentVariable("POSTGRES_USER")
                ?? "postgres";

            if (string.Equals(user, coreUser, StringComparison.OrdinalIgnoreCase))
            {
                pass =
                    Environment.GetEnvironmentVariable("CORE_POSTGRES_PASSWORD")
                    ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            }
        }

        if (string.IsNullOrWhiteSpace(pass))
        {
            throw new InvalidOperationException(
                "Database password is missing. Set ConnectionStrings:Default or set DB_PASSWORD / APP_DB_PASSWORD environment variable.");
        }

        var baseCs = $"Host={host};Port={portS};Database={db};Username={user};Password={pass}";

        var searchPath = Environment.GetEnvironmentVariable("APP_DB_SEARCH_PATH");
        if (string.IsNullOrWhiteSpace(searchPath)) searchPath = "crm,public";

        return $"{baseCs};Search Path={searchPath}";
    }

    private static bool IsRunningInContainer()
    {
        if (OperatingSystem.IsWindows()) return false;

        try
        {
            if (File.Exists("/.dockerenv")) return true;
        }
        catch
        {
            // ignore
        }

        var env = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        return string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void LogConnectionStringOnce(string connectionString)
    {
        if (_connectionStringLogged) return;
        _connectionStringLogged = true;

        try
        {
            var csb = new NpgsqlConnectionStringBuilder(connectionString) { Password = "*****" };
            Console.WriteLine($"[CRMManagement] Using connection string: {csb.ConnectionString}");
        }
        catch
        {
            var masked = connectionString.Replace("Password=", "Password=*****", StringComparison.OrdinalIgnoreCase);
            Console.WriteLine($"[CRMManagement] Using connection string: {masked}");
        }
    }
}
