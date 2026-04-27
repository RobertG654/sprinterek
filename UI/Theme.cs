namespace HotcakesWinFormsApp.UI;

/// <summary>
/// Centralized color and font palette used by every form in the app.
///
/// The palette intentionally mirrors the Pawpromise admin reference design:
///   • dark wine/burgundy header bar
///   • cream page background
///   • white "card" panels
///   • orange payment-status text
///   • soft pink rounded pills for order status
///
/// Keeping these in one place makes it easy to tweak the look later without
/// touching the actual form layout code.
/// </summary>
internal static class Theme
{
    // ── Surfaces ─────────────────────────────────────────────────────────────
    public static readonly Color HeaderBg     = Color.FromArgb(92, 31, 42);    // #5C1F2A wine
    public static readonly Color HeaderBgDark = Color.FromArgb(72, 22, 32);    // hover
    public static readonly Color HeaderText   = Color.White;
    public static readonly Color HeaderMuted  = Color.FromArgb(220, 200, 205);

    public static readonly Color PageBg       = Color.FromArgb(250, 245, 240); // cream
    public static readonly Color CardBg       = Color.White;
    public static readonly Color CardBorder   = Color.FromArgb(232, 220, 208); // light beige
    public static readonly Color SubtleBorder = Color.FromArgb(238, 228, 220);

    // ── Text ─────────────────────────────────────────────────────────────────
    public static readonly Color TextPrimary   = Color.FromArgb(44, 31, 31);
    public static readonly Color TextSecondary = Color.FromArgb(139, 123, 123);
    public static readonly Color TextMuted     = Color.FromArgb(170, 158, 158);

    // ── Accent / buttons ─────────────────────────────────────────────────────
    public static readonly Color Accent         = Color.FromArgb(92, 31, 42);   // wine
    public static readonly Color AccentHover    = Color.FromArgb(112, 44, 56);
    public static readonly Color AccentPressed  = Color.FromArgb(72, 22, 32);
    public static readonly Color AccentText     = Color.White;

    // Ghost button (transparent fill, wine border)
    public static readonly Color GhostBorder    = Color.FromArgb(210, 188, 184);
    public static readonly Color GhostHover     = Color.FromArgb(245, 232, 228);
    public static readonly Color GhostPressed   = Color.FromArgb(238, 220, 215);

    // ── Status colours ───────────────────────────────────────────────────────
    /// <summary>Payment status — bold colored text, no pill background.</summary>
    public static readonly Color PaymentPaidText   = Color.FromArgb(214, 130, 60);  // orange-amber for "Fizetve"
    public static readonly Color PaymentPendingText= Color.FromArgb(214, 130, 60);  // same family for "Függőben"
    public static readonly Color PaymentOtherText  = Color.FromArgb(139, 123, 123);

    /// <summary>Order status pill — soft pink background, wine text.</summary>
    public static readonly Color StatusPillBg   = Color.FromArgb(245, 214, 204);
    public static readonly Color StatusPillText = Color.FromArgb(92, 31, 42);

    public static readonly Color StatusPillNeutralBg   = Color.FromArgb(232, 220, 208);
    public static readonly Color StatusPillNeutralText = Color.FromArgb(92, 75, 75);

    // ── Grid colors ──────────────────────────────────────────────────────────
    public static readonly Color GridHeaderBg     = Color.FromArgb(252, 248, 244);
    public static readonly Color GridHeaderText   = Color.FromArgb(120, 95, 95);
    public static readonly Color GridLine         = Color.FromArgb(240, 230, 222);
    public static readonly Color GridSelectionBg  = Color.FromArgb(250, 235, 230);
    public static readonly Color GridSelectionText= Color.FromArgb(44, 31, 31);

    // ── Fonts ────────────────────────────────────────────────────────────────
    public static Font BodyFont       => new("Segoe UI", 9.5f);
    public static Font BodyBoldFont   => new("Segoe UI", 9.5f, FontStyle.Bold);
    public static Font SmallFont      => new("Segoe UI", 8.25f);
    public static Font SmallBoldFont  => new("Segoe UI", 8.25f, FontStyle.Bold);
    public static Font SectionFont    => new("Segoe UI Semibold", 10.5f, FontStyle.Bold);
    public static Font HeaderTitleFont=> new("Segoe UI Semibold", 13f, FontStyle.Bold);
    public static Font ButtonFont     => new("Segoe UI Semibold", 9.5f, FontStyle.Bold);
    public static Font GridHeaderFont => new("Segoe UI Semibold", 8.5f, FontStyle.Bold);
    public static Font CellFont       => new("Segoe UI", 9.25f);
    public static Font PillFont       => new("Segoe UI Semibold", 8f, FontStyle.Bold);
    public static Font PaymentFont    => new("Segoe UI Semibold", 8.75f, FontStyle.Bold);
}
