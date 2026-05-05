using Microsoft.AspNetCore.Http;
using CRMManagement.Application.Abstractions;

namespace CRMManagement.Infrastructure.Services;

public sealed class CookieCompanyContext : ICompanyContext
{
    public CookieCompanyContext(IHttpContextAccessor httpContextAccessor)
    {
        var raw = httpContextAccessor.HttpContext?.Request.Cookies["crm_company"];
        SelectedCompanyId = int.TryParse(raw, out var id) && id > 0 ? id : null;
    }

    public int? SelectedCompanyId { get; }
}
