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

        services.AddHttpContextAccessor();
        services.AddScoped<ICompanyContext, CookieCompanyContext>();

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
