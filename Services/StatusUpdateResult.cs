using System.Text;
using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Egy <see cref="OrderStatusUpdateService"/> hívás eredménye.
///
/// <para>A szerviz a POST UTÁN MINDIG verifikációs GET-et indít, és azt
/// jelenti, amit a szerver ténylegesen perzisztált, mert a Hotcakes néha
/// HTTP 200 OK-t ad anélkül, hogy a rekord ténylegesen módosulna.</para>
///
/// <list type="bullet">
///   <item>A <see cref="Success"/> csak akkor true, ha a verifikációs GET
///         a rendelésen az új <see cref="OrderStatus.StatusCode"/>-ot mutatja.</item>
///   <item>Az <see cref="ErrorMessage"/> egy magyar, felhasználó-megjelenítésre
///         alkalmas karakterlánc (sikernél null).</item>
///   <item>A <see cref="DebugLog"/> tartalmazza a teljes request / response
///         trace-t — hasznos, amíg a végpont viselkedését erősítjük meg.</item>
///   <item>A <see cref="VerifiedOrder"/> a rendelés úgy, ahogy a frissítés után
///         a szerver visszaadta, ha a verifikációs GET sikerült.</item>
/// </list>
/// </summary>
public sealed class StatusUpdateResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string DebugLog { get; init; } = "";
    public OrderDetail? VerifiedOrder { get; init; }

    public static StatusUpdateResult Ok(OrderDetail verified, string log) =>
        new() { Success = true, VerifiedOrder = verified, DebugLog = log };

    public static StatusUpdateResult Fail(string error, string log, OrderDetail? verified = null) =>
        new() { Success = false, ErrorMessage = error, DebugLog = log, VerifiedOrder = verified };

    /// <summary>
    /// Segéd, ami egy másolatot ad vissza extra napló-sorokkal kiegészítve —
    /// így a szerviz kód mentes marad a `var sb = ...; sb.AppendLine(...)`
    /// boilerplate-től.
    /// </summary>
    public StatusUpdateResult WithExtraLog(string extraLog)
    {
        var combined = new StringBuilder(DebugLog);
        if (DebugLog.Length > 0 && !DebugLog.EndsWith('\n')) combined.AppendLine();
        combined.Append(extraLog);
        return new StatusUpdateResult
        {
            Success = Success,
            ErrorMessage = ErrorMessage,
            DebugLog = combined.ToString(),
            VerifiedOrder = VerifiedOrder
        };
    }
}
