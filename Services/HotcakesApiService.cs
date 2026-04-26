using System.Text.Json;
using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Handles all HTTP communication with the Hotcakes Commerce REST API.
///
/// ══════════════════════════════════════════════════════════════════
/// ENDPOINT CONFIGURATION
/// ══════════════════════════════════════════════════════════════════
/// All endpoint paths are defined as constants at the top of this class.
/// If your Hotcakes installation uses different URL patterns, change the
/// constants below — no other code needs to change.
///
/// FINAL URL FORMAT:
///   {BaseUrl.TrimEnd('/')}/{ApiBasePath.Trim('/')}/{EndpointConstant}
///   e.g.: http://4.231.236.217/DesktopModules/Hotcakes/API/rest/v1/orders
///
/// ══════════════════════════════════════════════════════════════════
/// AUTHENTICATION
/// ══════════════════════════════════════════════════════════════════
/// API key is appended as query parameter: ?key={ApiKey}
/// This installation requires the key in the query string.
/// BuildUrl() handles this automatically for every request.
/// The query parameter name is controlled by ApiKeyQueryParam below.
///
/// ══════════════════════════════════════════════════════════════════
/// RESPONSE PARSING
/// ══════════════════════════════════════════════════════════════════
/// Hotcakes wraps responses in: { "Errors": [], "Content": { ... } }
/// The parser tries multiple shapes defensively and logs the raw JSON to
/// Console.WriteLine so you can inspect it during development.
/// ══════════════════════════════════════════════════════════════════
/// </summary>
public class HotcakesApiService
{
    // ------------------------------------------------------------------
    // Adjust these constants if your Hotcakes API uses different routes
    // ------------------------------------------------------------------

    /// <summary>
    /// Query parameter name used for API key authentication.
    /// Confirmed working format: /orders?key={ApiKey}
    /// </summary>
    private const string ApiKeyQueryParam = "key";

    /// <summary>Orders list endpoint (relative to ApiBasePath).</summary>
    private const string OrdersEndpoint = "orders";

    /// <summary>
    /// Order items endpoint. {0} = order Bvin (GUID string).
    /// Confirmed working: /orders/{bvin}/items?key={apiKey}
    /// </summary>
    private const string OrderItemsTemplate = "orders/{0}/items";

    // ------------------------------------------------------------------

    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        // Handles the "/Date(milliseconds)/" format used by Hotcakes Commerce.
        // Applied globally so all DateTime fields in all deserialized models are covered.
        Converters = { new DotNetJsonDateConverter() }
    };

    public HotcakesApiService(AppSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        // API key is injected per-URL in BuildUrl(), not via headers.
    }

    /// <summary>
    /// Builds a full URL from a relative endpoint path and appends the API key
    /// as a query parameter (?key=... or &amp;key=... depending on existing params).
    ///
    /// Examples:
    ///   BuildUrl("orders")                       → .../orders?key=abc123
    ///   BuildUrl("orders?pageNumber=1&amp;pageSize=50") → .../orders?pageNumber=1&amp;pageSize=50&amp;key=abc123
    ///   BuildUrl("orders/GUID-HERE")             → .../orders/GUID-HERE?key=abc123
    /// </summary>
    private string BuildUrl(string endpoint)
    {
        var baseUrl = _settings.Hotcakes.BaseUrl.TrimEnd('/');
        var apiPath = _settings.Hotcakes.ApiBasePath.Trim('/');
        var url = $"{baseUrl}/{apiPath}/{endpoint}";

        // Append key as query parameter; use & if the endpoint already has a ?
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{ApiKeyQueryParam}={Uri.EscapeDataString(_settings.Hotcakes.ApiKey)}";
    }

    // ──────────────────────────────────────────────────────────────────
    // Public API Methods
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches a paginated list of orders.
    /// Returns (orders, null) on success or (empty list, errorMessage) on failure.
    /// </summary>
    public async Task<(List<OrderSummary> Orders, string? ErrorMessage)> GetOrdersAsync(
        int pageNumber = 1, int pageSize = 50)
    {
        var url = BuildUrl($"{OrdersEndpoint}?pageNumber={pageNumber}&pageSize={pageSize}");

        try
        {
            Log($"GET {url}");
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Log($"HTTP error {(int)response.StatusCode}: {body[..Math.Min(300, body.Length)]}");
                return (new List<OrderSummary>(),
                    ApiExceptionMapper.MapHttpStatus((int)response.StatusCode, response.ReasonPhrase));
            }

            var json = await response.Content.ReadAsStringAsync();
            Log($"Response ({json.Length} chars): {json[..Math.Min(500, json.Length)]}");
            return ParseOrderListResponse(json);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            // Timeout: the HttpClient Timeout elapsed (not a user cancellation)
            Log("Request timed out after 30 s.");
            return (new List<OrderSummary>(), ApiExceptionMapper.MapTimeout());
        }
        catch (TaskCanceledException)
        {
            return (new List<OrderSummary>(), ApiExceptionMapper.MapCancelled());
        }
        catch (HttpRequestException ex)
        {
            Log($"HttpRequestException: {ex.Message} (StatusCode={ex.StatusCode})");
            return (new List<OrderSummary>(), ApiExceptionMapper.MapHttpRequest(ex));
        }
        catch (Exception ex)
        {
            Log($"Unexpected: {ex}");
            return (new List<OrderSummary>(), ApiExceptionMapper.MapUnexpected(ex));
        }
    }

    /// <summary>
    /// Loads the line items for a specific order using the confirmed endpoint:
    ///   GET /orders/{bvin}/items?key={apiKey}
    ///
    /// Returns (items, null) on success or (empty list, errorMessage) on failure.
    /// If bvin is empty/null, returns an error immediately without an HTTP call.
    /// </summary>
    public async Task<(List<OrderLine> Items, string? ErrorMessage)> GetOrderItemsAsync(string bvin)
    {
        if (string.IsNullOrWhiteSpace(bvin))
        {
            Log("GetOrderItemsAsync: bvin is empty — cannot call items endpoint.");
            return (new List<OrderLine>(),
                "A rendelés azonosítója (bvin) hiányzik. Nem lehet betölteni a tételeket.");
        }

        var url = BuildUrl(string.Format(OrderItemsTemplate, bvin));

        try
        {
            Log($"GET {url}");
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Log($"HTTP error {(int)response.StatusCode}: {body[..Math.Min(300, body.Length)]}");
                return (new List<OrderLine>(),
                    ApiExceptionMapper.MapHttpStatus((int)response.StatusCode, response.ReasonPhrase));
            }

            var json = await response.Content.ReadAsStringAsync();
            Log($"Items response ({json.Length} chars): {json[..Math.Min(500, json.Length)]}");
            return ParseOrderItemsResponse(json);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            Log("Items request timed out after 30 s.");
            return (new List<OrderLine>(), ApiExceptionMapper.MapTimeout());
        }
        catch (TaskCanceledException)
        {
            return (new List<OrderLine>(), ApiExceptionMapper.MapCancelled());
        }
        catch (HttpRequestException ex)
        {
            Log($"HttpRequestException (items): {ex.Message} (StatusCode={ex.StatusCode})");
            return (new List<OrderLine>(), ApiExceptionMapper.MapHttpRequest(ex));
        }
        catch (Exception ex)
        {
            Log($"Unexpected (items): {ex}");
            return (new List<OrderLine>(), ApiExceptionMapper.MapUnexpected(ex));
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Defensive JSON parsers — try multiple response shapes
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses the /orders response.
    ///
    /// CONFIRMED SHAPE:
    ///   { "Errors": [], "Content": [ { order }, { order }, ... ] }
    ///
    /// Content is a direct JSON array of OrderSummary objects.
    /// </summary>
    private (List<OrderSummary> Orders, string? ErrorMessage) ParseOrderListResponse(string json)
    {
        try
        {
            var wrapped = JsonSerializer.Deserialize<HotcakesApiResponse<List<OrderSummary>>>(json, JsonOptions);
            if (wrapped == null)
            {
                Log("Orders response deserialized as null.");
                return (new List<OrderSummary>(), ApiExceptionMapper.MapDeserialization());
            }

            if (wrapped.HasErrors)
            {
                Log($"Orders API returned errors: {wrapped.ErrorMessage}");
                return (new List<OrderSummary>(), ApiExceptionMapper.MapApiErrors(wrapped.ErrorMessage));
            }

            var orders = wrapped.Content ?? new List<OrderSummary>();
            Log($"Orders parsed OK — {orders.Count} orders.");
            return (orders, null);
        }
        catch (JsonException ex)
        {
            Log($"Orders parse failed: {ex.Message}");
            Log($"Raw JSON: {json[..Math.Min(800, json.Length)]}");
            return (new List<OrderSummary>(), ApiExceptionMapper.MapDeserialization());
        }
    }

    /// <summary>
    /// Parses the /orders/{bvin}/items response.
    ///
    /// CONFIRMED SHAPE:
    ///   { "Errors": [], "Content": { "Items": [...], "Coupons": [], ... } }
    ///
    /// Content is a SINGLE order-detail object — NOT an array.
    /// The actual item rows are in Content.Items.
    ///
    /// Shape 1 (CONFIRMED): {"Errors":[],"Content":{ ... "Items":[...] ... }}
    /// Shape 2 (FALLBACK):  {"Errors":[],"Content":[...]}  — Content directly an array
    /// </summary>
    private (List<OrderLine> Items, string? ErrorMessage) ParseOrderItemsResponse(string json)
    {
        // ── Shape 1 (CONFIRMED): Content is a single OrderDetail object ──────
        // The confirmed API returns Content as an object with an Items array inside.
        try
        {
            var wrapped = JsonSerializer.Deserialize<HotcakesApiResponse<OrderDetail>>(json, JsonOptions);
            if (wrapped != null)
            {
                if (wrapped.HasErrors)
                {
                    Log($"Items API returned errors: {wrapped.ErrorMessage}");
                    return (new List<OrderLine>(), ApiExceptionMapper.MapApiErrors(wrapped.ErrorMessage));
                }
                if (wrapped.Content?.Items is { } items)
                {
                    Log($"Items Shape 1 (Content.Items) parsed OK — {items.Count} items.");
                    return (items, null);
                }
            }
        }
        catch (JsonException ex)
        {
            Log($"Items Shape 1 (Content object) parse failed: {ex.Message}");
        }

        // ── Shape 2 (FALLBACK): Content is a direct array of items ──────────
        try
        {
            var wrapped = JsonSerializer.Deserialize<HotcakesApiResponse<List<OrderLine>>>(json, JsonOptions);
            if (wrapped?.Content is { Count: > 0 } directItems)
            {
                Log($"Items Shape 2 (Content array) parsed OK — {directItems.Count} items.");
                return (directItems, null);
            }
        }
        catch (JsonException ex)
        {
            Log($"Items Shape 2 (Content array) parse failed: {ex.Message}");
        }

        Log($"Items: all parse shapes failed. Raw: {json[..Math.Min(800, json.Length)]}");
        return (new List<OrderLine>(), ApiExceptionMapper.MapDeserialization());
    }

    /// <summary>
    /// Logging shim — writes to Debug Console.
    /// Replace with a proper logging framework (e.g. Serilog) if needed.
    /// </summary>
    private static void Log(string message) =>
        System.Diagnostics.Debug.WriteLine($"[HotcakesAPI] {DateTime.Now:HH:mm:ss.fff} {message}");
}
