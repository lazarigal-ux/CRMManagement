using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<ProductListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Products.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductListItemDto(p.Id, p.Sku, p.Name, p.Family, p.IsActive, p.StandardPrice))
            .ToListAsync(ct);
    }

    public async Task<ProductDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (x == null) return null;
        return new ProductDetailDto(x.Id, x.Sku, x.Name, x.Description, x.Family, x.IsActive, x.StandardPrice, x.Cost, x.Unit);
    }

    public async Task<Guid> UpsertAsync(ProductUpsertDto dto, CancellationToken ct)
    {
        Product e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                ?? throw new InvalidOperationException("Product not found");
        }
        else
        {
            e = new Product { Id = Guid.NewGuid() };
            _db.Products.Add(e);
        }
        e.Sku = dto.Sku;
        e.Name = dto.Name;
        e.Description = dto.Description;
        e.Family = dto.Family;
        e.IsActive = dto.IsActive;
        e.StandardPrice = dto.StandardPrice;
        e.Cost = dto.Cost;
        e.Unit = dto.Unit;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (e != null)
        {
            _db.Products.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }
}
