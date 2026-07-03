using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Services;

public class ReportService(ErpDbContext db)
{
    public async Task<DailyReportDto> GetDailyReportAsync(DateOnly? date = null)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = d.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var sales = await db.Sales
            .Include(x => x.Customer)
            .Include(x => x.Employee)
            .Include(x => x.Lines)
            .Where(x => x.SoldAt >= from && x.SoldAt <= to && x.Status == SaleStatus.Active)
            .OrderByDescending(x => x.SoldAt)
            .ToListAsync();

        var totalSales = sales.Sum(x => x.TotalAmount);

        var expenses = await db.CashTransactions
            .Include(x => x.CashBox)
            .Include(x => x.Employee)
            .Where(x => x.Kind == CashTransactionKind.Expense && x.CreatedAt >= from && x.CreatedAt <= to)
            .ToListAsync();

        var totalExpenses = expenses.Sum(x => x.Amount);

        return new DailyReportDto(
            d, totalSales, totalExpenses, totalSales - totalExpenses,
            sales.Count,
            sales.Select(s => new SaleSummaryDto(
                s.Id, s.SoldAt, s.Customer.Name, s.Employee?.Name,
                s.TotalAmount, s.PaymentMode, s.Lines.Count)).ToList(),
            expenses.Select(e => new CashTransactionDto(
                e.Id, e.CashBoxId, e.CashBox.Name,
                e.EmployeeId, e.Employee?.Name,
                e.Kind, e.CreatedAt, e.Amount, e.Motive)).ToList());
    }

    public async Task<PeriodReportDto> GetPeriodReportAsync(DateOnly from, DateOnly to)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var sales = await db.Sales
            .Include(x => x.Lines)
            .Include(x => x.Customer)
            .Where(x => x.SoldAt >= fromDt && x.SoldAt <= toDt && x.Status == SaleStatus.Active)
            .ToListAsync();

        var purchases = (decimal)await db.Purchases
            .Where(x => x.PurchasedAt >= fromDt && x.PurchasedAt <= toDt)
            .SumAsync(x => (double)x.TotalAmount);

        var expenses = (decimal)await db.CashTransactions
            .Where(x => x.Kind == CashTransactionKind.Expense && x.CreatedAt >= fromDt && x.CreatedAt <= toDt)
            .SumAsync(x => (double)x.Amount);

        var totalSales = sales.Sum(x => x.TotalAmount);

        var topProducts = sales
            .SelectMany(s => s.Lines)
            .GroupBy(l => l.ProductId)
            .Select(async g =>
            {
                var product = await db.Products.FindAsync(g.Key);
                return new TopProductDto(
                    g.Key, product?.Name ?? "?", g.Sum(l => l.Quantity),
                    g.Sum(l => l.Quantity * l.UnitSalePrice));
            });

        var topProductsList = new List<TopProductDto>();
        foreach (var task in topProducts)
            topProductsList.Add(await task);
        topProductsList = topProductsList.OrderByDescending(x => x.QuantitySold).Take(10).ToList();

        var topCustomers = sales
            .GroupBy(s => s.CustomerId)
            .Select(g => new TopCustomerDto(
                g.Key, g.First().Customer.Name, g.Count(), g.Sum(s => s.TotalAmount)))
            .OrderByDescending(x => x.TotalSpent)
            .Take(10)
            .ToList();

        return new PeriodReportDto(
            from, to, totalSales, purchases, expenses,
            totalSales - purchases - expenses,
            sales.Count, topProductsList, topCustomers);
    }

    public async Task<List<SalesByDayDto>> GetSalesByDayAsync(DateOnly from, DateOnly to)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var sales = await db.Sales
            .Where(x => x.SoldAt >= fromDt && x.SoldAt <= toDt && x.Status == SaleStatus.Active)
            .ToListAsync();

        return sales
            .GroupBy(s => DateOnly.FromDateTime(s.SoldAt))
            .Select(g => new SalesByDayDto(g.Key, g.Sum(s => s.TotalAmount), g.Count()))
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<FinanceSummaryDto> GetFinanceSummaryAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var fromDt = (from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)))
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = (to ?? DateOnly.FromDateTime(DateTime.UtcNow))
            .ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var salesIncome = (decimal)await db.Sales
            .Where(x => x.SoldAt >= fromDt && x.SoldAt <= toDt && x.Status == SaleStatus.Active)
            .SumAsync(x => (double)x.AmountPaid);

        var cashIncome = (decimal)await db.CashTransactions
            .Where(x => x.Kind == CashTransactionKind.Income && x.CreatedAt >= fromDt && x.CreatedAt <= toDt)
            .SumAsync(x => (double)x.Amount);

        var expenses = (decimal)await db.CashTransactions
            .Where(x => x.Kind == CashTransactionKind.Expense && x.CreatedAt >= fromDt && x.CreatedAt <= toDt)
            .SumAsync(x => (double)x.Amount);

        var purchases = (decimal)await db.Purchases
            .Where(x => x.PurchasedAt >= fromDt && x.PurchasedAt <= toDt)
            .SumAsync(x => (double)x.TotalAmount);

        var totalIncome = salesIncome + cashIncome;
        return new FinanceSummaryDto(totalIncome, expenses + purchases, totalIncome - expenses - purchases, totalIncome - expenses - purchases);
    }
}
