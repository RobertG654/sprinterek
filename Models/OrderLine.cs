using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// Egyetlen terméktétel-sor a következőből:
///   GET /orders/{bvin}/items  →  Content.Items[n]
///
/// MEGERŐSÍTETT VALÓDI MEZŐNEVEK (élő API-ról):
///   ProductName, ProductSku, Quantity,
///   BasePricePerItem, AdjustedPricePerItem, LineTotal,
///   ProductShortDescription (gyakran HTML, opció / variáns infóval),
///   SelectionData
///
/// FONTOS: A valódi SKU mező neve „ProductSku", nem „Sku".
///
/// VARIÁNS / OPCIÓ ADATOK:
///   A ProductShortDescription gyakran ilyen HTML-t tartalmaz:
///     <ul class="lineitemoptions"><li>Szín: XL</li></ul>
///   A VariantDisplay olvasható szöveget nyer ki ebből a HTML-ből.
///   Ha a ProductShortDescription üres, a SelectionData-ra esik vissza.
/// </summary>
public class OrderLine
{
    // ── Azonosító mezők ─────────────────────────────────────────────────────
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    /// <summary>Egész számos bolt-azonosító. Az API számként küldi.</summary>
    [JsonPropertyName("StoreId")]
    public int StoreId { get; set; }

    [JsonPropertyName("ProductName")]
    public string ProductName { get; set; } = "";

    /// <summary>
    /// Rövid leírás — gyakran HTML kódolt variáns / opció adat.
    /// Olvasható verzióhoz használd a VariantDisplay-t.
    /// </summary>
    [JsonPropertyName("ProductShortDescription")]
    public string ProductShortDescription { get; set; } = "";

    /// <summary>A Hotcakes API payloadban a valódi SKU mező.</summary>
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

    // ── Mennyiségi al-mezők ─────────────────────────────────────────────────
    [JsonPropertyName("QuantityReturned")]
    public int QuantityReturned { get; set; }

    [JsonPropertyName("QuantityShipped")]
    public int QuantityShipped { get; set; }

    [JsonPropertyName("FreeQuantity")]
    public int FreeQuantity { get; set; }

    // ── Szállítási költség mezők ────────────────────────────────────────────
    [JsonPropertyName("ShippingPortion")]
    public decimal ShippingPortion { get; set; }

    [JsonPropertyName("ShippingCharge")]
    public decimal ShippingCharge { get; set; }

    [JsonPropertyName("ExtraShipCharge")]
    public decimal ExtraShipCharge { get; set; }

    [JsonPropertyName("ShipFromMode")]
    public int ShipFromMode { get; set; }

    // ── ÁFA mezők ───────────────────────────────────────────────────────────
    [JsonPropertyName("TaxRate")]
    public decimal TaxRate { get; set; }

    [JsonPropertyName("TaxPortion")]
    public decimal TaxPortion { get; set; }

    [JsonPropertyName("TaxSchedule")]
    public int TaxSchedule { get; set; }

    // ── Fizikai méretek ─────────────────────────────────────────────────────
    [JsonPropertyName("ProductShippingWeight")]
    public decimal ProductShippingWeight { get; set; }

    [JsonPropertyName("ProductShippingLength")]
    public decimal ProductShippingLength { get; set; }

    [JsonPropertyName("ProductShippingWidth")]
    public decimal ProductShippingWidth { get; set; }

    [JsonPropertyName("ProductShippingHeight")]
    public decimal ProductShippingHeight { get; set; }

    // ── Logikai jelzők ──────────────────────────────────────────────────────
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

    // ── Állapot string-ek ───────────────────────────────────────────────────
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
    /// Opció / variáns kiválasztási referenciák. Az API objektum-tömbként küldi,
    /// minden objektum belső GUID-okat tartalmaz, amik Hotcakes opció-rekordokra
    /// hivatkoznak.
    /// Az ember által olvasható opció-szöveg a ProductShortDescription-ben van,
    /// nem itt.
    /// Megjelenítéshez a VariantDisplay-t használd.
    /// </summary>
    [JsonPropertyName("SelectionData")]
    public List<SelectionDataEntry> SelectionData { get; set; } = new();

    // ── Származtatott megjelenítési tulajdonságok ───────────────────────────

    /// <summary>Megjelenítési név — ProductName, sosem üres.</summary>
    [JsonIgnore]
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(ProductName) ? ProductName : "Ismeretlen termék";

    /// <summary>
    /// A legjobb elérhető egységár.
    /// Elsősorban AdjustedPricePerItem (kedvezmény után), különben
    /// BasePricePerItem.
    /// A grid és a PDF kódnak ezt kell használnia.
    /// </summary>
    [JsonIgnore]
    public decimal UnitPrice =>
        AdjustedPricePerItem != 0 ? AdjustedPricePerItem : BasePricePerItem;

    /// <summary>
    /// Megjelenítéshez / PDF-hez használt sor-összeg. Ha a LineTotal nem nulla,
    /// azt használja, különben Quantity × UnitPrice számítást ad vissza.
    /// </summary>
    [JsonIgnore]
    public decimal LineTotalResolved =>
        LineTotal != 0 ? LineTotal : Quantity * UnitPrice;

    /// <summary>
    /// Megjelenítésre alkalmas, olvasható variáns / opció szöveg.
    ///
    /// Sorrend:
    ///   1. A ProductShortDescription HTML &lt;li&gt; elemeiből kinyert szöveg,
    ///      pl. "&lt;ul&gt;&lt;li&gt;Szín: XL&lt;/li&gt;&lt;/ul&gt;" → "Szín: XL"
    ///   2. A teljes ProductShortDescription, HTML-tagek nélkül
    ///   3. A SelectionData úgy, ahogy van
    ///   4. Üres karakterlánc
    /// </summary>
    [JsonIgnore]
    public string VariantDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ProductShortDescription))
            {
                // Próbáljuk kinyerni a <li>...</li> belső szövegét (a megerősített HTML formátumot fedi le).
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

                // Visszaesés: minden HTML tag eltávolítása a teljes szövegből.
                var stripped = StripTags(ProductShortDescription).Trim();
                if (!string.IsNullOrWhiteSpace(stripped)) return stripped;
            }

            // A SelectionData csak belső GUID-okat tartalmaz — nem felhasználó-olvasható.
            // A ProductShortDescription az egyetlen forrás az olvasható opció-szöveghez.
            return "";
        }
    }

    /// <summary>Egyszerű regex-szel eltávolít minden HTML taget egy karakterláncból.</summary>
    private static string StripTags(string html) =>
        Regex.Replace(html, "<[^>]+>", "");
}

/// <summary>
/// Az OrderLine SelectionData tömbjének egyetlen eleme.
///
/// MEGERŐSÍTETT valódi alak:
///   { "OptionBvin": "cc24849502574e30875f85c0e41cd3c7",
///     "SelectionData": "a0293857050c489b8f0c5ab21524493b" }
///
/// Mindkét érték belső Hotcakes GUID (nem ember-olvasható).
/// Az ember által olvasható opció-szöveg az OrderLine.ProductShortDescription-ben él.
/// </summary>
public class SelectionDataEntry
{
    [JsonPropertyName("OptionBvin")]
    public string OptionBvin { get; set; } = "";

    [JsonPropertyName("SelectionData")]
    public string SelectionData { get; set; } = "";
}
