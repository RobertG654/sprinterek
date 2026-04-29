using System.Drawing.Drawing2D;

namespace HotcakesWinFormsApp.UI;

/// <summary>
/// Segédfüggvények a DataGridView CellPainting handlerek számára, a
/// referencia dizájn két egyedi állapot-vizualizációjának kirajzolásához:
///
///   • fizetési állapot — vastag narancssárga szöveg, háttér pill nélkül.
///   • rendelés állapot — szöveg lágy rózsaszín, lekerekített pill-ben.
///
/// A festést manuálisan csináljuk, hogy a cella továbbra is együttműködjön
/// a kijelöléssel, váltakozó háttérrel stb. (a hátteret is mi rajzoljuk először).
/// </summary>
internal static class StatusCellPainter
{
    public static void PaintPaymentStatus(DataGridViewCellPaintingEventArgs e, string value)
    {
        PaintCellBackground(e);

        var color = ResolvePaymentColor(value);
        var font  = Theme.PaymentFont;
        TextRenderer.DrawText(e.Graphics!, (value ?? "").ToUpperInvariant(), font,
            e.CellBounds, color,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding |
            TextFormatFlags.LeftAndRightPadding);
        font.Dispose();
        e.Handled = true;
    }

    public static void PaintStatusPill(DataGridViewCellPaintingEventArgs e, string value)
    {
        PaintCellBackground(e);

        var text = (value ?? "").ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(text)) { e.Handled = true; return; }

        var font = Theme.PillFont;
        var textSize = TextRenderer.MeasureText(e.Graphics!, text, font, Size.Empty, TextFormatFlags.NoPadding);

        int padX = 12;
        int padY = 5;
        int pillW = Math.Min(textSize.Width + padX * 2, e.CellBounds.Width - 12);
        int pillH = textSize.Height + padY * 2;

        int x = e.CellBounds.Left + 8;
        int y = e.CellBounds.Top + (e.CellBounds.Height - pillH) / 2;

        var pillRect = new Rectangle(x, y, pillW, pillH);

        var (bg, fg) = ResolveStatusColors(value);

        var g = e.Graphics!;
        var prevSmoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = ModernButton.RoundedRect(pillRect, pillH / 2))
        using (var brush = new SolidBrush(bg))
            g.FillPath(brush, path);
        g.SmoothingMode = prevSmoothing;

        TextRenderer.DrawText(g, text, font, pillRect, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        font.Dispose();
        e.Handled = true;
    }

    /// <summary>
    /// Újrafesti a cella hátterét — a kijelölést is figyelembe véve —, hogy
    /// utána ráfesthessünk anélkül, hogy az alapértelmezett szöveg-rendering
    /// belezavarna.
    /// </summary>
    private static void PaintCellBackground(DataGridViewCellPaintingEventArgs e)
    {
        var g = e.Graphics!;
        bool selected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
        var bg = selected ? Theme.GridSelectionBg : (e.CellStyle?.BackColor ?? Theme.CardBg);
        using (var brush = new SolidBrush(bg))
            g.FillRectangle(brush, e.CellBounds);

        // Alsó sor-elválasztó, ahogy a referencia dizájnban.
        using (var pen = new Pen(Theme.GridLine))
            g.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1,
                            e.CellBounds.Right, e.CellBounds.Bottom - 1);
    }

    private static Color ResolvePaymentColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Theme.PaymentOtherText;
        var v = value.Trim().ToLowerInvariant();
        if (v.Contains("fizet"))   return Theme.PaymentPaidText;
        if (v.Contains("függő") || v.Contains("fuggo") || v.Contains("pending"))
            return Theme.PaymentPendingText;
        return Theme.PaymentOtherText;
    }

    private static (Color bg, Color fg) ResolveStatusColors(string? value)
    {
        // Alapértelmezett a lágy rózsaszín pill — ez az „aktív" megjelenés
        // a referenciában. Néhány állapot („Cancelled" stb.) semleges szürke-bézs
        // változatot kap, így a felhasználó egy pillantással meg tudja
        // különböztetni őket.
        if (string.IsNullOrWhiteSpace(value)) return (Theme.StatusPillNeutralBg, Theme.StatusPillNeutralText);
        var v = value.Trim().ToLowerInvariant();
        if (v.Contains("cancel") || v.Contains("töröl") || v.Contains("torol"))
            return (Theme.StatusPillNeutralBg, Theme.StatusPillNeutralText);
        return (Theme.StatusPillBg, Theme.StatusPillText);
    }
}
