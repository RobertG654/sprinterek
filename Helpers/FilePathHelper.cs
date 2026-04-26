namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// Centralizes all file path logic for generated PDF files.
/// Output root: Documents\GeneratedFiles\
///   Invoices: Documents\GeneratedFiles\Invoices\
///   Labels:   Documents\GeneratedFiles\Labels\
/// </summary>
public static class FilePathHelper
{
    private static readonly string BaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "GeneratedFiles");

    public static string InvoiceDir => Path.Combine(BaseDir, "Invoices");
    public static string LabelDir => Path.Combine(BaseDir, "Labels");

    public static void EnsureInvoiceDirectory() => Directory.CreateDirectory(InvoiceDir);
    public static void EnsureLabelDirectory() => Directory.CreateDirectory(LabelDir);

    /// <summary>
    /// Returns the full output path for an invoice PDF.
    /// Format: Szamla_{OrderId}_{yyyyMMdd_HHmmss}.pdf
    /// Also ensures the Invoices directory exists.
    /// </summary>
    public static string GetInvoicePath(string orderId)
    {
        EnsureInvoiceDirectory();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeId = SanitizeFileName(orderId);
        return Path.Combine(InvoiceDir, $"Szamla_{safeId}_{timestamp}.pdf");
    }

    /// <summary>
    /// Returns the full output path for a shipping label PDF.
    /// Format: Cimke_{OrderId}_{yyyyMMdd_HHmmss}.pdf
    /// Also ensures the Labels directory exists.
    /// </summary>
    public static string GetLabelPath(string orderId)
    {
        EnsureLabelDirectory();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeId = SanitizeFileName(orderId);
        return Path.Combine(LabelDir, $"Cimke_{safeId}_{timestamp}.pdf");
    }

    /// <summary>Strips characters that are invalid in Windows file names.</summary>
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
