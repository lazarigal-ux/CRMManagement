using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Identity;

namespace CRMManagement.Infrastructure.Data;

public sealed class DbInitializer
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public DbInitializer(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
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
        await SeedPipelineAsync(ct);
        await SeedPriceBookAsync(ct);
        await SeedTagsAsync(ct);
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
