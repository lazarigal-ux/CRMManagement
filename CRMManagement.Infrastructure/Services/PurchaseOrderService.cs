using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly AppDbContext _db;
    public PurchaseOrderService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PurchaseOrderListItemDto>> ListAsync(CancellationToken ct) =>
        await _db.PurchaseOrders.AsNoTracking()
            .OrderByDescending(p => p.PoDate ?? p.CreatedAt)
            .Select(p => new PurchaseOrderListItemDto(p.Id, p.PoNumber, p.Subject, p.VendorId, p.Status,
                p.Total, p.Currency, p.PoDate, p.DueDate)
            {
                RequisitionNo    = p.RequisitionNo,
                CarrierName      = p.CarrierName,
                Subtotal         = p.Subtotal,
                Discount         = p.Discount,
                Tax              = p.Tax,
                AdjustmentAmount = p.AdjustmentAmount,
                Description      = p.Description,
                ZohoCreatedTime  = p.ZohoCreatedTime,
                ZohoModifiedTime = p.ZohoModifiedTime,
                VendorName       = p.VendorId == null
                    ? null
                    : _db.Vendors.Where(v => v.Id == p.VendorId).Select(v => v.Name).FirstOrDefault(),
                OwnerName        = p.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == p.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);

    public async Task<PurchaseOrderDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        var lines = p.Lines.OrderBy(l => l.SortOrder)
            .Select(l => new PurchaseOrderLineDto(l.Id, l.PurchaseOrderId, l.ProductId, l.Description,
                l.Quantity, l.UnitPrice, l.Discount, l.LineTotal, l.SortOrder))
            .ToList();
        return new PurchaseOrderDetailDto(p.Id, p.PoNumber, p.Subject, p.RequisitionNo, p.VendorId, p.Status, p.PoDate, p.DueDate,
            p.CarrierName, p.Subtotal, p.Discount, p.Tax, p.AdjustmentAmount, p.Total, p.Currency,
            p.Description, p.TermsAndConditions, p.BillingAddress, p.ShippingAddress, p.OwnerUserId, lines);
    }

    public async Task<Guid> UpsertAsync(PurchaseOrderUpsertDto dto, CancellationToken ct)
    {
        PurchaseOrder e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Purchase Order not found");
        }
        else
        {
            e = new PurchaseOrder { Id = Guid.NewGuid() };
            _db.PurchaseOrders.Add(e);
        }
        e.PoNumber = dto.PoNumber;
        e.Subject = dto.Subject;
        e.RequisitionNo = dto.RequisitionNo;
        e.VendorId = dto.VendorId;
        e.Status = dto.Status;
        e.PoDate = dto.PoDate;
        e.DueDate = dto.DueDate;
        e.CarrierName = dto.CarrierName;
        e.Subtotal = dto.Subtotal;
        e.Discount = dto.Discount;
        e.Tax = dto.Tax;
        e.AdjustmentAmount = dto.AdjustmentAmount;
        e.Total = dto.Total;
        e.Currency = dto.Currency;
        e.Description = dto.Description;
        e.TermsAndConditions = dto.TermsAndConditions;
        e.BillingAddress = dto.BillingAddress;
        e.ShippingAddress = dto.ShippingAddress;
        e.OwnerUserId = dto.OwnerUserId;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return;
        _db.PurchaseOrders.Remove(p);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddLineAsync(Guid poId, PurchaseOrderLineUpsertDto line, CancellationToken ct)
    {
        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == poId, ct)
            ?? throw new InvalidOperationException("Purchase Order not found");
        var lineTotal = (line.Quantity * line.UnitPrice) - line.Discount;
        _db.PurchaseOrderLines.Add(new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = po.Id,
            ProductId = line.ProductId,
            Description = line.Description,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            Discount = line.Discount,
            LineTotal = lineTotal,
            SortOrder = line.SortOrder,
        });
        po.Subtotal += lineTotal;
        po.Total = po.Subtotal - po.Discount + po.Tax + po.AdjustmentAmount;
        await _db.SaveChangesAsync(ct);
    }
}
