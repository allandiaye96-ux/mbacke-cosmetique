using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Services;

public class PurchaseService(ErpDbContext db, StockService stock)
{
    public async Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseDto dto)
    {
        var supplier = await db.Suppliers.FindAsync(dto.SupplierId)
            ?? throw new KeyNotFoundException("Fournisseur introuvable");

        if (dto.Lines.Count == 0)
            throw new InvalidOperationException("L'achat doit contenir au moins une ligne");

        var purchase = new Purchase
        {
            SupplierId = dto.SupplierId,
            PurchasedAt = DateTime.UtcNow,
            DeliveredAt = DateTime.UtcNow,
            Status = PurchaseStatus.Delivered
        };

        decimal total = 0;
        foreach (var line in dto.Lines)
        {
            var product = await db.Products.FindAsync(line.ProductId)
                ?? throw new KeyNotFoundException($"Produit {line.ProductId} introuvable");

            total += line.UnitPurchasePrice * line.Quantity;
            purchase.Lines.Add(new PurchaseLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPurchasePrice = line.UnitPurchasePrice,
                ExpirationDate = line.ExpirationDate
            });

            product.PurchasePrice = line.UnitPurchasePrice;
            await stock.AddStockFromPurchaseAsync(
                line.ProductId, line.Quantity, line.ExpirationDate,
                $"Achat #{purchase.Id}");
        }

        purchase.TotalAmount = total;
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync();

        return await GetPurchaseAsync(purchase.Id) ?? throw new InvalidOperationException();
    }

    public async Task<List<PurchaseDto>> GetPurchasesAsync(int? supplierId = null)
    {
        var query = db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        var purchases = await query.OrderByDescending(x => x.PurchasedAt).ToListAsync();
        return purchases.Select(MapPurchase).ToList();
    }

    public async Task<PurchaseDto?> GetPurchaseAsync(int id)
    {
        var p = await db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? null : MapPurchase(p);
    }

    public async Task<PurchaseDto> UpdatePurchaseAsync(int id, CreatePurchaseDto dto)
    {
        var purchase = await db.Purchases
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Achat introuvable");

        if (purchase.Status == PurchaseStatus.Cancelled)
            throw new InvalidOperationException("Impossible de modifier un achat annulé");

        if (purchase.AmountPaid > 0)
            throw new InvalidOperationException("Impossible de modifier un achat avec paiement enregistré");

        if (dto.Lines.Count == 0)
            throw new InvalidOperationException("L'achat doit contenir au moins une ligne");

        foreach (var line in purchase.Lines)
            await stock.DeductStockForSaleAsync(line.ProductId, line.Quantity, $"Modification achat #{id}");

        db.PurchaseLines.RemoveRange(purchase.Lines);
        purchase.Lines.Clear();
        purchase.SupplierId = dto.SupplierId;

        decimal total = 0;
        foreach (var line in dto.Lines)
        {
            var product = await db.Products.FindAsync(line.ProductId)
                ?? throw new KeyNotFoundException($"Produit {line.ProductId} introuvable");

            total += line.UnitPurchasePrice * line.Quantity;
            purchase.Lines.Add(new PurchaseLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPurchasePrice = line.UnitPurchasePrice,
                ExpirationDate = line.ExpirationDate
            });

            product.PurchasePrice = line.UnitPurchasePrice;
            await stock.AddStockFromPurchaseAsync(
                line.ProductId, line.Quantity, line.ExpirationDate,
                $"Modification achat #{id}");
        }

        purchase.TotalAmount = total;
        await db.SaveChangesAsync();
        return (await GetPurchaseAsync(id))!;
    }

    public async Task<PurchaseDto> CancelPurchaseAsync(int id)
    {
        var purchase = await db.Purchases
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Achat introuvable");

        if (purchase.Status == PurchaseStatus.Cancelled)
            throw new InvalidOperationException("Achat déjà annulé");

        if (purchase.AmountPaid > 0)
            throw new InvalidOperationException("Impossible d'annuler un achat avec paiement enregistré");

        foreach (var line in purchase.Lines)
            await stock.DeductStockForSaleAsync(line.ProductId, line.Quantity, $"Annulation achat #{id}");

        purchase.Status = PurchaseStatus.Cancelled;
        await db.SaveChangesAsync();
        return (await GetPurchaseAsync(id))!;
    }

    public async Task<SupplierOrderDto> CreateOrderAsync(CreateSupplierOrderDto dto)
    {
        var supplier = await db.Suppliers.FindAsync(dto.SupplierId)
            ?? throw new KeyNotFoundException("Fournisseur introuvable");

        var order = new SupplierOrder
        {
            SupplierId = dto.SupplierId,
            Note = dto.Note,
            OrderedAt = DateTime.UtcNow
        };

        decimal total = 0;
        foreach (var line in dto.Lines)
        {
            total += line.UnitPrice * line.Quantity;
            order.Lines.Add(new SupplierOrderLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            });
        }
        order.TotalAmount = total;

        db.SupplierOrders.Add(order);
        await db.SaveChangesAsync();
        return await GetOrderAsync(order.Id) ?? throw new InvalidOperationException();
    }

    public async Task<SupplierOrderDto> DeliverOrderAsync(int orderId)
    {
        var order = await db.SupplierOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId)
            ?? throw new KeyNotFoundException("Commande introuvable");

        if (order.Status == SupplierOrderStatus.Delivered)
            throw new InvalidOperationException("Commande déjà livrée");

        order.Status = SupplierOrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;

        var purchaseDto = new CreatePurchaseDto(order.SupplierId,
            order.Lines.Select(l => new CreatePurchaseLineDto(
                l.ProductId, l.Quantity, l.UnitPrice,
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)))).ToList());

        await CreatePurchaseAsync(purchaseDto);
        await db.SaveChangesAsync();

        return await GetOrderAsync(orderId) ?? throw new InvalidOperationException();
    }

    public async Task<List<SupplierOrderDto>> GetOrdersAsync(int? supplierId = null)
    {
        var query = db.SupplierOrders
            .Include(x => x.Supplier)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        var orders = await query.OrderByDescending(x => x.OrderedAt).ToListAsync();
        return orders.Select(MapOrder).ToList();
    }

    public async Task<SupplierOrderDto?> GetOrderAsync(int id)
    {
        var o = await db.SupplierOrders
            .Include(x => x.Supplier)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        return o is null ? null : MapOrder(o);
    }

    public async Task<SupplierPaymentDto> RecordPaymentAsync(CreateSupplierPaymentDto dto)
    {
        var supplier = await db.Suppliers.FindAsync(dto.SupplierId)
            ?? throw new KeyNotFoundException("Fournisseur introuvable");

        var payment = new SupplierPayment
        {
            SupplierId = dto.SupplierId,
            PurchaseId = dto.PurchaseId,
            Amount = dto.Amount,
            Note = dto.Note,
            PaidAt = DateTime.UtcNow
        };

        if (dto.PurchaseId.HasValue)
        {
            var purchase = await db.Purchases.FindAsync(dto.PurchaseId.Value)
                ?? throw new KeyNotFoundException("Achat introuvable");
            purchase.AmountPaid += dto.Amount;
        }

        db.SupplierPayments.Add(payment);
        await db.SaveChangesAsync();

        return new SupplierPaymentDto(
            payment.Id, supplier.Id, supplier.Name,
            payment.PurchaseId, payment.Amount, payment.PaidAt, payment.Note);
    }

    private static PurchaseDto MapPurchase(Purchase p) =>
        new(p.Id, p.SupplierId, p.Supplier.Name, p.PurchasedAt, p.DeliveredAt,
            p.Status, p.TotalAmount, p.AmountPaid,
            p.Lines.Select(l => new PurchaseLineDto(
                l.ProductId, l.Product.Name, l.Quantity, l.UnitPurchasePrice, l.ExpirationDate)).ToList());

    private static SupplierOrderDto MapOrder(SupplierOrder o) =>
        new(o.Id, o.SupplierId, o.Supplier.Name, o.OrderedAt, o.DeliveredAt,
            o.Status, o.TotalAmount, o.Note,
            o.Lines.Select(l => new SupplierOrderLineDto(
                l.ProductId, l.Product.Name, l.Quantity, l.UnitPrice)).ToList());
}
