using System.Drawing.Drawing2D;

namespace HotcakesWinFormsApp.UI;

/// <summary>
/// Fehér, lekerekített „kártya" panel vékony meleg-bézs szegéllyel. A
/// referencia dizájnnak megfelelően helyettesíti az alapértelmezett WinForms
/// <see cref="GroupBox"/> felirat-mezőjét.
///
/// A kártyának van saját <c>Header</c> területe felül, így a hívók
/// belerakhatnak egy szekció-címet (és opcionálisan jobbra igazított számláló /
/// hint szöveget) anélkül, hogy egyedi paint kóddal kellene foglalkozniuk.
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
        BackColor = Theme.PageBg;       // egyezik a szülővel — csak a lekerekített kitöltés átlátszatlan
        ForeColor = Theme.TextPrimary;
        Padding   = new Padding(16, 14, 16, 14);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        // A lekerekített forma sarok-réseit a lap háttérszínével festjük át,
        // hogy vizuálisan beleolvadjon a szülőbe. Manuálisan tesszük, mert
        // a UserPaint elnyomja az alapértelmezett háttér-kitöltést.
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
/// Szekció-fejléc csík egy <see cref="CardPanel"/> tetejére — bal oldalon a
/// vastag szekció-cím, jobb oldalon egy opcionális tompa számláló
/// (pl. „15 db rendelés").
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
