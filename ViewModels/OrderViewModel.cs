using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.ViewModels;

/// <summary>
/// Flat display-only projection of OrderSummary for the orders DataGridView.
///
/// Only contains the columns actually shown in the UI — no internal API fields,
/// no raw integer enums, no GUIDs, no JSON artefacts.
///
/// The original OrderSummary is kept in <see cref="Source"/> so the form can
/// use it for OrderDetail.FromSummary() (PDF generation) and bvin lookup
/// (items API call) without needing a separate lookup table.
/// </summary>
public sealed class OrderViewModel
{
    // ── Internal reference — NOT a grid column ──────────────────────────────
    /// <summary>Original API model. Used for PDF generation and items loading. Not displayed.</summary>
    public OrderSummary Source { get; }

    /// <summary>GUID needed for /orders/{bvin}/items call. Not displayed as a column.</summary>
    public string Bvin { get; }

    // ── Display columns ─────────────────────────────────────────────────────

    /// <summary>Human-readable order number; falls back to Bvin prefix for drafts.</summary>
    public string OrderNumber { get; }

    /// <summary>Customer full name from BillingAddress; falls back to UserEmail.</summary>
    public string CustomerName { get; }

    public string UserEmail { get; }

    public decimal TotalGrand { get; }

    /// <summary>Localised payment status text (e.g. "Fizetve").</summary>
    public string PaymentStatus { get; }

    /// <summary>Shipping method display name; "Nincs adat" when missing.</summary>
    public string ShippingMethod { get; }

    /// <summary>Order date in local time. DateTime.MinValue for drafts with no date.</summary>
    public DateTime OrderDate { get; }

    /// <summary>"Piszkozat" for unplaced orders, otherwise StatusName.</summary>
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
