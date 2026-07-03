using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpCosmetiqueApi.Services;

public class CustomerService(ErpDbContext db)
{
    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var customers = await db.Customers
            .Include(x => x.Sales)
            .Include(x => x.Payments)
            .ToListAsync();

        return customers.Select(MapCustomer).ToList();
    }

    public async Task<CustomerDto?> GetAsync(int id)
    {
        var c = await db.Customers
            .Include(x => x.Sales)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);
        return c is null ? null : MapCustomer(c);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            IsLoyal = dto.IsLoyal
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return MapCustomer(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await db.Customers
            .Include(x => x.Sales)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (customer is null) return null;

        customer.Name = dto.Name;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.IsLoyal = dto.IsLoyal;
        await db.SaveChangesAsync();
        return MapCustomer(customer);
    }

    public async Task<List<CustomerDto>> GetLoyalCustomersAsync()
    {
        var customers = await db.Customers
            .Include(x => x.Sales)
            .Include(x => x.Payments)
            .Where(x => x.IsLoyal || x.LoyaltyPoints >= 100)
            .ToListAsync();
        return customers.Select(MapCustomer).ToList();
    }

    public async Task<List<SaleDto>> GetPurchaseHistoryAsync(int customerId)
    {
        var sales = await db.Sales
            .Include(x => x.Customer)
            .Include(x => x.Employee)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .Include(x => x.Invoice)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.SoldAt)
            .ToListAsync();

        return sales.Select(s => new SaleDto(
            s.Id, s.SoldAt, s.CustomerId, s.Customer.Name,
            s.EmployeeId, s.Employee?.Name,
            s.SubTotal, s.DiscountAmount, s.DiscountPercent, s.TotalAmount, s.AmountPaid,
            s.PaymentMode, s.Status, s.CancellationReason, s.CancelledAt,
            s.Invoice?.Id, s.Invoice?.InvoiceNumber,
            s.Lines.Select(l => new SaleLineDto(
                l.ProductId, l.Product.Name, l.Quantity, l.UnitSalePrice, l.LineTotal)).ToList()
        )).ToList();
    }

    public async Task<CustomerPaymentDto> RecordPaymentAsync(CreateCustomerPaymentDto dto)
    {
        var customer = await db.Customers.FindAsync(dto.CustomerId)
            ?? throw new KeyNotFoundException("Client introuvable");

        if (dto.SaleId.HasValue)
        {
            var sale = await db.Sales.FindAsync(dto.SaleId.Value)
                ?? throw new KeyNotFoundException("Vente introuvable");
            sale.AmountPaid += dto.Amount;
        }

        var payment = new CustomerPayment
        {
            CustomerId = dto.CustomerId,
            SaleId = dto.SaleId,
            Amount = dto.Amount,
            PaymentMode = dto.PaymentMode,
            Note = dto.Note,
            PaidAt = DateTime.UtcNow
        };

        db.CustomerPayments.Add(payment);
        await db.SaveChangesAsync();

        return new CustomerPaymentDto(
            payment.Id, customer.Id, customer.Name,
            payment.SaleId, payment.Amount, payment.PaidAt,
            payment.PaymentMode, payment.Note);
    }

    public async Task<List<CustomerPaymentDto>> GetPaymentsAsync(int? customerId = null)
    {
        var query = db.CustomerPayments
            .Include(x => x.Customer)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        return await query
            .OrderByDescending(x => x.PaidAt)
            .Select(x => new CustomerPaymentDto(
                x.Id, x.CustomerId, x.Customer.Name,
                x.SaleId, x.Amount, x.PaidAt, x.PaymentMode, x.Note))
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await db.Customers
            .Include(x => x.Sales)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (customer is null) return false;
        if (customer.Sales.Any())
            throw new InvalidOperationException("Impossible de supprimer un client avec des ventes enregistrées");
        db.Customers.Remove(customer);
        await db.SaveChangesAsync();
        return true;
    }

    private static CustomerDto MapCustomer(Customer c)
    {
        var debt = c.Sales
            .Where(s => s.Status == SaleStatus.Active)
            .Sum(s => s.TotalAmount - s.AmountPaid);
        debt -= c.Payments.Sum(p => p.Amount);
        return new CustomerDto(
            c.Id, c.Name, c.Phone, c.Email, c.Address,
            c.LoyaltyPoints, c.IsLoyal, Math.Max(0, debt),
            c.Sales.Count(s => s.Status == SaleStatus.Active));
    }
}
