using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.ViewModels;

/// <summary>
/// Flat display-only projection of OrderLine for the items DataGridView.
///
/// Only contains the six columns shown in the UI. All internal API fields
/// (IDs, flags, tax internals, shipping dimensions, etc.) are excluded.
///
/// HTML in ProductShortDescription is stripped via OrderLine.VariantDisplay
/// so the Option column always contains plain text.
/// </summary>
public sealed class OrderItemViewModel
{
    public string ProductName { get; }

    /// <summary>SKU from ProductSku field.</summary>
    public string Sku { get; }

    /// <summary>
    /// Plain-text option/variant info extracted from ProductShortDescription HTML.
    /// Example: "&lt;ul&gt;&lt;li&gt;Szín: XL&lt;/li&gt;&lt;/ul&gt;" → "Szín: XL"
    /// Empty string when no option data is present.
    /// </summary>
    public string Option { get; }

    public int Quantity { get; }

    /// <summary>AdjustedPricePerItem when non-zero, otherwise BasePricePerItem.</summary>
    public decimal UnitPrice { get; }

    /// <summary>LineTotal when non-zero, otherwise Quantity × UnitPrice.</summary>
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
