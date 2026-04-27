using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;
using HotcakesWinFormsApp.Services;
using HotcakesWinFormsApp.UI;
using HotcakesWinFormsApp.ViewModels;

namespace HotcakesWinFormsApp.Forms;

/// <summary>
/// Main application form.
///
/// Layout (visual redesign — Pawpromise palette):
///   AppHeaderBar (wine bar with brand, API url, mock badge, refresh / settings buttons)
///   Orders card:
///     - section header ("RENDELÉSEK" + count)
///     - search bar
///     - DataGridView showing ONE page of 20 orders, with custom-painted status cells
///     - pager (Prev / "Page X / Y" / Next)
///   Lines card:
///     - section header ("RENDELÉS TÉTELEI" + selected order number)
///     - DataGridView with the items of the selected order
///   Action strip at the bottom:
///     - rounded "Számla generálás" / "Címke generálás" primary buttons
///     - right-aligned ghost "Visszaállítás 'Received'" button
///   StatusStrip at the very bottom
///
/// SEARCH + PAGINATION are client-side only — no extra API calls are issued.
/// FUNCTIONALITY IS UNCHANGED — only the visuals were reworked.
/// </summary>
public class MainForm : Form
{
    // ── UI controls ──────────────────────────────────────────────────────────
    private AppHeaderBar _headerBar = null!;
    private Label _lblApiUrl = null!;
    private Label _lblMockBadge = null!;
    private ModernButton _btnRefresh = null!;
    private ModernButton _btnApiSettings = null!;

    private CardPanel _grpOrders = null!;
    private SectionHeader _ordersHeader = null!;
    private DataGridView _dgvOrders = null!;

    // Search bar
    private Panel _searchPanel = null!;
    private Label _lblSearch = null!;
    private TextBox _txtSearch = null!;

    // Pagination controls
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

    // ── Services ─────────────────────────────────────────────────────────────
    // Not readonly: when the user updates API settings via the in-app
    // ApiSettingsForm we recreate these so the new URL/key takes effect
    // without restarting the application.
    private HotcakesApiService _apiService;
    private readonly PdfService _pdfService;
    private OrderStatusUpdateService _statusService;

    // ── State ────────────────────────────────────────────────────────────────
    private OrderDetail? _selectedOrder;
    private List<OrderViewModel> _allOrders = new();
    private List<OrderViewModel> _filteredOrders = new();

    // Pagination state
    private const int _pageSize = 20;
    private int _currentPage = 1;
    private int _totalPages = 1;

    // Indices of the cell-painted columns in each grid (resolved once after
    // the grids are built so the CellPainting handlers don't have to look the
    // names up on every paint).
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
        Size = new Size(1180, 760);
        MinimumSize = new Size(960, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = Theme.BodyFont;
        BackColor = Theme.PageBg;

        // Docking is processed in REVERSE Z-order, so we add bottom-most things
        // first and top-most last. End result, top-down: header bar, toolbar,
        // body, status strip.
        BuildStatusStrip();
        BuildBody();
        BuildToolbar();
        BuildHeaderBar();

        Load += async (_, _) => await LoadOrdersAsync();
    }

    // ── UI Construction ──────────────────────────────────────────────────────

    private void BuildHeaderBar()
    {
        // The wine bar is now visual-only — branding and the decorative admin
        // avatar. Action buttons live on the toolbar below where ghost buttons
        // can read clearly against the cream page background.
        _headerBar = new AppHeaderBar { Title = "Pawpromise Rendelések" };
        Controls.Add(_headerBar);
    }

    /// <summary>
    /// Toolbar row sitting between the wine header bar and the body cards.
    /// Holds the API URL display, mock-mode badge, and the Refresh / API
    /// settings ghost buttons. Cream background so the buttons read properly.
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

        // Discoverable in-app way to update the API URL / key. Without this the
        // user would have to delete bin\...\apisettings.json by hand to force the
        // settings form to reappear (because that file overrides appsettings.json).
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

        // Pin the buttons to the right edge whenever the toolbar resizes.
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
    /// Two stacked cards (orders / lines) plus an action button strip — laid out
    /// with a TableLayoutPanel so the cards re-flow nicely when the form resizes.
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

        // ── Orders card ─────────────────────────────────────────────────────
        _grpOrders = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
        _ordersHeader = new SectionHeader { Title = "RENDELÉSEK", Hint = "" };
        _dgvOrders = CreateOrdersGrid();
        _searchPanel = CreateSearchPanel();
        _pagerPanel  = CreatePagerPanel();

        // Add Fill (grid) first so docked Top/Bottom carve out from the edges.
        _grpOrders.Controls.Add(_dgvOrders);
        _grpOrders.Controls.Add(_pagerPanel);
        _grpOrders.Controls.Add(_searchPanel);
        _grpOrders.Controls.Add(_ordersHeader);

        bodyLayout.Controls.Add(_grpOrders, 0, 0);

        // ── Lines card ──────────────────────────────────────────────────────
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

        // ── Action button strip ─────────────────────────────────────────────
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
        // Right-anchored "revert to Received" button. Only enabled when the currently
        // selected order is in the Complete state (see OnOrderSelectedAsync).
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

        // Initial position + keep pinned to the right when the form resizes.
        void PositionRevertButton() =>
            _btnRevertToReceived.Location =
                new Point(Math.Max(420, btnPanel.Width - _btnRevertToReceived.Width), 6);
        PositionRevertButton();
        btnPanel.Resize += (_, _) => PositionRevertButton();

        bodyLayout.Controls.Add(btnPanel, 0, 2);

        bodyHost.Controls.Add(bodyLayout);
        Controls.Add(bodyHost);
    }

    /// <summary>Search bar docked to the top of the orders card (under the section header).</summary>
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

    /// <summary>Pagination bar docked to the bottom of the orders card.</summary>
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

    // ── Grid configuration ──────────────────────────────────────────────────

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

        // Custom-painted cells for the two visual statuses.
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

    /// <summary>Shared base styling for both the orders and lines grids.</summary>
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

        // Bottom hairline for each row, mirroring the reference design's table style.
        dgv.RowPostPaint += (_, e) =>
        {
            using var pen = new Pen(Theme.GridLine);
            int y = e.RowBounds.Bottom - 1;
            e.Graphics.DrawLine(pen, e.RowBounds.Left, y, e.RowBounds.Right, y);
        };

        return dgv;
    }

    // ── Data loading ────────────────────────────────────────────────────────

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

    // ── Search + pagination (client-side) ───────────────────────────────────

    /// <summary>
    /// Applies the current search filter and re-renders the current page.
    /// Called on each keystroke, on page change, and after data reloads.
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
    /// Case-insensitive filter across OrderNumber, CustomerName, and UserEmail.
    /// Empty query returns the input list unchanged.
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

    /// <summary>Binds the current page slice to the grid and updates pager labels.</summary>
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

    // ── Order selection → item loading ──────────────────────────────────────

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
    /// Enable the "revert to Received" button only when the currently selected
    /// order's StatusCode matches the well-known Complete GUID. Comparison is
    /// case-insensitive because the API returns the GUID in mixed case.
    /// </summary>
    private void UpdateRevertButtonState()
    {
        var isComplete = _selectedOrder != null
            && string.Equals(
                _selectedOrder.StatusCode,
                HotcakesOrderStatus.Complete.StatusCode,
                StringComparison.OrdinalIgnoreCase);

        // In mock mode there's no live API to talk to — keep the button disabled
        // so the user gets a consistent experience.
        _btnRevertToReceived.Enabled = isComplete && !Program.Settings.UseMockData;
    }

    // ── PDF generation ──────────────────────────────────────────────────────

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

            // After the PDF is safely on disk, set the order status to "Complete".
            // We deliberately do this AFTER the PDF (not before): if status update
            // fails the user still has the invoice file.
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
    /// Calls <see cref="OrderStatusUpdateService"/> to mark the order Complete and
    /// surfaces the result. Skipped in mock mode (no live API). On failure the
    /// debug log is shown so the user can paste it into a bug report; on success
    /// the orders grid is refreshed so the new status appears immediately.
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

        // Always echo the trace into the debug output so it's there even when the
        // user dismisses the dialog without inspecting it.
        System.Diagnostics.Debug.WriteLine("[StatusUpdate] " + result.DebugLog);

        if (result.Success)
        {
            SetStatus($"A rendelés ({order.DisplayOrderNumber}) állapota frissítve: Complete.");

            var btn = MessageBox.Show(
                $"A számla létrejött, és a rendelés állapota \"Complete\"-re frissült.\n\n" +
                $"Rendelés: {order.DisplayOrderNumber}\n" +
                "Megjeleníti a részletes naplót?",
                "Állapot frissítve",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (btn == DialogResult.Yes)
                ShowStatusLog("Állapot frissítés naplója", result.DebugLog);

            // Pull a fresh list so the grid reflects the new server state.
            await LoadOrdersAsync();
        }
        else
        {
            SetStatus($"A rendelés állapotának frissítése sikertelen: {result.ErrorMessage}");
            var btn = MessageBox.Show(
                $"A számla létrejött, de a rendelés állapotát NEM sikerült frissíteni.\n\n" +
                $"{result.ErrorMessage}\n\n" +
                "Megjeleníti a részletes naplót?",
                "Állapot frissítési hiba",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (btn == DialogResult.Yes)
                ShowStatusLog("Állapot frissítés naplója (hiba)", result.DebugLog);
        }
    }

    private void ShowStatusLog(string title, string log)
    {
        using var dlg = new StatusUpdateLogForm(title, log);
        dlg.ShowDialog(this);
    }

    /// <summary>
    /// Reverts the currently selected order from "Complete" back to "Received".
    /// The button is only enabled when the selected order is in Complete state
    /// (see <see cref="UpdateRevertButtonState"/>), but we also re-validate here
    /// in case the underlying state changed since the last selection event.
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

        var confirm = MessageHelper.ShowQuestion(
            $"Biztosan vissza szeretné állítani a(z) {_selectedOrder.DisplayOrderNumber} számú " +
            $"rendelést \"Complete\" állapotból \"Received\" állapotba?\n\n" +
            "Ez a művelet a Hotcakes szerveren is megtörténik.",
            "Megerősítés");
        if (confirm != DialogResult.Yes) return;

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

        System.Diagnostics.Debug.WriteLine("[StatusUpdate] " + result.DebugLog);

        if (result.Success)
        {
            SetStatus($"A rendelés ({_selectedOrder.DisplayOrderNumber}) állapota visszaállítva: Received.");

            var btn = MessageBox.Show(
                $"A rendelés állapota \"Received\"-re visszaállt.\n\n" +
                $"Rendelés: {_selectedOrder.DisplayOrderNumber}\n" +
                "Megjeleníti a részletes naplót?",
                "Állapot visszaállítva",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (btn == DialogResult.Yes)
                ShowStatusLog("Állapot visszaállítás naplója", result.DebugLog);

            // Refresh the grid so the new status is visible. LoadOrdersAsync clears
            // _selectedOrder, which will leave the revert button disabled — that's
            // the correct end state since the order is no longer Complete.
            await LoadOrdersAsync();
        }
        else
        {
            SetStatus($"A rendelés állapotának visszaállítása sikertelen: {result.ErrorMessage}");
            var btn = MessageBox.Show(
                $"A rendelés állapotát NEM sikerült visszaállítani.\n\n" +
                $"{result.ErrorMessage}\n\n" +
                "Megjeleníti a részletes naplót?",
                "Állapot visszaállítási hiba",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (btn == DialogResult.Yes)
                ShowStatusLog("Állapot visszaállítás naplója (hiba)", result.DebugLog);

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

    // ── API settings (in-app re-open) ───────────────────────────────────────

    /// <summary>
    /// Opens <see cref="ApiSettingsForm"/> so the user can change the Hotcakes
    /// connection (Base URL / API path / API key) without restarting the app
    /// or hand-editing JSON files.
    ///
    /// On a successful save, <see cref="ApiSettingsForm.SaveAndClose"/> already
    /// mirrors the new values into <see cref="Program.Settings"/> — but the
    /// existing service instances captured the OLD URL/key, so we recreate them
    /// here and refresh the UI bits that depend on the URL.
    /// </summary>
    private async Task OpenApiSettingsAsync()
    {
        using var dlg = new ApiSettingsForm();
        var result = dlg.ShowDialog(this);
        if (result != DialogResult.OK) return;

        // Settings have already been written by the form. Rebuild services that
        // captured the old configuration.
        _apiService    = new HotcakesApiService(Program.Settings);
        _statusService = new OrderStatusUpdateService(Program.Settings);

        // Refresh the URL label in the top panel.
        _lblApiUrl.Text =
            $"API: {Program.Settings.Hotcakes.BaseUrl.TrimEnd('/')}/" +
            $"{Program.Settings.Hotcakes.ApiBasePath.Trim('/')}";

        SetStatus("API beállítások frissítve — rendelések újratöltése...");
        await LoadOrdersAsync();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        if (IsDisposed) return;
        if (InvokeRequired) Invoke(() => _lblStatus.Text = message);
        else _lblStatus.Text = message;
    }
}
