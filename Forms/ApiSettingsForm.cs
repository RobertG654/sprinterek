using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Services;
using HotcakesWinFormsApp.UI;

namespace HotcakesWinFormsApp.Forms;

/// <summary>
/// A régi LoginForm helyettesítője.
///
/// A felhasználónév / jelszó alapú helyi hitelesítés helyett a felhasználó
/// most közvetlenül a Hotcakes REST API kapcsolatot konfigurálja:
///   • Alap URL    — pl. http://4.231.236.217/
///   • API útvonal — pl. DesktopModules/Hotcakes/API/rest/v1/
///   • API kulcs   — DNN > Hotcakes > Configuration > API alól bemásolva
///
/// Viselkedés:
///   • Megnyitáskor betölti a meglévő értékeket az apisettings.json-ból.
///   • A „Kapcsolat tesztelése" GET /orders-t hív a beírt értékekkel, és
///     jelenti a sikert vagy a leképezett hibaüzenetet.
///   • A „Mentés és folytatás" elmenti az értékeket az apisettings.json-ba,
///     frissíti a globális <c>Program.Settings.Hotcakes</c>-t, és
///     DialogResult.OK-val zár, hogy a hívó a MainForm-ra folytathasson.
///
/// A megjelenés a MainForm-mal egyezik — bor fejléc, fehér kártya törzs,
/// lekerekített gombok.
/// </summary>
public class ApiSettingsForm : Form
{
    // ── UI ───────────────────────────────────────────────────────────────────
    private AppHeaderBar _header = null!;
    private CardPanel _card = null!;

    private Label _lblTitle = null!;
    private Label _lblSubtitle = null!;

    private Label _lblBaseUrl = null!;
    private TextBox _txtBaseUrl = null!;

    private Label _lblApiPath = null!;
    private TextBox _txtApiPath = null!;

    private Label _lblApiKey = null!;
    private TextBox _txtApiKey = null!;

    private ModernButton _btnTest = null!;
    private ModernButton _btnSave = null!;
    private Label _lblStatus = null!;

    // ── State ────────────────────────────────────────────────────────────────
    private readonly ApiSettingsStore _store;

    public ApiSettingsForm()
    {
        // Meglévő beállítások betöltése (a fájl lehet, hogy nincs — ekkor üres alapértékek)
        _store = ApiSettingsStore.Load();

        // Az üres mezőket az appsettings.json-ból szedjük, hogy legyenek értelmes alapok
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
        // DPI-konzisztens skálázás — az indoklást lásd a MainForm-ban.
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);

        ClientSize = new Size(560, 540);
        MinimumSize = new Size(560, 540);
        MaximumSize = new Size(720, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = Theme.BodyFont;
        BackColor = Theme.PageBg;

        // ── Fejléc csík ────────────────────────────────────────────────────
        _header = new AppHeaderBar
        {
            Title = "PAWPROMISE BEÁLLÍTÁSOK",
            Subtitle = "Hotcakes REST API kapcsolat"
        };
        Controls.Add(_header);

        // ── Kártya törzs ──────────────────────────────────────────────────
        var bodyHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.PageBg,
            Padding = new Padding(24, 20, 24, 20)
        };

        _card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 22, 24, 22)
        };

        _lblTitle = new Label
        {
            Text = "Hotcakes API beállítások",
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            ForeColor = Theme.TextPrimary,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(2, 4)
        };
        _lblSubtitle = new Label
        {
            Text = "Adja meg a Hotcakes Commerce REST API elérési adatait.",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(2, 32)
        };

        // A mező-szélességeket Anchor-ral követjük, így a form szépen skálázódik,
        // ha a felhasználó a megengedett tartományon belül átméretezi.
        const int fieldLeft = 2;
        const int fieldWidth = 488;

        // ── Alap URL mező ─────────────────────────────────────────────────
        _lblBaseUrl = MakeFieldLabel("Alap URL",  new Point(fieldLeft, 70));
        _txtBaseUrl = MakeFieldTextBox(new Point(fieldLeft, 92), fieldWidth, _store.BaseUrl);

        // ── API elérési út mező ───────────────────────────────────────────
        _lblApiPath = MakeFieldLabel("API elérési út", new Point(fieldLeft, 132));
        _txtApiPath = MakeFieldTextBox(new Point(fieldLeft, 154), fieldWidth, _store.ApiBasePath);

        // ── API kulcs mező ────────────────────────────────────────────────
        _lblApiKey = MakeFieldLabel("API kulcs", new Point(fieldLeft, 194));
        _txtApiKey = MakeFieldTextBox(new Point(fieldLeft, 216), fieldWidth, _store.ApiKey);
        _txtApiKey.UseSystemPasswordChar = true;  // maszkolt — kezeljük titokként

        // ── Gombok ────────────────────────────────────────────────────────
        _btnTest = new ModernButton
        {
            Text = "Kapcsolat tesztelése",
            Style = ModernButton.ButtonStyle.Ghost,
            Size = new Size(200, 40),
            Location = new Point(fieldLeft, 268)
        };
        _btnTest.Click += async (_, _) => await TestConnectionAsync();

        _btnSave = new ModernButton
        {
            Text = "Mentés és folytatás",
            Style = ModernButton.ButtonStyle.Primary,
            Size = new Size(200, 40),
            Location = new Point(fieldLeft + fieldWidth - 200, 268)
        };
        _btnSave.Click += (_, _) => SaveAndClose();

        // ── Státusz címke (megosztott a teszt eredményhez és a validációhoz) ──
        _lblStatus = new Label
        {
            AutoSize = false,
            Location = new Point(fieldLeft, 322),
            Size = new Size(fieldWidth, 60),
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            Font = Theme.SmallFont
        };

        _card.Controls.AddRange(new Control[]
        {
            _lblTitle, _lblSubtitle,
            _lblBaseUrl, _txtBaseUrl,
            _lblApiPath, _txtApiPath,
            _lblApiKey, _txtApiKey,
            _btnTest, _btnSave, _lblStatus
        });

        bodyHost.Controls.Add(_card);
        Controls.Add(bodyHost);

        // Az API kulcs mezőben az Enter mentést indít.
        _txtApiKey.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) SaveAndClose();
        };

        AcceptButton = _btnSave;
    }

    private static Label MakeFieldLabel(string text, Point location) => new()
    {
        Text = text.ToUpperInvariant(),
        AutoSize = true,
        Location = location,
        Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
        ForeColor = Theme.TextSecondary,
        BackColor = Color.Transparent
    };

    private static TextBox MakeFieldTextBox(Point location, int width, string value) => new()
    {
        Location = location,
        Width = width,
        Text = value,
        Font = Theme.BodyFont,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Theme.CardBg,
        ForeColor = Theme.TextPrimary
    };

    // ──────────────────────────────────────────────────────────────────────
    // Műveletek
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /orders-t hív a jelenleg beírt beállításokkal, és jelenti az
    /// eredményt. Ideiglenes AppSettings-et használ, így a felhasználó
    /// mentés ELŐTT is tesztelhet.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        if (!ValidateInputs(requireApiKey: true)) return;

        _btnTest.Enabled = false;
        _btnSave.Enabled = false;
        SetStatus("Kapcsolat tesztelése folyamatban...", Theme.TextSecondary);

        try
        {
            // Egyszer használatos AppSettings a JELENLEG BEÍRT értékekkel
            // (nem a perzisztáltakkal) — így a felhasználó mentés előtt tesztelhet.
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
                          Color.FromArgb(60, 130, 80));
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
    /// Elmenti a beírt beállításokat az apisettings.json-ba, leképezi a
    /// globális Program.Settings.Hotcakes-be, és DialogResult.OK-val zár.
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

        // Leképezzük az élő AppSettings-be, hogy az alkalmazás többi része
        // újraindítás nélkül a most mentett értékeket használja.
        Program.Settings.Hotcakes.BaseUrl     = _store.BaseUrl;
        Program.Settings.Hotcakes.ApiBasePath = _store.ApiBasePath;
        Program.Settings.Hotcakes.ApiKey      = _store.ApiKey;

        DialogResult = DialogResult.OK;
        Close();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Segédek
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
