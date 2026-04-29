using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// A PDF generálást orchestrálja.
/// Ez az alkalmazás összes PDF műveletének egyetlen belépési pontja.
///
/// InvoiceTemplateService — A4 számla PDF-ek (a CompanySettings-et használja az „eladó" blokkhoz)
/// LabelTemplateService   — A6 szállítási címke PDF-ek (ZXing.Net vonalkóddal)
///
/// Mindkét szerviz a Dokumentumok\GeneratedFiles\ alá menti a kimenetét, és
/// visszaadja a létrehozott fájl abszolút útvonalát.
/// </summary>
public class PdfService
{
    private readonly InvoiceTemplateService _invoiceService;
    private readonly LabelTemplateService _labelService = new();

    /// <summary>
    /// Létrehozza a szervizt egy explicit CompanySettings példánnyal.
    /// Add át az induláskor betöltött példányt, hogy a json fájlt sessionönként
    /// csak egyszer olvassuk be.
    /// </summary>
    public PdfService(CompanySettings company)
    {
        _invoiceService = new InvoiceTemplateService(company);
    }

    /// <summary>Paraméter nélküli konstruktor — a companysettings.json-t a lemezről tölti be.</summary>
    public PdfService() : this(CompanySettings.LoadOrCreate()) { }

    /// <summary>
    /// Számla PDF-et generál a megadott rendeléshez.
    /// Visszaadja az elmentett PDF fájl abszolút útvonalát.
    /// </summary>
    public string GenerateInvoice(OrderDetail order) =>
        _invoiceService.GenerateInvoice(order);

    /// <summary>
    /// Szállítási címke PDF-et generál a megadott rendeléshez.
    /// Visszaadja az elmentett PDF fájl abszolút útvonalát.
    /// </summary>
    public string GenerateLabel(OrderDetail order) =>
        _labelService.GenerateLabel(order);
}
