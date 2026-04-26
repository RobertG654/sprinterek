using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Services;

namespace HotcakesWinFormsApp.Forms;

/// <summary>
/// Replacement for the old LoginForm.
///
/// Instead of local username/password authentication, the user now configures
/// the Hotcakes REST API connection directly:
///   • Base URL    — e.g. http://4.231.236.217/
///   • API Path    — e.g. DesktopModules/Hotcakes/API/rest/v1/
///   • API Key     — pasted from DNN > Hotcakes > Configuration > API
///
/// Behavior:
///   • Loads existing values from apisettings.json on open.
///   • "Kapcsolat tesztelése" (Test connection) calls GET /orders with the
///     entered values and reports success or the mapped error message.
///   • "Mentés és folytatás" saves the values to apisettings.json, updates
///     the global <c>Program.Settings.Hotcakes</c>, and closes with
///     DialogResult.OK so the caller can proceed to MainForm.
///
/// This form is shown automatically on startup when no API key has been saved
/// yet, and can also be reopened later by the user (if a menu entry is added).
/// </summary>
public class ApiSettingsForm : Form
{
    // ── UI ───────────────────────────────────────────────────────────────────
    private Label _lblTitle = null!;
    private Label _lblSubtitle = null!;

    private Label _lblBaseUrl = null!;
    private TextBox _txtBaseUrl = null!;

    private Label _lblApiPath = null!;
    private TextBox _txtApiPath = null!;

    private Label _lblApiKey = null!;
    private TextBox _txtApiKey = null!;

    private Button _btnTest = null!;
    private Button _btnSave = null!;
    private Label _lblStatus = null!;

    // ── State ────────────────────────────────────────────────────────────────
    private readonly ApiSettingsStore _store;

    public ApiSettingsForm()
    {
        // Load existing settings (file may not exist — defaults are blank)
        _store = ApiSettingsStore.Load();

        // Seed blank fields from appsettings.json so the user has sensible defaults
        if (string.IsNullOrWhiteSpace(_store.BaseUrl))
            _store.BaseUrl = Program.Settings.Hotcakes.BaseUrl;
        if (string.IsNullOrWhiteSpace(_store.ApiBasePath))
            _store.ApiBasePath = Program.Settings.Hotcakes.ApiBasePath;
        if (string.IsNullOrWhiteSpace(_store.ApiKey))
            _store.ApiKey = Program.Settings.Hotcakes.ApiKey;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "API kapcsolat beállítása";
        ClientSize = new Size(480, 360);
        MinimumSize = new Size(480, 360);
        MaximumSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9f);

        // ── Title / subtitle ──────────────────────────────────────────────
        _lblTitle = new Label
        {
            Text = "Hotcakes API beállítások",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 15)
        };
        _lblSubtitle = new Label
        {
            Text = "Adja meg a Hotcakes Commerce REST API elérési adatait.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(90, 90, 110),
            AutoSize = true,
            Location = new Point(20, 42)
        };

        // ── Base URL field ────────────────────────────────────────────────
        _lblBaseUrl = new Label { Text = "Alap URL:", AutoSize = true, Location = new Point(20, 80) };
        _txtBaseUrl = new TextBox
        {
            Location = new Point(20, 100),
            Width = 440,
            Text = _store.BaseUrl
        };

        // ── API path field ────────────────────────────────────────────────
        _lblApiPath = new Label { Text = "API elérési út:", AutoSize = true, Location = new Point(20, 135) };
        _txtApiPath = new TextBox
        {
            Location = new Point(20, 155),
            Width = 440,
            Text = _store.ApiBasePath
        };

        // ── API key field ─────────────────────────────────────────────────
        _lblApiKey = new Label { Text = "API kulcs:", AutoSize = true, Location = new Point(20, 190) };
        _txtApiKey = new TextBox
        {
            Location = new Point(20, 210),
            Width = 440,
            Text = _store.ApiKey,
            UseSystemPasswordChar = true  // masked — treat it like a secret
        };

        // ── Buttons ───────────────────────────────────────────────────────
        _btnTest = new Button
        {
            Text = "Kapcsolat tesztelése",
            Location = new Point(20, 255),
            Size = new Size(180, 32)
        };
        _btnTest.Click += async (_, _) => await TestConnectionAsync();

        _btnSave = new Button
        {
            Text = "Mentés és folytatás",
            Location = new Point(280, 255),
            Size = new Size(180, 32)
        };
        _btnSave.Click += (_, _) => SaveAndClose();

        // ── Status label (shared for test results + validation) ──────────
        _lblStatus = new Label
        {
            AutoSize = false,
            Location = new Point(20, 300),
            Size = new Size(440, 50),
            ForeColor = Color.FromArgb(60, 60, 90),
            Font = new Font("Segoe UI", 8.5f)
        };

        Controls.AddRange(new Control[]
        {
            _lblTitle, _lblSubtitle,
            _lblBaseUrl, _txtBaseUrl,
            _lblApiPath, _txtApiPath,
            _lblApiKey, _txtApiKey,
            _btnTest, _btnSave, _lblStatus
        });

        // Pressing Enter in the API key field triggers save
        _txtApiKey.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) SaveAndClose();
        };
    }

    // ──────────────────────────────────────────────────────────────────────
    // Actions
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls GET /orders with the currently-entered settings and reports the
    /// outcome. Uses a temporary AppSettings so the user can test unsaved values.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        if (!ValidateInputs(requireApiKey: true)) return;

        _btnTest.Enabled = false;
        _btnSave.Enabled = false;
        SetStatus("Kapcsolat tesztelése folyamatban...", Color.FromArgb(60, 60, 90));

        try
        {
            // Build a one-off AppSettings with the currently-entered values,
            // not the persisted ones — so the user can test before saving.
            var probeSettings = new AppSettings
            {
                Hotcakes = new HotcakesSettings
                {
                    BaseUrl     = _txtBaseUrl.Text.Trim(),
                    ApiBasePath = _txtApiPath.Text.Trim(),
                    ApiKey      = _txtApiKey.Text.Trim()
                },
                UseMockData = false
            };

            var service = new HotcakesApiService(probeSettings);
            var (orders, error) = await service.GetOrdersAsync(pageNumber: 1, pageSize: 1);

            if (error != null)
                SetStatus($"Sikertelen kapcsolat: {error}", Color.Crimson);
            else
                SetStatus($"Sikeres kapcsolat. Rendelések elérhetők ({orders.Count} minta válaszban).",
                          Color.DarkGreen);
        }
        catch (Exception ex)
        {
            SetStatus($"Váratlan hiba: {ex.Message}", Color.Crimson);
        }
        finally
        {
            _btnTest.Enabled = true;
            _btnSave.Enabled = true;
        }
    }

    /// <summary>
    /// Persists the entered settings to apisettings.json, mirrors them into
    /// the global Program.Settings.Hotcakes, and closes with DialogResult.OK.
    /// </summary>
    private void SaveAndClose()
    {
        if (!ValidateInputs(requireApiKey: true)) return;

        _store.BaseUrl     = _txtBaseUrl.Text.Trim();
        _store.ApiBasePath = _txtApiPath.Text.Trim();
        _store.ApiKey      = _txtApiKey.Text.Trim();

        try
        {
            _store.Save();
        }
        catch (Exception ex)
        {
            MessageHelper.ShowError(
                $"Nem sikerült menteni az API beállításokat:\n\n{ex.Message}",
                "Mentési hiba");
            return;
        }

        // Mirror into the live AppSettings so the rest of the app uses the
        // just-saved values without a restart.
        Program.Settings.Hotcakes.BaseUrl     = _store.BaseUrl;
        Program.Settings.Hotcakes.ApiBasePath = _store.ApiBasePath;
        Program.Settings.Hotcakes.ApiKey      = _store.ApiKey;

        DialogResult = DialogResult.OK;
        Close();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private bool ValidateInputs(bool requireApiKey)
    {
        if (string.IsNullOrWhiteSpace(_txtBaseUrl.Text))
        {
            SetStatus("Az Alap URL mező nem lehet üres.", Color.Crimson);
            _txtBaseUrl.Focus();
            return false;
        }
        if (string.IsNullOrWhiteSpace(_txtApiPath.Text))
        {
            SetStatus("Az API elérési út mező nem lehet üres.", Color.Crimson);
            _txtApiPath.Focus();
            return false;
        }
        if (requireApiKey && string.IsNullOrWhiteSpace(_txtApiKey.Text))
        {
            SetStatus("Az API kulcs mező nem lehet üres.", Color.Crimson);
            _txtApiKey.Focus();
            return false;
        }
        return true;
    }

    private void SetStatus(string message, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = message;
    }
}
