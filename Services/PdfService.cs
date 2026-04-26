using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Orchestrates PDF generation.
/// This is the single entry point for all PDF operations in the application.
///
/// InvoiceTemplateService — generates A4 invoice PDFs (uses CompanySettings for seller block)
/// LabelTemplateService   — generates A6 shipping label PDFs (uses ZXing.Net barcode)
///
/// Both services save their output under Documents\GeneratedFiles\
/// and return the absolute path to the created file.
/// </summary>
public class PdfService
{
    private readonly InvoiceTemplateService _invoiceService;
    private readonly LabelTemplateService _labelService = new();

    /// <summary>
    /// Constructs the service with an explicit CompanySettings instance.
    /// Pass in the one loaded at app startup so the json file is only read once.
    /// </summary>
    public PdfService(CompanySettings company)
    {
        _invoiceService = new InvoiceTemplateService(company);
    }

    /// <summary>Parameterless constructor — reads companysettings.json from disk.</summary>
    public PdfService() : this(CompanySettings.LoadOrCreate()) { }

    /// <summary>
    /// Generates an invoice PDF for the given order.
    /// Returns the absolute path of the saved PDF file.
    /// </summary>
    public string GenerateInvoice(OrderDetail order) =>
        _invoiceService.GenerateInvoice(order);

    /// <summary>
    /// Generates a shipping label PDF for the given order.
    /// Returns the absolute path of the saved PDF file.
    /// </summary>
    public string GenerateLabel(OrderDetail order) =>
        _labelService.GenerateLabel(order);
}
