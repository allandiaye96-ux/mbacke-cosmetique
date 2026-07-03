using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Services;

public class DashboardService(ErpDbContext db, CashService cash, StockService stock)
{
    public async Task<DashboardDto> GetDashboardAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var todaySales = (decimal)await db.Sales
            .Where(x => x.SoldAt >= from && x.SoldAt <= to && x.Status == SaleStatus.Active)
            .SumAsync(x => (double)x.TotalAmount);

        var todaySaleCount = await db.Sales
            .CountAsync(x => x.SoldAt >= from && x.SoldAt <= to && x.Status == SaleStatus.Active);

        var todayExpenses = (decimal)await db.CashTransactions
            .Where(x => x.Kind == CashTransactionKind.Expense && x.CreatedAt >= from && x.CreatedAt <= to)
            .SumAsync(x => (double)x.Amount);

        var cashBox = await db.CashBoxes.FirstOrDefaultAsync();
        decimal cashBalance = 0;
        if (cashBox is not null)
            cashBalance = await cash.GetBalanceAsync(cashBox.Id);

        var customerDebts = await CalculateCustomerDebtsAsync();
        var lowStock = await stock.GetLowStockProductsAsync();

        return new DashboardDto(
            todaySales, todayExpenses, todaySales - todayExpenses,
            cashBalance, customerDebts,
            lowStock.Count, todaySaleCount);
    }

    private async Task<decimal> CalculateCustomerDebtsAsync()
    {
        var salesDebt = (decimal)await db.Sales
            .Where(x => x.Status == SaleStatus.Active)
            .SumAsync(x => (double)(x.TotalAmount - x.AmountPaid));

        return Math.Max(0, salesDebt);
    }
}
