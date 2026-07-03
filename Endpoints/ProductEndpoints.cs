using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using ErpCosmetiqueApi.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Produits").RequireAuthorization();

        group.MapGet("/", async (ErpDbContext db, StockService stock) =>
        {
            var products = await db.Products
                .Include(x => x.Category)
                .Include(x => x.Brand)
                .Include(x => x.StockLots)
                .Where(x => x.IsActive)
                .ToListAsync();

            return products.Select(p => new ProductDto(
                p.Id, p.Name, p.Sku,
                p.CategoryId, p.Category.Name,
                p.BrandId, p.Brand.Name,
                p.PurchasePrice, p.SalePrice,
                p.MinStockLevel, p.StockLots.Sum(l => l.QuantityOnHand), p.IsActive));
        });

        group.MapGet("/low-stock", async (StockService stock) =>
            await stock.GetLowStockProductsAsync());

        group.MapGet("/{id:int}", async (int id, ErpDbContext db) =>
        {
            var p = await db.Products
                .Include(x => x.Category)
                .Include(x => x.Brand)
                .Include(x => x.StockLots)
                .FirstOrDefaultAsync(x => x.Id == id);
            return p is null ? Results.NotFound() : Results.Ok(new ProductDto(
                p.Id, p.Name, p.Sku,
                p.CategoryId, p.Category.Name,
                p.BrandId, p.Brand.Name,
                p.PurchasePrice, p.SalePrice,
                p.MinStockLevel, p.StockLots.Sum(l => l.QuantityOnHand), p.IsActive));
        });

        group.MapPost("/", async (CreateProductDto dto, ErpDbContext db) =>
        {
            var product = new Product
            {
                Name = dto.Name, Sku = dto.Sku,
                CategoryId = dto.CategoryId, BrandId = dto.BrandId,
                PurchasePrice = dto.PurchasePrice, SalePrice = dto.SalePrice,
                MinStockLevel = dto.MinStockLevel
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/products/{product.Id}", product);
        });

        group.MapPut("/{id:int}", async (int id, UpdateProductDto dto, ErpDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();
            product.Name = dto.Name;
            product.Sku = dto.Sku;
            product.CategoryId = dto.CategoryId;
            product.BrandId = dto.BrandId;
            product.PurchasePrice = dto.PurchasePrice;
            product.SalePrice = dto.SalePrice;
            product.MinStockLevel = dto.MinStockLevel;
            product.IsActive = dto.IsActive;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:int}", async (int id, ErpDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();
            product.IsActive = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        var categories = app.MapGroup("/api/categories").WithTags("Catégories").RequireAuthorization();
        categories.MapGet("/", async (ErpDbContext db) =>
            await db.Categories.Select(x => new CategoryDto(x.Id, x.Name)).ToListAsync());
        categories.MapPost("/", async (CreateCategoryDto dto, ErpDbContext db) =>
        {
            var cat = new Category { Name = dto.Name };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            return Results.Created($"/api/categories/{cat.Id}", new CategoryDto(cat.Id, cat.Name));
        });
        categories.MapPut("/{id:int}", async (int id, CreateCategoryDto dto, ErpDbContext db) =>
        {
            var cat = await db.Categories.FindAsync(id);
            if (cat is null) return Results.NotFound();
            cat.Name = dto.Name;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        categories.MapDelete("/{id:int}", async (int id, ErpDbContext db) =>
        {
            var cat = await db.Categories.FindAsync(id);
            if (cat is null) return Results.NotFound();
            db.Categories.Remove(cat);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        var brands = app.MapGroup("/api/brands").WithTags("Marques").RequireAuthorization();
        brands.MapGet("/", async (ErpDbContext db) =>
            await db.Brands.Select(x => new BrandDto(x.Id, x.Name)).ToListAsync());
        brands.MapPost("/", async (CreateBrandDto dto, ErpDbContext db) =>
        {
            var brand = new Brand { Name = dto.Name };
            db.Brands.Add(brand);
            await db.SaveChangesAsync();
            return Results.Created($"/api/brands/{brand.Id}", new BrandDto(brand.Id, brand.Name));
        });
        brands.MapPut("/{id:int}", async (int id, CreateBrandDto dto, ErpDbContext db) =>
        {
            var brand = await db.Brands.FindAsync(id);
            if (brand is null) return Results.NotFound();
            brand.Name = dto.Name;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        brands.MapDelete("/{id:int}", async (int id, ErpDbContext db) =>
        {
            var brand = await db.Brands.FindAsync(id);
            if (brand is null) return Results.NotFound();
            db.Brands.Remove(brand);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public static class StockEndpoints
{
    public static void MapStockEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/stock").WithTags("Stock").RequireAuthorization();

        group.MapGet("/movements", async (StockService stock, int? productId, int limit = 100) =>
            await stock.GetMovementsAsync(productId, limit));

        group.MapGet("/lots/{productId:int}", async (int productId, StockService stock) =>
            await stock.GetProductLotsAsync(productId));

        group.MapGet("/quantity/{productId:int}", async (int productId, StockService stock) =>
            new { productId, quantity = await stock.GetProductStockAsync(productId) });
    }
}
