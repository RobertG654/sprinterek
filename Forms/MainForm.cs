using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;
using HotcakesWinFormsApp.Services;
using HotcakesWinFormsApp.ViewModels;

namespace HotcakesWinFormsApp.Forms;

/// <summary>
/// Main application form.
///
/// Layout:
///   Top panel (API URL info, mock badge, refresh button)
///   Orders group:
///     - search bar (filters OrderNumber / CustomerName / UserEmail in real time)
///     - DataGridView showing ONE page of 20 orders
///     - pager (Prev / "Page X / Y" / Next)
///   Lines group:
///     - DataGridView showing items of the selected order
///     - action buttons (invoice / label PDF)
///   Status strip
///
/// SEARCH + PAGINATION are client-side only — no extra API calls are issued.
/// </summary>
public class MainForm : Form
{
    // ── UI controls ──────────────────────────────────────────────────────────
    private Panel _topPanel = null!;
    private Label _lblApiUrl = null!;
    private Label _lblMockBadge = null!;
    private Button _btnRefresh = null!;
    private Button _btnApiSettings = null!;

    private GroupBox _grpOrders = null!;
    private DataGridView _dgvOrders = null!;

    // Search bar
    private Panel _searchPanel = null!;
    private Label _lblSearch = null!;
    private TextBox _txtSearch = null!;

    // Pagination controls
    private Panel _pagerPanel = null!;
    private Button _btnPrevPage = null!;
    private Button _btnNextPage = null!;
    private Label _lblPageIndicator = null!;

    private GroupBox _grpLines = null!;
    private DataGridView _dgvLines = null!;
    private Label _lblNoItemsInfo = null!;

    private Button _btnInvoice = null!;
    private Button _btnLabel = null!;
    private Button _btnRevertToReceived = null!;

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

    public MainForm()
    {
        _apiService = new HotcakesApiService(Program.Settings);
        _pdfService = new PdfService(Program.Company);
        _statusService = new OrderStatusUpdateService(Program.Settings);
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Hotcakes Rendelések";
        Size = new Size(1150, 720);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        BuildSplitContainer();
        BuildStatusStrip();
        BuildTopPanel();

        Load += async (_, _) => await LoadOrdersAsync();
    }

    // ── UI Construction ──────────────────────────────────────────────────────

    private void BuildTopPanel()
    {
        _topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.FromArgb(235, 240, 248),
            Padding = new Padding(8, 0, 8, 0)
        };

        _lblApiUrl = new Label
        {
            Text = $"API: {Program.Settings.Hotcakes.BaseUrl.TrimEnd('/')}/{Program.Settings.Hotcakes.ApiBasePath.Trim('/')}",
            AutoSize = true,
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(60, 60, 120),
            Location = new Point(8, 8)
        };

        _lblMockBadge = new Label
        {
            Text = Program.Settings.UseMockData ? "⚠ MOCK MÓD AKTÍV" : "",
            ForeColor = Color.DarkOrange,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(8, 28)
        };

        _btnRefresh = new Button
        {
            Text = "Rendelések frissítése",
            Size = new Size(175, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnRefresh.Click += async (_, _) => await LoadOrdersAsync();

        // Discoverable in-app way to update the API URL / key. Without this the
        // user would have to delete bin\...\apisettings.json by hand to force the
        // settings form to reappear (because that file overrides appsettings.json).
        _btnApiSettings = new Button
        {
            Text = "⚙ API beállítások",
            Size = new Size(150, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnApiSettings.Click += async (_, _) => await OpenApiSettingsAsync();

        _topPanel.Controls.AddRange(new Control[] { _lblApiUrl, _lblMockBadge, _btnApiSettings, _btnRefresh });
        PositionTopRightButtons();
        _topPanel.Resize += (_, _) => PositionTopRightButtons();
        Controls.Add(_topPanel);
    }

    private void PositionTopRightButtons()
    {
        _btnRefresh.Location     = new Point(_topPanel.Width - _btnRefresh.Width - 8, 9);
        _btnApiSettings.Location = new Point(_btnRefresh.Left - _btnApiSettings.Width - 6, 9);
    }

    private void BuildStatusStrip()
    {
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _lblStatus = new ToolStripStatusLabel("Kész.") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip.Items.Add(_lblStatus);
        Controls.Add(_statusStrip);
    }

    private void BuildSplitContainer()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 340,
            Panel1MinSize = 160,
            Panel2MinSize = 130
        };

        // ── Top panel: orders grid + search + pager ─────────────────────────
        _grpOrders = new GroupBox { Text = "Rendelések", Dock = DockStyle.Fill, Padding = new Padding(6) };
        _dgvOrders = CreateOrdersGrid();
        _searchPanel = CreateSearchPanel();
        _pagerPanel  = CreatePagerPanel();

        // Add Fill first so Top/Bottom docks carve out space from the edges.
        _grpOrders.Controls.Add(_dgvOrders);
        _grpOrders.Controls.Add(_pagerPanel);
        _grpOrders.Controls.Add(_searchPanel);
        split.Panel1.Controls.Add(_grpOrders);

        // ── Bottom panel: items section + action buttons ────────────────────
        var bottomLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        bottomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));

        _grpLines = new GroupBox { Text = "Rendelés tételei", Dock = DockStyle.Fill, Padding = new Padding(6) };
        _dgvLines = CreateLinesGrid();
        _grpLines.Controls.Add(_dgvLines);

        _lblNoItemsInfo = new Label
        {
            Text = "Válasszon ki egy rendelést a listából.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(100, 100, 120),
            Font = new Font("Segoe UI", 9f),
            Visible = true
        };
        _grpLines.Controls.Add(_lblNoItemsInfo);
        _dgvLines.Visible = false;

        var btnPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 6, 4, 4) };
        _btnInvoice = new Button { Text = "Számla generálása", Location = new Point(4, 4),   Size = new Size(160, 32) };
        _btnLabel   = new Button { Text = "Címke generálása",  Location = new Point(172, 4), Size = new Size(160, 32) };
        // Right-anchored "revert to Received" button. Only enabled when the currently
        // selected order is in the Complete state (see OnOrderSelectedAsync).
        _btnRevertToReceived = new Button
        {
            Text = "Visszaállítás 'Received'-re",
            Size = new Size(210, 32),
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
                new Point(btnPanel.Width - _btnRevertToReceived.Width - 8, 4);
        PositionRevertButton();
        btnPanel.Resize += (_, _) => PositionRevertButton();

        bottomLayout.Controls.Add(_grpLines, 0, 0);
        bottomLayout.Controls.Add(btnPanel,  0, 1);
        split.Panel2.Controls.Add(bottomLayout);

        Controls.Add(split);
    }

    /// <summary>Search bar docked to the top of the orders GroupBox.</summary>
    private Panel CreateSearchPanel()
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(2, 4, 2, 4) };

        _lblSearch = new Label
        {
            Text = "Keresés:",
            AutoSize = true,
            Location = new Point(2, 8),
            Font = new Font("Segoe UI", 9f)
        };
        _txtSearch = new TextBox
        {
            Location = new Point(70, 5),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Width = 400,
            PlaceholderText = "Szűrés — rendelésszám, vevő neve vagy email"
        };
        _txtSearch.TextChanged += (_, _) => ApplySearchAndPaging(resetPage: true);

        panel.Controls.Add(_lblSearch);
        panel.Controls.Add(_txtSearch);
        panel.Resize += (_, _) =>
        {
            _txtSearch.Width = Math.Max(100, panel.Width - _txtSearch.Left - 8);
        };
        return panel;
    }

    /// <summary>Pagination bar docked to the bottom of the orders GroupBox.</summary>
    private Panel CreatePagerPanel()
    {
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(2, 4, 2, 4) };

        _btnPrevPage = new Button
        {
            Text = "◀ Előző",
            Size = new Size(90, 26),
            Location = new Point(2, 4),
            Enabled = false
        };
        _btnPrevPage.Click += (_, _) => ChangePage(-1);

        _lblPageIndicator = new Label
        {
            AutoSize = true,
            Location = new Point(110, 9),
            Font = new Font("Segoe UI", 9f),
            Text = "Oldal 0 / 0"
        };

        _btnNextPage = new Button
        {
            Text = "Következő ▶",
            Size = new Size(110, 26),
            Location = new Point(340, 4),
            Enabled = false
        };
        _btnNextPage.Click += (_, _) => ChangePage(+1);

        panel.Controls.AddRange(new Control[] { _btnPrevPage, _lblPageIndicator, _btnNextPage });
        return panel;
    }

    // ── Grid configuration ──────────────────────────────────────────────────

    private DataGridView CreateOrdersGrid()
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
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(220, 225, 235)
        };

        dgv.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "colOrderNum", HeaderText = "Rendelésszám",
                DataPropertyName = nameof(OrderViewModel.OrderNumber), FillWeight = 11
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colCustomer", HeaderText = "Vevő neve",
                DataPropertyName = nameof(OrderViewModel.CustomerName), FillWeight = 17
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
                    Format = "N0"
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
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy.MM.dd HH:mm" }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colStatus", HeaderText = "Állapot",
                DataPropertyName = nameof(OrderViewModel.Status), FillWeight = 9
            }
        });

        dgv.SelectionChanged += async (_, _) => await OnOrderSelectedAsync();
        return dgv;
    }

    private DataGridView CreateLinesGrid()
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
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(220, 225, 235)
        };

        dgv.Columns.AddRange(new DataGridViewColumn[]
        {
            new DataGridViewTextBoxColumn
            {
                Name = "colProductName", HeaderText = "Termék neve",
                DataPropertyName = nameof(OrderItemViewModel.ProductName), FillWeight = 30
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colSku", HeaderText = "SKU",
                DataPropertyName = nameof(OrderItemViewModel.Sku), FillWeight = 13
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colOption", HeaderText = "Variáns / opció",
                DataPropertyName = nameof(OrderItemViewModel.Option), FillWeight = 22
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colQty", HeaderText = "Mennyiség",
                DataPropertyName = nameof(OrderItemViewModel.Quantity), FillWeight = 7,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colUnitPrice", HeaderText = "Egységár (Ft)",
                DataPropertyName = nameof(OrderItemViewModel.UnitPrice), FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" }
            },
            new DataGridViewTextBoxColumn
            {
                Name = "colLineTotal", HeaderText = "Összeg (Ft)",
                DataPropertyName = nameof(OrderItemViewModel.LineTotal), FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" }
            }
        });

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
