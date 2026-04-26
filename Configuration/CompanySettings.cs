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
    /// <summary>Company legal name that appears as "ELADÓ" on the invoice.</summary>
    public string CompanyName { get; set; } = "Demo Webshop Kft.";

    /// <summary>Hungarian tax/VAT number (Adószám).</summary>
    public string TaxNumber { get; set; } = "12345678-2-41";

    /// <summary>Postal address of the company (single line).</summary>
    public string Address { get; set; } = "Minta utca 1., 1000 Budapest";

    /// <summary>Contact email printed on the invoice.</summary>
    public string Email { get; set; } = "info@demowebshop.hu";

    /// <summary>Contact phone number printed on the invoice.</summary>
    public string Phone { get; set; } = "+36 1 234 5678";

    /// <summary>Optional bank account number. When empty, the line is omitted from the invoice.</summary>
    public string BankAccount { get; set; } = "";

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
