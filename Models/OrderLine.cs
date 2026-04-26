using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// A single product line item from:
///   GET /orders/{bvin}/items  →  Content.Items[n]
///
/// CONFIRMED REAL FIELD NAMES (from live API):
///   ProductName, ProductSku, Quantity,
///   BasePricePerItem, AdjustedPricePerItem, LineTotal,
///   ProductShortDescription (often HTML with option/variant info),
///   SelectionData
///
/// NOTE: The real SKU field is "ProductSku", not "Sku".
///
/// VARIANT/OPTION DATA:
///   ProductShortDescription often contains HTML such as:
///     <ul class="lineitemoptions"><li>Szín: XL</li></ul>
///   VariantDisplay extracts readable text from this HTML.
///   Falls back to SelectionData if ProductShortDescription is empty.
/// </summary>
public class OrderLine
{
    // ── Identity fields ─────────────────────────────────────────────────────
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    /// <summary>Integer store ID. API sends as number.</summary>
    [JsonPropertyName("StoreId")]
    public int StoreId { get; set; }

    [JsonPropertyName("ProductName")]
    public string ProductName { get; set; } = "";

    /// <summary>
    /// Short description — often contains HTML-encoded variant/option data.
    /// Use VariantDisplay for a readable version.
    /// </summary>
    [JsonPropertyName("ProductShortDescription")]
    public string ProductShortDescription { get; set; } = "";

    /// <summary>Real SKU field in the Hotcakes API payload.</summary>
    [JsonPropertyName("ProductSku")]
    public string Sku { get; set; } = "";

    [JsonPropertyName("Quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("BasePricePerItem")]
    public decimal BasePricePerItem { get; set; }

    [JsonPropertyName("AdjustedPricePerItem")]
    public decimal AdjustedPricePerItem { get; set; }

    [JsonPropertyName("LineTotal")]
    public decimal LineTotal { get; set; }

    // ── Quantity sub-fields ─────────────────────────────────────────────────
    [JsonPropertyName("QuantityReturned")]
    public int QuantityReturned { get; set; }

    [JsonPropertyName("QuantityShipped")]
    public int QuantityShipped { get; set; }

    [JsonPropertyName("FreeQuantity")]
    public int FreeQuantity { get; set; }

    // ── Shipping cost fields ────────────────────────────────────────────────
    [JsonPropertyName("ShippingPortion")]
    public decimal ShippingPortion { get; set; }

    [JsonPropertyName("ShippingCharge")]
    public decimal ShippingCharge { get; set; }

    [JsonPropertyName("ExtraShipCharge")]
    public decimal ExtraShipCharge { get; set; }

    [JsonPropertyName("ShipFromMode")]
    public int ShipFromMode { get; set; }

    // ── Tax fields ──────────────────────────────────────────────────────────
    [JsonPropertyName("TaxRate")]
    public decimal TaxRate { get; set; }

    [JsonPropertyName("TaxPortion")]
    public decimal TaxPortion { get; set; }

    [JsonPropertyName("TaxSchedule")]
    public int TaxSchedule { get; set; }

    // ── Physical dimensions ─────────────────────────────────────────────────
    [JsonPropertyName("ProductShippingWeight")]
    public decimal ProductShippingWeight { get; set; }

    [JsonPropertyName("ProductShippingLength")]
    public decimal ProductShippingLength { get; set; }

    [JsonPropertyName("ProductShippingWidth")]
    public decimal ProductShippingWidth { get; set; }

    [JsonPropertyName("ProductShippingHeight")]
    public decimal ProductShippingHeight { get; set; }

    // ── Boolean flags ───────────────────────────────────────────────────────
    [JsonPropertyName("IsUserSuppliedPrice")]
    public bool IsUserSuppliedPrice { get; set; }

    [JsonPropertyName("IsBundle")]
    public bool IsBundle { get; set; }

    [JsonPropertyName("IsGiftCard")]
    public bool IsGiftCard { get; set; }

    [JsonPropertyName("IsNonShipping")]
    public bool IsNonShipping { get; set; }

    [JsonPropertyName("ShipSeparately")]
    public bool ShipSeparately { get; set; }

    [JsonPropertyName("IsUpchargeAllowed")]
    public bool IsUpchargeAllowed { get; set; }

    // ── Status strings ──────────────────────────────────────────────────────
    [JsonPropertyName("StatusCode")]
    public string StatusCode { get; set; } = "";

    [JsonPropertyName("StatusName")]
    public string StatusName { get; set; } = "";

    [JsonPropertyName("OrderBvin")]
    public string OrderBvin { get; set; } = "";

    [JsonPropertyName("ProductId")]
    public string ProductId { get; set; } = "";

    [JsonPropertyName("VariantId")]
    public string VariantId { get; set; } = "";

    /// <summary>
    /// Option/variant selection references. The API sends this as an array of objects,
    /// each holding internal GUIDs that cross-reference Hotcakes option records.
    /// The human-readable option text is in ProductShortDescription, not here.
    /// Use VariantDisplay for display.
    /// </summary>
    [JsonPropertyName("SelectionData")]
    public List<SelectionDataEntry> SelectionData { get; set; } = new();

    // ── Computed display properties ─────────────────────────────────────────

    /// <summary>Display name — ProductName, never empty.</summary>
    [JsonIgnore]
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(ProductName) ? ProductName : "Ismeretlen termék";

    /// <summary>
    /// Best available unit price.
    /// Prefers AdjustedPricePerItem (post-discount), falls back to BasePricePerItem.
    /// Grid and PDF code should use this.
    /// </summary>
    [JsonIgnore]
    public decimal UnitPrice =>
        AdjustedPricePerItem != 0 ? AdjustedPricePerItem : BasePricePerItem;

    /// <summary>
    /// Line total for display/PDF. Uses LineTotal if non-zero,
    /// otherwise computes Quantity × UnitPrice.
    /// </summary>
    [JsonIgnore]
    public decimal LineTotalResolved =>
        LineTotal != 0 ? LineTotal : Quantity * UnitPrice;

    /// <summary>
    /// Readable variant/option text for display.
    ///
    /// Priority:
    ///   1. &lt;li&gt; text extracted from ProductShortDescription HTML
    ///      e.g. "&lt;ul&gt;&lt;li&gt;Szín: XL&lt;/li&gt;&lt;/ul&gt;" → "Szín: XL"
    ///   2. ProductShortDescription with all HTML tags stripped
    ///   3. SelectionData as-is
    ///   4. Empty string
    /// </summary>
    [JsonIgnore]
    public string VariantDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ProductShortDescription))
            {
                // Try to extract <li>...</li> inner text (covers the confirmed HTML format)
                var liMatches = Regex.Matches(ProductShortDescription,
                    @"<li[^>]*>(.*?)</li>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (liMatches.Count > 0)
                {
                    var parts = liMatches
                        .Select(m => StripTags(m.Groups[1].Value).Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s));
                    var joined = string.Join(", ", parts);
                    if (!string.IsNullOrWhiteSpace(joined)) return joined;
                }

                // Fallback: strip all HTML tags from the full string
                var stripped = StripTags(ProductShortDescription).Trim();
                if (!string.IsNullOrWhiteSpace(stripped)) return stripped;
            }

            // SelectionData entries contain only internal GUIDs — not user-readable.
            // ProductShortDescription is the only source of readable option text.
            return "";
        }
    }

    /// <summary>Strips all HTML tags from a string using a simple regex.</summary>
    private static string StripTags(string html) =>
        Regex.Replace(html, "<[^>]+>", "");
}

/// <summary>
/// One element of the SelectionData array on an OrderLine.
///
/// CONFIRMED real shape:
///   { "OptionBvin": "cc24849502574e30875f85c0e41cd3c7",
///     "SelectionData": "a0293857050c489b8f0c5ab21524493b" }
///
/// Both values are internal Hotcakes GUIDs (not human-readable).
/// Human-readable option text lives in OrderLine.ProductShortDescription.
/// </summary>
public class SelectionDataEntry
{
    [JsonPropertyName("OptionBvin")]
    public string OptionBvin { get; set; } = "";

    [JsonPropertyName("SelectionData")]
    public string SelectionData { get; set; } = "";
}
