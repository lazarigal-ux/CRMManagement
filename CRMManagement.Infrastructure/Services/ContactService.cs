using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class ContactService : IContactService
{
    private readonly AppDbContext _db;
    public ContactService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<ContactListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Contacts.AsNoTracking()
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Select(c => new ContactListItemDto(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.AccountId))
            .ToListAsync(ct);
    }

    public async Task<ContactDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (x == null) return null;
        return new ContactDetailDto(x.Id, x.FirstName, x.LastName, x.Title, x.Email, x.Phone, x.Mobile, x.AccountId, x.OwnerUserId, x.Department, x.Address, x.Description, x.IsPrimary, x.DoNotContact);
    }

    public async Task<Guid> UpsertAsync(ContactUpsertDto dto, CancellationToken ct)
    {
        Contact e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new InvalidOperationException("Contact not found");
        }
        else
        {
            e = new Contact { Id = Guid.NewGuid() };
            _db.Contacts.Add(e);
        }
        e.FirstName = dto.FirstName;
        e.LastName = dto.LastName;
        e.Title = dto.Title;
        e.Email = dto.Email;
        e.Phone = dto.Phone;
        e.Mobile = dto.Mobile;
        e.AccountId = dto.AccountId;
        e.OwnerUserId = dto.OwnerUserId;
        e.Department = dto.Department;
        e.Address = dto.Address;
        e.Description = dto.Description;
        e.IsPrimary = dto.IsPrimary;
        e.DoNotContact = dto.DoNotContact;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (e != null)
        {
            _db.Contacts.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }
}
