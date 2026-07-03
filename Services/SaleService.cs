using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErpCosmetiqueApi.Services;

public class SaleService(ErpDbContext db, StockService stock, InvoiceService invoice, CashService cash, IOptions<ShopSettings> shopOptions)
{
    private readonly ShopSettings _shop = shopOptions.Value;
    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto)
    {
        var customer = await db.Customers.FindAsync(dto.CustomerId)
            ?? throw new KeyNotFoundException("Client introuvable");

        if (dto.EmployeeId.HasValue && await db.Employees.FindAsync(dto.EmployeeId) is null)
            throw new KeyNotFoundException("Employé introuvable");

        if (dto.Lines.Count == 0)
            throw new InvalidOperationException("La vente doit contenir au moins un produit");

        var sale = new Sale
        {
            CustomerId = dto.CustomerId,
            EmployeeId = dto.EmployeeId,
            PaymentMode = dto.PaymentMode,
            SoldAt = DateTime.UtcNow
        };

        decimal subTotal = 0;
        foreach (var line in dto.Lines)
        {
            var product = await db.Products.FindAsync(line.ProductId)
                ?? throw new KeyNotFoundException($"Produit {line.ProductId} introuvable");

            var unitPrice = line.UnitSalePrice ?? product.SalePrice;
            subTotal += unitPrice * line.Quantity;

            sale.Lines.Add(new SaleLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitSalePrice = unitPrice
            });

            await stock.DeductStockForSaleAsync(line.ProductId, line.Quantity, $"Vente en cours");
        }

        sale.SubTotal = subTotal;
        sale.DiscountAmount = dto.DiscountAmount;
        sale.DiscountPercent = dto.DiscountPercent;

        var discountFromPercent = subTotal * (dto.DiscountPercent / 100m);
        sale.TotalAmount = Math.Max(0, subTotal - dto.DiscountAmount - discountFromPercent);
        sale.AmountPaid = dto.AmountPaid ?? sale.TotalAmount;

        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        if (dto.PaymentMode == PaymentMode.Cash)
        {
            var cashBox = await db.CashBoxes.FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Aucune caisse configurée");
            await cash.RecordIncomeAsync(cashBox.Id, sale.AmountPaid, $"Vente #{sale.Id}", dto.EmployeeId);
        }

        if (sale.AmountPaid < sale.TotalAmount)
        {
            customer.LoyaltyPoints += (int)(sale.TotalAmount / 1000);
            if (customer.LoyaltyPoints >= 100) customer.IsLoyal = true;
        }

        await db.SaveChangesAsync();

        var inv = await invoice.CreateFromSaleAsync(sale.Id);
        sale = await LoadSaleAsync(sale.Id);
        return MapSale(sale, inv?.Id, inv?.InvoiceNumber);
    }

    public async Task<SaleDto> CancelSaleAsync(int saleId, CancelSaleDto dto)
    {
        var sale = await LoadSaleAsync(saleId)
            ?? throw new KeyNotFoundException("Vente introuvable");

        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("Vente déjà annulée");

        sale.Status = SaleStatus.Cancelled;
        sale.CancellationReason = dto.Reason;
        sale.CancelledAt = DateTime.UtcNow;

        foreach (var line in sale.Lines)
        {
            await stock.RestoreStockFromCancellationAsync(
                line.ProductId, line.Quantity, $"Annulation vente #{sale.Id}");
        }

        if (sale.PaymentMode == PaymentMode.Cash && sale.AmountPaid > 0)
        {
            var cashBox = await db.CashBoxes.FirstOrDefaultAsync();
            if (cashBox is not null)
            {
                await cash.RecordExpenseAsync(
                    cashBox.Id, sale.AmountPaid,
                    $"Annulation vente #{sale.Id}", sale.EmployeeId);
            }
        }

        if (sale.Invoice is not null)
            sale.Invoice.Status = InvoiceStatus.Cancelled;

        await db.SaveChangesAsync();
        return MapSale(sale, sale.Invoice?.Id, sale.Invoice?.InvoiceNumber);
    }

    public async Task<List<SaleDto>> GetSalesAsync(DateOnly? from = null, DateOnly? to = null, SaleStatus? status = null)
    {
        var query = db.Sales
            .Include(x => x.Customer)
            .Include(x => x.Employee)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .Include(x => x.Invoice)
            .AsQueryable();

        if (from.HasValue)
        {
            var fromDt = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.SoldAt >= fromDt);
        }
        if (to.HasValue)
        {
            var toDt = to.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(x => x.SoldAt <= toDt);
        }
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var sales = await query.OrderByDescending(x => x.SoldAt).ToListAsync();
        return sales.Select(s => MapSale(s, s.Invoice?.Id, s.Invoice?.InvoiceNumber)).ToList();
    }

    public async Task<SaleDto?> GetSaleAsync(int id)
    {
        var sale = await LoadSaleAsync(id);
        return sale is null ? null : MapSale(sale, sale.Invoice?.Id, sale.Invoice?.InvoiceNumber);
    }

    public async Task<ReceiptDto?> GetReceiptAsync(int saleId)
    {
        var sale = await LoadSaleAsync(saleId);
        return sale is null ? null : BuildReceipt(sale);
    }

    public async Task<string?> GetReceiptHtmlAsync(int saleId)
    {
        var receipt = await GetReceiptAsync(saleId);
        return receipt is null ? null : ReceiptPrintService.GenerateReceiptHtml(receipt);
    }

    private ReceiptDto BuildReceipt(Sale sale)
    {
        var discountFromPercent = sale.SubTotal * (sale.DiscountPercent / 100m);
        var discountTotal = sale.DiscountAmount + discountFromPercent;
        var change = Math.Max(0, sale.AmountPaid - sale.TotalAmount);
        var balance = Math.Max(0, sale.TotalAmount - sale.AmountPaid);
        var localTime = sale.SoldAt.ToLocalTime();
        var lines = sale.Lines.Select(l => new SaleLineDto(
            l.ProductId, l.Product.Name, l.Quantity, l.UnitSalePrice, l.LineTotal)).ToList();

        return new ReceiptDto(
            sale.Id,
            _shop.Name,
            _shop.Tagline,
            _shop.Phone,
            _shop.Address,
            _shop.City,
            _shop.Email,
            sale.SoldAt,
            localTime.ToString("dd/MM/yyyy à HH:mm:ss"),
            sale.Invoice?.InvoiceNumber,
            sale.Customer.Name,
            sale.Customer.Phone,
            sale.Employee?.Name,
            lines,
            lines.Count,
            lines.Sum(l => l.Quantity),
            sale.SubTotal,
            sale.DiscountPercent,
            sale.DiscountAmount,
            discountTotal,
            sale.TotalAmount,
            sale.PaymentMode,
            PaymentModeLabel(sale.PaymentMode),
            sale.AmountPaid,
            change,
            balance);
    }

    private static string PaymentModeLabel(PaymentMode mode) => mode switch
    {
        PaymentMode.Cash => "Espèces",
        PaymentMode.MobileMoney => "Mobile Money",
        PaymentMode.Card => "Carte bancaire",
        PaymentMode.Credit => "Crédit",
        _ => mode.ToString()
    };

    private async Task<Sale?> LoadSaleAsync(int id) =>
        await db.Sales
            .Include(x => x.Customer)
            .Include(x => x.Employee)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.Id == id);

    private static SaleDto MapSale(Sale s, int? invoiceId, string? invoiceNumber) =>
        new(s.Id, s.SoldAt, s.CustomerId, s.Customer.Name,
            s.EmployeeId, s.Employee?.Name,
            s.SubTotal, s.DiscountAmount, s.DiscountPercent, s.TotalAmount, s.AmountPaid,
            s.PaymentMode, s.Status, s.CancellationReason, s.CancelledAt,
            invoiceId, invoiceNumber,
            s.Lines.Select(l => new SaleLineDto(
                l.ProductId, l.Product.Name, l.Quantity, l.UnitSalePrice, l.LineTotal)).ToList());
}
