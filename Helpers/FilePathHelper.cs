namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// A generált PDF fájlok útvonalkezelését központosítja.
/// Kimeneti gyökér: Dokumentumok\GeneratedFiles\
///   Számlák:        Dokumentumok\GeneratedFiles\Invoices\
///   Címkék:         Dokumentumok\GeneratedFiles\Labels\
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
    /// Visszaadja a számla PDF teljes kimeneti útvonalát.
    /// Formátum: Szamla_{OrderId}_{yyyyMMdd_HHmmss}.pdf
    /// Egyúttal biztosítja, hogy az Invoices könyvtár létezzen.
    /// </summary>
    public static string GetInvoicePath(string orderId)
    {
        EnsureInvoiceDirectory();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeId = SanitizeFileName(orderId);
        return Path.Combine(InvoiceDir, $"Szamla_{safeId}_{timestamp}.pdf");
    }

    /// <summary>
    /// Visszaadja a szállítási címke PDF teljes kimeneti útvonalát.
    /// Formátum: Cimke_{OrderId}_{yyyyMMdd_HHmmss}.pdf
    /// Egyúttal biztosítja, hogy a Labels könyvtár létezzen.
    /// </summary>
    public static string GetLabelPath(string orderId)
    {
        EnsureLabelDirectory();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeId = SanitizeFileName(orderId);
        return Path.Combine(LabelDir, $"Cimke_{safeId}_{timestamp}.pdf");
    }

    /// <summary>Eltávolítja a Windows fájlnévben érvénytelen karaktereket.</summary>
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
