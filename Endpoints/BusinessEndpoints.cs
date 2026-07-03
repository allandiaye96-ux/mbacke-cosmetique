using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using ErpCosmetiqueApi.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Endpoints;

public static class SaleEndpoints
{
    public static void MapSaleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sales").WithTags("Ventes").RequireAuthorization();

        group.MapGet("/", async (SaleService sales, DateOnly? from, DateOnly? to, SaleStatus? status) =>
            await sales.GetSalesAsync(from, to, status));

        group.MapGet("/{id:int}", async (int id, SaleService sales) =>
        {
            var sale = await sales.GetSaleAsync(id);
            return sale is null ? Results.NotFound() : Results.Ok(sale);
        });

        group.MapPost("/", async (CreateSaleDto dto, SaleService sales) =>
        {
            try
            {
                var sale = await sales.CreateSaleAsync(dto);
                return Results.Created($"/api/sales/{sale.Id}", sale);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapPost("/{id:int}/cancel", async (int id, CancelSaleDto dto, SaleService sales) =>
        {
            try
            {
                var sale = await sales.CancelSaleAsync(id, dto);
                return Results.Ok(sale);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });

        group.MapGet("/{id:int}/receipt", async (int id, SaleService sales) =>
        {
            var receipt = await sales.GetReceiptAsync(id);
            return receipt is null ? Results.NotFound() : Results.Ok(receipt);
        });

        group.MapGet("/{id:int}/receipt/print", async (int id, SaleService sales) =>
        {
            var html = await sales.GetReceiptHtmlAsync(id);
            return html is null ? Results.NotFound() : Results.Content(html, "text/html; charset=utf-8");
        });
    }
}

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Clients").RequireAuthorization();

        group.MapGet("/", async (CustomerService customers) => await customers.GetAllAsync());
        group.MapGet("/loyal", async (CustomerService customers) => await customers.GetLoyalCustomersAsync());
        group.MapGet("/{id:int}", async (int id, CustomerService customers) =>
        {
            var c = await customers.GetAsync(id);
            return c is null ? Results.NotFound() : Results.Ok(c);
        });
        group.MapPost("/", async (CreateCustomerDto dto, CustomerService customers) =>
        {
            var c = await customers.CreateAsync(dto);
            return Results.Created($"/api/customers/{c.Id}", c);
        });
        group.MapPut("/{id:int}", async (int id, UpdateCustomerDto dto, CustomerService customers) =>
        {
            var c = await customers.UpdateAsync(id, dto);
            return c is null ? Results.NotFound() : Results.Ok(c);
        });
        group.MapDelete("/{id:int}", async (int id, CustomerService customers) =>
        {
            try
            {
                var ok = await customers.DeleteAsync(id);
                return ok ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        });
        group.MapGet("/{id:int}/purchases", async (int id, CustomerService customers) =>
            await customers.GetPurchaseHistoryAsync(id));

        var payments = app.MapGroup("/api/customer-payments").WithTags("Paiements clients").RequireAuthorization();
        payments.MapGet("/", async (CustomerService customers, int? customerId) =>
            await customers.GetPaymentsAsync(customerId));
        payments.MapPost("/", async (CreateCustomerPaymentDto dto, CustomerService customers) =>
        {
            try
            {
                var p = await customers.RecordPaymentAsync(dto);
                return Results.Created($"/api/customer-payments/{p.Id}", p);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        });
    }
}

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/suppliers").WithTags("Fournisseurs").RequireAuthorization();

        group.MapGet("/", async (ErpDbContext db) =>
        {
            var suppliers = await db.Suppliers
                .Include(x => x.Purchases)
                .Include(x => x.Payments)
                .ToListAsync();
            return suppliers.Select(s => new SupplierDto(
                s.Id, s.Name, s.Phone, s.Email, s.Address,
                s.Purchases.Sum(p => p.TotalAmount),
                s.Payments.Sum(p => p.Amount),
                s.Purchases.Sum(p => p.TotalAmount) - s.Payments.Sum(p => p.Amount)));
        });

        group.MapGet("/{id:int}", async (int id, ErpDbContext db) =>
        {
            var s = await db.Suppliers
                .Include(x => x.Purchases)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            return Results.Ok(new SupplierDto(
                s.Id, s.Name, s.Phone, s.Email, s.Address,
                s.Purchases.Sum(p => p.TotalAmount),
                s.Payments.Sum(p => p.Amount),
                s.Purchases.Sum(p => p.TotalAmount) - s.Payments.Sum(p => p.Amount)));
        });

        group.MapPost("/", async (CreateSupplierDto dto, ErpDbContext db) =>
        {
            var s = new Supplier { Name = dto.Name, Phone = dto.Phone, Email = dto.Email, Address = dto.Address };
            db.Suppliers.Add(s);
            await db.SaveChangesAsync();
            return Results.Created($"/api/suppliers/{s.Id}", s);
        });

        group.MapPut("/{id:int}", async (int id, CreateSupplierDto dto, ErpDbContext db) =>
        {
            var s = await db.Suppliers.FindAsync(id);
            if (s is null) return Results.NotFound();
            s.Name = dto.Name; s.Phone = dto.Phone; s.Email = dto.Email; s.Address = dto.Address;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:int}", async (int id, ErpDbContext db) =>
        {
            var s = await db.Suppliers
                .Include(x => x.Purchases)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            if (s.Purchases.Any())
                return Results.BadRequest(new { error = "Impossible de supprimer un fournisseur avec des achats" });
            db.Suppliers.Remove(s);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public static class PurchaseEndpoints
{
    public static void MapPurchaseEndpoints(this WebApplication app)
    {
        var purchases = app.MapGroup("/api/purchases").WithTags("Achats").RequireAuthorization();
        purchases.MapGet("/", async (PurchaseService svc, int? supplierId) => await svc.GetPurchasesAsync(supplierId));
        purchases.MapGet("/{id:int}", async (int id, PurchaseService svc) =>
        {
            var p = await svc.GetPurchaseAsync(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });
        purchases.MapPost("/", async (CreatePurchaseDto dto, PurchaseService svc) =>
        {
            try
            {
                var p = await svc.CreatePurchaseAsync(dto);
                return Results.Created($"/api/purchases/{p.Id}", p);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
            { return Results.BadRequest(new { error = ex.Message }); }
        });
        purchases.MapPut("/{id:int}", async (int id, CreatePurchaseDto dto, PurchaseService svc) =>
        {
            try
            {
                var p = await svc.UpdatePurchaseAsync(id, dto);
                return Results.Ok(p);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
            { return Results.BadRequest(new { error = ex.Message }); }
        });
        purchases.MapPost("/{id:int}/cancel", async (int id, PurchaseService svc) =>
        {
            try
            {
                var p = await svc.CancelPurchaseAsync(id);
                return Results.Ok(p);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
            { return Results.BadRequest(new { error = ex.Message }); }
        });

        var orders = app.MapGroup("/api/supplier-orders").WithTags("Commandes fournisseurs").RequireAuthorization();
        orders.MapGet("/", async (PurchaseService svc, int? supplierId) => await svc.GetOrdersAsync(supplierId));
        orders.MapGet("/{id:int}", async (int id, PurchaseService svc) =>
        {
            var o = await svc.GetOrderAsync(id);
            return o is null ? Results.NotFound() : Results.Ok(o);
        });
        orders.MapPost("/", async (CreateSupplierOrderDto dto, PurchaseService svc) =>
        {
            try
            {
                var o = await svc.CreateOrderAsync(dto);
                return Results.Created($"/api/supplier-orders/{o.Id}", o);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        });
        orders.MapPost("/{id:int}/deliver", async (int id, PurchaseService svc) =>
        {
            try
            {
                var o = await svc.DeliverOrderAsync(id);
                return Results.Ok(o);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
            { return Results.BadRequest(new { error = ex.Message }); }
        });

        var payments = app.MapGroup("/api/supplier-payments").WithTags("Paiements fournisseurs").RequireAuthorization();
        payments.MapPost("/", async (CreateSupplierPaymentDto dto, PurchaseService svc) =>
        {
            try
            {
                var p = await svc.RecordPaymentAsync(dto);
                return Results.Created($"/api/supplier-payments/{p.Id}", p);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        });
    }
}
