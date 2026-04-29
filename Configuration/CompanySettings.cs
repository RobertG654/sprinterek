using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Configuration;

/// <summary>
/// A generált számlák „ELADÓ" blokkjához használt cégadatok.
///
/// A futtatható mellett található <c>companysettings.json</c>-ból töltődik be
/// (AppContext.BaseDirectory). Ha a fájl az első induláskor nem létezik,
/// automatikusan létrejön placeholder alapértelmezett értékekkel, hogy a
/// felhasználónak legyen egy szerkeszthető sablonja.
///
/// HASZNÁLAT:
///   var company = CompanySettings.LoadOrCreate();
///   // majd átadás az InvoiceTemplateService-nek
///
/// SZERIALIZÁCIÓ:
///   Az alapértelmezett System.Text.Json szerializálót használja, szépen
///   formázott (indented) kimenettel. A fájl UTF-8 kódolású.
/// </summary>
public class CompanySettings
{
    // ── Cég azonosító adatok ──────────────────────────────────────────────
    /// <summary>A cég hivatalos neve, ami „ELADÓ"-ként jelenik meg a számlán.</summary>
    public string CompanyName { get; set; } = "Demo Webshop Kft.";

    /// <summary>Magyar adószám / áfa szám (Adószám).</summary>
    public string TaxNumber { get; set; } = "12345678-2-41";

    /// <summary>
    /// Cégjegyzékszám. Opcionális — ha üres, a sor kimarad a számláról.
    /// </summary>
    public string RegistrationNumber { get; set; } = "";

    // ── Kapcsolat ─────────────────────────────────────────────────────────
    /// <summary>A cég postacíme (egy sorban).</summary>
    public string Address { get; set; } = "Minta utca 1., 1000 Budapest";

    /// <summary>A számlán szereplő kapcsolattartási e-mail.</summary>
    public string Email { get; set; } = "info@pawpromise.hu";

    /// <summary>A számlán szereplő kapcsolattartási telefonszám.</summary>
    public string Phone { get; set; } = "+36 1 234 5678";

    /// <summary>Cég weboldal. Opcionális — ha üres, a sor kimarad a számláról.</summary>
    public string Website { get; set; } = "";

    // ── Banki adatok ──────────────────────────────────────────────────────
    /// <summary>A bankszámlaszám mellett opcionálisan kiírt bank neve.</summary>
    public string BankName { get; set; } = "";

    /// <summary>Opcionális bankszámlaszám. Ha üres, a sor kimarad a számláról.</summary>
    public string BankAccount { get; set; } = "";

    // ── Számla dokumentum testreszabható szövegei ─────────────────────────
    /// <summary>A számla bal felső sarkában megjelenő nagy cím (alapértelmezetten „SZÁMLA").</summary>
    public string InvoiceTitle { get; set; } = "SZÁMLA";

    /// <summary>
    /// Opcionális alcím a számla cím alatt (pl. „Demo bizonylat — nem hivatalos
    /// számla"). Üres karakterláncra állítva eltűnik a disclaimer.
    /// </summary>
    public string InvoiceSubtitle { get; set; } = "Demo bizonylat — nem hivatalos számla";

    /// <summary>
    /// A jobb felső sarokban automatikusan generált számla-azonosító előtagja.
    /// Alapértelmezett: „DEMO" — érdemes a saját cég számla-előtagjára cserélni
    /// (pl. „INV", „SZ", „2026-").
    /// </summary>
    public string InvoiceIdPrefix { get; set; } = "DEMO";

    /// <summary>
    /// A lap alján megjelenő disclaimer szöveg. Üres karakterlánc esetén a
    /// lábjegyzet teljesen kimarad.
    /// </summary>
    public string InvoiceFooterNote { get; set; } =
        "Ez egy DEMO számla. Nem érvényes pénzügyi bizonylat. " +
        "Jogi megfelelőséghez hitelesített számlázó program szükséges.";

    // ──────────────────────────────────────────────────────────────────────
    // Perzisztencia
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A companysettings.json abszolút útvonala — mindig a futtatható mellett.
    /// </summary>
    [JsonIgnore]
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "companysettings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null // PascalCase tulajdonságnevek megtartása az olvashatóság érdekében
    };

    /// <summary>
    /// Betölti a cégadatokat a lemezről. Ha a fájl hiányzik, alapértelmezett
    /// értékekkel létrehozza, hogy a felhasználó utána már csak szerkeszteni
    /// kelljen. Sérült (nem-deszerializálható) JSON esetén szintén újraírja —
    /// érvényes alapértelmezett sablonra cserélve.
    ///
    /// <para>Ha a fájl létezik, de újonnan bevezetett mezők hiányoznak belőle
    /// (pl. egy app-frissítés után), a fájl újra mentődik, hogy minden elérhető
    /// mező látható és szerkeszthető legyen anélkül, hogy a felhasználónak
    /// kézzel kellene felvennie őket.</para>
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

            // „Régi" JSON fájlok detektálása: a lemezen lévő gyökér-szintű kulcsok
            // halmazát összevetjük a jelenlegi modell tulajdonságaival. Ha
            // különböznek, újra szerializálunk, hogy a fájl megkapja a hiányzó
            // mezőket a C# alapértelmezett értékeikkel — szerkeszthetővé téve őket.
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
            catch { /* a lemez JSON parse-olása csak best-effort összehasonlítás miatt fut */ }

            return loaded;
        }
        catch (Exception)
        {
            // Bármilyen IO vagy JSON hibára visszaesünk az alapértelmezettekre,
            // anélkül hogy az alkalmazás összeomlana.
            return new CompanySettings();
        }
    }

    /// <summary>A jelenlegi beállításokat szépen formázott JSON-ként a lemezre írja.</summary>
    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
