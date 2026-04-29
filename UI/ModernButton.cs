using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HotcakesWinFormsApp.UI;

/// <summary>
/// Tiszta, lekerekített gomb — a <see cref="Control"/>-ból származik, nem a
/// <see cref="Button"/>-ból, így mi birtokoljuk a teljes paint pipeline-t.
/// Nincs harc az ősosztály fókusz-téglalapjával, nincs duplán festett szöveg,
/// nincs villogó hover-átmenet.
///
/// Két vizuális változat:
///   • <see cref="ButtonStyle.Primary"/> — tömör bor kitöltés, fehér szöveg.
///                                         Hős akciókhoz.
///   • <see cref="ButtonStyle.Ghost"/>   — átlátszó kitöltés, vékony bor szegély.
///                                         Másodlagos / mégse akciókhoz.
///
/// Implementálja az <see cref="IButtonControl"/>-t, így működik a
/// <c>Form.AcceptButton</c> / <c>Form.CancelButton</c>-nal.
/// </summary>
internal class ModernButton : Control, IButtonControl
{
    public enum ButtonStyle { Primary, Ghost }

    private ButtonStyle _style = ButtonStyle.Primary;
    private bool _hover;
    private bool _pressed;
    private bool _isDefault;

    public ButtonStyle Style
    {
        get => _style;
        set { _style = value; Invalidate(); }
    }

    /// <summary>Sarok-sugár pixelben. Alapértelmezetten 10 — tiszta és visszafogott.</summary>
    public int CornerRadius { get; set; } = 10;

    public DialogResult DialogResult { get; set; } = DialogResult.None;

    public ModernButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.UserPaint
               | ControlStyles.SupportsTransparentBackColor
               | ControlStyles.StandardClick
               | ControlStyles.Selectable, true);

        BackColor = Color.Transparent;
        ForeColor = Theme.AccentText;
        Font      = Theme.ButtonFont;
        Cursor    = Cursors.Hand;
        Size      = new Size(140, 36);
        TabStop   = true;
    }

    // ── IButtonControl ──────────────────────────────────────────────────────
    public void NotifyDefault(bool value)
    {
        _isDefault = value;
        Invalidate();
    }

    public void PerformClick()
    {
        if (CanSelect) OnClick(EventArgs.Empty);
    }

    // ── Egér / billentyűzet állapot ─────────────────────────────────────────
    protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _pressed = true; Focus(); Invalidate(); }
        base.OnMouseDown(e);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _pressed = false; Invalidate(); }
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space) { _pressed = true; Invalidate(); }
        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            _pressed = false;
            Invalidate();
            PerformClick();
        }
        else if (e.KeyCode == Keys.Enter && _isDefault)
        {
            PerformClick();
        }
        base.OnKeyUp(e);
    }

    // Csak akkor mutatunk halvány fókusz-gyűrűt, ha a gombnak fókusza van ÉS
    // az egér nem épp fölötte van. Így nem marad ott egy kattintás után
    // ácsorgó gyűrű, miközben a billentyűzetes felhasználók kapnak látható célt.
    protected override bool ShowFocusCues => Focused && !_hover;

    // ── Festés ──────────────────────────────────────────────────────────────
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        ResolveColors(out var fill, out var border, out var text);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, CornerRadius);

        if (fill.A > 0)
        {
            using var brush = new SolidBrush(fill);
            g.FillPath(brush, path);
        }

        if (border.A > 0)
        {
            using var pen = new Pen(border, 1f);
            g.DrawPath(pen, path);
        }

        // Szöveg — pontosan középre rajzolva, semmi extra glyph, semmi meglepetés.
        var caption = Text ?? "";
        if (caption.Length > 0)
        {
            TextRenderer.DrawText(g, caption, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis);
        }

        // Halvány billentyűzet-fókusz gyűrű — épp a lekerekített téglalapon
        // belülre rajzolva, hogy ne fedjen rá az él anti-aliasing-ára.
        if (Focused && ShowFocusCues)
        {
            var ringRect = new Rectangle(2, 2, Width - 5, Height - 5);
            using var ringPath = RoundedRect(ringRect, Math.Max(1, CornerRadius - 2));
            using var ringPen = new Pen(Color.FromArgb(120, Theme.Accent), 1f) { DashStyle = DashStyle.Dot };
            g.DrawPath(ringPen, ringPath);
        }
    }

    private void ResolveColors(out Color fill, out Color border, out Color text)
    {
        switch (_style)
        {
            case ButtonStyle.Primary:
                fill = !Enabled ? Color.FromArgb(200, 180, 165, 168)
                     : _pressed ? Theme.AccentPressed
                     : _hover   ? Theme.AccentHover
                     :            Theme.Accent;
                border = fill;
                text   = Enabled ? Theme.AccentText : Color.FromArgb(220, 245, 240, 240);
                break;

            case ButtonStyle.Ghost:
            default:
                fill = !Enabled ? Color.Transparent
                     : _pressed ? Theme.GhostPressed
                     : _hover   ? Theme.GhostHover
                     :            Color.Transparent;
                border = !Enabled ? Color.FromArgb(200, 200, 195)
                       : Theme.GhostBorder;
                text   = Enabled ? Theme.Accent : Color.FromArgb(140, 110, 95, 100);
                break;
        }
    }

    /// <summary>Egy lekerekített GraphicsPath-t épít a megadott sarok-sugárral.</summary>
    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = Math.Max(1, radius * 2);
        var path = new GraphicsPath();
        if (d >= r.Width || d >= r.Height)
        {
            path.AddEllipse(r);
            path.CloseFigure();
            return path;
        }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
