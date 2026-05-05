using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CRMManagement.Application.Abstractions;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Identity;

namespace CRMManagement.Infrastructure.Data;

public sealed class DbInitializer
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _cfg;
    private readonly IZohoConnectionService _zohoConnections;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration cfg,
        IZohoConnectionService zohoConnections,
        ILogger<DbInitializer> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _cfg = cfg;
        _zohoConnections = zohoConnections;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await EnsureSchemaExistsAsync(ct);

        try
        {
            await _db.Database.MigrateAsync(ct);
        }
        catch
        {
            await _db.Database.EnsureCreatedAsync(ct);
        }

        await SeedRolesAsync(ct);
        await SeedAdminUserAsync(ct);
        await SeedSalesmanUserAsync(ct);
        await SeedMoranUserAsync(ct);
        await SeedPipelineAsync(ct);
        await SeedPriceBookAsync(ct);
        await SeedTagsAsync(ct);
        await SeedZohoConnectionFromConfigAsync(ct);
    }

    private async Task EnsureSchemaExistsAsync(CancellationToken ct)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS crm;", ct);
        }
        catch
        {
            // ignore
        }
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        string[] roles = { "Admin", "SalesManager", "SalesRep", "Support", "User" };
        foreach (var name in roles)
        {
            if (!await _roleManager.RoleExistsAsync(name))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = name });
            }
        }
    }

    private async Task SeedAdminUserAsync(CancellationToken ct)
    {
        var anyUsers = await _db.Users.AnyAsync(ct);
        var existing = await _userManager.FindByNameAsync("admin");
        if (existing == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@ldatabrain.local",
                EmailConfirmed = true,
                IsActive = true
            };

            IdentityResult result;
            if (!anyUsers)
            {
                result = await _userManager.CreateAsync(admin, "Admin#12345");
            }
            else
            {
                result = await _userManager.CreateAsync(admin);
            }

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, "Admin");
            }
        }
        else
        {
            if (!await _userManager.IsInRoleAsync(existing, "Admin"))
            {
                await _userManager.AddToRoleAsync(existing, "Admin");
            }
        }
    }

    private async Task SeedSalesmanUserAsync(CancellationToken ct)
    {
        const string SalesmanUserName = "aviyam@lcontrol.com";
        const string SalesmanPassword = "TLFTLF1212%%";

        var user = await _userManager.FindByNameAsync(SalesmanUserName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = SalesmanUserName,
                Email = SalesmanUserName,
                EmailConfirmed = true,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };

            // The Identity password policy (mixed-case + digits) doesn't admit "TLFTLF1212%%".
            // We mirror the user's Zoho-side password verbatim per the operator's request,
            // so we hash directly to bypass the policy check on this single seeded account.
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, SalesmanPassword);

            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded) return;
        }

        if (!await _userManager.IsInRoleAsync(user, "SalesManager"))
        {
            await _userManager.AddToRoleAsync(user, "SalesManager");
        }
    }

    private async Task SeedPipelineAsync(CancellationToken ct)
    {
        if (await _db.Pipelines.AnyAsync(ct)) return;

        var pipeline = new Pipeline
        {
            Id = Guid.NewGuid(),
            Name = "Sales Pipeline",
            Description = "Default sales pipeline",
            IsDefault = true,
            SortOrder = 0
        };
        _db.Pipelines.Add(pipeline);

        var stages = new (string Name, int Order, int Prob, bool Won, bool Lost)[]
        {
            ("Prospecting", 0, 10, false, false),
            ("Qualification", 1, 25, false, false),
            ("Needs Analysis", 2, 40, false, false),
            ("Proposal", 3, 60, false, false),
            ("Negotiation", 4, 80, false, false),
            ("Closed Won", 5, 100, true, false),
            ("Closed Lost", 6, 0, false, true),
        };

        foreach (var s in stages)
        {
            _db.PipelineStages.Add(new PipelineStage
            {
                Id = Guid.NewGuid(),
                PipelineId = pipeline.Id,
                Name = s.Name,
                SortOrder = s.Order,
                Probability = s.Prob,
                IsWon = s.Won,
                IsLost = s.Lost
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedPriceBookAsync(CancellationToken ct)
    {
        if (await _db.PriceBooks.AnyAsync(ct)) return;

        _db.PriceBooks.Add(new PriceBook
        {
            Id = Guid.NewGuid(),
            Name = "Standard",
            Description = "Default standard price book",
            IsActive = true,
            Currency = "USD"
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedMoranUserAsync(CancellationToken ct)
    {
        const string MoranUserName = "moranlazar1995@gmail.com";
        const string MoranPassword = "Dipsy2017!@#";

        var user = await _userManager.FindByNameAsync(MoranUserName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = MoranUserName,
                Email = MoranUserName,
                EmailConfirmed = true,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
            // Direct hash — same pattern as the aviyam seed; keeps the seed idempotent
            // even if the Identity password policy ever changes.
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, MoranPassword);

            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded) return;
        }

        if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.AddToRoleAsync(user, "Admin");
        }
    }

    private async Task SeedZohoConnectionFromConfigAsync(CancellationToken ct)
    {
        var clientId = _cfg["Zoho:ClientId"];
        var clientSecret = _cfg["Zoho:ClientSecret"];
        var region = string.IsNullOrWhiteSpace(_cfg["Zoho:Region"]) ? "com" : _cfg["Zoho:Region"]!;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return;
        }

        var existing = await _db.ZohoConnections.AsNoTracking().FirstOrDefaultAsync(ct);

        // Don't trash a working connection: if the same client_id is already wired up
        // with a refresh token, leave it alone. Operators can disconnect via the UI.
        if (existing != null
            && string.Equals(existing.ClientId, clientId, StringComparison.Ordinal)
            && !string.IsNullOrEmpty(existing.RefreshTokenProtected))
        {
            return;
        }

        try
        {
            await _zohoConnections.SaveAppCredentialsAsync(region, clientId, clientSecret, ct);
            _logger.LogInformation("Zoho app credentials seeded from configuration for region '{Region}'.", region);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed Zoho app credentials from configuration.");
        }
    }

    private async Task SeedTagsAsync(CancellationToken ct)
    {
        if (await _db.Tags.AnyAsync(ct)) return;

        var defaults = new (string Name, string Color, string Scope)[]
        {
            ("VIP", "#f59e0b", "All"),
            ("Hot", "#ef4444", "Lead"),
            ("Strategic", "#3b82f6", "Account"),
            ("Upsell", "#10b981", "Opportunity"),
        };

        foreach (var t in defaults)
        {
            _db.Tags.Add(new Tag { Id = Guid.NewGuid(), Name = t.Name, Color = t.Color, Scope = t.Scope });
        }

        await _db.SaveChangesAsync(ct);
    }
}
