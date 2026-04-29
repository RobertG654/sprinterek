using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// Az OrderSummary-t bővíti egy tételsor listával.
///
/// A TÉTELEK JELENLEGI HELYZETE:
/// A megerősített /orders végpont NEM ad vissza terméktétel-sorokat.
/// Az Items lista üres lesz, ha valós API válaszból építjük.
/// A telepítéshez külön detail / items végpont még nincs megerősítve.
///
/// HOGYAN LEHET VALÓDI TÉTEL TÁMOGATÁST HOZZÁADNI, ha egy végpont
/// megerősítésre kerül:
///   1. Vegyél fel egy GetOrderItemsAsync(bvin) metódust a HotcakesApiService-be.
///   2. A MainForm.OnOrderSelectedAsync-ban hívd meg, és állítsd be a
///      _selectedOrder.Items értékét.
///   3. A tétel-grid és a számla PDF automatikusan a kitöltött listát fogja használni.
///
/// MOCK MÓD: Ha UseMockData = true, a MockDataService.GetMockOrderItems()
/// kitölti az Items listát, így az UI élő API nélkül is teljes körűen
/// bemutatható.
/// </summary>
public class OrderDetail : OrderSummary
{
    /// <summary>
    /// A rendelés terméktétel-sorai.
    /// Üres, hacsak nem töltöttük ki kifejezetten (mock mód vagy jövőbeli
    /// detail végpont).
    /// </summary>
    [JsonPropertyName("Items")]
    public List<OrderLine> Items { get; set; } = new();

    /// <summary>
    /// OrderDetail létrehozása OrderSummary-ből, minden mező átmásolásával egy
    /// JSON oda-vissza konverzióval.
    /// Az Items üres lesz — külön kell feltölteni, ha van megerősített végpont.
    ///
    /// A JSON round-trip-et azért használjuk, hogy elkerüljük a törékeny,
    /// kézi mező-másoló metódust, amit minden új OrderSummary mező felvételekor
    /// frissíteni kellene.
    /// </summary>
    public static OrderDetail FromSummary(OrderSummary s)
    {
        // A summary szerializálása (DateTime mezők → ISO 8601 a DotNetJsonDateConverter.Write-on át)
        // Majd OrderDetail-ként deszerializáljuk (az ISO dátumokat a DotNetJsonDateConverter fallback parse-olja).
        var json = System.Text.Json.JsonSerializer.Serialize(s);
        var detail = System.Text.Json.JsonSerializer.Deserialize<OrderDetail>(json) ?? new OrderDetail();
        detail.Items = new List<OrderLine>(); // mindig üresen indul; a hívó tölti fel, ha van adat
        return detail;
    }
}
