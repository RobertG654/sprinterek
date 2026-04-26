using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// Extends OrderSummary with a line-items list.
///
/// CURRENT STATUS OF LINE ITEMS:
/// The confirmed /orders endpoint does NOT return product line items.
/// The Items list will be empty when built from a real API response.
/// A separate detail/items endpoint has not yet been confirmed for this installation.
///
/// TO ADD REAL ITEMS SUPPORT when an endpoint is confirmed:
///   1. Add GetOrderItemsAsync(bvin) to HotcakesApiService
///   2. In MainForm.OnOrderSelectedAsync, call it and set _selectedOrder.Items
///   3. The items grid and invoice PDF will automatically use the populated list
///
/// MOCK MODE: When UseMockData = true, MockDataService.GetMockOrderItems() populates
/// Items so the UI can be fully demonstrated without a live API.
/// </summary>
public class OrderDetail : OrderSummary
{
    /// <summary>
    /// Product line items for this order.
    /// Empty unless explicitly populated (mock mode or future detail endpoint).
    /// </summary>
    [JsonPropertyName("Items")]
    public List<OrderLine> Items { get; set; } = new();

    /// <summary>
    /// Creates an OrderDetail from an OrderSummary by copying all fields via JSON round-trip.
    /// Items will be empty — populate them separately when a confirmed endpoint is available.
    ///
    /// The JSON round-trip is used to avoid a brittle manual field-copy method that would
    /// need updating every time a new field is added to OrderSummary.
    /// </summary>
    public static OrderDetail FromSummary(OrderSummary s)
    {
        // Serialize the summary (DateTime fields → ISO 8601 via DotNetJsonDateConverter.Write)
        // Then deserialize as OrderDetail (ISO dates parsed by DotNetJsonDateConverter fallback)
        var json = System.Text.Json.JsonSerializer.Serialize(s);
        var detail = System.Text.Json.JsonSerializer.Deserialize<OrderDetail>(json) ?? new OrderDetail();
        detail.Items = new List<OrderLine>(); // always start empty; caller populates if available
        return detail;
    }
}
