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

        await EnsureCustomersTableAsync(ct);

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

    // Owns the crm_customers table. On first run this physically relocates the
    // legacy service.svc_customers table (with its 198 rows, indexes and inbound
    // FKs) into the crm schema — a one‑time, data‑preserving move. It is fully
    // idempotent: once crm.crm_customers exists the move is skipped, and a fresh
    // environment with no source table simply gets an empty table created.
    private async Task EnsureCustomersTableAsync(CancellationToken ct)
    {
        try
        {
            // One‑time relocation: service.svc_customers → crm.crm_customers.
            await _db.Database.ExecuteSqlRawAsync(
                """
                DO $$
                BEGIN
                    IF to_regclass('crm.crm_customers') IS NULL
                       AND to_regclass('service.svc_customers') IS NOT NULL THEN
                        ALTER TABLE service.svc_customers SET SCHEMA crm;
                        ALTER TABLE crm.svc_customers RENAME TO crm_customers;
                    END IF;
                END $$;
                """, ct);

            // Fresh environment (no source table to move): create it directly.
            await _db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS crm.crm_customers (
                    "Id" uuid NOT NULL PRIMARY KEY,
                    "MainProjectId" uuid NULL,
                    "ExternalNumber" character varying(20) NOT NULL,
                    "Name" character varying(300) NOT NULL,
                    "CustomerType" character varying(120) NULL,
                    "City" character varying(120) NULL,
                    "Address" character varying(300) NULL,
                    "Status" character varying(40) NULL,
                    "OpenedOn" date NULL,
                    "StatusUpdatedOn" date NULL,
                    "Phone" character varying(60) NULL,
                    "Fax" character varying(60) NULL,
                    "Email" character varying(200) NULL,
                    "CompanyNumber" character varying(40) NULL,
                    "VatFileNumber" character varying(40) NULL,
                    "PaymentTermsCode" character varying(20) NULL,
                    "PaymentTermsName" character varying(60) NULL,
                    "CreatedAt" timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc')
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_crm_customers_ExternalNumber" ON crm.crm_customers ("ExternalNumber");
                CREATE INDEX IF NOT EXISTS "IX_crm_customers_Name" ON crm.crm_customers ("Name");
                CREATE INDEX IF NOT EXISTS "IX_crm_customers_Status" ON crm.crm_customers ("Status");
                CREATE INDEX IF NOT EXISTS "IX_crm_customers_MainProjectId" ON crm.crm_customers ("MainProjectId");
                """, ct);
        }
        catch
        {
            // ignore — table will be present after the prod schema move
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
        var existing = await _userManager.FindByNameAsync("admin");
        if (existing == null)
        {
            var seedPassword = FirstNonWhiteEnv(
                "CRMMANAGEMENT_SEED_ADMIN_PASSWORD",
                "CRM_SEED_ADMIN_PASSWORD",
                "SEED_ADMIN_PASSWORD");
            if (string.IsNullOrWhiteSpace(seedPassword)) return;

            var admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@ldatabrain.local",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(admin, seedPassword);
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

    private static string? FirstNonWhiteEnv(params string[] names)
    {
        foreach (var name in names)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }

        return null;
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
