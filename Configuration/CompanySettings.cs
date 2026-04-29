using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Configuration;

/// <summary>
/// Company information used as the "seller" (ELADÓ) block on generated invoices.
///
/// Loaded from <c>companysettings.json</c> sitting next to the executable
/// (AppContext.BaseDirectory). If the file does not exist on first run it is
/// created automatically with placeholder default values so the user has a
/// template to edit.
///
/// USAGE:
///   var company = CompanySettings.LoadOrCreate();
///   // then pass to InvoiceTemplateService
///
/// SERIALIZATION:
///   Uses the default System.Text.Json serializer with pretty-printing.
///   File is UTF-8 encoded.
/// </summary>
public class CompanySettings
{
    // ── Identity ──────────────────────────────────────────────────────────
    /// <summary>Company legal name that appears as "ELADÓ" on the invoice.</summary>
    public string CompanyName { get; set; } = "Paw Promise Kft.";

    /// <summary>Hungarian tax/VAT number (Adószám).</summary>
    public string TaxNumber { get; set; } = "12345678-1-41";

    /// <summary>
    /// Company registration number (Cégjegyzékszám). Optional — when empty,
    /// the line is omitted from the invoice.
    /// </summary>
    public string RegistrationNumber { get; set; } = "";

    // ── Contact ───────────────────────────────────────────────────────────
    /// <summary>Postal address of the company (single line).</summary>
    public string Address { get; set; } = "Minta utca 1., 1000 Budapest";

    /// <summary>Contact email printed on the invoice.</summary>
    public string Email { get; set; } = "info@pawpromise.hu";

    /// <summary>Contact phone number printed on the invoice.</summary>
    public string Phone { get; set; } = "+36 1 234 5678";

    /// <summary>Company website. Optional — when empty, the line is omitted from the invoice.</summary>
    public string Website { get; set; } = "http://4.231.236.217/";

    // ── Banking ───────────────────────────────────────────────────────────
    /// <summary>Optional bank name printed alongside the bank account number.</summary>
    public string BankName { get; set; } = "";

    /// <summary>Optional bank account number. When empty, the line is omitted from the invoice.</summary>
    public string BankAccount { get; set; } = "";

    // ── Invoice document text (customizable disclaimers / titles) ─────────
    /// <summary>Heading printed on the invoice (default: "SZÁMLA").</summary>
    public string InvoiceTitle { get; set; } = "SZÁMLA";

    /// <summary>
    /// Optional subtitle below the invoice heading (e.g., "Demo bizonylat —
    /// nem hivatalos számla"). Set to empty string to remove the disclaimer.
    /// </summary>
    public string InvoiceSubtitle { get; set; } = "Demo bizonylat — nem hivatalos számla";

    /// <summary>
    /// Prefix used in the auto-generated invoice ID printed in the top-right
    /// corner. Default "DEMO" — replace with your company's invoice prefix
    /// (e.g., "INV", "SZ", "2026-").
    /// </summary>
    public string InvoiceIdPrefix { get; set; } = "DEMO";

    /// <summary>
    /// Disclaimer text printed in the page footer. Set to empty string to
    /// hide the footer note completely.
    /// </summary>
    public string InvoiceFooterNote { get; set; } =
        "Ez egy DEMO számla. Nem érvényes pénzügyi bizonylat. " +
        "Jogi megfelelőséghez hitelesített számlázó program szükséges.";

    // ──────────────────────────────────────────────────────────────────────
    // Persistence
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Absolute path to the companysettings.json file — always next to the executable.
    /// </summary>
    [JsonIgnore]
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "companysettings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null // keep PascalCase property names for readability
    };

    /// <summary>
    /// Loads company settings from disk. Creates the file with default values
    /// if it is missing, so the user can just edit it afterwards.
    /// Also re-saves the file when deserialization fails (corrupted JSON)
    /// — replacing it with a valid default template.
    ///
    /// <para>If the file exists but is missing newly-introduced fields (e.g.,
    /// after an app upgrade), the file is re-saved so all available fields
    /// become visible for editing without the user having to add them by hand.</para>
    /// </summary>
    public static CompanySettings LoadOrCreate()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                var defaults = new CompanySettings();
                defaults.Save();
                return defaults;
            }

            var json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<CompanySettings>(json, JsonOptions);
            if (loaded == null)
            {
                var defaults = new CompanySettings();
                defaults.Save();
                return defaults;
            }

            // Detect "old" JSON files written before new fields were added by
            // counting top-level property keys against the current model. When
            // they differ, re-serialise so the file gains the missing fields
            // populated with their C# defaults — making them editable.
            try
            {
                using var doc = JsonDocument.Parse(json);
                var diskKeys = doc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
                var modelKeys = typeof(CompanySettings)
                    .GetProperties()
                    .Where(p => p.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length == 0)
                    .Select(p => p.Name)
                    .ToHashSet();
                if (!modelKeys.IsSubsetOf(diskKeys))
                    loaded.Save();
            }
            catch { /* parsing the disk JSON for diff is best-effort only */ }

            return loaded;
        }
        catch (Exception)
        {
            // On any I/O or JSON error, fall back to defaults without crashing the app.
            return new CompanySettings();
        }
    }

    /// <summary>Writes the current settings to disk as pretty-printed JSON.</summary>
    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
