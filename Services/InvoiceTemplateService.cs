using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Számla PDF-eket generál a QuestPDF segítségével.
///
/// AMIKOR VANNAK TÉTELSOROK (mock mód, vagy jövőbeli megerősített végpont):
///   Teljes, tételes számla készül egy termék-táblával.
///
/// AMIKOR NINCSENEK TÉTELSOROK (élő API, még meg nem erősített végpont):
///   Összesítő számla készül, ami tartalmazza a rendelés fejléc-adatait,
///   címeket és a végösszeget — egy egyértelmű megjegyzéssel arról, hogy
///   a tételes sorok nem elérhetők. Ez a biztonságosabb opció: a PDF így
///   is használható referenciaként, és sosem omlik össze egy üres tétel-lista
///   miatt.
///
/// EZ EGY DEMO SZÁMLA — nem felel meg a magyar számlázási jogszabályoknak.
/// </summary>
public class InvoiceTemplateService
{
    private readonly CompanySettings _company;

    /// <summary>
    /// Létrehozza a számla szervizt a megadott cégadatokkal az „eladó" blokkhoz.
    /// Add át az induláskor betöltött példányt, hogy a fájlt sessionönként
    /// csak egyszer olvassuk be.
    /// </summary>
    public InvoiceTemplateService(CompanySettings company)
    {
        _company = company;
    }

    /// <summary>Paraméter nélküli konstruktor a kompatibilitás kedvéért — a lemezről tölt be alapértékeket.</summary>
    public InvoiceTemplateService() : this(CompanySettings.LoadOrCreate()) { }

    public string GenerateInvoice(OrderDetail order)
    {
        var outputPath = FilePathHelper.GetInvoicePath(order.Bvin);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, order));
                page.Footer().Element(ComposeFooter);
            });
        });

        document.GeneratePdf(outputPath);
        return outputPath;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Layout szekciók
    // ──────────────────────────────────────────────────────────────────────

    private void ComposeHeader(IContainer container)
    {
        // A cím, alcím és azonosító-előtag mind a companysettings.json-ból
        // jön, így egy nem programozó is át tudja brand-elni a dokumentumot
        // a JSON fájl szerkesztésével.
        var title    = string.IsNullOrWhiteSpace(_company.InvoiceTitle)    ? "SZÁMLA" : _company.InvoiceTitle;
        var subtitle = _company.InvoiceSubtitle ?? "";
        var idPrefix = string.IsNullOrWhiteSpace(_company.InvoiceIdPrefix) ? "DEMO"   : _company.InvoiceIdPrefix.Trim();

        container.PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(title).FontSize(22).Bold();
                if (!string.IsNullOrWhiteSpace(subtitle))
                    col.Item().Text(subtitle).FontSize(8).FontColor(Colors.Red.Medium);
            });
            row.ConstantItem(185).Column(col =>
            {
                col.Item().AlignRight().Text($"Kiállítás dátuma: {DateTime.Now:yyyy. MM. dd.}");
                col.Item().AlignRight()
                    .Text($"Azonosító: {idPrefix}-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmm}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(IContainer container, OrderDetail order)
    {
        container.Column(col =>
        {
            col.Spacing(14);
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeSellerInfo);
                row.ConstantItem(16);
                row.RelativeItem().Element(c => ComposeBuyerInfo(c, order));
            });
            col.Item().Element(c => ComposeOrderInfo(c, order));

            // Tételes tábla vagy „nincs tétel" megjegyzés, attól függően mi áll rendelkezésre.
            if (order.Items.Count > 0)
                col.Item().Element(c => ComposeItemsTable(c, order));
            else
                col.Item().Element(c => ComposeNoItemsNotice(c));

            col.Item().Element(c => ComposeTotals(c, order));
        });
    }

    private void ComposeSellerInfo(IContainer container)
    {
        // A cégadatok a CompanySettings-en át a companysettings.json-ból jönnek.
        // Az abban szereplő minden mezőt feltételesen renderelünk — hagyd
        // üresen, hogy az adott sor kimaradjon a számláról. Így egy nem
        // programozó is át tudja brand-elni a számlát (cégnévváltás, adószám
        // módosítás, bankváltás, weboldal felvétele stb.) egyetlen fájl
        // szerkesztésével.
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text("ELADÓ").Bold().FontSize(8).FontColor(Colors.Grey.Medium);

            if (!string.IsNullOrWhiteSpace(_company.CompanyName))
                col.Item().Text(_company.CompanyName).Bold();

            if (!string.IsNullOrWhiteSpace(_company.Address))
                col.Item().Text($"Cím: {_company.Address}");

            if (!string.IsNullOrWhiteSpace(_company.TaxNumber))
                col.Item().Text($"Adószám: {_company.TaxNumber}");

            if (!string.IsNullOrWhiteSpace(_company.RegistrationNumber))
                col.Item().Text($"Cégjegyzékszám: {_company.RegistrationNumber}");

            if (!string.IsNullOrWhiteSpace(_company.Email))
                col.Item().Text($"Email: {_company.Email}");

            if (!string.IsNullOrWhiteSpace(_company.Phone))
                col.Item().Text($"Tel: {_company.Phone}");

            if (!string.IsNullOrWhiteSpace(_company.Website))
                col.Item().Text($"Web: {_company.Website}");

            // Bank-sor: ha mindkettő ki van töltve, kombináljuk a bank nevét és a számlaszámot.
            if (!string.IsNullOrWhiteSpace(_company.BankAccount))
            {
                var bankLine = string.IsNullOrWhiteSpace(_company.BankName)
                    ? $"Bankszámla: {_company.BankAccount}"
                    : $"Bankszámla ({_company.BankName}): {_company.BankAccount}";
                col.Item().Text(bankLine);
            }
        });
    }

    private void ComposeBuyerInfo(IContainer container, OrderDetail order)
    {
        var billing = order.BillingAddress;
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
        {
            col.Item().Text("VEVŐ").Bold().FontSize(8).FontColor(Colors.Grey.Medium);
            col.Item().Text(order.CustomerName).Bold();
            if (billing != null)
            {
                if (!string.IsNullOrWhiteSpace(billing.Line1))  col.Item().Text(billing.Line1);
                if (!string.IsNullOrWhiteSpace(billing.Line2))  col.Item().Text(billing.Line2);
                var city = $"{billing.PostalCode} {billing.City}".Trim();
                if (!string.IsNullOrWhiteSpace(city))           col.Item().Text(city);
                if (!string.IsNullOrWhiteSpace(billing.CountryName)) col.Item().Text(billing.CountryName);
                if (!string.IsNullOrWhiteSpace(billing.Phone))  col.Item().Text($"Tel: {billing.Phone}");
            }
            if (!string.IsNullOrWhiteSpace(order.UserEmail))
                col.Item().Text($"Email: {order.UserEmail}");
        });
    }

    private void ComposeOrderInfo(IContainer container, OrderDetail order)
    {
        var orderDate = order.OrderDateLocal == DateTime.MinValue
            ? "–"
            : order.OrderDateLocal.ToString("yyyy. MM. dd. HH:mm");

        container.Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text($"Rendelésszám: {order.DisplayOrderNumber}").Bold();
                col.Item().Text($"Rendelés dátuma: {orderDate}");
            });
            row.RelativeItem().Column(col =>
            {
                col.Item().Text($"Állapot:    {order.DisplayStatus}");
                col.Item().Text($"Fizetés:    {order.PaymentStatusDisplay}");
                col.Item().Text($"Szállítás:  {order.ShippingDisplay}");
            });
        });
    }

    /// <summary>Teljes tételes termék-tábla — akkor használjuk, ha vannak tételek.</summary>
    private void ComposeItemsTable(IContainer container, OrderDetail order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(5);   // Termék neve
                cols.RelativeColumn(2);   // SKU
                cols.ConstantColumn(45);  // Db
                cols.RelativeColumn(2);   // Egységár
                cols.RelativeColumn(2);   // Összeg
            });

            table.Header(header =>
            {
                static void H(IContainer c, string text, bool right = false)
                {
                    var cell = c.Background(Colors.Grey.Darken2).Padding(5);
                    if (right) cell.AlignRight().Text(text).Bold().FontColor(Colors.White).FontSize(9);
                    else       cell.Text(text).Bold().FontColor(Colors.White).FontSize(9);
                }
                H(header.Cell(), "Termék neve");
                H(header.Cell(), "SKU");
                H(header.Cell(), "Db", right: true);
                H(header.Cell(), "Egységár", right: true);
                H(header.Cell(), "Összeg", right: true);
            });

            bool alt = false;
            foreach (var item in order.Items)
            {
                var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                alt = !alt;
                table.Cell().Background(bg).Padding(5).Column(c =>
                {
                    c.Item().Text(item.DisplayName);
                    // VariantDisplay: a ProductShortDescription HTML-jéből kinyert opció-szöveg.
                    if (!string.IsNullOrWhiteSpace(item.VariantDisplay))
                        c.Item().Text(item.VariantDisplay).FontSize(8).FontColor(Colors.Grey.Medium);
                });
                table.Cell().Background(bg).Padding(5).Text(item.Sku).FontSize(9);
                table.Cell().Background(bg).Padding(5).AlignRight().Text(item.Quantity.ToString());
                table.Cell().Background(bg).Padding(5).AlignRight().Text($"{item.UnitPrice:N0} Ft");
                table.Cell().Background(bg).Padding(5).AlignRight().Text($"{item.LineTotalResolved:N0} Ft");
            }
        });
    }

    /// <summary>
    /// Akkor jelenik meg, ha az Items üres — vagy az API nem adott vissza
    /// tételeket, vagy a tétel-betöltés sikertelen volt. A számla ettől
    /// még legenerálódik összesítő dokumentumként.
    /// </summary>
    private void ComposeNoItemsNotice(IContainer container)
    {
        container
            .Background(Colors.Orange.Lighten4)
            .Border(1).BorderColor(Colors.Orange.Lighten1)
            .Padding(10)
            .Column(col =>
            {
                col.Item().Text("Megjegyzés a számlához").Bold().FontSize(10);
                col.Item().PaddingTop(5).Text(
                    "Ehhez a rendeléshez nem állnak rendelkezésre tételes sorok. " +
                    "Ez a számla csak az összesítő rendelés-adatokat tartalmazza " +
                    "(rendelésszám, vevő adatok, végösszeg). " +
                    "A tételes kimutatáshoz ellenőrizze a Hotcakes Commerce admin felületét.");
            });
    }

    private void ComposeTotals(IContainer container, OrderDetail order)
    {
        container.AlignRight().Column(col =>
        {
            col.Spacing(4);

            void Row(string label, string value, bool bold = false)
            {
                col.Item().Row(r =>
                {
                    var lbl = r.ConstantItem(200).AlignRight().Text(label);
                    var val = r.ConstantItem(110).AlignRight().Text(value);
                    if (bold) { lbl.Bold(); val.Bold(); }
                    else lbl.FontColor(Colors.Grey.Medium);
                });
            }

            // Csak akkor mutatjuk a részletezést, ha van értelmes adat.
            if (order.TotalOrderBeforeDiscounts > 0 &&
                order.TotalOrderBeforeDiscounts != order.TotalGrand)
                Row("Rendelés összege (kedvezmény előtt):", $"{order.TotalOrderBeforeDiscounts:N0} Ft");

            if (order.TotalOrderDiscounts > 0)
                Row("Kedvezmény:", $"–{order.TotalOrderDiscounts:N0} Ft");

            if (order.TotalShippingBeforeDiscounts > 0)
                Row("Szállítási díj:", $"{order.TotalShippingBeforeDiscounts:N0} Ft");

            if (order.TotalShippingDiscounts > 0)
                Row("Szállítási kedvezmény:", $"–{order.TotalShippingDiscounts:N0} Ft");

            if (order.TotalHandling > 0)
                Row("Kezelési díj:", $"{order.TotalHandling:N0} Ft");

            if (order.TotalTax > 0)
                Row("ÁFA:", $"{order.TotalTax:N0} Ft");

            col.Item().PaddingVertical(4).BorderTop(1).BorderColor(Colors.Grey.Medium);
            Row("VÉGÖSSZEG:", $"{order.TotalGrand:N0} Ft", bold: true);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        // A lábjegyzet disclaimer a companysettings.json-ból jön (InvoiceFooterNote).
        // Hagyd üresen a JSON-ban, hogy teljesen elrejtsd a disclaimert.
        var note = _company.InvoiceFooterNote ?? "";

        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(6).Row(row =>
        {
            if (!string.IsNullOrWhiteSpace(note))
                row.RelativeItem().Text(note).FontSize(7).FontColor(Colors.Grey.Medium);
            else
                row.RelativeItem().Text(""); // tartsa a sort kiegyensúlyozottan akkor is, ha üres

            row.ConstantItem(60).AlignRight().Text(t =>
            {
                t.Span("Oldal ").FontSize(7);
                t.CurrentPageNumber().FontSize(7);
                t.Span(" / ").FontSize(7);
                t.TotalPages().FontSize(7);
            });
        });
    }
}
