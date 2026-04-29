using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Forms;
using Microsoft.Extensions.Configuration;
using QuestPDF.Infrastructure;

namespace HotcakesWinFormsApp;

internal static class Program
{
    /// <summary>
    /// Az appsettings.json-ból betöltött alkalmazás-szintű beállítások.
    /// Induláskor töltődik fel, majd az apisettings.json (ha létezik)
    /// értékei felülírják, hogy a felhasználó API kapcsolati adatai
    /// elsőbbséget élvezzenek.
    /// Az alkalmazásban a <see cref="Settings"/>-en keresztül érhető el.
    /// </summary>
    public static AppSettings Settings { get; private set; } = new();

    /// <summary>
    /// A generált számlák „eladó" blokkjához használt cégadatok.
    /// Induláskor a companysettings.json-ból töltődik be; a fájl az első
    /// futtatáskor automatikusan létrejön alapértelmezett értékekkel.
    /// </summary>
    public static CompanySettings Company { get; private set; } = new();

    [STAThread]
    static void Main()
    {
        // QuestPDF community licenc — szabad, nem kereskedelmi használathoz
        // szükséges. Ha kereskedelmi termékben használod, ellenőrizd a
        // https://www.questpdf.com/license/ oldalt.
        QuestPDF.Settings.License = LicenseType.Community;

        ApplicationConfiguration.Initialize();

        // ── 1. appsettings.json betöltése az alkalmazás kimeneti mappájából ──
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var settings = new AppSettings();
        config.Bind(settings);
        Settings = settings;

        // ── 2. A felhasználó által szerkeszthető API beállítások felülírása (apisettings.json) ──
        // Az ApiSettingsForm-on keresztül mentett értékek elsőbbséget élveznek
        // az appsettings.json-ban szereplőkkel szemben.
        var apiStore = ApiSettingsStore.Load();
        if (!string.IsNullOrWhiteSpace(apiStore.BaseUrl))
            Settings.Hotcakes.BaseUrl = apiStore.BaseUrl;
        if (!string.IsNullOrWhiteSpace(apiStore.ApiBasePath))
            Settings.Hotcakes.ApiBasePath = apiStore.ApiBasePath;
        if (!string.IsNullOrWhiteSpace(apiStore.ApiKey))
            Settings.Hotcakes.ApiKey = apiStore.ApiKey;

        // ── 3. Cégadatok betöltése (a fájl auto-létrehozás az első futtatáskor) ──
        Company = CompanySettings.LoadOrCreate();

        // ── 4. Ha nincs beállítva API kulcs, először a beállító ablakot nyitjuk meg ──
        // Csak akkor érvényesítjük, ha nem mock módban vagyunk — a mock demo-hoz
        // nincs szükség API kulcsra.
        if (!Settings.UseMockData && !HasUsableApiKey())
        {
            using var apiForm = new ApiSettingsForm();
            var result = apiForm.ShowDialog();

            // Ha a felhasználó mégse-t nyomott a beállító ablakon, kilépünk.
            if (result != DialogResult.OK || !HasUsableApiKey())
                return;
        }

        Application.Run(new MainForm());
    }

    /// <summary>
    /// Igaz, ha a jelenlegi API kulcs használhatónak tűnik (nem üres és nem a
    /// sablonnal érkező „PASTE_YOUR_API_KEY_HERE" placeholder).
    /// </summary>
    private static bool HasUsableApiKey()
    {
        var key = Settings.Hotcakes.ApiKey;
        return !string.IsNullOrWhiteSpace(key) && key != "PASTE_YOUR_API_KEY_HERE";
    }
}
