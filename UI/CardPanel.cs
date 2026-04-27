using System.Drawing.Drawing2D;

namespace HotcakesWinFormsApp.UI;

/// <summary>
/// White rounded "card" panel with a thin warm-beige border. Replaces the default
/// WinForms <see cref="GroupBox"/> caption box with something that matches the
/// reference design.
///
/// The card has its own <c>Header</c> area at the top so callers can drop in a
/// section title (and optional right-aligned counter / hint text) without dealing
/// with custom paint code.
/// </summary>
internal class CardPanel : Panel
{
    public int CornerRadius { get; set; } = 12;

    public CardPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.UserPaint, true);
        BackColor = Theme.PageBg;       // match parent — only the rounded fill is opaque
        ForeColor = Theme.TextPrimary;
        Padding   = new Padding(16, 14, 16, 14);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        // Clear the corner gaps with the page background so the rounded
        // shape blends visually into the parent. We do this manually
        // because UserPaint suppresses the default background fill.
        using (var bg = new SolidBrush(Theme.PageBg))
            g.FillRectangle(bg, ClientRectangle);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = ModernButton.RoundedRect(rect, CornerRadius);
        using (var brush = new SolidBrush(Theme.CardBg))
            g.FillPath(brush, path);
        using (var pen = new Pen(Theme.CardBorder))
            g.DrawPath(pen, path);
    }
}

/// <summary>
/// Section header strip used at the top of a <see cref="CardPanel"/> — left side
/// shows the bold section title, right side shows an optional muted counter
/// (e.g. "15 db rendelés").
/// </summary>
internal class SectionHeader : Panel
{
    private readonly Label _title;
    private readonly Label _hint;

    public string Title
    {
        get => _title.Text;
        set => _title.Text = (value ?? "").ToUpperInvariant();
    }

    public string Hint
    {
        get => _hint.Text;
        set => _hint.Text = value ?? "";
    }

    public SectionHeader()
    {
        Dock = DockStyle.Top;
        Height = 28;
        BackColor = Color.Transparent;

        _title = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Left,
            Padding = new Padding(0, 5, 0, 0),
            Font = Theme.SectionFont,
            ForeColor = Theme.TextPrimary,
            BackColor = Color.Transparent,
            Text = ""
        };
        _hint = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            Padding = new Padding(0, 7, 2, 0),
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            Text = ""
        };

        Controls.Add(_hint);
        Controls.Add(_title);
    }
}
