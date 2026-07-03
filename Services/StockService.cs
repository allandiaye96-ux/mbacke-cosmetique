using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Services;

public class StockService(ErpDbContext db)
{
    public async Task<int> GetProductStockAsync(int productId)
    {
        return await db.StockLots
            .Where(x => x.ProductId == productId)
            .SumAsync(x => x.QuantityOnHand);
    }

    public async Task<List<StockLotDto>> GetProductLotsAsync(int productId)
    {
        return await db.StockLots
            .Include(x => x.Product)
            .Where(x => x.ProductId == productId && x.QuantityOnHand > 0)
            .OrderBy(x => x.ExpirationDate)
            .Select(x => new StockLotDto(x.Id, x.ProductId, x.Product.Name, x.ExpirationDate, x.QuantityOnHand))
            .ToListAsync();
    }

    public async Task<List<StockMovementDto>> GetMovementsAsync(int? productId = null, int limit = 100)
    {
        var query = db.StockMovements
            .Include(x => x.StockLot).ThenInclude(x => x.Product)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(x => x.StockLot.ProductId == productId.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new StockMovementDto(
                x.Id, x.Type.ToString(), x.StockLot.Product.Name,
                x.QuantityDelta, x.Note, x.CreatedAt))
            .ToListAsync();
    }

    public async Task AddStockFromPurchaseAsync(int productId, int quantity, DateOnly expirationDate, string? note = null)
    {
        var lot = await db.StockLots
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.ExpirationDate == expirationDate);

        if (lot is null)
        {
            lot = new StockLot { ProductId = productId, ExpirationDate = expirationDate, QuantityOnHand = 0 };
            db.StockLots.Add(lot);
            await db.SaveChangesAsync();
        }

        lot.QuantityOnHand += quantity;
        db.StockMovements.Add(new StockMovement
        {
            StockLotId = lot.Id,
            Type = StockMovementType.PurchaseIn,
            QuantityDelta = quantity,
            Note = note
        });
        await db.SaveChangesAsync();
    }

    public async Task DeductStockForSaleAsync(int productId, int quantity, string? note = null)
    {
        var remaining = quantity;
        var lots = await db.StockLots
            .Where(x => x.ProductId == productId && x.QuantityOnHand > 0)
            .OrderBy(x => x.ExpirationDate)
            .ToListAsync();

        foreach (var lot in lots)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, lot.QuantityOnHand);
            lot.QuantityOnHand -= take;
            remaining -= take;

            db.StockMovements.Add(new StockMovement
            {
                StockLotId = lot.Id,
                Type = StockMovementType.SaleOut,
                QuantityDelta = -take,
                Note = note
            });
        }

        if (remaining > 0)
            throw new InvalidOperationException($"Stock insuffisant pour le produit {productId}. Manque: {remaining}");

        await db.SaveChangesAsync();
    }

    public async Task RestoreStockFromCancellationAsync(int productId, int quantity, string? note = null)
    {
        var lot = await db.StockLots
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.ExpirationDate)
            .FirstOrDefaultAsync();

        if (lot is null)
        {
            lot = new StockLot
            {
                ProductId = productId,
                ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                QuantityOnHand = 0
            };
            db.StockLots.Add(lot);
            await db.SaveChangesAsync();
        }

        lot.QuantityOnHand += quantity;
        db.StockMovements.Add(new StockMovement
        {
            StockLotId = lot.Id,
            Type = StockMovementType.SaleCancel,
            QuantityDelta = quantity,
            Note = note
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync()
    {
        var products = await db.Products
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.StockLots)
            .Where(x => x.IsActive)
            .ToListAsync();

        return products
            .Where(p => p.StockLots.Sum(l => l.QuantityOnHand) <= p.MinStockLevel)
            .Select(p => new ProductDto(
                p.Id, p.Name, p.Sku,
                p.CategoryId, p.Category.Name,
                p.BrandId, p.Brand.Name,
                p.PurchasePrice, p.SalePrice,
                p.MinStockLevel, p.StockLots.Sum(l => l.QuantityOnHand), p.IsActive))
            .ToList();
    }
}
