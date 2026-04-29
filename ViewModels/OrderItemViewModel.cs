using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.ViewModels;

/// <summary>
/// Lapos, csak megjelenítésre szánt projekció az OrderLine-ból, a tétel
/// DataGridView-hez.
///
/// Csak a hat oszlopot tartalmazza, ami az UI-on látszik. Minden belső API
/// mező (ID-k, flag-ek, ÁFA-belső adatok, szállítási méretek stb.) ki van
/// hagyva.
///
/// A ProductShortDescription HTML-jét az OrderLine.VariantDisplay-en keresztül
/// nyitjuk ki, így az Option oszlop mindig sima szöveget tartalmaz.
/// </summary>
public sealed class OrderItemViewModel
{
    public string ProductName { get; }

    /// <summary>SKU a ProductSku mezőből.</summary>
    public string Sku { get; }

    /// <summary>
    /// Tiszta szöveges opció / variáns infó, a ProductShortDescription HTML-ből
    /// kinyerve.
    /// Példa: "&lt;ul&gt;&lt;li&gt;Szín: XL&lt;/li&gt;&lt;/ul&gt;" → "Szín: XL"
    /// Üres karakterlánc, ha nincs opció adat.
    /// </summary>
    public string Option { get; }

    public int Quantity { get; }

    /// <summary>AdjustedPricePerItem ha nem nulla, különben BasePricePerItem.</summary>
    public decimal UnitPrice { get; }

    /// <summary>LineTotal ha nem nulla, különben Quantity × UnitPrice.</summary>
    public decimal LineTotal { get; }

    // ──────────────────────────────────────────────────────────────────────────

    public OrderItemViewModel(OrderLine line)
    {
        ProductName = line.DisplayName;
        Sku         = line.Sku;
        Option      = line.VariantDisplay;
        Quantity    = line.Quantity;
        UnitPrice   = line.UnitPrice;
        LineTotal   = line.LineTotalResolved;
    }
}
