using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Services;

public class CashService(ErpDbContext db)
{
    public async Task<decimal> GetBalanceAsync(int cashBoxId)
    {
        var box = await db.CashBoxes.FindAsync(cashBoxId)
            ?? throw new KeyNotFoundException("Caisse introuvable");

        var income = (decimal)await db.CashTransactions
            .Where(x => x.CashBoxId == cashBoxId && x.Kind == CashTransactionKind.Income)
            .SumAsync(x => (double)x.Amount);

        var expenses = (decimal)await db.CashTransactions
            .Where(x => x.CashBoxId == cashBoxId && x.Kind == CashTransactionKind.Expense)
            .SumAsync(x => (double)x.Amount);

        return box.OpeningBalance + income - expenses;
    }

    public async Task<CashTransactionDto> RecordExpenseAsync(
        int cashBoxId, decimal amount, string motive, int? employeeId = null)
    {
        var box = await db.CashBoxes.FindAsync(cashBoxId)
            ?? throw new KeyNotFoundException("Caisse introuvable");

        var tx = new CashTransaction
        {
            CashBoxId = cashBoxId,
            EmployeeId = employeeId,
            Kind = CashTransactionKind.Expense,
            Amount = amount,
            Motive = motive,
            CreatedAt = DateTime.UtcNow
        };

        db.CashTransactions.Add(tx);
        await db.SaveChangesAsync();
        return await MapTransactionAsync(tx);
    }

    public async Task<CashTransactionDto> RecordIncomeAsync(
        int cashBoxId, decimal amount, string motive, int? employeeId = null)
    {
        var box = await db.CashBoxes.FindAsync(cashBoxId)
            ?? throw new KeyNotFoundException("Caisse introuvable");

        var tx = new CashTransaction
        {
            CashBoxId = cashBoxId,
            EmployeeId = employeeId,
            Kind = CashTransactionKind.Income,
            Amount = amount,
            Motive = motive,
            CreatedAt = DateTime.UtcNow
        };

        db.CashTransactions.Add(tx);
        await db.SaveChangesAsync();
        return await MapTransactionAsync(tx);
    }

    public async Task<CashTransactionDto> CreateExpenseAsync(CreateCashExpenseDto dto)
    {
        return await RecordExpenseAsync(dto.CashBoxId, dto.Amount, dto.Motive, dto.EmployeeId);
    }

    public async Task<List<CashTransactionDto>> GetTransactionsAsync(
        int? cashBoxId = null, CashTransactionKind? kind = null,
        DateOnly? from = null, DateOnly? to = null)
    {
        var query = db.CashTransactions
            .Include(x => x.CashBox)
            .Include(x => x.Employee)
            .AsQueryable();

        if (cashBoxId.HasValue)
            query = query.Where(x => x.CashBoxId == cashBoxId.Value);
        if (kind.HasValue)
            query = query.Where(x => x.Kind == kind.Value);
        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        var txs = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        var result = new List<CashTransactionDto>();
        foreach (var tx in txs)
            result.Add(await MapTransactionAsync(tx));
        return result;
    }

    public async Task<List<CashBoxDto>> GetCashBoxesAsync()
    {
        var boxes = await db.CashBoxes.ToListAsync();
        var result = new List<CashBoxDto>();
        foreach (var box in boxes)
        {
            var balance = await GetBalanceAsync(box.Id);
            result.Add(new CashBoxDto(box.Id, box.Name, box.OpeningBalance, balance));
        }
        return result;
    }

    public async Task<CashBoxDto> CreateCashBoxAsync(CreateCashBoxDto dto)
    {
        var box = new CashBox { Name = dto.Name, OpeningBalance = dto.OpeningBalance };
        db.CashBoxes.Add(box);
        await db.SaveChangesAsync();
        return new CashBoxDto(box.Id, box.Name, box.OpeningBalance, box.OpeningBalance);
    }

    public async Task<CashTransactionDto?> UpdateExpenseAsync(long id, CreateCashExpenseDto dto)
    {
        var tx = await db.CashTransactions.FindAsync(id);
        if (tx is null || tx.Kind != CashTransactionKind.Expense) return null;
        tx.CashBoxId = dto.CashBoxId;
        tx.EmployeeId = dto.EmployeeId;
        tx.Amount = dto.Amount;
        tx.Motive = dto.Motive;
        await db.SaveChangesAsync();
        return await MapTransactionAsync(tx);
    }

    public async Task<bool> DeleteTransactionAsync(long id)
    {
        var tx = await db.CashTransactions.FindAsync(id);
        if (tx is null) return false;
        db.CashTransactions.Remove(tx);
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<CashTransactionDto> MapTransactionAsync(CashTransaction tx)
    {
        await db.Entry(tx).Reference(x => x.CashBox).LoadAsync();
        if (tx.EmployeeId.HasValue)
            await db.Entry(tx).Reference(x => x.Employee).LoadAsync();

        return new CashTransactionDto(
            tx.Id, tx.CashBoxId, tx.CashBox.Name,
            tx.EmployeeId, tx.Employee?.Name,
            tx.Kind, tx.CreatedAt, tx.Amount, tx.Motive);
    }
}
