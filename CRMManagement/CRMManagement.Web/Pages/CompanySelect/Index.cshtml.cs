using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;

namespace CRMManagement.Web.Pages.CompanySelect;

[Authorize]
[IgnoreAntiforgeryToken]
public sealed class IndexModel : PageModel
{
    private readonly IConfiguration _cfg;

    public IndexModel(IConfiguration cfg) => _cfg = cfg;

    public sealed record CompanyRow(int Id, string Name, string? Email, string? Phone, int ProjectCount);

    public List<CompanyRow> Companies { get; private set; } = new();
    public int ActiveCompanyId { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var raw = Request.Cookies["crm_company"];
        if (int.TryParse(raw, out var companyId) && companyId > 0) ActiveCompanyId = companyId;

        var connStr = ResolveConnectionString();
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(@"
SELECT c.id,
       c.name,
       c.email,
       c.phone,
       COUNT(DISTINCT p.id) AS project_count
FROM public.licensing_companies c
LEFT JOIN public.licensing_customers cu ON cu.company_id = c.id
LEFT JOIN public.licensing_projects p ON p.customer_id = cu.id
WHERE c.is_archived = FALSE
GROUP BY c.id, c.name, c.email, c.phone
ORDER BY c.name;", conn);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            Companies.Add(new CompanyRow(
                r.GetInt32(0),
                r.IsDBNull(1) ? "(unnamed)" : r.GetString(1),
                r.IsDBNull(2) ? null : r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? 0 : r.GetInt32(4)));
        }
    }

    public IActionResult OnPostSelect([FromForm] int companyId)
    {
        if (companyId <= 0)
            return RedirectToPage();

        Response.Cookies.Append("crm_company", companyId.ToString(), new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.None,
            Secure = true,
            MaxAge = TimeSpan.FromDays(90),
            Path = "/"
        });

        var pb = Request?.PathBase.HasValue == true ? Request.PathBase.Value : string.Empty;
        return Redirect(pb + "/Home");
    }

    private string ResolveConnectionString()
    {
        var fromConfig = _cfg.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(fromConfig) && !fromConfig.Contains("__SET_ME__"))
            return fromConfig;

        var host = Environment.GetEnvironmentVariable("DB_HOST")
            ?? Environment.GetEnvironmentVariable("POSTGRES_IP")
            ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT")
            ?? Environment.GetEnvironmentVariable("POSTGRES_PORT")
            ?? "5432";
        var db = Environment.GetEnvironmentVariable("DB_NAME")
            ?? Environment.GetEnvironmentVariable("APP_DB_NAME")
            ?? "ldatabrain";
        var user = Environment.GetEnvironmentVariable("DB_USER")
            ?? Environment.GetEnvironmentVariable("APP_DB_USER")
            ?? "ldataapp";
        var pass = Environment.GetEnvironmentVariable("DB_PASSWORD")
            ?? Environment.GetEnvironmentVariable("APP_DB_PASSWORD")
            ?? "";

        return $"Host={host};Port={port};Database={db};Username={user};Password={pass}";
    }
}
