using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class AccountService : IAccountService
{
    private readonly AppDbContext _db;
    public AccountService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<AccountListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Accounts.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AccountListItemDto(x.Id, x.Name, x.Industry, x.Website, x.Phone, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<AccountDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (x == null) return null;
        return new AccountDetailDto(x.Id, x.Name, x.LegalName, x.Industry, x.Website, x.Phone, x.Email, x.BillingAddress, x.ShippingAddress, x.AnnualRevenue, x.EmployeeCount, x.OwnerUserId, x.ParentAccountId, x.Description, x.IsActive);
    }

    public async Task<Guid> UpsertAsync(AccountUpsertDto dto, CancellationToken ct)
    {
        Account e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct)
                ?? throw new InvalidOperationException("Account not found");
        }
        else
        {
            e = new Account { Id = Guid.NewGuid() };
            _db.Accounts.Add(e);
        }
        e.Name = dto.Name;
        e.LegalName = dto.LegalName;
        e.Industry = dto.Industry;
        e.Website = dto.Website;
        e.Phone = dto.Phone;
        e.Email = dto.Email;
        e.BillingAddress = dto.BillingAddress;
        e.ShippingAddress = dto.ShippingAddress;
        e.AnnualRevenue = dto.AnnualRevenue;
        e.EmployeeCount = dto.EmployeeCount;
        e.OwnerUserId = dto.OwnerUserId;
        e.ParentAccountId = dto.ParentAccountId;
        e.Description = dto.Description;
        e.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (e != null)
        {
            _db.Accounts.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }
}
