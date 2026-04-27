using System.Drawing.Drawing2D;

namespace HotcakesWinFormsApp.UI;

/// <summary>
/// Wine-coloured brand strip rendered at the top of every form.
///
/// Layout:
///   [● logo dot]   [APP TITLE]                                [right slot]   [👤 avatar]
///
/// The "right slot" is a generic container: callers put refresh / settings buttons
/// there. The avatar circle is purely decorative — it mirrors the reference design's
/// "ADMINISZTRÁTOR A" pill — but does not add any auth functionality.
/// </summary>
internal class AppHeaderBar : Panel
{
    private readonly Label _title;
    private readonly Label _subtitle;
    public Panel RightSlot { get; }

    public string Title
    {
        get => _title.Text;
        set => _title.Text = (value ?? "").ToUpperInvariant();
    }

    public string Subtitle
    {
        get => _subtitle.Text;
        set
        {
            _subtitle.Text = value ?? "";
            _subtitle.Visible = !string.IsNullOrWhiteSpace(_subtitle.Text);
        }
    }

    public AppHeaderBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.UserPaint, true);

        Dock = DockStyle.Top;
        Height = 64;
        BackColor = Theme.HeaderBg;
        ForeColor = Theme.HeaderText;

        _title = new Label
        {
            AutoSize = true,
            Font = Theme.HeaderTitleFont,
            ForeColor = Theme.HeaderText,
            BackColor = Color.Transparent,
            Location = new Point(56, 14),
            Text = "PAWPROMISE RENDELÉSEK"
        };
        _subtitle = new Label
        {
            AutoSize = true,
            Font = Theme.SmallFont,
            ForeColor = Theme.HeaderMuted,
            BackColor = Color.Transparent,
            Location = new Point(56, 38),
            Visible = false
        };

        RightSlot = new Panel
        {
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Height = 40,
            Width = 420
        };

        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(RightSlot);

        Resize += (_, _) => LayoutChildren();
        LayoutChildren();
    }

    private void LayoutChildren()
    {
        // Right slot gets pinned to the right edge, leaving room for the avatar circle.
        const int avatarSize = 36;
        const int avatarRightMargin = 20;
        int rightEdge = Width - avatarRightMargin - avatarSize - 14;
        RightSlot.Height = 40;
        RightSlot.Top = (Height - RightSlot.Height) / 2;
        RightSlot.Left = Math.Max(120, rightEdge - RightSlot.Width);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Bottom hairline to separate the header from the page body.
        using (var pen = new Pen(Color.FromArgb(70, 22, 30)))
            g.DrawLine(pen, 0, Height - 1, Width, Height - 1);

        // Logo "dot" — a small soft-pink circle, like the reference mark.
        var dotRect = new Rectangle(20, (Height - 22) / 2, 22, 22);
        using (var b = new SolidBrush(Color.FromArgb(245, 214, 204)))
            g.FillEllipse(b, dotRect);
        using (var b = new SolidBrush(Theme.HeaderBg))
            g.FillEllipse(b, dotRect.X + 6, dotRect.Y + 6, 10, 10);

        // Decorative right-side avatar circle ("A" for Adminisztrátor).
        const int avatarSize = 36;
        const int avatarRightMargin = 20;
        var avatarRect = new Rectangle(
            Width - avatarRightMargin - avatarSize,
            (Height - avatarSize) / 2,
            avatarSize, avatarSize);
        using (var b = new SolidBrush(Color.FromArgb(245, 214, 204)))
            g.FillEllipse(b, avatarRect);
        using (var pen = new Pen(Color.FromArgb(220, 180, 175)))
            g.DrawEllipse(pen, avatarRect);

        var letterFont = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
        TextRenderer.DrawText(g, "A", letterFont, avatarRect, Theme.Accent,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        letterFont.Dispose();

        // "ADMINISZTRÁTOR" label to the left of the avatar.
        var adminLabelFont = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
        var labelSize = TextRenderer.MeasureText(g, "ADMINISZTRÁTOR", adminLabelFont);
        var labelPoint = new Point(avatarRect.Left - labelSize.Width - 10,
                                   (Height - labelSize.Height) / 2);
        TextRenderer.DrawText(g, "ADMINISZTRÁTOR", adminLabelFont, labelPoint,
            Theme.HeaderMuted, TextFormatFlags.NoPadding);
        adminLabelFont.Dispose();
    }
}
