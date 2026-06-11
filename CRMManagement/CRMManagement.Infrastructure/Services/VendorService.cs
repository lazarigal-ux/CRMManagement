using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class VendorService : IVendorService
{
    private readonly AppDbContext _db;
    public VendorService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<VendorListItemDto>> ListAsync(CancellationToken ct) =>
        await _db.Vendors.AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VendorListItemDto(v.Id, v.Name, v.Category, v.Email, v.Phone, v.Website, v.IsActive)
            {
                Description      = v.Description,
                City             = v.City,
                State            = v.State,
                Country          = v.Country,
                GlAccount        = v.GlAccount,
                ZohoCreatedTime  = v.ZohoCreatedTime,
                ZohoModifiedTime = v.ZohoModifiedTime,
                OwnerName        = v.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == v.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);

    public async Task<VendorDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var v = await _db.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return v is null ? null
            : new VendorDetailDto(v.Id, v.Name, v.Category, v.Email, v.Phone, v.Website, v.Description,
                v.Street, v.City, v.State, v.ZipCode, v.Country, v.GlAccount, v.OwnerUserId, v.IsActive);
    }

    public async Task<Guid> UpsertAsync(VendorUpsertDto dto, CancellationToken ct)
    {
        Vendor e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Vendor not found");
        }
        else
        {
            e = new Vendor { Id = Guid.NewGuid() };
            _db.Vendors.Add(e);
        }
        e.Name = dto.Name;
        e.Category = dto.Category;
        e.Email = dto.Email;
        e.Phone = dto.Phone;
        e.Website = dto.Website;
        e.Description = dto.Description;
        e.Street = dto.Street;
        e.City = dto.City;
        e.State = dto.State;
        e.ZipCode = dto.ZipCode;
        e.Country = dto.Country;
        e.GlAccount = dto.GlAccount;
        e.OwnerUserId = dto.OwnerUserId;
        e.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return;
        _db.Vendors.Remove(v);
        await _db.SaveChangesAsync(ct);
    }
}
