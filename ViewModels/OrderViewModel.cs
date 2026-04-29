using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.ViewModels;

/// <summary>
/// Lapos, csak megjelenítésre szánt projekció az OrderSummary-ből, a rendelés
/// DataGridView-hez.
///
/// Csak azokat az oszlopokat tartalmazza, amik valóban látszanak az UI-on —
/// nincsenek belső API mezők, nyers egész enum-ok, GUID-ok, sem JSON
/// melléktermékek.
///
/// Az eredeti OrderSummary a <see cref="Source"/>-ban marad, hogy a form
/// fel tudja használni az OrderDetail.FromSummary()-hez (PDF generálás)
/// és a bvin alapú items híváshoz, anélkül hogy külön lookup-táblára
/// lenne szüksége.
/// </summary>
public sealed class OrderViewModel
{
    // ── Belső hivatkozás — NEM grid oszlop ─────────────────────────────────
    /// <summary>Eredeti API modell. PDF generáláshoz és tétel-betöltéshez használjuk. Nem jelenik meg.</summary>
    public OrderSummary Source { get; }

    /// <summary>A /orders/{bvin}/items híváshoz szükséges GUID. Nem oszlopként jelenik meg.</summary>
    public string Bvin { get; }

    // ── Megjelenített oszlopok ─────────────────────────────────────────────

    /// <summary>Ember által olvasható rendelésszám; piszkozat esetén Bvin előtagra esik vissza.</summary>
    public string OrderNumber { get; }

    /// <summary>A vevő teljes neve a BillingAddress-ből; UserEmail-re esik vissza.</summary>
    public string CustomerName { get; }

    public string UserEmail { get; }

    public decimal TotalGrand { get; }

    /// <summary>Lokalizált fizetési állapot szöveg (pl. „Fizetve").</summary>
    public string PaymentStatus { get; }

    /// <summary>Szállítási mód megjelenítendő neve; ha hiányzik, „Nincs adat".</summary>
    public string ShippingMethod { get; }

    /// <summary>Rendelés dátuma helyi időben. DateTime.MinValue dátum nélküli piszkozatnál.</summary>
    public DateTime OrderDate { get; }

    /// <summary>„Piszkozat" a nem véglegesített rendeléseknél, különben a StatusName.</summary>
    public string Status { get; }

    // ──────────────────────────────────────────────────────────────────────────

    public OrderViewModel(OrderSummary s)
    {
        Source       = s;
        Bvin         = s.Bvin;
        OrderNumber  = s.DisplayOrderNumber;
        CustomerName = s.CustomerName;
        UserEmail    = s.UserEmail;
        TotalGrand   = s.TotalGrand;
        PaymentStatus  = s.PaymentStatusDisplay;
        ShippingMethod = s.ShippingDisplay;
        OrderDate    = s.OrderDateLocal;
        Status       = s.DisplayStatus;
    }
}
