using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Provides static mock data for UI demos and offline testing.
/// Activated when UseMockData = true in appsettings.json.
///
/// Mock orders use the SAME field names as the real OrderSummary model.
/// PaymentStatus and ShippingStatus are integers matching the Hotcakes enum.
/// IsPlaced = true on all 3 sample orders (placed/finalized).
///
/// GetMockOrderItems() returns product lines so the items grid can be fully
/// demonstrated in mock mode — reflecting what the UI will look like once a
/// real items endpoint is confirmed.
/// </summary>
public static class MockDataService
{
    // ── Orders list ──────────────────────────────────────────────────────────

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
            PaymentStatus   = 3,    // Paid
            ShippingStatus  = 1,    // Unshipped
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
            PaymentStatus   = 3,    // Paid
            ShippingStatus  = 3,    // FullyShipped
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
            PaymentStatus   = 1,    // Unpaid
            ShippingStatus  = 4,    // NonShipping (personal pickup)
            ShippingMethodDisplayName = "Személyes átvétel",
            TimeOfOrderUtc  = new DateTime(2024, 1, 20, 9, 0, 0, DateTimeKind.Utc),
            LastUpdatedUtc  = new DateTime(2024, 1, 20, 9, 0, 0, DateTimeKind.Utc)
        }
    };

    // ── Order line items (mock only — real endpoint not yet confirmed) ────────

    /// <summary>
    /// Returns product line items for a mock order.
    /// These are used only in mock mode to demonstrate the items grid.
    /// In live API mode the items list is empty until an endpoint is confirmed.
    /// </summary>
    public static List<OrderLine> GetMockOrderItems(string bvin) => bvin switch
    {
        // Mock items use ProductShortDescription for human-readable option text,
        // matching the real API where SelectionData is an array of internal GUIDs.
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

    // ── Private address helpers ─────────────────────────────────────────────

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
