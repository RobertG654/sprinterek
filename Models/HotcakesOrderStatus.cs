namespace HotcakesWinFormsApp.Models;

/// <summary>
/// Jól ismert Hotcakes rendelés-állapot kódok.
///
/// A Hotcakes-ben a rendelés állapota egy <see cref="StatusCode"/> GUID-ként
/// és egy denormalizált <see cref="StatusName"/> string-ként van tárolva.
/// Frissítéskor mindkettőt együtt kell küldeni, különben a szerver
/// elutasíthatja a változtatást.
///
/// Új állapot felvétele:
///   1. Keresd ki a GUID-ot itt: DNN > Hotcakes > Configuration > Order Status
///   2. Adj hozzá egy új readonly mezőt alább
///   3. Hivatkozz rá a <c>OrderStatusUpdateService</c>-ből
/// </summary>
public static class HotcakesOrderStatus
{
    /// <summary>
    /// „Complete" — sikeres számla generálás után automatikusan ezt állítjuk be.
    /// A GUID élő DNN / Hotcakes telepítésen ellenőrizve.
    /// </summary>
    public static readonly OrderStatus Complete =
        new("09D7305D-BD95-48d2-A025-16ADC827582A", "Complete");

    /// <summary>
    /// „Received" — a frissen leadott rendelés kezdő állapota a Hotcakes-ben.
    /// A manuális „visszaállítás Received-re" akció használja a már lezárt
    /// rendeléseken. A GUID a Hotcakes Commerce telepítőjének közismert
    /// alapértéke (DNN > Hotcakes > Configuration > Order Status).
    /// </summary>
    public static readonly OrderStatus Received =
        new("058B09EE-9FB4-4c4f-AC83-7E15F4CA1C46", "Received");
}

/// <summary>
/// (StatusCode, StatusName) pár — a Hotcakes elutasítja a parciális frissítést,
/// így ezeket mindig együtt adjuk át.
/// </summary>
public sealed record OrderStatus(string StatusCode, string StatusName);
