using ErpCosmetiqueApi.Data;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(ErpDbContext db)
    {
        if (!await db.Database.CanConnectAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DbSeeder.SeedAsync(db);
            await DbSeeder.SeedUsersAsync(db);
            return;
        }

        if (!await TableExistsAsync(db, "Users"))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            await DbSeeder.SeedAsync(db);
        }
        else if (!await db.Categories.AnyAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        await DbSeeder.SeedUsersAsync(db);
        await DbSeeder.SeedStockForAllAsync(db);
    }

    private static async Task<bool> TableExistsAsync(ErpDbContext db, string tableName)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            await conn.CloseAsync();
        }
    }
}
