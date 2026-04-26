using System.Text.Json;
using System.Text.Json.Serialization;
using HotcakesWinFormsApp.Helpers;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// Maps the REAL Hotcakes Commerce order payload returned by:
///   GET {BaseUrl}/{ApiBasePath}/orders?key={apiKey}
///
/// CONFIRMED FIELD LIST (from live API response):
///   Id, bvin, StoreId, LastUpdatedUtc, TimeOfOrderUtc,
///   OrderNumber, ThirdPartyOrderId, UserEmail, UserID, CustomProperties,
///   PaymentStatus (int), ShippingStatus (int), IsPlaced,
///   StatusCode, StatusName, BillingAddress, ShippingAddress,
///   ItemsTax, ShippingTax, TotalTax,
///   TotalOrderBeforeDiscounts, TotalShippingBeforeDiscounts,
///   TotalShippingDiscounts, TotalOrderDiscounts, TotalHandling, TotalGrand,
///   AffiliateID, FraudScore, Instructions,
///   ShippingMethodId, ShippingMethodDisplayName,
///   ShippingProviderId, ShippingProviderServiceCode
///
/// NOTE: The /orders endpoint does NOT return line items (Items/products).
/// See OrderDetail for the items-capable model and HotcakesApiService for
/// the stub endpoint awaiting confirmation.
///
/// DATE FORMAT: Hotcakes returns dates as "/Date(milliseconds)/"
/// This is handled automatically by DotNetJsonDateConverter.
/// </summary>
public class OrderSummary
{
    /// <summary>Auto-increment integer ID (less stable than Bvin for references).</summary>
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    /// <summary>
    /// Unique GUID string identifier (Hotcakes calls it "Bvin").
    /// This is the stable primary key for orders. Note: lowercase "bvin" in the API.
    /// </summary>
    [JsonPropertyName("bvin")]
    public string Bvin { get; set; } = "";

    /// <summary>Integer store ID. The API sends this as a number, not a string.</summary>
    [JsonPropertyName("StoreId")]
    public int StoreId { get; set; }

    [JsonPropertyName("LastUpdatedUtc")]
    [JsonConverter(typeof(DotNetJsonDateConverter))]
    public DateTime LastUpdatedUtc { get; set; }

    /// <summary>Order placement time in UTC. Use OrderDateLocal for display.</summary>
    [JsonPropertyName("TimeOfOrderUtc")]
    [JsonConverter(typeof(DotNetJsonDateConverter))]
    public DateTime TimeOfOrderUtc { get; set; }

    /// <summary>
    /// Human-readable order number (e.g. "1001").
    /// May be empty for draft/unplaced orders — use DisplayOrderNumber for display.
    /// </summary>
    [JsonPropertyName("OrderNumber")]
    public string OrderNumber { get; set; } = "";

    [JsonPropertyName("ThirdPartyOrderId")]
    public string ThirdPartyOrderId { get; set; } = "";

    [JsonPropertyName("UserEmail")]
    public string UserEmail { get; set; } = "";

    [JsonPropertyName("UserID")]
    public string UserId { get; set; } = "";

    /// <summary>
    /// Arbitrary key-value metadata. Stored as raw JsonElement to avoid
    /// requiring a sub-model definition. Not displayed in the UI currently.
    /// </summary>
    [JsonPropertyName("CustomProperties")]
    public JsonElement? CustomProperties { get; set; }

    /// <summary>
    /// Payment status as Hotcakes integer enum.
    /// 0=Unknown  1=Unpaid  2=PartiallyPaid  3=Paid  4=Overpaid  5=Refunded
    /// See PaymentStatusDisplay for the Hungarian mapped text.
    /// </summary>
    [JsonPropertyName("PaymentStatus")]
    public int PaymentStatus { get; set; }

    /// <summary>
    /// Shipping status as Hotcakes integer enum.
    /// 0=Unknown  1=Unshipped  2=PartiallyShipped  3=FullyShipped  4=NonShipping
    /// See ShippingStatusDisplay for the Hungarian mapped text.
    /// </summary>
    [JsonPropertyName("ShippingStatus")]
    public int ShippingStatus { get; set; }

    /// <summary>
    /// True = order has been finalized/placed by the customer.
    /// False = draft order — PDF generation is blocked for unplaced orders.
    /// </summary>
    [JsonPropertyName("IsPlaced")]
    public bool IsPlaced { get; set; }

    [JsonPropertyName("StatusCode")]
    public string StatusCode { get; set; } = "";

    /// <summary>Human-readable order status from Hotcakes. Use DisplayStatus for display.</summary>
    [JsonPropertyName("StatusName")]
    public string StatusName { get; set; } = "";

    [JsonPropertyName("BillingAddress")]
    public AddressInfo? BillingAddress { get; set; }

    [JsonPropertyName("ShippingAddress")]
    public AddressInfo? ShippingAddress { get; set; }

    [JsonPropertyName("ItemsTax")]
    public decimal ItemsTax { get; set; }

    [JsonPropertyName("ShippingTax")]
    public decimal ShippingTax { get; set; }

    [JsonPropertyName("TotalTax")]
    public decimal TotalTax { get; set; }

    [JsonPropertyName("TotalOrderBeforeDiscounts")]
    public decimal TotalOrderBeforeDiscounts { get; set; }

    [JsonPropertyName("TotalShippingBeforeDiscounts")]
    public decimal TotalShippingBeforeDiscounts { get; set; }

    [JsonPropertyName("TotalShippingDiscounts")]
    public decimal TotalShippingDiscounts { get; set; }

    [JsonPropertyName("TotalOrderDiscounts")]
    public decimal TotalOrderDiscounts { get; set; }

    [JsonPropertyName("TotalHandling")]
    public decimal TotalHandling { get; set; }

    /// <summary>Final order total after all discounts, taxes, and shipping.</summary>
    [JsonPropertyName("TotalGrand")]
    public decimal TotalGrand { get; set; }

    [JsonPropertyName("AffiliateID")]
    public string AffiliateId { get; set; } = "";

    [JsonPropertyName("FraudScore")]
    public decimal FraudScore { get; set; }

    [JsonPropertyName("Instructions")]
    public string Instructions { get; set; } = "";

    [JsonPropertyName("ShippingMethodId")]
    public string ShippingMethodId { get; set; } = "";

    [JsonPropertyName("ShippingMethodDisplayName")]
    public string ShippingMethodDisplayName { get; set; } = "";

    [JsonPropertyName("ShippingProviderId")]
    public string ShippingProviderId { get; set; } = "";

    [JsonPropertyName("ShippingProviderServiceCode")]
    public string ShippingProviderServiceCode { get; set; } = "";

    // ──────────────────────────────────────────────────────────────────
    // Computed display properties — NOT in the JSON payload
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Order number for UI display. Falls back to Bvin if OrderNumber is empty
    /// (common for draft/unplaced orders).
    /// </summary>
    [JsonIgnore]
    public string DisplayOrderNumber =>
        !string.IsNullOrWhiteSpace(OrderNumber) ? OrderNumber :
        !string.IsNullOrWhiteSpace(Bvin)        ? Bvin        :
        "N/A";

    /// <summary>
    /// Customer display name derived from BillingAddress.
    /// Falls back: FullName → UserEmail → "Nincs adat"
    /// </summary>
    [JsonIgnore]
    public string CustomerName
    {
        get
        {
            var name = BillingAddress?.FullName;
            if (!string.IsNullOrWhiteSpace(name))  return name;
            if (!string.IsNullOrWhiteSpace(UserEmail)) return UserEmail;
            return "Nincs adat";
        }
    }

    /// <summary>
    /// Status text for the orders grid.
    /// Unplaced orders show "Piszkozat" regardless of StatusName.
    /// </summary>
    [JsonIgnore]
    public string DisplayStatus =>
        IsPlaced
            ? (!string.IsNullOrWhiteSpace(StatusName) ? StatusName : "N/A")
            : "Piszkozat";

    /// <summary>Hungarian payment status mapped from the integer enum.</summary>
    [JsonIgnore]
    public string PaymentStatusDisplay => PaymentStatus switch
    {
        0 => "Ismeretlen",
        1 => "Fizetetlen",
        2 => "Részben fizetve",
        3 => "Fizetve",
        4 => "Túlfizetett",
        5 => "Visszatérítve",
        _ => $"({PaymentStatus})"
    };

    /// <summary>Hungarian shipping status mapped from the integer enum.</summary>
    [JsonIgnore]
    public string ShippingStatusDisplay => ShippingStatus switch
    {
        0 => "Ismeretlen",
        1 => "Nem szállítva",
        2 => "Részben szállítva",
        3 => "Szállítva",
        4 => "Nem szállítandó",
        _ => $"({ShippingStatus})"
    };

    /// <summary>Shipping method display with "Nincs adat" fallback.</summary>
    [JsonIgnore]
    public string ShippingDisplay =>
        !string.IsNullOrWhiteSpace(ShippingMethodDisplayName)
            ? ShippingMethodDisplayName
            : "Nincs adat";

    /// <summary>
    /// Order date converted to local time for display.
    /// Returns DateTime.MinValue if TimeOfOrderUtc was not set (e.g. draft with no date).
    /// </summary>
    [JsonIgnore]
    public DateTime OrderDateLocal =>
        TimeOfOrderUtc == DateTime.MinValue
            ? DateTime.MinValue
            : TimeOfOrderUtc.ToLocalTime();
}
