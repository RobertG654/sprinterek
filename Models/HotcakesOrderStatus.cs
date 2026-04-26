namespace HotcakesWinFormsApp.Models;

/// <summary>
/// Well-known Hotcakes order status codes.
///
/// In Hotcakes the order status is stored as a <see cref="StatusCode"/> GUID
/// plus a denormalized <see cref="StatusName"/> string. Both must be sent
/// together when updating, otherwise the server may reject the change.
///
/// Adding a new status:
///   1. Look up the GUID in DNN > Hotcakes > Configuration > Order Status
///   2. Add a new readonly field below
///   3. Reference it from <c>OrderStatusUpdateService</c>
/// </summary>
public static class HotcakesOrderStatus
{
    /// <summary>
    /// "Complete" — set automatically after a successful invoice generation.
    /// GUID confirmed against the live DNN/Hotcakes installation.
    /// </summary>
    public static readonly OrderStatus Complete =
        new("09D7305D-BD95-48d2-A025-16ADC827582A", "Complete");

    /// <summary>
    /// "Received" — initial state of a freshly placed order in Hotcakes Commerce.
    /// Used by the manual "revert to received" action on already-completed orders.
    /// GUID is the well-known default from the Hotcakes Commerce installer
    /// (DNN > Hotcakes > Configuration > Order Status).
    /// </summary>
    public static readonly OrderStatus Received =
        new("058B09EE-9FB4-4c4f-AC83-7E15F4CA1C46", "Received");
}

/// <summary>
/// Pair of (StatusCode, StatusName) — Hotcakes refuses partial updates so
/// these are always passed around together.
/// </summary>
public sealed record OrderStatus(string StatusCode, string StatusName);
