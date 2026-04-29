using System.Text.Json;
using System.Text.Json.Serialization;
using HotcakesWinFormsApp.Helpers;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// A VALÓDI Hotcakes Commerce rendelés payload-ot képezi le, amit a
///   GET {BaseUrl}/{ApiBasePath}/orders?key={apiKey}
/// végpont ad vissza.
///
/// MEGERŐSÍTETT MEZŐLISTA (élő API válaszból):
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
/// FONTOS: Az /orders végpont NEM ad vissza tételsorokat (Items / termékek).
/// Lásd az OrderDetail-t a tételeket kezelő modellhez, és a HotcakesApiService-t
/// a megerősítésre váró stub végponthoz.
///
/// DÁTUM FORMÁTUM: A Hotcakes „/Date(milliszekundum)/" formában adja vissza
/// a dátumokat. Ezt automatikusan a DotNetJsonDateConverter kezeli.
/// </summary>
public class OrderSummary
{
    /// <summary>Auto-növekvő egész azonosító (kevésbé stabil hivatkozásokhoz, mint a Bvin).</summary>
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    /// <summary>
    /// Egyedi GUID karakterlánc azonosító (a Hotcakes „Bvin"-nek hívja).
    /// Ez a rendelések stabil elsődleges kulcsa. Megjegyzés: a JSON-ban kis
    /// kezdőbetűvel: „bvin".
    /// </summary>
    [JsonPropertyName("bvin")]
    public string Bvin { get; set; } = "";

    /// <summary>Egész számos bolt-azonosító. Az API számként küldi, nem stringként.</summary>
    [JsonPropertyName("StoreId")]
    public int StoreId { get; set; }

    [JsonPropertyName("LastUpdatedUtc")]
    [JsonConverter(typeof(DotNetJsonDateConverter))]
    public DateTime LastUpdatedUtc { get; set; }

    /// <summary>A rendelés leadásának időpontja UTC-ben. Megjelenítéshez használd az OrderDateLocal-t.</summary>
    [JsonPropertyName("TimeOfOrderUtc")]
    [JsonConverter(typeof(DotNetJsonDateConverter))]
    public DateTime TimeOfOrderUtc { get; set; }

    /// <summary>
    /// Ember által olvasható rendelésszám (pl. „1001").
    /// Lehet üres piszkozat / nem véglegesített rendeléseknél — megjelenítéshez
    /// használd a DisplayOrderNumber-t.
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
    /// Tetszőleges kulcs-érték metaadat. Nyers JsonElement-ként tároljuk, hogy
    /// ne kelljen al-modellt definiálni hozzá. Jelenleg az UI nem jeleníti meg.
    /// </summary>
    [JsonPropertyName("CustomProperties")]
    public JsonElement? CustomProperties { get; set; }

    /// <summary>
    /// Fizetési állapot Hotcakes egész enum-ként.
    /// 0=Ismeretlen  1=Fizetetlen  2=Részben fizetve  3=Fizetve
    /// 4=Túlfizetett  5=Visszatérítve
    /// A magyar nyelvű leképezést lásd: PaymentStatusDisplay.
    /// </summary>
    [JsonPropertyName("PaymentStatus")]
    public int PaymentStatus { get; set; }

    /// <summary>
    /// Szállítási állapot Hotcakes egész enum-ként.
    /// 0=Ismeretlen  1=Nem szállítva  2=Részben szállítva  3=Szállítva
    /// 4=Nem szállítandó
    /// A magyar nyelvű leképezést lásd: ShippingStatusDisplay.
    /// </summary>
    [JsonPropertyName("ShippingStatus")]
    public int ShippingStatus { get; set; }

    /// <summary>
    /// True = a rendelést a vevő véglegesítette / leadta.
    /// False = piszkozat — a PDF generálás piszkozat rendeléseknél le van tiltva.
    /// </summary>
    [JsonPropertyName("IsPlaced")]
    public bool IsPlaced { get; set; }

    [JsonPropertyName("StatusCode")]
    public string StatusCode { get; set; } = "";

    /// <summary>Ember által olvasható állapot a Hotcakes-ből. Megjelenítéshez használd a DisplayStatus-t.</summary>
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

    /// <summary>Végösszeg az összes kedvezmény, ÁFA és szállítás után.</summary>
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
    // Származtatott megjelenítési tulajdonságok — NEM részei a JSON payload-nak
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Megjelenítendő rendelésszám az UI-on. Ha az OrderNumber üres (piszkozat /
    /// nem véglegesített rendeléseknél jellemző), a Bvin-re esik vissza.
    /// </summary>
    [JsonIgnore]
    public string DisplayOrderNumber =>
        !string.IsNullOrWhiteSpace(OrderNumber) ? OrderNumber :
        !string.IsNullOrWhiteSpace(Bvin)        ? Bvin        :
        "N/A";

    /// <summary>
    /// A vevő megjelenített neve, a BillingAddress-ből származtatva.
    /// Visszaesési sorrend: FullName → UserEmail → „Nincs adat".
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
    /// Az állapot szövege a rendelés-grid-hez.
    /// A nem véglegesített rendelések „Piszkozat"-ot mutatnak, függetlenül a StatusName-től.
    /// </summary>
    [JsonIgnore]
    public string DisplayStatus =>
        IsPlaced
            ? (!string.IsNullOrWhiteSpace(StatusName) ? StatusName : "N/A")
            : "Piszkozat";

    /// <summary>Magyar fizetési állapot az egész enum-ból leképezve.</summary>
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

    /// <summary>Magyar szállítási állapot az egész enum-ból leképezve.</summary>
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

    /// <summary>A szállítási mód megjelenítése, „Nincs adat" fallback-kel.</summary>
    [JsonIgnore]
    public string ShippingDisplay =>
        !string.IsNullOrWhiteSpace(ShippingMethodDisplayName)
            ? ShippingMethodDisplayName
            : "Nincs adat";

    /// <summary>
    /// A rendelés dátuma helyi időzónára konvertálva, megjelenítéshez.
    /// DateTime.MinValue, ha a TimeOfOrderUtc nincs beállítva (pl. dátum
    /// nélküli piszkozat).
    /// </summary>
    [JsonIgnore]
    public DateTime OrderDateLocal =>
        TimeOfOrderUtc == DateTime.MinValue
            ? DateTime.MinValue
            : TimeOfOrderUtc.ToLocalTime();
}
