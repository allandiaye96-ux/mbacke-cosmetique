using ErpCosmetiqueApi.Models;
using ErpCosmetiqueApi.Services;
using Microsoft.EntityFrameworkCore;
namespace ErpCosmetiqueApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ErpDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var categories = new[] { "Soins visage", "Maquillage", "Parfums", "Soins cheveux", "Hygiène" };
        foreach (var name in categories)
            db.Categories.Add(new Category { Name = name });

        var brands = new[] { "L'Oréal", "Nivea", "Maybelline", "Garnier", "Dove" };
        foreach (var name in brands)
            db.Brands.Add(new Brand { Name = name });

        await db.SaveChangesAsync();

        var catIds = await db.Categories.Select(x => x.Id).ToListAsync();
        var brandIds = await db.Brands.Select(x => x.Id).ToListAsync();

        var products = new[]
        {
            ("Crème hydratante 50ml", 2500m, 4500m, 10),
            ("Rouge à lèvres mat", 1500m, 3500m, 15),
            ("Parfum 30ml", 8000m, 15000m, 5),
            ("Shampoing 400ml", 2000m, 4000m, 12),
            ("Déodorant roll-on", 1200m, 2500m, 20),
            ("Mascara volume", 1800m, 3800m, 10),
            ("Lait corporel 250ml", 2200m, 4200m, 8),
            ("Eau micellaire 200ml", 3000m, 5500m, 10),
        };

        for (var i = 0; i < products.Length; i++)
        {
            var (name, purchase, sale, minStock) = products[i];
            db.Products.Add(new Product
            {
                Name = name,
                Sku = $"SKU-{i + 1:D4}",
                CategoryId = catIds[i % catIds.Count],
                BrandId = brandIds[i % brandIds.Count],
                PurchasePrice = purchase,
                SalePrice = sale,
                MinStockLevel = minStock
            });
        }

        db.EmployeeRoles.AddRange(
            new EmployeeRole { Name = "Administrateur" },
            new EmployeeRole { Name = "Vendeur" },
            new EmployeeRole { Name = "Caissier" },
            new EmployeeRole { Name = "Gérant" });

        await db.SaveChangesAsync();

        var roleVendeur = await db.EmployeeRoles.FirstAsync(x => x.Name == "Vendeur");
        db.Employees.Add(new Employee
        {
            Name = "Marie Dupont",
            Phone = "+225 07 00 00 01",
            RoleId = roleVendeur.Id,
            MonthlySalary = 150000
        });

        db.Customers.Add(new Customer
        {
            Name = "Client comptoir",
            Phone = "0000000000"
        });

        db.Suppliers.Add(new Supplier
        {
            Name = "Grossiste Cosmétique CI",
            Phone = "+225 01 00 00 00",
            Email = "contact@grossiste.ci"
        });

        db.CashBoxes.Add(new CashBox
        {
            Name = "Caisse principale",
            OpeningBalance = 50000
        });

        await db.SaveChangesAsync();

        var product = await db.Products.FirstAsync();
        db.StockLots.Add(new StockLot
        {
            ProductId = product.Id,
            ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
            QuantityOnHand = 50
        });

        await db.SaveChangesAsync();
        await SeedStockAsync(db);
    }

    public static async Task SeedUsersAsync(ErpDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var employee = await db.Employees.FirstOrDefaultAsync();
        db.Users.AddRange(
            new AppUser
            {
                Username = "admin",
                PasswordHash = AuthService.HashPassword("admin123"),
                Role = "Administrateur",
                EmployeeId = employee?.Id
            },
            new AppUser
            {
                Username = "vendeur",
                PasswordHash = AuthService.HashPassword("vendeur123"),
                Role = "Vendeur",
                EmployeeId = employee?.Id
            });
        await db.SaveChangesAsync();
    }

    public static async Task SeedStockForAllAsync(ErpDbContext db)
    {
        if (!await db.Products.AnyAsync()) return;
        await SeedStockAsync(db);
    }

    private static async Task SeedStockAsync(ErpDbContext db)
    {
        var products = await db.Products.ToListAsync();
        foreach (var product in products)
        {
            if (await db.StockLots.AnyAsync(x => x.ProductId == product.Id)) continue;
            db.StockLots.Add(new StockLot
            {
                ProductId = product.Id,
                ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1 + Random.Shared.Next(1, 3))),
                QuantityOnHand = 30 + Random.Shared.Next(0, 40)
            });
        }
        await db.SaveChangesAsync();
    }
}
