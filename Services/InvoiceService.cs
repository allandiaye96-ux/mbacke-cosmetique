using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using ErpCosmetiqueApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ErpCosmetiqueApi.Services;

public class InvoiceService(ErpDbContext db, IOptions<ShopSettings> shopOptions)
{
    private readonly ShopSettings _shop = shopOptions.Value;

    static InvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
    public async Task<InvoiceDto> CreateFromSaleAsync(int saleId)
    {
        var sale = await db.Sales
            .Include(x => x.Customer)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == saleId)
            ?? throw new KeyNotFoundException("Vente introuvable");

        if (await db.Invoices.AnyAsync(x => x.SaleId == saleId))
        {
            var existing = await GetInvoiceBySaleAsync(saleId);
            return existing!;
        }

        var invoice = new Invoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            CustomerId = sale.CustomerId,
            SaleId = saleId,
            IssuedAt = DateTime.UtcNow,
            SubTotal = sale.SubTotal,
            DiscountAmount = sale.DiscountAmount + sale.SubTotal * (sale.DiscountPercent / 100m),
            TotalAmount = sale.TotalAmount,
            Status = sale.AmountPaid >= sale.TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.Issued,
            Lines = sale.Lines.Select(l => new InvoiceLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitSalePrice
            }).ToList()
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return (await GetInvoiceAsync(invoice.Id))!;
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(int id)
    {
        var inv = await db.Invoices
            .Include(x => x.Customer)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
        return inv is null ? null : MapInvoice(inv);
    }

    public async Task<InvoiceDto?> GetInvoiceBySaleAsync(int saleId)
    {
        var inv = await db.Invoices
            .Include(x => x.Customer)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.SaleId == saleId);
        return inv is null ? null : MapInvoice(inv);
    }

    public async Task<InvoiceDto> CancelInvoiceAsync(int id)
    {
        var inv = await db.Invoices.FindAsync(id)
            ?? throw new KeyNotFoundException("Facture introuvable");

        if (inv.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Facture déjà annulée");

        inv.Status = InvoiceStatus.Cancelled;
        await db.SaveChangesAsync();
        return (await GetInvoiceAsync(id))!;
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var query = db.Invoices
            .Include(x => x.Customer)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(x => x.IssuedAt >= from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (to.HasValue)
            query = query.Where(x => x.IssuedAt <= to.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        var invoices = await query.OrderByDescending(x => x.IssuedAt).ToListAsync();
        return invoices.Select(MapInvoice).ToList();
    }

    public async Task<byte[]> GeneratePdfAsync(int invoiceId)
    {
        var inv = await db.Invoices
            .Include(x => x.Customer)
            .Include(x => x.Lines).ThenInclude(x => x.Product)
            .Include(x => x.Sale!).ThenInclude(x => x!.Employee)
            .FirstOrDefaultAsync(x => x.Id == invoiceId)
            ?? throw new KeyNotFoundException("Facture introuvable");

        var sale = inv.Sale;
        var localDate = inv.IssuedAt.ToLocalTime();
        var discountPercent = sale?.DiscountPercent ?? 0;
        var discountFixed = sale?.DiscountAmount ?? inv.DiscountAmount;
        var paymentLabel = sale is null ? "—" : sale.PaymentMode switch
        {
            PaymentMode.Cash => "Espèces",
            PaymentMode.MobileMoney => "Mobile Money",
            PaymentMode.Card => "Carte bancaire",
            PaymentMode.Credit => "Crédit",
            _ => sale.PaymentMode.ToString()
        };

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(_shop.Name.ToUpper()).Bold().FontSize(24).FontColor(Colors.Purple.Darken3);
                    col.Item().AlignCenter().Text(_shop.Tagline).FontSize(12).FontColor(Colors.Grey.Darken1);
                    col.Item().AlignCenter().PaddingTop(4).Text($"{_shop.Phone}  |  {_shop.Address}  |  {_shop.City}").FontSize(9);
                    col.Item().PaddingTop(10).AlignCenter().Text("FACTURE").Bold().FontSize(16);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Informations facture").Bold().FontSize(11);
                            c.Item().Text($"N° facture : {inv.InvoiceNumber}");
                            c.Item().Text($"N° vente    : {inv.SaleId?.ToString() ?? "—"}");
                            c.Item().Text($"Date        : {localDate:dd/MM/yyyy}");
                            c.Item().Text($"Heure       : {localDate:HH:mm:ss}");
                            c.Item().Text($"Statut      : {InvoiceStatusLabel(inv.Status)}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Client").Bold().FontSize(11);
                            c.Item().Text(inv.Customer.Name);
                            if (!string.IsNullOrEmpty(inv.Customer.Phone))
                                c.Item().Text($"Tél : {inv.Customer.Phone}");
                            if (!string.IsNullOrEmpty(inv.Customer.Email))
                                c.Item().Text($"Email : {inv.Customer.Email}");
                        });
                    });

                    if (sale is not null)
                    {
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text($"Vendeur : {sale.Employee?.Name ?? "—"}");
                            row.RelativeItem().AlignRight().Text($"Mode de paiement : {paymentLabel}");
                        });
                    }

                    col.Item().PaddingVertical(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30);
                            c.RelativeColumn(4);
                            c.ConstantColumn(45);
                            c.ConstantColumn(70);
                            c.ConstantColumn(80);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Purple.Lighten4).Padding(5).Text("#").Bold();
                            h.Cell().Background(Colors.Purple.Lighten4).Padding(5).Text("Désignation").Bold();
                            h.Cell().Background(Colors.Purple.Lighten4).Padding(5).Text("Qté").Bold();
                            h.Cell().Background(Colors.Purple.Lighten4).Padding(5).AlignRight().Text("P.U (F)").Bold();
                            h.Cell().Background(Colors.Purple.Lighten4).Padding(5).AlignRight().Text("Total (F)").Bold();
                        });

                        var i = 1;
                        foreach (var line in inv.Lines)
                        {
                            table.Cell().Padding(5).Text(i.ToString());
                            table.Cell().Padding(5).Text(line.Product.Name);
                            table.Cell().Padding(5).Text(line.Quantity.ToString());
                            table.Cell().Padding(5).AlignRight().Text($"{line.UnitPrice:N0}");
                            table.Cell().Padding(5).AlignRight().Text($"{line.Quantity * line.UnitPrice:N0}");
                            i++;
                        }
                    });

                    col.Item().PaddingTop(15).AlignRight().Width(220).Column(c =>
                    {
                        c.Item().Row(r => { r.RelativeItem().Text("Sous-total :"); r.ConstantItem(90).AlignRight().Text($"{inv.SubTotal:N0} F"); });
                        if (discountPercent > 0)
                            c.Item().Row(r => { r.RelativeItem().Text($"Remise ({discountPercent:N0}%) :"); r.ConstantItem(90).AlignRight().Text($"-{inv.SubTotal * discountPercent / 100m:N0} F"); });
                        if (discountFixed > 0)
                            c.Item().Row(r => { r.RelativeItem().Text("Remise montant :"); r.ConstantItem(90).AlignRight().Text($"-{discountFixed:N0} F"); });
                        c.Item().PaddingTop(4).Row(r => { r.RelativeItem().Text("TOTAL :").Bold().FontSize(13); r.ConstantItem(90).AlignRight().Text($"{inv.TotalAmount:N0} F").Bold().FontSize(13); });
                        if (sale is not null)
                        {
                            c.Item().PaddingTop(6).Row(r => { r.RelativeItem().Text("Montant payé :"); r.ConstantItem(90).AlignRight().Text($"{sale.AmountPaid:N0} F"); });
                            var balance = Math.Max(0, sale.TotalAmount - sale.AmountPaid);
                            if (balance > 0)
                                c.Item().Row(r => { r.RelativeItem().Text("Reste à payer :").Bold(); r.ConstantItem(90).AlignRight().Text($"{balance:N0} F").Bold(); });
                        }
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().AlignCenter().Text("Merci pour votre confiance !").FontSize(10);
                    col.Item().AlignCenter().Text($"Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm:ss} — {_shop.Name}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string InvoiceStatusLabel(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "Brouillon",
        InvoiceStatus.Issued => "Émise",
        InvoiceStatus.Paid => "Payée",
        InvoiceStatus.Cancelled => "Annulée",
        _ => status.ToString()
    };

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.Invoices.CountAsync(x => x.IssuedAt.Year == year);
        return $"FAC-{year}-{(count + 1):D5}";
    }

    private static InvoiceDto MapInvoice(Invoice inv) =>
        new(inv.Id, inv.InvoiceNumber, inv.IssuedAt,
            inv.CustomerId, inv.Customer.Name, inv.SaleId,
            inv.SubTotal, inv.DiscountAmount, inv.TotalAmount, inv.Status,
            inv.Lines.Select(l => new InvoiceLineDto(
                l.ProductId, l.Product.Name, l.Quantity, l.UnitPrice,
                l.Quantity * l.UnitPrice)).ToList());
}
