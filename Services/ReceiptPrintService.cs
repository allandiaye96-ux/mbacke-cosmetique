using System.Text;
using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;

namespace ErpCosmetiqueApi.Services;

public static class ReceiptPrintService
{
    public static string GenerateReceiptHtml(ReceiptDto r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"fr\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<title>Reçu Mbacke Cosmetique</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"@media print { @page { size: 80mm auto; margin: 2mm; } }
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: 'Courier New', Courier, monospace; font-size: 11px; width: 72mm; margin: 0 auto; padding: 8px; color: #000; }
.center { text-align: center; }
.bold { font-weight: bold; }
.shop-name { font-size: 16px; font-weight: bold; letter-spacing: 1px; margin: 4px 0; }
.shop-tag { font-size: 10px; margin-bottom: 2px; }
.shop-info { font-size: 9px; line-height: 1.4; margin-bottom: 6px; }
.sep { border-top: 1px dashed #000; margin: 6px 0; }
.sep-double { border-top: 2px solid #000; margin: 6px 0; }
.row { display: flex; justify-content: space-between; margin: 2px 0; }
.row-label { flex: 1; }
.row-value { text-align: right; white-space: nowrap; }
table { width: 100%; border-collapse: collapse; font-size: 10px; }
th { text-align: left; border-bottom: 1px solid #000; padding: 3px 0; font-size: 9px; }
td { padding: 3px 0; vertical-align: top; }
.col-qty { width: 20px; text-align: center; }
.col-pu { width: 42px; text-align: right; }
.col-total { width: 48px; text-align: right; }
.total-section { margin-top: 6px; }
.total-line { font-size: 13px; font-weight: bold; }
.footer { text-align: center; font-size: 9px; margin-top: 10px; line-height: 1.5; }
@media screen { body { border: 1px solid #ccc; margin: 20px auto; } }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class=\"center\">");
        sb.AppendLine($"<div class=\"shop-name\">{Esc(r.ShopName.ToUpper())}</div>");
        sb.AppendLine($"<div class=\"shop-tag\">{Esc(r.ShopTagline)}</div>");
        if (!string.IsNullOrEmpty(r.ShopPhone))
            sb.AppendLine($"<div class=\"shop-info\">Tél : {Esc(r.ShopPhone)}</div>");
        if (!string.IsNullOrEmpty(r.ShopAddress))
            sb.AppendLine($"<div class=\"shop-info\">{Esc(r.ShopAddress)}</div>");
        if (!string.IsNullOrEmpty(r.ShopCity))
            sb.AppendLine($"<div class=\"shop-info\">{Esc(r.ShopCity)}</div>");
        if (!string.IsNullOrEmpty(r.ShopEmail))
            sb.AppendLine($"<div class=\"shop-info\">{Esc(r.ShopEmail)}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"sep-double\"></div>");
        sb.AppendLine("<div class=\"bold center\">TICKET DE CAISSE</div>");
        sb.AppendLine("<div class=\"sep\"></div>");

        sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">N° Vente</span><span class=\"row-value\">{r.SaleId:D6}</span></div>");
        if (!string.IsNullOrEmpty(r.InvoiceNumber))
            sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">N° Facture</span><span class=\"row-value\">{Esc(r.InvoiceNumber)}</span></div>");
        sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Date</span><span class=\"row-value\">{Esc(r.SoldAtFormatted)}</span></div>");
        sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Client</span><span class=\"row-value\">{Esc(r.CustomerName)}</span></div>");
        if (!string.IsNullOrEmpty(r.CustomerPhone))
            sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Tél client</span><span class=\"row-value\">{Esc(r.CustomerPhone)}</span></div>");
        if (!string.IsNullOrEmpty(r.EmployeeName))
            sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Vendeur</span><span class=\"row-value\">{Esc(r.EmployeeName)}</span></div>");
        sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Paiement</span><span class=\"row-value\">{Esc(r.PaymentModeLabel)}</span></div>");

        sb.AppendLine("<div class=\"sep\"></div>");
        sb.AppendLine("<table><thead><tr><th>Article</th><th class=\"col-qty\">Qté</th><th class=\"col-pu\">P.U</th><th class=\"col-total\">Total</th></tr></thead><tbody>");
        foreach (var line in r.Lines)
        {
            var name = line.ProductName.Length > 18 ? line.ProductName[..18] : line.ProductName;
            sb.AppendLine($"<tr><td>{Esc(name)}</td><td class=\"col-qty\">{line.Quantity}</td><td class=\"col-pu\">{Fmt(line.UnitSalePrice)}</td><td class=\"col-total\">{Fmt(line.LineTotal)}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<div class=\"sep\"></div>");
        sb.AppendLine("<div class=\"total-section\">");
        sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Articles ({r.ItemCount}) / Qté ({r.TotalQuantity})</span></div>");
        sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Sous-total</span><span class=\"row-value\">{Fmt(r.SubTotal)} F</span></div>");
        if (r.DiscountPercent > 0)
            sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Remise ({r.DiscountPercent:N0}%)</span><span class=\"row-value\">-{Fmt(r.SubTotal * r.DiscountPercent / 100m)} F</span></div>");
        if (r.DiscountAmountFixed > 0)
            sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Remise montant</span><span class=\"row-value\">-{Fmt(r.DiscountAmountFixed)} F</span></div>");
        if (r.DiscountTotal > 0)
            sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Total remises</span><span class=\"row-value\">-{Fmt(r.DiscountTotal)} F</span></div>");
        sb.AppendLine("<div class=\"sep\"></div>");
        sb.AppendLine($"<div class=\"row total-line\"><span class=\"row-label\">TOTAL À PAYER</span><span class=\"row-value\">{Fmt(r.TotalAmount)} F</span></div>");
        sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Montant reçu</span><span class=\"row-value\">{Fmt(r.AmountPaid)} F</span></div>");
        if (r.ChangeGiven > 0)
            sb.AppendLine($"<div class=\"row\"><span class=\"row-label\">Monnaie rendue</span><span class=\"row-value\">{Fmt(r.ChangeGiven)} F</span></div>");
        if (r.BalanceDue > 0)
            sb.AppendLine($"<div class=\"row bold\"><span class=\"row-label\">Reste à payer</span><span class=\"row-value\">{Fmt(r.BalanceDue)} F</span></div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"sep-double\"></div>");
        sb.AppendLine($"<div class=\"footer\">Merci pour votre achat !<br/>À bientôt chez <strong>{Esc(r.ShopName)}</strong><br/>");
        sb.AppendLine($"Imprimé le {DateTime.Now:dd/MM/yyyy à HH:mm:ss}</div>");

        sb.AppendLine("<script>window.onload=function(){setTimeout(function(){window.print()},300)}</script>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string Fmt(decimal n) => n.ToString("N0");
    private static string Esc(string s) => System.Net.WebUtility.HtmlEncode(s);
}
