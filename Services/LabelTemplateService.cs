using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Generates shipping label PDFs using QuestPDF.
/// Label size: A6 (105 × 148 mm).
///
/// Prefers ShippingAddress; falls back to BillingAddress if ShippingAddress is null.
/// All address fields are rendered with null-safe fallbacks.
/// </summary>
public class LabelTemplateService
{
    public string GenerateLabel(OrderDetail order)
    {
        var outputPath = FilePathHelper.GetLabelPath(order.Bvin);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(0.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                page.Content().Element(c => ComposeLabel(c, order));
            });
        });

        document.GeneratePdf(outputPath);
        return outputPath;
    }

    private void ComposeLabel(IContainer container, OrderDetail order)
    {
        // Prefer shipping address; fall back to billing address
        var address = order.ShippingAddress ?? order.BillingAddress;

        container.Border(2).BorderColor(Colors.Black).Column(col =>
        {
            // ── Header bar ──────────────────────────────────────────────────
            col.Item()
               .Background(Colors.Black)
               .Padding(6)
               .Row(row =>
               {
                   row.RelativeItem()
                      .Text("SZÁLLÍTÁSI CIMKE")
                      .Bold().FontSize(13).FontColor(Colors.White);
                   row.ConstantItem(72)
                      .AlignRight()
                      .Text(DateTime.Now.ToString("yyyy.MM.dd"))
                      .FontColor(Colors.Grey.Lighten3).FontSize(9);
               });

            // ── Recipient ───────────────────────────────────────────────────
            col.Item().Padding(10).Column(c =>
            {
                c.Item().Text("CÍMZETT").Bold().FontSize(7).FontColor(Colors.Grey.Medium);

                var recipientName = address?.FullName;
                if (string.IsNullOrWhiteSpace(recipientName))
                    recipientName = order.CustomerName; // falls back to email or "Nincs adat"

                c.Item().PaddingTop(2).Text(recipientName).Bold().FontSize(16);

                if (address != null)
                {
                    c.Item().PaddingTop(4).Column(addr =>
                    {
                        if (!string.IsNullOrWhiteSpace(address.Line1))
                            addr.Item().Text(address.Line1).FontSize(12);
                        if (!string.IsNullOrWhiteSpace(address.Line2))
                            addr.Item().Text(address.Line2).FontSize(12);

                        var cityLine = $"{address.PostalCode} {address.City}".Trim();
                        if (!string.IsNullOrWhiteSpace(cityLine))
                            addr.Item().Text(cityLine).FontSize(13).Bold();

                        if (!string.IsNullOrWhiteSpace(address.CountryName))
                            addr.Item().Text(address.CountryName).FontSize(11);
                    });
                }
                else
                {
                    c.Item().PaddingTop(4).Text("Szállítási cím nem elérhető.")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                }
            });

            // ── Contact + order info ─────────────────────────────────────────
            col.Item()
               .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
               .Padding(8)
               .Row(row =>
               {
                   row.RelativeItem().Column(c =>
                   {
                       c.Item().Text("KAPCSOLAT").Bold().FontSize(7).FontColor(Colors.Grey.Medium);
                       var phone = address?.Phone;
                       if (!string.IsNullOrWhiteSpace(phone))
                           c.Item().Text($"Tel: {phone}").FontSize(9);
                       if (!string.IsNullOrWhiteSpace(order.UserEmail))
                           c.Item().Text(order.UserEmail).FontSize(8).FontColor(Colors.Grey.Medium);
                   });

                   row.RelativeItem().Column(c =>
                   {
                       c.Item().Text("RENDELÉS").Bold().FontSize(7).FontColor(Colors.Grey.Medium);
                       c.Item().Text(order.DisplayOrderNumber).FontSize(9);
                       if (!string.IsNullOrWhiteSpace(order.ShippingMethodDisplayName))
                           c.Item().Text(order.ShippingMethodDisplayName)
                               .FontSize(8).FontColor(Colors.Grey.Medium);
                   });
               });

            // ── Barcode (real CODE_128 via ZXing.Net) ────────────────────────
            // Prefer OrderNumber; fall back to Bvin when the order is a draft.
            var barcodeContent = BarcodeGenerator.ResolveBarcodeContent(
                order.OrderNumber, order.Bvin);
            var barcodePng = BarcodeGenerator.GeneratePng(barcodeContent,
                width: 300, height: 70);

            col.Item()
               .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
               .PaddingVertical(6)
               .Column(bc =>
               {
                   if (barcodePng != null)
                   {
                       // Fit the barcode into the label width and label it below
                       bc.Item().AlignCenter().Height(45).Image(barcodePng);
                       bc.Item()
                         .AlignCenter()
                         .Text(barcodeContent)
                         .FontSize(8).FontColor(Colors.Grey.Darken2);
                   }
                   else
                   {
                       // If encoding failed for any reason, degrade gracefully
                       // rather than breaking the whole PDF.
                       bc.Item()
                         .AlignCenter()
                         .Text($"Vonalkód nem generálható ({barcodeContent})")
                         .FontSize(8).FontColor(Colors.Grey.Medium);
                   }
               });
        });
    }
}
