using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Statikus mintaadatokat biztosít UI demókhoz és offline teszteléshez.
/// Akkor aktiválódik, ha az appsettings.json-ban UseMockData = true.
///
/// A mock rendelések UGYANAZOKAT a mezőneveket használják, mint a valódi
/// OrderSummary modell.
/// A PaymentStatus és ShippingStatus a Hotcakes enum-mal egyező egész szám.
/// Mind a 3 mintarendelésen IsPlaced = true (leadott / véglegesített).
///
/// A GetMockOrderItems() termék-sorokat ad vissza, így mock módban a tétel
/// grid teljes körűen bemutatható — az UI pontosan úgy fog kinézni, ahogy
/// egy megerősített items végpont esetén.
/// </summary>
public static class MockDataService
{
    // ── Rendelés-lista ──────────────────────────────────────────────────────

    public static List<OrderSummary> GetMockOrders() => new()
    {
        new OrderSummary
        {
            Id = 1,
            Bvin            = "MOCK-001-ABCDEF",
            OrderNumber     = "1001",
            UserEmail       = "kovacs.istvan@example.com",
            BillingAddress  = HunBillingAddress("Kovács", "István", "Fő utca 12.", "Budapest", "1051", "+36 30 123 4567"),
            ShippingAddress = HunShippingAddress("Kovács", "István", "Fő utca 12.", "Budapest", "1051"),
            TotalGrand      = 15990m,
            TotalOrderBeforeDiscounts = 14490m,
            TotalShippingBeforeDiscounts = 1500m,
            TotalTax        = 0m,
            IsPlaced        = true,
            StatusName      = "Feldolgozva",
            PaymentStatus   = 3,    // Fizetve
            ShippingStatus  = 1,    // Nem szállítva
            ShippingMethodDisplayName = "Magyar Posta – standard",
            TimeOfOrderUtc  = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            LastUpdatedUtc  = new DateTime(2024, 1, 16, 8, 0, 0, DateTimeKind.Utc)
        },
        new OrderSummary
        {
            Id = 2,
            Bvin            = "MOCK-002-GHIJKL",
            OrderNumber     = "1002",
            UserEmail       = "nagy.erzsebet@example.com",
            BillingAddress  = HunBillingAddress("Nagy", "Erzsébet", "Kossuth tér 3.", "Debrecen", "4024", "+36 20 987 6543"),
            ShippingAddress = HunShippingAddress("Nagy", "Erzsébet", "Kossuth tér 3.", "Debrecen", "4024"),
            TotalGrand      = 28500m,
            TotalOrderBeforeDiscounts = 26500m,
            TotalShippingBeforeDiscounts = 2000m,
            TotalTax        = 0m,
            IsPlaced        = true,
            StatusName      = "Szállítás alatt",
            PaymentStatus   = 3,    // Fizetve
            ShippingStatus  = 3,    // Teljesen szállítva
            ShippingMethodDisplayName = "GLS futárszolgálat",
            TimeOfOrderUtc  = new DateTime(2024, 1, 18, 14, 15, 0, DateTimeKind.Utc),
            LastUpdatedUtc  = new DateTime(2024, 1, 19, 9, 30, 0, DateTimeKind.Utc)
        },
        new OrderSummary
        {
            Id = 3,
            Bvin            = "MOCK-003-MNOPQR",
            OrderNumber     = "1003",
            UserEmail       = "szabo.gabor@example.com",
            BillingAddress  = HunBillingAddress("Szabó", "Gábor", "Petőfi utca 7.", "Pécs", "7621", "+36 70 456 7890"),
            ShippingAddress = HunShippingAddress("Szabó", "Gábor", "Petőfi utca 7.", "Pécs", "7621"),
            TotalGrand      = 9990m,
            TotalOrderBeforeDiscounts = 9990m,
            TotalShippingBeforeDiscounts = 0m,
            TotalTax        = 0m,
            IsPlaced        = true,
            StatusName      = "Új rendelés",
            PaymentStatus   = 1,    // Fizetetlen
            ShippingStatus  = 4,    // Nem szállítandó (személyes átvétel)
            ShippingMethodDisplayName = "Személyes átvétel",
            TimeOfOrderUtc  = new DateTime(2024, 1, 20, 9, 0, 0, DateTimeKind.Utc),
            LastUpdatedUtc  = new DateTime(2024, 1, 20, 9, 0, 0, DateTimeKind.Utc)
        }
    };

    // ── Rendelési tételek (csak mock — a valós végpont még nincs megerősítve) ────

    /// <summary>
    /// Egy mock rendelés terméktétel-sorait adja vissza.
    /// Ezeket csak mock módban használjuk, hogy a tétel grid bemutatható legyen.
    /// Élő API módban a tétel-lista üres, amíg egy végpont meg nincs erősítve.
    /// </summary>
    public static List<OrderLine> GetMockOrderItems(string bvin) => bvin switch
    {
        // A mock tételek a ProductShortDescription-t használják az ember által
        // olvasható opció-szöveghez — a valós API-val egyezően, ahol a
        // SelectionData belső GUID-tömb.
        "MOCK-001-ABCDEF" => new List<OrderLine>
        {
            new() { ProductName = "Pamut póló – Fehér",  Sku = "TSH-WHT-M",       Quantity = 2, BasePricePerItem = 4995m,  LineTotal = 9990m,  ProductShortDescription = "<ul class=\"lineitemoptions\"><li>Méret: M</li></ul>" },
            new() { ProductName = "Sportzokni (3 pár)",  Sku = "SOC-3PK-39",      Quantity = 1, BasePricePerItem = 2500m,  LineTotal = 2500m,  ProductShortDescription = "<ul class=\"lineitemoptions\"><li>Méret: 39–42</li></ul>" },
            new() { ProductName = "Szállítási díj",      Sku = "SHIP-POSTA",      Quantity = 1, BasePricePerItem = 1500m,  LineTotal = 1500m }
        },
        "MOCK-002-GHIJKL" => new List<OrderLine>
        {
            new() { ProductName = "Fleece kabát – Kék",  Sku = "JKT-BLU-L",      Quantity = 1, BasePricePerItem = 12990m, LineTotal = 12990m, ProductShortDescription = "<ul class=\"lineitemoptions\"><li>Méret: L</li><li>Szín: Kék</li></ul>" },
            new() { ProductName = "Nadrág – Fekete",     Sku = "PNT-BLK-32",     Quantity = 1, BasePricePerItem = 8990m,  LineTotal = 8990m,  ProductShortDescription = "<ul class=\"lineitemoptions\"><li>Méret: 32×32</li></ul>" },
            new() { ProductName = "Sapka",               Sku = "HAT-UNI-BLK",    Quantity = 1, BasePricePerItem = 3520m,  LineTotal = 3520m,  ProductShortDescription = "<ul class=\"lineitemoptions\"><li>Szín: Fekete</li></ul>" },
            new() { ProductName = "Szállítás – GLS",     Sku = "SHIP-GLS",       Quantity = 1, BasePricePerItem = 2000m,  LineTotal = 2000m }
        },
        "MOCK-003-MNOPQR" => new List<OrderLine>
        {
            new() { ProductName = "Futócipő – Szürke",  Sku = "SHO-RUN-GRY-42", Quantity = 1, BasePricePerItem = 9990m,  LineTotal = 9990m,  ProductShortDescription = "<ul class=\"lineitemoptions\"><li>Méret: 42</li><li>Szín: Szürke</li></ul>" }
        },
        _ => new List<OrderLine>()
    };

    // ── Privát cím-segédek ─────────────────────────────────────────────────

    private static AddressInfo HunBillingAddress(
        string first, string last, string line1, string city, string zip, string phone) =>
        new()
        {
            FirstName = first, LastName = last,
            Line1 = line1, City = city, PostalCode = zip,
            CountryName = "Magyarország", Phone = phone
        };

    private static AddressInfo HunShippingAddress(
        string first, string last, string line1, string city, string zip) =>
        new()
        {
            FirstName = first, LastName = last,
            Line1 = line1, City = city, PostalCode = zip,
            CountryName = "Magyarország"
        };
}
