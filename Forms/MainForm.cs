using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;
using HotcakesWinFormsApp.Services;
using HotcakesWinFormsApp.UI;
using HotcakesWinFormsApp.ViewModels;

namespace HotcakesWinFormsApp.Forms;

/// <summary>
/// Az alkalmazás főablaka.
///
/// Elrendezés (vizuális újratervezés — Pawpromise paletta):
///   AppHeaderBar (bor csík márkával, API url-lel, mock jelvénnyel, frissítés /
///   beállítások gombokkal)
///   Rendelések kártya:
///     - szekció fejléc („RENDELÉSEK" + darabszám)
///     - kereső sáv
///     - DataGridView, ami EGY oldalt mutat 20 rendeléssel, egyedileg festett
///       állapot-cellákkal
///     - lapozó (Előző / „Oldal X / Y" / Következő)
///   Tételek kártya:
///     - szekció fejléc („RENDELÉS TÉTELEI" + a kiválasztott rendelésszám)
///     - DataGridView a kiválasztott rendelés tételeivel
///   Akció csík alul:
///     - lekerekített „Számla generálás" / „Címke generálás" elsődleges gombok
///     - jobbra igazított ghost „Visszaállítás 'Received'-re" gomb
///   StatusStrip a legalján
///
/// A KERESÉS és a LAPOZÁS kizárólag kliensoldali — nincs extra API hívás.
/// A FUNKCIONALITÁS VÁLTOZATLAN — csak a vizuálok lettek átalakítva.
/// </summary>
public class MainForm : Form
{
    // ── UI vezérlők ──────────────────────────────────────────────────────────
    private AppHeaderBar _headerBar = null!;
    private Label _lblApiUrl = null!;
    private Label _lblMockBadge = null!;
    private ModernButton _btnRefresh = null!;
    private ModernButton _btnApiSettings = null!;

    private CardPanel _grpOrders = null!;
    private SectionHeader _ordersHeader = null!;
    private DataGridView _dgvOrders = null!;

    // Kereső sáv
    private Panel _searchPanel = null!;
    private Label _lblSearch = null!;
    private TextBox _txtSearch = null!;

    // Lapozó vezérlők
    private Panel _pagerPanel = null!;
    private ModernButton _btnPrevPage = null!;
    private ModernButton _btnNextPage = null!;
    private Label _lblPageIndicator = null!;

    private CardPanel _grpLines = null!;
    private SectionHeader _linesHeader = null!;
    private DataGridView _dgvLines = null!;
    private Label _lblNoItemsInfo = null!;

    private ModernButton _btnInvoice = null!;
    private ModernButton _btnLabel = null!;
    private ModernButton _btnRevertToReceived = null!;

    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;

    // ── Szervizek ────────────────────────────────────────────────────────────
    // Nem readonly: amikor a felhasználó az in-app ApiSettingsForm-on át
    // frissíti az API beállításokat, ezeket újra létrehozzuk, hogy az új
    // URL / kulcs azonnal érvénybe lépjen — anélkül, hogy újra kellene
    // indítani az alkalmazást.
    private HotcakesApiService _apiService;
    private readonly PdfService _pdfService;
    private OrderStatusUpdateService _statusService;

    // ── Állapot ──────────────────────────────────────────────────────────────
    private OrderDetail? _selectedOrder;
    private List<OrderViewModel> _allOrders = new();
    private List<OrderViewModel> _filteredOrders = new();

    // Lapozó állapot
    private const int _pageSize = 20;
    private int _currentPage = 1;
    private int _totalPages = 1;

    // A két grid egyedileg festett oszlopainak indexei (a grid felépítése
    // után egyszer megoldva, hogy a CellPainting handlernek ne kelljen minden
    // egyes festéskor névből kikeresnie).
    private int _colPaymentIndex = -1;
    private int _colStatusIndex  = -1;

    public MainForm()
    {
        _apiService = new HotcakesApiService(Program.Settings);
        _pdfService = new PdfService(Program.Company);
        _statusService = new OrderStatusUpdateService(Program.Settings);
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Pawpromise Rendelések";
        // ── DPI-konzisztens skálázás ─────────────────────────────────────────
        // Az AutoScaleMode = Dpi a PerMonitorV2 alkalmazás-szintű beállítással
        // párban (lásd HotcakesWinFormsApp.csproj) garantálja, hogy a form
        // fizikailag azonos méretben jelenik meg minden monitoron, függetlenül
        // a felbontástól. A 96 DPI-s AutoScaleDimensions kiindulás az, amit a
        // WinForms a kézzel kódolt elrendezés base-elésére használ.
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);

        Size = new Size(1180, 760);
        MinimumSize = new Size(960, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Theme.BodyFont;
        BackColor = Theme.PageBg;

        // A docking FORDÍTOTT Z-sorrendben dolgozódik fel: ezért az alulra
        // szánt dolgokat adjuk hozzá először, a felülre kerülőket utoljára.
        // Felülről lefelé az eredmény: fejléc csík, eszköztár, törzs, státusz csík.
        BuildStatusStrip();
        BuildBody();
        BuildToolbar();
        BuildHeaderBar();

        Load += async (_, _) => await LoadOrdersAsync();
    }

    // ── UI felépítés ─────────────────────────────────────────────────────────

    private void BuildHeaderBar()
    {
        // A bor csík mostantól csak vizuális — márka és a dekoratív admin
        // avatár. Az akció gombok a lenti eszköztáron élnek, ahol a ghost
        // gombok jól olvashatók a krém színű háttéren.
        _headerBar = new AppHeaderBar { Title = "Pawpromise Rendelések" };
        Controls.Add(_headerBar);
    }

    /// <summary>
    /// Eszköztár sor, ami a bor fejléc csík és a törzs kártyák között ül.
    /// Tartalmazza az API URL kijelzést, a mock-mód jelvényt és a
    /// Frissítés / API beállítások ghost gombokat. Krém háttér, hogy a
    /// gombok rendesen olvashatók legyenek.
    /// </summary>
    private void BuildToolbar()
    {
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Theme.PageBg,
            Padding = new Padding(20, 8, 20, 8)
        };

        _lblApiUrl = new Label
        {
            Text = $"API: {Program.Settings.Hotcakes.BaseUrl.TrimEnd('/')}/{Program.Settings.Hotcakes.ApiBasePath.Trim('/')}",
            AutoSize = true,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            Location = new Point(20, 11),
            MaximumSize = new Size(500, 0)
        };
        _lblMockBadge = new Label
        {
            Text = Program.Settings.UseMockData ? "MOCK MÓD AKTÍV" : "",
            ForeColor = Color.FromArgb(192, 110, 40),
            Font = Theme.SmallBoldFont,
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(20, 27)
        };

        _btnRefresh = new ModernButton
        {
            Text = "Frissítés",
            Style = ModernButton.ButtonStyle.Ghost,
            Size = new Size(110, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnRefresh.Click += async (_, _) => await LoadOrdersAsync();

        // Felderíthető, in-app mód az API URL / kulcs frissítésére. E nélkül
        // a felhasználónak kézzel kellene törölnie a
        // bin\...\apisettings.json fájlt ahhoz, hogy a beállító ablak újra
        // megjelenjen (mivel ez a fájl felülírja az appsettings.json-t).
        _btnApiSettings = new ModernButton
        {
            Text = "API beállítások",
            Style = ModernButton.ButtonStyle.Ghost,
            Size = new Size(150, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnApiSettings.Click += async (_, _) => await OpenApiSettingsAsync();

        toolbar.Controls.Add(_lblApiUrl);
        toolbar.Controls.Add(_lblMockBadge);
        toolbar.Controls.Add(_btnRefresh);
        toolbar.Controls.Add(_btnApiSettings);

        // A gombok a jobb szélhez tapadnak, valahányszor az eszköztár átméreteződik.
        void PositionRightButtons()
        {
            _btnRefresh.Location     = new Point(toolbar.Width - 20 - _btnRefresh.Width, 9);
            _btnApiSettings.Location = new Point(_btnRefresh.Left - 8 - _btnApiSettings.Width, 9);
        }
        PositionRightButtons();
        toolbar.Resize += (_, _) => PositionRightButtons();

        Controls.Add(toolbar);
    }

    private void BuildStatusStrip()
    {
        _statusStrip = new StatusStrip
        {
            Dock = DockStyle.Bottom,
            BackColor = Theme.CardBg,
            SizingGrip = false,
            Padding = new Padding(8, 0, 8, 0)
        };
        _lblStatus = new ToolStripStatusLabel("Kész.")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.TextSecondary,
            Font = Theme.SmallFont
        };
        _statusStrip.Items.Add(_lblStatus);
        Controls.Add(_statusStrip);
    }

    /// <summary>
    /// Két egymásra rakott kártya (rendelések / tételek), plusz egy akció-gomb
    /// csík — TableLayoutPanel-lel elrendezve, hogy a kártyák szépen
    /// újrarendeződjenek, amikor a form átméreteződik.
    /// </summary>
    private void BuildBody()
    {
        var bodyHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.PageBg,
            Padding = new Padding(20, 16, 20, 12)
        };

        var bodyLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
        bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
        bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));

        // ── Rendelések kártya ───────────────────────────────────────────────
        _grpOrders = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
        _ordersHeader = new SectionHeader { Title = "RENDELÉSEK", Hint = "" };
        _dgvOrders = CreateOrdersGrid();
        _searchPanel = CreateSearchPanel();
        _pagerPanel  = CreatePagerPanel();

        // A Fill-t (grid) adjuk hozzá ELSŐKÉNT, hogy a Top/Bottom dock-oltak
        // a szélekről metszhessenek le helyet.
        _grpOrders.Controls.Add(_dgvOrders);
        _grpOrders.Controls.Add(_pagerPanel);
        _grpOrders.Controls.Add(_searchPanel);
        _grpOrders.Controls.Add(_ordersHeader);

        bodyLayout.Controls.Add(_grpOrders, 0, 0);

        // ── Tételek kártya ──────────────────────────────────────────────────
        _grpLines = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
        _linesHeader = new SectionHeader { Title = "RENDELÉS TÉTELEI", Hint = "" };
        _dgvLines = CreateLinesGrid();

        _lblNoItemsInfo = new Label
        {
            Text = "Válasszon ki egy rendelést a listából.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.TextSecondary,
            Font = Theme.BodyFont,
            BackColor = Color.Transparent,
            Visible = true
        };
        _grpLines.Controls.Add(_dgvLines);
        _grpLines.Controls.Add(_lblNoItemsInfo);
        _grpLines.Controls.Add(_linesHeader);
        _dgvLines.Visible = false;

        bodyLayout.Controls.Add(_grpLines, 0, 1);

        // ── Akció gomb csík ─────────────────────────────────────────────────
        var btnPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 14, 0, 6)
        };

        _btnInvoice = new ModernButton
        {
            Text = "Számla generálás",
            Style = ModernButton.ButtonStyle.Primary,
            Size = new Size(180, 40),
            Location = new Point(0, 6)
        };
        _btnLabel = new ModernButton
        {
            Text = "Címke generálás",
            Style = ModernButton.ButtonStyle.Primary,
            Size = new Size(180, 40),
            Location = new Point(192, 6)
        };
        // Jobb-anchorral pin-elt „visszaállítás Received-re" gomb. Csak akkor
        // engedélyezett, ha a kiválasztott rendelés Complete állapotban van
        // (lásd OnOrderSelectedAsync).
        _btnRevertToReceived = new ModernButton
        {
            Text = "Visszaállítás 'Received'-re",
            Style = ModernButton.ButtonStyle.Ghost,
            Size = new Size(220, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Enabled = false
        };
        _btnInvoice.Click          += async (_, _) => await GenerateInvoiceAsync();
        _btnLabel.Click            += async (_, _) => await GenerateLabelAsync();
        _btnRevertToReceived.Click += async (_, _) => await RevertOrderToReceivedAsync();
        btnPanel.Controls.AddRange(new Control[] { _btnInvoice, _btnLabel, _btnRevertToReceived });

        // Kezdő pozíció + a form átméretezésekor maradjon a jobb szélen.
        void PositionRevertButton() =>
            _btnRevertToReceived.Location =
                new Point(Math.Max(420, btnPanel.Width - _btnRevertToReceived.Width), 6);
        PositionRevertButton();
        btnPanel.Resize += (_, _) => PositionRevertButton();

        bodyLayout.Controls.Add(btnPanel, 0, 2);

        bodyHost.Controls.Add(bodyLayout);
        Controls.Add(bodyHost);
    }

    /// <summary>A rendelések kártya tetejére (a szekció fejléc alá) dockolt kereső sáv.</summary>
    private Panel CreateSearchPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            Padding = new Padding(0, 4, 0, 6),
            BackColor = Color.Transparent
        };

        _lblSearch = new Label
        {
            Text = "Keresés:",
            AutoSize = true,
            Location = new Point(0, 9),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent
        };
        _txtSearch = new TextBox
        {
            Location = new Point(60, 5),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Width = 420,
            Font = Theme.BodyFont,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.CardBg,
            ForeColor = Theme.TextPrimary,
            PlaceholderText = "Szűrés — rendelésszám, vevő neve vagy email"
        };
        _txtSearch.TextChanged += (_, _) => ApplySearchAndPaging(resetPage: true);

        panel.Controls.Add(_lblSearch);
        panel.Controls.Add(_txtSearch);
        panel.Resize += (_, _) =>
        {
            _txtSearch.Width = Math.Max(120, panel.Width - _txtSearch.Left - 4);
        };
        return panel;
    }

    /// <summary>A rendelések kártya aljára dockolt lapozó sáv.</summary>
    private Panel CreatePagerPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(0, 6, 0, 4),
            BackColor = Color.Transparent
        };

        _btnPrevPage = new ModernButton
        {
            Text = "Előző",
            Style = ModernButton.ButtonStyle.Ghost,
            Size = new Size(90, 32),
            Location = new Point(0, 6),
            Enabled = false
        };
        _btnPrevPage.Click += (_, _) => ChangePage(-1);

        _lblPageIndicator = new Label
        {
            AutoSize = true,
            Location = new Point(112, 13),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            Text = "Oldal 0 / 0"
        };

        _btnNextPage = new ModernButton
        {
            Text = "Következő",
            Style = ModernButton.ButtonStyle.Ghost,
            Size = new Size(110, 32),
            Location = new Point(340, 6),
            Enabled = false
        };
        _btnNextPage.Click += (_, _) => ChangePage(+1);

        panel.Controls.AddRange(new Control[] { _btnPrevPage, _lblPageIndicator, _btnNextPage });
        return panel;
    }

    // ── Grid konfiguráció ──────────────────────────────────────────────────

    private DataGridView CreateOrdersGrid()
    {
        var dgv = BuildStyledGrid();

        dgv.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "colOrderNum", HeaderText = "Rendelésszám",
                DataPropertyName = nameof(OrderViewModel.OrderNumber), FillWeight = 11,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = Theme.BodyBoldFont,
                    ForeColor = Theme.TextPrimary,
                    BackColor = Theme.CardBg,
                    SelectionBackColor = Theme.GridSelectionBg,
                    SelectionForeColor = Theme.GridSelectionText,
                    Padding = new Padding(8, 0, 6, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colCustomer", HeaderText = "Vevő neve",
                DataPropertyName = nameof(OrderViewModel.CustomerName), FillWeight = 16
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colEmail", HeaderText = "Email",
                DataPropertyName = nameof(OrderViewModel.UserEmail), FillWeight = 19
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colTotal", HeaderText = "Végösszeg (Ft)",
                DataPropertyName = nameof(OrderViewModel.TotalGrand), FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N0",
                    Font = Theme.BodyBoldFont,
                    ForeColor = Theme.TextPrimary,
                    Padding = new Padding(4, 0, 8, 0)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colPayment", HeaderText = "Fizetési állapot",
                DataPropertyName = nameof(OrderViewModel.PaymentStatus), FillWeight = 13
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colShipping", HeaderText = "Szállítás",
                DataPropertyName = nameof(OrderViewModel.ShippingMethod), FillWeight = 13
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colDate", HeaderText = "Rendelés dátuma",
                DataPropertyName = nameof(OrderViewModel.OrderDate), FillWeight = 11,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "yyyy.MM.dd HH:mm",
                    ForeColor = Theme.TextSecondary,
                    Padding = new Padding(8, 0, 6, 0)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colStatus", HeaderText = "Állapot",
                DataPropertyName = nameof(OrderViewModel.Status), FillWeight = 12
            }
        });

        _colPaymentIndex = dgv.Columns["colPayment"]!.Index;
        _colStatusIndex  = dgv.Columns["colStatus"]!.Index;

        // Egyedileg festett cellák a két vizuális állapothoz.
        dgv.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex == _colPaymentIndex)
                StatusCellPainter.PaintPaymentStatus(e, e.Value?.ToString() ?? "");
            else if (e.ColumnIndex == _colStatusIndex)
                StatusCellPainter.PaintStatusPill(e, e.Value?.ToString() ?? "");
        };

        dgv.SelectionChanged += async (_, _) => await OnOrderSelectedAsync();
        return dgv;
    }

    private DataGridView CreateLinesGrid()
    {
        var dgv = BuildStyledGrid();

        dgv.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "colProductName", HeaderText = "Termék neve",
                DataPropertyName = nameof(OrderItemViewModel.ProductName), FillWeight = 30,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = Theme.BodyBoldFont,
                    ForeColor = Theme.TextPrimary,
                    Padding = new Padding(8, 0, 6, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colSku", HeaderText = "SKU",
                DataPropertyName = nameof(OrderItemViewModel.Sku), FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Cascadia Mono", 8.5f),
                    ForeColor = Theme.TextSecondary,
                    Padding = new Padding(8, 0, 6, 0)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colOption", HeaderText = "Variáns",
                DataPropertyName = nameof(OrderItemViewModel.Option), FillWeight = 22
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colQty", HeaderText = "Mennyiség",
                DataPropertyName = nameof(OrderItemViewModel.Quantity), FillWeight = 7,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 6, 0)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colUnitPrice", HeaderText = "Egységár (Ft)",
                DataPropertyName = nameof(OrderItemViewModel.UnitPrice), FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N0",
                    Padding = new Padding(4, 0, 8, 0)
                }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colLineTotal", HeaderText = "Összeg (Ft)",
                DataPropertyName = nameof(OrderItemViewModel.LineTotal), FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N0",
                    Font = Theme.BodyBoldFont,
                    ForeColor = Theme.TextPrimary,
                    Padding = new Padding(4, 0, 8, 0)
                }
            }
        });

        return dgv;
    }

    /// <summary>Közös alap-stílus a rendelések és a tételek grid-jéhez.</summary>
    private static DataGridView BuildStyledGrid()
    {
        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing,
            ColumnHeadersHeight = 36,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            BackgroundColor = Theme.CardBg,
            BorderStyle = BorderStyle.None,
            GridColor = Theme.GridLine,
            CellBorderStyle = DataGridViewCellBorderStyle.None,
            RowTemplate = { Height = 38 },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.CardBg,
                ForeColor = Theme.TextPrimary,
                SelectionBackColor = Theme.GridSelectionBg,
                SelectionForeColor = Theme.GridSelectionText,
                Font = Theme.CellFont,
                Padding = new Padding(8, 0, 6, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                WrapMode = DataGridViewTriState.False
            }
        };

        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.GridHeaderBg,
            ForeColor = Theme.GridHeaderText,
            SelectionBackColor = Theme.GridHeaderBg,
            SelectionForeColor = Theme.GridHeaderText,
            Font = Theme.GridHeaderFont,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 6, 0),
            WrapMode = DataGridViewTriState.False
        };

        // Alsó hajszálvonal minden sorhoz, a referencia dizájn tábla-stílusát tükrözve.
        dgv.RowPostPaint += (_, e) =>
        {
            using var pen = new Pen(Theme.GridLine);
            int y = e.RowBounds.Bottom - 1;
            e.Graphics.DrawLine(pen, e.RowBounds.Left, y, e.RowBounds.Right, y);
        };

        return dgv;
    }

    // ── Adatbetöltés ────────────────────────────────────────────────────────

    private async Task LoadOrdersAsync()
    {
        SetStatus("Rendelések betöltése...");
        _btnRefresh.Enabled = false;
        _dgvOrders.DataSource = null;
        _dgvLines.DataSource = null;
        _dgvLines.Visible = false;
        _lblNoItemsInfo.Text = "Válasszon ki egy rendelést a listából.";
        _lblNoItemsInfo.Visible = true;
        _selectedOrder = null;
        UpdateRevertButtonState();

        try
        {
            List<OrderSummary> rawOrders;

            if (Program.Settings.UseMockData)
            {
                await Task.Delay(400);
                rawOrders = MockDataService.GetMockOrders();
                SetStatus($"{rawOrders.Count} rendelés betöltve (mock mód).");
            }
            else
            {
                var (orders, error) = await _apiService.GetOrdersAsync();
                if (error != null)
                {
                    SetStatus($"Hiba: {error}");
                    MessageHelper.ShowApiError(error);
                    return;
                }
                rawOrders = orders;
                SetStatus(rawOrders.Count == 0
                    ? "Nem található rendelés."
                    : $"{rawOrders.Count} rendelés betöltve.");
            }

            _allOrders = rawOrders.Select(o => new OrderViewModel(o)).ToList();
            _currentPage = 1;
            ApplySearchAndPaging(resetPage: true);
        }
        catch (Exception ex)
        {
            SetStatus($"Váratlan hiba: {ex.Message}");
            MessageHelper.ShowApiError(ex.Message);
        }
        finally
        {
            _btnRefresh.Enabled = true;
        }
    }

    // ── Keresés + lapozás (kliensoldali) ────────────────────────────────────

    /// <summary>
    /// Alkalmazza a jelenlegi keresési szűrőt, és újrarendereli az aktuális
    /// oldalt. Minden billentyűleütésnél, oldal-váltáskor és adat-újratöltés
    /// után fut.
    /// </summary>
    private void ApplySearchAndPaging(bool resetPage)
    {
        var query = _txtSearch?.Text?.Trim() ?? "";
        _filteredOrders = FilterOrders(_allOrders, query);

        _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredOrders.Count / (double)_pageSize));
        if (resetPage) _currentPage = 1;
        _currentPage = Math.Clamp(_currentPage, 1, _totalPages);

        RenderCurrentPage();
    }

    /// <summary>
    /// Kis- és nagybetű érzéketlen szűrő OrderNumber, CustomerName és
    /// UserEmail szerint. Üres lekérdezésre a bemeneti listát változatlanul
    /// adja vissza.
    /// </summary>
    private static List<OrderViewModel> FilterOrders(List<OrderViewModel> source, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return source;

        return source.Where(o =>
            (o.OrderNumber  ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (o.CustomerName ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (o.UserEmail    ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    /// <summary>Az aktuális oldal-szeletet bind-eli a gridhez és frissíti a lapozó címkéket.</summary>
    private void RenderCurrentPage()
    {
        var skip = (_currentPage - 1) * _pageSize;
        var pageItems = _filteredOrders.Skip(skip).Take(_pageSize).ToList();

        _dgvOrders.DataSource = new BindingSource { DataSource = pageItems };

        _lblPageIndicator.Text = _filteredOrders.Count == 0
            ? "Nincs találat"
            : $"Oldal {_currentPage} / {_totalPages}  ({_filteredOrders.Count} rendelés)";
        _btnPrevPage.Enabled = _currentPage > 1;
        _btnNextPage.Enabled = _currentPage < _totalPages;

        _ordersHeader.Hint = _filteredOrders.Count == 0
            ? "Nincs találat"
            : $"{_filteredOrders.Count} db rendelés";
    }

    private void ChangePage(int delta)
    {
        var newPage = Math.Clamp(_currentPage + delta, 1, _totalPages);
        if (newPage == _currentPage) return;
        _currentPage = newPage;
        RenderCurrentPage();
    }

    // ── Rendelés kiválasztása → tétel betöltése ─────────────────────────────

    private async Task OnOrderSelectedAsync()
    {
        if (_dgvOrders.SelectedRows.Count == 0) return;
        if (_dgvOrders.SelectedRows[0].DataBoundItem is not OrderViewModel vm) return;

        _selectedOrder = null;
        _dgvLines.DataSource = null;
        _dgvLines.Visible = false;
        _lblNoItemsInfo.Text = $"Tételek betöltése: {vm.OrderNumber}...";
        _lblNoItemsInfo.Visible = true;
        _linesHeader.Hint = vm.OrderNumber ?? "";

        SetStatus($"Kiválasztva: {vm.OrderNumber} — tételek betöltése...");

        _selectedOrder = OrderDetail.FromSummary(vm.Source);

        if (Program.Settings.UseMockData)
        {
            await Task.Delay(150);
            _selectedOrder.Items = MockDataService.GetMockOrderItems(vm.Bvin);
        }
        else
        {
            var (items, itemError) = await _apiService.GetOrderItemsAsync(vm.Bvin);
            if (itemError != null)
            {
                _lblNoItemsInfo.Text = $"Tételek betöltése sikertelen:\n{itemError}";
                SetStatus($"Kiválasztva: {vm.OrderNumber} — tételek betöltése sikertelen.");
                return;
            }
            _selectedOrder.Items = items;
        }

        bool hasItems = _selectedOrder.Items.Count > 0;
        _dgvLines.Visible = hasItems;
        _lblNoItemsInfo.Visible = !hasItems;

        if (hasItems)
        {
            var itemVms = _selectedOrder.Items.Select(i => new OrderItemViewModel(i)).ToList();
            _dgvLines.DataSource = new BindingSource { DataSource = itemVms };
        }
        else
        {
            _lblNoItemsInfo.Text = "Ehhez a rendeléshez nem találhatók tételek az API-ban.";
        }

        SetStatus(
            $"Kiválasztva: {vm.OrderNumber} | Vevő: {vm.CustomerName} | " +
            $"Végösszeg: {vm.TotalGrand:N0} Ft | Állapot: {vm.Status}" +
            (hasItems ? $" | {_selectedOrder.Items.Count} tétel" : " | Nincs tétel"));

        UpdateRevertButtonState();
    }

    /// <summary>
    /// Csak akkor engedélyezi a „visszaállítás Received-re" gombot, ha a
    /// kiválasztott rendelés StatusCode-ja megegyezik a jól ismert Complete
    /// GUID-dal. Az összehasonlítás kis- és nagybetű érzéketlen, mert az API
    /// vegyes nagybetűs formában adja vissza a GUID-ot.
    /// </summary>
    private void UpdateRevertButtonState()
    {
        var isComplete = _selectedOrder != null
            && string.Equals(
                _selectedOrder.StatusCode,
                HotcakesOrderStatus.Complete.StatusCode,
                StringComparison.OrdinalIgnoreCase);

        // Mock módban nincs élő API, amivel beszélhetnénk — a gombot
        // letiltva tartjuk, hogy a felhasználó konzisztens élményt kapjon.
        _btnRevertToReceived.Enabled = isComplete && !Program.Settings.UseMockData;
    }

    // ── PDF generálás ───────────────────────────────────────────────────────

    private async Task GenerateInvoiceAsync()
    {
        if (_selectedOrder == null)
        {
            MessageHelper.ShowWarning(
                "Nincs kiválasztott rendelés.\nKérjük, válasszon ki egyet a listából.",
                "Nincs kiválasztott rendelés");
            return;
        }

        if (!_selectedOrder.IsPlaced)
        {
            MessageHelper.ShowWarning(
                $"Ez a rendelés még nem véglegesített (piszkozat állapotban van).\n\n" +
                $"Rendelésszám: {_selectedOrder.DisplayOrderNumber}\n" +
                $"Állapot: {_selectedOrder.DisplayStatus}\n\n" +
                "Számla csak véglegesített (IsPlaced = true) rendeléshez generálható.",
                "Rendelés nem véglegesített");
            return;
        }

        if (_selectedOrder.CustomerName == "Nincs adat" || _selectedOrder.BillingAddress == null)
        {
            var result = MessageHelper.ShowQuestion(
                "A rendelés nem tartalmaz ügyfél / számlázási adatokat.\n" +
                "A generált számla hiányos lesz.\n\nFolytatja?",
                "Hiányzó számlázási adatok");
            if (result != DialogResult.Yes) return;
        }

        SetStatus("Számla generálása...");
        _btnInvoice.Enabled = false;

        try
        {
            var path = await Task.Run(() => _pdfService.GenerateInvoice(_selectedOrder));
            SetStatus($"Számla elmentve: {path}");
            MessageHelper.ShowPdfSaved(path);

            // Miután a PDF biztonságosan a lemezre került, a rendelés
            // állapotát „Complete"-re állítjuk. Szándékosan a PDF UTÁN
            // (nem előtte): ha az állapot frissítés meghiúsul, a számla
            // fájl akkor is megmarad a felhasználónál.
            await UpdateStatusAfterInvoiceAsync(_selectedOrder);
        }
        catch (Exception ex)
        {
            SetStatus($"Hiba a számla generálásakor: {ex.Message}");
            MessageHelper.ShowError($"Hiba a számla PDF generálásakor:\n\n{ex.Message}", "PDF Hiba");
        }
        finally
        {
            _btnInvoice.Enabled = true;
        }
    }

    /// <summary>
    /// Az <see cref="OrderStatusUpdateService"/>-t hívja, hogy a rendelést
    /// „Complete"-re állítsa, és az eredményt a státusz csíkban közli.
    /// Mock módban kihagyva (nincs élő API). Sikeresség esetén a rendelés-
    /// listát frissíti, hogy az új állapot azonnal látszódjon.
    ///
    /// <para>A részletes request / response trace csak a Debug Output-ba
    /// kerül — nem jelenik meg dialógusként, így a felhasználó egyetlen
    /// kattintással „számla → kész" folyamatot kap, extra megerősítő
    /// felugró ablak nélkül. Súlyos hiba esetén egyetlen figyelmeztető
    /// dialógus jelenik meg.</para>
    /// </summary>
    private async Task UpdateStatusAfterInvoiceAsync(OrderDetail order)
    {
        if (Program.Settings.UseMockData)
        {
            SetStatus("Mock mód: a rendelés állapotának frissítése kihagyva.");
            return;
        }

        SetStatus("Rendelés állapotának frissítése (Complete)...");

        StatusUpdateResult result;
        try
        {
            result = await _statusService.UpdateOrderStatusAsync(order.Bvin);
        }
        catch (Exception ex)
        {
            SetStatus($"Hiba az állapot frissítésekor: {ex.Message}");
            MessageHelper.ShowError(
                $"Váratlan hiba történt a rendelés állapotának frissítésekor:\n\n{ex.Message}",
                "Állapot frissítési hiba");
            return;
        }

        // A trace-t a debug output-ra is kiírjuk, hogy egy fejlesztő szükség
        // esetén a Visual Studio Output ablakából tudja átnézni.
        System.Diagnostics.Debug.WriteLine("[StatusUpdate] " + result.DebugLog);

        if (result.Success)
        {
            SetStatus($"A rendelés ({order.DisplayOrderNumber}) állapota frissítve: Complete.");
            // Friss listát kérünk, hogy a grid az új szerver-állapotot mutassa.
            await LoadOrdersAsync();
        }
        else
        {
            SetStatus($"A rendelés állapotának frissítése sikertelen: {result.ErrorMessage}");
            MessageHelper.ShowWarning(
                $"A számla létrejött, de a rendelés állapotát NEM sikerült frissíteni.\n\n" +
                $"{result.ErrorMessage}",
                "Állapot frissítési hiba");
        }
    }

    /// <summary>
    /// A kiválasztott rendelést „Complete"-ből visszaállítja „Received"-re.
    /// A gomb csak akkor engedélyezett, ha a kiválasztott rendelés Complete
    /// állapotú (lásd <see cref="UpdateRevertButtonState"/>), de itt is
    /// újra validáljuk arra az esetre, ha az alapul szolgáló állapot a
    /// legutóbbi kiválasztás óta változott.
    ///
    /// <para>A visszaállítás kattintásra azonnal fut — nincs megerősítő
    /// dialógus, és nincs napló-dialógus. Maga a gomb a gesztus; a státusz
    /// kimenet az alsó státusz csíkba kerül. Súlyos hibánál továbbra is
    /// figyelmeztető dialógus jelenik meg, hogy a felhasználó tudja:
    /// a szerveren nem történt változás.</para>
    /// </summary>
    private async Task RevertOrderToReceivedAsync()
    {
        if (_selectedOrder == null)
        {
            MessageHelper.ShowWarning(
                "Nincs kiválasztott rendelés.\nKérjük, válasszon ki egyet a listából.",
                "Nincs kiválasztott rendelés");
            return;
        }

        var isComplete = string.Equals(
            _selectedOrder.StatusCode,
            HotcakesOrderStatus.Complete.StatusCode,
            StringComparison.OrdinalIgnoreCase);
        if (!isComplete)
        {
            MessageHelper.ShowWarning(
                $"Csak \"Complete\" állapotú rendelések állíthatók vissza \"Received\" állapotra.\n\n" +
                $"Aktuális állapot: {_selectedOrder.DisplayStatus}",
                "Nem visszaállítható");
            return;
        }

        SetStatus("Rendelés állapotának visszaállítása (Received)...");
        _btnRevertToReceived.Enabled = false;

        StatusUpdateResult result;
        try
        {
            result = await _statusService.UpdateOrderStatusAsync(
                _selectedOrder.Bvin, HotcakesOrderStatus.Received);
        }
        catch (Exception ex)
        {
            SetStatus($"Hiba az állapot visszaállításakor: {ex.Message}");
            MessageHelper.ShowError(
                $"Váratlan hiba történt a rendelés állapotának visszaállításakor:\n\n{ex.Message}",
                "Állapot visszaállítási hiba");
            UpdateRevertButtonState();
            return;
        }

        // A trace csak a Debug Output-ba kerül — nincs in-app napló dialógus.
        System.Diagnostics.Debug.WriteLine("[StatusUpdate] " + result.DebugLog);

        if (result.Success)
        {
            SetStatus($"A rendelés ({_selectedOrder.DisplayOrderNumber}) állapota visszaállítva: Received.");

            // Frissítjük a gridet, hogy az új állapot látszódjon. A
            // LoadOrdersAsync törli a _selectedOrder-t, ami a visszaállító
            // gombot letiltva hagyja — ez a helyes végállapot, mert a
            // rendelés már nem Complete.
            await LoadOrdersAsync();
        }
        else
        {
            SetStatus($"A rendelés állapotának visszaállítása sikertelen: {result.ErrorMessage}");
            MessageHelper.ShowWarning(
                $"A rendelés állapotát NEM sikerült visszaállítani.\n\n{result.ErrorMessage}",
                "Állapot visszaállítási hiba");

            UpdateRevertButtonState();
        }
    }

    private async Task GenerateLabelAsync()
    {
        if (_selectedOrder == null)
        {
            MessageHelper.ShowWarning(
                "Nincs kiválasztott rendelés.\nKérjük, válasszon ki egyet a listából.",
                "Nincs kiválasztott rendelés");
            return;
        }

        if (!_selectedOrder.IsPlaced)
        {
            MessageHelper.ShowWarning(
                $"Ez a rendelés még nem véglegesített (piszkozat állapotban van).\n\n" +
                $"Rendelésszám: {_selectedOrder.DisplayOrderNumber}\n" +
                $"Állapot: {_selectedOrder.DisplayStatus}\n\n" +
                "Cimke csak véglegesített (IsPlaced = true) rendeléshez generálható.",
                "Rendelés nem véglegesített");
            return;
        }

        if (_selectedOrder.ShippingAddress == null && _selectedOrder.BillingAddress == null)
        {
            var result = MessageHelper.ShowQuestion(
                "A rendeléshez nincs szállítási cím megadva.\n" +
                "A cimkén nem fognak megjelenni a cím adatok.\n\nFolytatja?",
                "Hiányzó szállítási cím");
            if (result != DialogResult.Yes) return;
        }

        SetStatus("Szállítási cimke generálása...");
        _btnLabel.Enabled = false;

        try
        {
            var path = await Task.Run(() => _pdfService.GenerateLabel(_selectedOrder));
            SetStatus($"Cimke elmentve: {path}");
            MessageHelper.ShowPdfSaved(path);
        }
        catch (Exception ex)
        {
            SetStatus($"Hiba a cimke generálásakor: {ex.Message}");
            MessageHelper.ShowError($"Hiba a cimke PDF generálásakor:\n\n{ex.Message}", "PDF Hiba");
        }
        finally
        {
            _btnLabel.Enabled = true;
        }
    }

    // ── API beállítások (in-app újranyitás) ─────────────────────────────────

    /// <summary>
    /// Megnyitja az <see cref="ApiSettingsForm"/>-ot, hogy a felhasználó
    /// úgy tudja módosítani a Hotcakes kapcsolatot (Alap URL / API útvonal /
    /// API kulcs), hogy közben az alkalmazást nem kell újraindítani, és
    /// JSON fájlokat sem kell kézzel szerkeszteni.
    ///
    /// Sikeres mentéskor az <see cref="ApiSettingsForm.SaveAndClose"/> már
    /// leképezte az új értékeket a <see cref="Program.Settings"/>-be — de a
    /// meglévő szerviz példányok a RÉGI URL-t / kulcsot fogták el, így itt
    /// újra létrehozzuk őket, és frissítjük az URL-től függő UI elemeket.
    /// </summary>
    private async Task OpenApiSettingsAsync()
    {
        using var dlg = new ApiSettingsForm();
        var result = dlg.ShowDialog(this);
        if (result != DialogResult.OK) return;

        // A beállításokat a form már kiírta. A régi konfigurációt
        // elfogott szervizeket újra létrehozzuk.
        _apiService    = new HotcakesApiService(Program.Settings);
        _statusService = new OrderStatusUpdateService(Program.Settings);

        // Frissítjük a felső panelben az URL címkét.
        _lblApiUrl.Text =
            $"API: {Program.Settings.Hotcakes.BaseUrl.TrimEnd('/')}/" +
            $"{Program.Settings.Hotcakes.ApiBasePath.Trim('/')}";

        SetStatus("API beállítások frissítve — rendelések újratöltése...");
        await LoadOrdersAsync();
    }

    // ── Segédek ─────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        if (IsDisposed) return;
        if (InvokeRequired) Invoke(() => _lblStatus.Text = message);
        else _lblStatus.Text = message;
    }
}
