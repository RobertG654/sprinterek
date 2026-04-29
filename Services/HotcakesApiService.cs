using System.Text.Json;
using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Az összes HTTP kommunikációt kezeli a Hotcakes Commerce REST API-val.
///
/// ══════════════════════════════════════════════════════════════════
/// VÉGPONT KONFIGURÁCIÓ
/// ══════════════════════════════════════════════════════════════════
/// Az összes végpont elérési út konstansként van definiálva az osztály
/// tetején. Ha a Hotcakes telepítésed más URL mintát használ, csak az alábbi
/// konstansokat kell módosítani — más kódhoz nem kell hozzányúlni.
///
/// VÉGSŐ URL FORMÁTUM:
///   {BaseUrl.TrimEnd('/')}/{ApiBasePath.Trim('/')}/{EndpointConstant}
///   pl.: http://4.231.236.217/DesktopModules/Hotcakes/API/rest/v1/orders
///
/// ══════════════════════════════════════════════════════════════════
/// HITELESÍTÉS
/// ══════════════════════════════════════════════════════════════════
/// Az API kulcs query paraméterként van hozzáfűzve: ?key={ApiKey}
/// Ez a telepítés a kulcsot a query string-ben várja.
/// A BuildUrl() ezt minden kérésnél automatikusan elintézi.
/// A query paraméter neve az alábbi ApiKeyQueryParam-mel állítható.
///
/// ══════════════════════════════════════════════════════════════════
/// VÁLASZ FELDOLGOZÁS
/// ══════════════════════════════════════════════════════════════════
/// A Hotcakes a válaszokat így csomagolja: { "Errors": [], "Content": { ... } }
/// A parser több formára defenzíven próbálkozik, és a nyers JSON-t a
/// Console.WriteLine-on át kiírja, így fejlesztés közben átnézheted.
/// ══════════════════════════════════════════════════════════════════
/// </summary>
public class HotcakesApiService
{
    // ------------------------------------------------------------------
    // Állítsd át ezeket a konstansokat, ha a Hotcakes API más útvonalakat használ
    // ------------------------------------------------------------------

    /// <summary>
    /// API kulcs hitelesítéshez használt query paraméter neve.
    /// Megerősítve működő formátum: /orders?key={ApiKey}
    /// </summary>
    private const string ApiKeyQueryParam = "key";

    /// <summary>Rendelés-lista végpont (relatív az ApiBasePath-hoz).</summary>
    private const string OrdersEndpoint = "orders";

    /// <summary>
    /// Rendelési tételek végpontja. {0} = rendelés Bvin (GUID karakterlánc).
    /// Megerősítve működő: /orders/{bvin}/items?key={apiKey}
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
        // A Hotcakes Commerce által használt „/Date(milliszekundum)/" formát kezeli.
        // Globálisan alkalmazva, így minden deszerializált modell összes DateTime
        // mezője lefedettre kerül.
        Converters = { new DotNetJsonDateConverter() }
    };

    public HotcakesApiService(AppSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        // Az API kulcs URL-enként a BuildUrl()-ben kerül be, nem fejléceken át.
    }

    /// <summary>
    /// Teljes URL-t épít egy relatív végpont útvonalból, és hozzáfűzi az
    /// API kulcsot query paraméterként (?key=... vagy &amp;key=... attól
    /// függően, hogy van-e már query string).
    ///
    /// Példák:
    ///   BuildUrl("orders")                       → .../orders?key=abc123
    ///   BuildUrl("orders?pageNumber=1&amp;pageSize=50") → .../orders?pageNumber=1&amp;pageSize=50&amp;key=abc123
    ///   BuildUrl("orders/GUID-HERE")             → .../orders/GUID-HERE?key=abc123
    /// </summary>
    private string BuildUrl(string endpoint)
    {
        var baseUrl = _settings.Hotcakes.BaseUrl.TrimEnd('/');
        var apiPath = _settings.Hotcakes.ApiBasePath.Trim('/');
        var url = $"{baseUrl}/{apiPath}/{endpoint}";

        // A kulcsot query paraméterként fűzzük hozzá; ha az endpoint-on már
        // van ?, akkor &-t használunk.
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{ApiKeyQueryParam}={Uri.EscapeDataString(_settings.Hotcakes.ApiKey)}";
    }

    // ──────────────────────────────────────────────────────────────────
    // Publikus API metódusok
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lapozott rendelés-listát kér le.
    /// Sikeres esetben (orders, null), hiba esetén (üres lista, errorMessage).
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
                Log($"HTTP hiba {(int)response.StatusCode}: {body[..Math.Min(300, body.Length)]}");
                return (new List<OrderSummary>(),
                    ApiExceptionMapper.MapHttpStatus((int)response.StatusCode, response.ReasonPhrase));
            }

            var json = await response.Content.ReadAsStringAsync();
            Log($"Válasz ({json.Length} karakter): {json[..Math.Min(500, json.Length)]}");
            return ParseOrderListResponse(json);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            // Timeout: a HttpClient Timeout járt le (nem felhasználói cancellation)
            Log("A kérés 30 mp után időtúllépésbe futott.");
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
            Log($"Váratlan: {ex}");
            return (new List<OrderSummary>(), ApiExceptionMapper.MapUnexpected(ex));
        }
    }

    /// <summary>
    /// Egy konkrét rendelés tételsorait tölti be a megerősített végponton:
    ///   GET /orders/{bvin}/items?key={apiKey}
    ///
    /// Sikeres esetben (items, null), hiba esetén (üres lista, errorMessage).
    /// Ha a bvin üres / null, azonnal hibával tér vissza HTTP hívás nélkül.
    /// </summary>
    public async Task<(List<OrderLine> Items, string? ErrorMessage)> GetOrderItemsAsync(string bvin)
    {
        if (string.IsNullOrWhiteSpace(bvin))
        {
            Log("GetOrderItemsAsync: a bvin üres — nem hívható az items endpoint.");
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
                Log($"HTTP hiba {(int)response.StatusCode}: {body[..Math.Min(300, body.Length)]}");
                return (new List<OrderLine>(),
                    ApiExceptionMapper.MapHttpStatus((int)response.StatusCode, response.ReasonPhrase));
            }

            var json = await response.Content.ReadAsStringAsync();
            Log($"Tétel válasz ({json.Length} karakter): {json[..Math.Min(500, json.Length)]}");
            return ParseOrderItemsResponse(json);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            Log("A tételek kérése 30 mp után időtúllépésbe futott.");
            return (new List<OrderLine>(), ApiExceptionMapper.MapTimeout());
        }
        catch (TaskCanceledException)
        {
            return (new List<OrderLine>(), ApiExceptionMapper.MapCancelled());
        }
        catch (HttpRequestException ex)
        {
            Log($"HttpRequestException (tételek): {ex.Message} (StatusCode={ex.StatusCode})");
            return (new List<OrderLine>(), ApiExceptionMapper.MapHttpRequest(ex));
        }
        catch (Exception ex)
        {
            Log($"Váratlan (tételek): {ex}");
            return (new List<OrderLine>(), ApiExceptionMapper.MapUnexpected(ex));
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Defenzív JSON parserek — több válasz-formára is próbálkoznak
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Az /orders válasz feldolgozása.
    ///
    /// MEGERŐSÍTETT FORMA:
    ///   { "Errors": [], "Content": [ { rendelés }, { rendelés }, ... ] }
    ///
    /// A Content közvetlenül egy OrderSummary objektum-tömb.
    /// </summary>
    private (List<OrderSummary> Orders, string? ErrorMessage) ParseOrderListResponse(string json)
    {
        try
        {
            var wrapped = JsonSerializer.Deserialize<HotcakesApiResponse<List<OrderSummary>>>(json, JsonOptions);
            if (wrapped == null)
            {
                Log("A rendelés-válasz null-ként deszerializálódott.");
                return (new List<OrderSummary>(), ApiExceptionMapper.MapDeserialization());
            }

            if (wrapped.HasErrors)
            {
                Log($"Az orders API hibákat adott vissza: {wrapped.ErrorMessage}");
                return (new List<OrderSummary>(), ApiExceptionMapper.MapApiErrors(wrapped.ErrorMessage));
            }

            var orders = wrapped.Content ?? new List<OrderSummary>();
            Log($"Rendelések rendben feldolgozva — {orders.Count} db.");
            return (orders, null);
        }
        catch (JsonException ex)
        {
            Log($"A rendelések parse-olása sikertelen: {ex.Message}");
            Log($"Nyers JSON: {json[..Math.Min(800, json.Length)]}");
            return (new List<OrderSummary>(), ApiExceptionMapper.MapDeserialization());
        }
    }

    /// <summary>
    /// Az /orders/{bvin}/items válasz feldolgozása.
    ///
    /// MEGERŐSÍTETT FORMA:
    ///   { "Errors": [], "Content": { "Items": [...], "Coupons": [], ... } }
    ///
    /// A Content EGYETLEN order-detail objektum — NEM tömb.
    /// Az igazi tétel-sorok a Content.Items-ben vannak.
    ///
    /// 1. forma (MEGERŐSÍTETT): {"Errors":[],"Content":{ ... "Items":[...] ... }}
    /// 2. forma (FALLBACK):     {"Errors":[],"Content":[...]} — Content közvetlenül tömb
    /// </summary>
    private (List<OrderLine> Items, string? ErrorMessage) ParseOrderItemsResponse(string json)
    {
        // ── 1. forma (MEGERŐSÍTETT): a Content egyetlen OrderDetail objektum ──
        // A megerősített API a Content-et objektumként adja vissza, benne egy
        // Items tömbbel.
        try
        {
            var wrapped = JsonSerializer.Deserialize<HotcakesApiResponse<OrderDetail>>(json, JsonOptions);
            if (wrapped != null)
            {
                if (wrapped.HasErrors)
                {
                    Log($"A tétel API hibákat adott vissza: {wrapped.ErrorMessage}");
                    return (new List<OrderLine>(), ApiExceptionMapper.MapApiErrors(wrapped.ErrorMessage));
                }
                if (wrapped.Content?.Items is { } items)
                {
                    Log($"Tételek 1. forma (Content.Items) feldolgozva — {items.Count} db.");
                    return (items, null);
                }
            }
        }
        catch (JsonException ex)
        {
            Log($"Tételek 1. forma (Content objektum) parse-olása sikertelen: {ex.Message}");
        }

        // ── 2. forma (FALLBACK): a Content közvetlenül tétel-tömb ─────────
        try
        {
            var wrapped = JsonSerializer.Deserialize<HotcakesApiResponse<List<OrderLine>>>(json, JsonOptions);
            if (wrapped?.Content is { Count: > 0 } directItems)
            {
                Log($"Tételek 2. forma (Content tömb) feldolgozva — {directItems.Count} db.");
                return (directItems, null);
            }
        }
        catch (JsonException ex)
        {
            Log($"Tételek 2. forma (Content tömb) parse-olása sikertelen: {ex.Message}");
        }

        Log($"Tételek: minden parse-forma sikertelen. Nyers: {json[..Math.Min(800, json.Length)]}");
        return (new List<OrderLine>(), ApiExceptionMapper.MapDeserialization());
    }

    /// <summary>
    /// Naplózó shim — a Debug konzolra ír.
    /// Cseréld le egy rendes naplózó keretrendszerre (pl. Serilog), ha szükséges.
    /// </summary>
    private static void Log(string message) =>
        System.Diagnostics.Debug.WriteLine($"[HotcakesAPI] {DateTime.Now:HH:mm:ss.fff} {message}");
}
