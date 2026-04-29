namespace HotcakesWinFormsApp.UI;

/// <summary>
/// Az alkalmazás összes form-ja által használt központi szín- és font-paletta.
///
/// A paletta szándékosan a Pawpromise admin referencia dizájnt tükrözi:
///   • sötét bor / burgundi fejléc-csík
///   • krémszínű lap-háttér
///   • fehér „kártya" panelek
///   • narancssárga fizetési állapot szöveg
///   • lágy rózsaszín, lekerekített pill-ek a rendelés-állapotokhoz
///
/// Ha minden ilyen érték egy helyen van, a kinézet könnyen finomhangolható
/// később, anélkül hogy a tényleges form-elrendezés kódhoz nyúlni kelljen.
/// </summary>
internal static class Theme
{
    // ── Felületek ────────────────────────────────────────────────────────────
    public static readonly Color HeaderBg     = Color.FromArgb(92, 31, 42);    // #5C1F2A bor
    public static readonly Color HeaderBgDark = Color.FromArgb(72, 22, 32);    // hover
    public static readonly Color HeaderText   = Color.White;
    public static readonly Color HeaderMuted  = Color.FromArgb(220, 200, 205);

    public static readonly Color PageBg       = Color.FromArgb(250, 245, 240); // krém
    public static readonly Color CardBg       = Color.White;
    public static readonly Color CardBorder   = Color.FromArgb(232, 220, 208); // világos bézs
    public static readonly Color SubtleBorder = Color.FromArgb(238, 228, 220);

    // ── Szöveg ───────────────────────────────────────────────────────────────
    public static readonly Color TextPrimary   = Color.FromArgb(44, 31, 31);
    public static readonly Color TextSecondary = Color.FromArgb(139, 123, 123);
    public static readonly Color TextMuted     = Color.FromArgb(170, 158, 158);

    // ── Akcent / gombok ──────────────────────────────────────────────────────
    public static readonly Color Accent         = Color.FromArgb(92, 31, 42);   // bor
    public static readonly Color AccentHover    = Color.FromArgb(112, 44, 56);
    public static readonly Color AccentPressed  = Color.FromArgb(72, 22, 32);
    public static readonly Color AccentText     = Color.White;

    // Ghost gomb (átlátszó kitöltés, bor szegély)
    public static readonly Color GhostBorder    = Color.FromArgb(210, 188, 184);
    public static readonly Color GhostHover     = Color.FromArgb(245, 232, 228);
    public static readonly Color GhostPressed   = Color.FromArgb(238, 220, 215);

    // ── Állapot színek ───────────────────────────────────────────────────────
    /// <summary>Fizetési állapot — vastag színes szöveg, pill háttér nélkül.</summary>
    public static readonly Color PaymentPaidText   = Color.FromArgb(214, 130, 60);  // narancs-borostyán a „Fizetve"-hez
    public static readonly Color PaymentPendingText= Color.FromArgb(214, 130, 60);  // ugyanaz a család a „Függőben"-hez
    public static readonly Color PaymentOtherText  = Color.FromArgb(139, 123, 123);

    /// <summary>Rendelés állapot pill — lágy rózsaszín háttér, bor szöveg.</summary>
    public static readonly Color StatusPillBg   = Color.FromArgb(245, 214, 204);
    public static readonly Color StatusPillText = Color.FromArgb(92, 31, 42);

    public static readonly Color StatusPillNeutralBg   = Color.FromArgb(232, 220, 208);
    public static readonly Color StatusPillNeutralText = Color.FromArgb(92, 75, 75);

    // ── Grid színek ──────────────────────────────────────────────────────────
    public static readonly Color GridHeaderBg     = Color.FromArgb(252, 248, 244);
    public static readonly Color GridHeaderText   = Color.FromArgb(120, 95, 95);
    public static readonly Color GridLine         = Color.FromArgb(240, 230, 222);
    public static readonly Color GridSelectionBg  = Color.FromArgb(250, 235, 230);
    public static readonly Color GridSelectionText= Color.FromArgb(44, 31, 31);

    // ── Fontok ───────────────────────────────────────────────────────────────
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
