using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Forms;
using Microsoft.Extensions.Configuration;
using QuestPDF.Infrastructure;

namespace HotcakesWinFormsApp;

internal static class Program
{
    /// <summary>
    /// Application settings loaded from appsettings.json.
    /// Populated at startup and then overlaid with values from apisettings.json
    /// (if present) so the user's API connection data takes precedence.
    /// Accessible application-wide via <see cref="Settings"/>.
    /// </summary>
    public static AppSettings Settings { get; private set; } = new();

    /// <summary>
    /// Company data used as the "seller" block on generated invoices.
    /// Loaded from companysettings.json on startup; the file is auto-created
    /// with defaults on first run.
    /// </summary>
    public static CompanySettings Company { get; private set; } = new();

    [STAThread]
    static void Main()
    {
        // QuestPDF community license — required for free non-commercial use.
        // If you use this in a commercial product, check https://www.questpdf.com/license/
        QuestPDF.Settings.License = LicenseType.Community;

        ApplicationConfiguration.Initialize();

        // ── 1. Load appsettings.json from the application's output directory ──
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var settings = new AppSettings();
        config.Bind(settings);
        Settings = settings;

        // ── 2. Overlay user-editable API settings (apisettings.json) ──────────
        // Values saved via ApiSettingsForm take precedence over appsettings.json.
        var apiStore = ApiSettingsStore.Load();
        if (!string.IsNullOrWhiteSpace(apiStore.BaseUrl))
            Settings.Hotcakes.BaseUrl = apiStore.BaseUrl;
        if (!string.IsNullOrWhiteSpace(apiStore.ApiBasePath))
            Settings.Hotcakes.ApiBasePath = apiStore.ApiBasePath;
        if (!string.IsNullOrWhiteSpace(apiStore.ApiKey))
            Settings.Hotcakes.ApiKey = apiStore.ApiKey;

        // ── 3. Load company settings (auto-creates file on first run) ─────────
        Company = CompanySettings.LoadOrCreate();

        // ── 4. If no API key is configured, open the settings form first ──────
        // Only enforced when NOT in mock mode — mock demos don't need an API key.
        if (!Settings.UseMockData && !HasUsableApiKey())
        {
            using var apiForm = new ApiSettingsForm();
            var result = apiForm.ShowDialog();

            // If the user cancelled the settings form, exit the app.
            if (result != DialogResult.OK || !HasUsableApiKey())
                return;
        }

        Application.Run(new MainForm());
    }

    /// <summary>
    /// True when the current API key looks usable (non-empty and not the
    /// "PASTE_YOUR_API_KEY_HERE" placeholder shipped with the template).
    /// </summary>
    private static bool HasUsableApiKey()
    {
        var key = Settings.Hotcakes.ApiKey;
        return !string.IsNullOrWhiteSpace(key) && key != "PASTE_YOUR_API_KEY_HERE";
    }
}
