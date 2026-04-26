using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// Updates the Hotcakes order status (StatusCode + StatusName) via the
/// REST API and verifies the change actually persisted.
///
/// ── ENDPOINT BEHAVIOR (confirmed against the live install) ──────────
/// Update      →  POST /orders/{bvin}?key={apiKey}&amp;recalculateOrder=false
///                 with the FULL OrderDTO as body, mutated to carry the
///                 desired StatusCode + StatusName (and an Instructions note).
///                 Notes:
///                   • PUT returns HTTP 500.
///                   • POST to /orders (no bvin in URL) is the CREATE-NEW
///                     route — sending an update payload there 500s.
///                   • Minimal-body POST to /orders/{bvin} returns HTTP 200
///                     but does NOT persist the change. The endpoint replaces
///                     the order with whatever you send; missing fields get
///                     reset to defaults instead of preserved. So we always
///                     send the full snapshot read back via GET.
///                   • DateTime fields MUST be ISO 8601 in the body — the
///                     server explicitly returns "/Date(...) is not a valid
///                     value for DateTime." if you send the WCF format.
///                     <see cref="DotNetJsonDateConverter"/> writes ISO 8601.
/// Verify      →  GET  /orders/{bvin}?key={apiKey}, after ~500 ms.
///                 Compare StatusCode against the requested value — Hotcakes
///                 will sometimes 200 OK without persisting.
///
/// ── INTEGRATION ─────────────────────────────────────────────────────
/// Call from MainForm.GenerateInvoiceAsync after the PDF is saved:
///   var result = await statusService.UpdateOrderStatusAsync(order.Bvin);
///   if (!result.Success) { ... show error ... }
/// </summary>
public class OrderStatusUpdateService
{
    private const string OrdersEndpoint = "orders";
    private const string OrderByBvinTemplate = "orders/{0}";
    private const string ApiKeyQueryParam = "key";

    /// <summary>How long to wait between the update POST and the verification GET.</summary>
    private static readonly TimeSpan VerificationDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>The status this service applies. Currently only "Complete" is in use.</summary>
    public OrderStatus TargetStatus { get; }

    private readonly AppSettings _settings;
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new DotNetJsonDateConverter() }
    };

    /// <summary>
    /// Pretty-printed for logs and for the fallback POST so a human inspecting 
    /// network traces can see exactly what we sent.
    /// </summary>
    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public OrderStatusUpdateService(AppSettings settings, OrderStatus? targetStatus = null)
    {
        _settings = settings;
        TargetStatus = targetStatus ?? HotcakesOrderStatus.Complete;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    // ──────────────────────────────────────────────────────────────────
    // Public entry point
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the order's status to the service's default <see cref="TargetStatus"/>
    /// (typically <c>Complete</c>) and verifies the change. Convenience overload
    /// for the common post-invoice case.
    /// </summary>
    public Task<StatusUpdateResult> UpdateOrderStatusAsync(string bvin)
        => UpdateOrderStatusAsync(bvin, TargetStatus);

    /// <summary>
    /// Sets the order's status to <paramref name="targetStatus"/> and verifies the change.
    ///
    /// Single-strategy flow (minimal-body POST proved to silently lose data —
    /// see the class-level summary):
    ///   1. GET /orders/{bvin}    — fetch the current full snapshot.
    ///   2. Mutate StatusCode + StatusName + Instructions on the snapshot.
    ///   3. POST /orders/{bvin}   — send the modified snapshot back.
    ///   4. Wait <see cref="VerificationDelay"/>.
    ///   5. GET /orders/{bvin}    — verify the new StatusCode persisted.
    ///
    /// The explicit <paramref name="targetStatus"/> overload is what lets the
    /// same service instance push an order to <c>Complete</c> after invoice
    /// generation AND revert it back to <c>Received</c> from the manual
    /// "revert" button on the main form.
    /// </summary>
    public async Task<StatusUpdateResult> UpdateOrderStatusAsync(string bvin, OrderStatus targetStatus)
    {
        var log = new StringBuilder();
        log.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] UpdateOrderStatusAsync(bvin='{bvin}') target='{targetStatus.StatusName}'");

        if (string.IsNullOrWhiteSpace(bvin))
        {
            log.AppendLine("  ✘ Bvin is empty — refusing to call API.");
            return StatusUpdateResult.Fail(
                "Hiányzik a rendelés azonosítója (Bvin) — az állapot frissítése nem küldhető el.",
                log.ToString());
        }

        return await TryFullUpdateAsync(bvin, targetStatus, log);
    }

    // ──────────────────────────────────────────────────────────────────
    // GET full → mutate → POST full → verify
    //
    // This is the only strategy that actually works against this Hotcakes
    // installation. Sending a partial body silently loses unspecified fields.
    // ──────────────────────────────────────────────────────────────────

    private async Task<StatusUpdateResult> TryFullUpdateAsync(string bvin, OrderStatus targetStatus, StringBuilder log)
    {
        // ── 1. GET the current snapshot ─────────────────────────────────
        log.AppendLine("  Step 1: GET current order snapshot.");
        var (full, fetchErr) = await FetchOrderAsync(bvin, log);
        if (fetchErr != null)
        {
            return StatusUpdateResult.Fail(
                $"A rendelés lekérése sikertelen: {fetchErr}",
                log.ToString());
        }
        if (full == null)
        {
            return StatusUpdateResult.Fail(
                "A rendelés lekérése sikertelen — üres válasz érkezett.",
                log.ToString());
        }

        // Already in the requested status? Skip the round-trip and report success.
        if (StatusMatches(full, targetStatus))
        {
            log.AppendLine($"  ✔ Order already has target status '{targetStatus.StatusName}' — no POST needed.");
            return StatusUpdateResult.Ok(full, log.ToString());
        }

        // ── 2. Mutate status fields on the snapshot ────────────────────
        var previousStatusCode = full.StatusCode;
        var previousStatusName = full.StatusName;
        full.StatusCode = targetStatus.StatusCode;
        full.StatusName = targetStatus.StatusName;

        var noteSuffix = $"Status changed to '{targetStatus.StatusName}': {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        full.Instructions = string.IsNullOrWhiteSpace(full.Instructions)
            ? noteSuffix
            : $"{full.Instructions} | {noteSuffix}";

        log.AppendLine($"  Step 2: status mutation '{previousStatusName}' ({previousStatusCode}) → " +
                       $"'{targetStatus.StatusName}' ({targetStatus.StatusCode}).");

        // ── 3. POST the full object back ───────────────────────────────
        var url = BuildPostUrl(bvin);
        var body = JsonSerializer.Serialize(full, JsonWriteOptions);

        log.AppendLine($"  Step 3: POST {url}  ({body.Length} chars)");

        var (postOk, postError) = await SendPostAsync(url, body, log);
        if (!postOk)
        {
            return StatusUpdateResult.Fail(
                postError ?? "Ismeretlen hiba a teljes rendelés visszaküldése közben.",
                log.ToString());
        }

        // ── 4. Wait, then 5. verify ────────────────────────────────────
        await Task.Delay(VerificationDelay);

        log.AppendLine("  Step 5: verification GET.");
        var (verified, verifyError) = await FetchOrderAsync(bvin, log);
        if (verifyError != null)
        {
            return StatusUpdateResult.Fail(
                $"A POST után a visszaolvasás sikertelen: {verifyError}",
                log.ToString());
        }

        if (StatusMatches(verified, targetStatus))
        {
            log.AppendLine($"  ✔ Verified: server now reports StatusCode='{verified!.StatusCode}', StatusName='{verified.StatusName}'.");
            return StatusUpdateResult.Ok(verified, log.ToString());
        }

        log.AppendLine($"  ✘ Verification mismatch: expected '{targetStatus.StatusCode}/{targetStatus.StatusName}', " +
                       $"got '{verified?.StatusCode}/{verified?.StatusName}'.");
        return StatusUpdateResult.Fail(
            "A POST után az állapot nem változott meg a szerveren.",
            log.ToString(),
            verified);
    }

    // ──────────────────────────────────────────────────────────────────
    // HTTP helpers
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds POST /orders/{bvin}?key=...&amp;recalculateOrder=false.
    ///
    /// <para>The bvin is in the URL because this is the Hotcakes Commerce
    /// REST controller's UPDATE route. Posting to /orders (no bvin in URL)
    /// hits the CREATE-NEW route instead and explodes server-side.</para>
    ///
    /// <para>recalculateOrder=false avoids re-running pricing rules — we're
    /// only changing status, so we don't want the totals to drift.</para>
    /// </summary>
    private string BuildPostUrl(string bvin)
    {
        var baseUrl = _settings.Hotcakes.BaseUrl.TrimEnd('/');
        var apiPath = _settings.Hotcakes.ApiBasePath.Trim('/');
        var endpoint = string.Format(OrderByBvinTemplate, Uri.EscapeDataString(bvin));
        var url = $"{baseUrl}/{apiPath}/{endpoint}" +
                  $"?{ApiKeyQueryParam}={Uri.EscapeDataString(_settings.Hotcakes.ApiKey)}" +
                  $"&recalculateOrder=false";
        return url;
    }

    private string BuildGetUrl(string bvin)
    {
        var baseUrl = _settings.Hotcakes.BaseUrl.TrimEnd('/');
        var apiPath = _settings.Hotcakes.ApiBasePath.Trim('/');
        var endpoint = string.Format(OrderByBvinTemplate, bvin);
        return $"{baseUrl}/{apiPath}/{endpoint}" +
               $"?{ApiKeyQueryParam}={Uri.EscapeDataString(_settings.Hotcakes.ApiKey)}";
    }

    /// <summary>
    /// POSTs the JSON body and returns (true, null) on 2xx or (false, mappedError)
    /// on transport errors / 4xx / 5xx. Adds compact request/response info to the log.
    /// </summary>
    private async Task<(bool Ok, string? Error)> SendPostAsync(string url, string body, StringBuilder log)
    {
        try
        {
            var content = new StringContent(body, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(url, content);
            var respBody = await response.Content.ReadAsStringAsync();

            log.AppendLine($"  ← HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            log.AppendLine("  Response:");
            log.AppendLine(IndentLines(Truncate(respBody, 800), 4));

            if (!response.IsSuccessStatusCode)
            {
                return (false, ApiExceptionMapper.MapHttpStatus(
                    (int)response.StatusCode, response.ReasonPhrase));
            }

            // Even on 200, the wrapper may carry { "Errors": [...] }.
            // We surface those as failures so the verification step can run informedly.
            if (TryExtractEnvelopeErrors(respBody, out var envErrors))
            {
                log.AppendLine($"  ! Envelope reported errors: {envErrors}");
                return (false, ApiExceptionMapper.MapApiErrors(envErrors));
            }

            return (true, null);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            log.AppendLine("  ✘ POST timed out after 30 s.");
            return (false, ApiExceptionMapper.MapTimeout());
        }
        catch (HttpRequestException ex)
        {
            log.AppendLine($"  ✘ HttpRequestException: {ex.Message}");
            return (false, ApiExceptionMapper.MapHttpRequest(ex));
        }
        catch (Exception ex)
        {
            log.AppendLine($"  ✘ Unexpected: {ex.Message}");
            return (false, ApiExceptionMapper.MapUnexpected(ex));
        }
    }

    /// <summary>GET /orders/{bvin} — returns the order as Hotcakes currently has it.</summary>
    private async Task<(OrderDetail? Order, string? Error)> FetchOrderAsync(string bvin, StringBuilder log)
    {
        var url = BuildGetUrl(bvin);
        log.AppendLine($"  GET {url}");

        try
        {
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            log.AppendLine($"  ← HTTP {(int)response.StatusCode} ({json.Length} chars)");

            if (!response.IsSuccessStatusCode)
                return (null, ApiExceptionMapper.MapHttpStatus((int)response.StatusCode, response.ReasonPhrase));

            // Hotcakes wraps responses in { "Errors": [...], "Content": { ... } }
            var wrapped = JsonSerializer.Deserialize<HotcakesApiResponse<OrderDetail>>(json, JsonReadOptions);
            if (wrapped == null)
                return (null, ApiExceptionMapper.MapDeserialization());

            if (wrapped.HasErrors)
                return (null, ApiExceptionMapper.MapApiErrors(wrapped.ErrorMessage));

            return (wrapped.Content, null);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            log.AppendLine("  ✘ GET timed out after 30 s.");
            return (null, ApiExceptionMapper.MapTimeout());
        }
        catch (HttpRequestException ex)
        {
            log.AppendLine($"  ✘ HttpRequestException: {ex.Message}");
            return (null, ApiExceptionMapper.MapHttpRequest(ex));
        }
        catch (JsonException ex)
        {
            log.AppendLine($"  ✘ JSON parse error: {ex.Message}");
            return (null, ApiExceptionMapper.MapDeserialization());
        }
        catch (Exception ex)
        {
            log.AppendLine($"  ✘ Unexpected: {ex.Message}");
            return (null, ApiExceptionMapper.MapUnexpected(ex));
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Pure helpers
    // ──────────────────────────────────────────────────────────────────

    private static bool StatusMatches(OrderDetail? order, OrderStatus target)
    {
        if (order == null) return false;
        return string.Equals(order.StatusCode, target.StatusCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(order.StatusName, target.StatusName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Best-effort attempt to pull the "Errors" array out of an arbitrary Hotcakes
    /// response envelope without binding to a specific Content shape.
    /// Returns false when the body has no errors or cannot be parsed.
    /// </summary>
    private static bool TryExtractEnvelopeErrors(string body, out string errors)
    {
        errors = "";
        if (string.IsNullOrWhiteSpace(body)) return false;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("Errors", out var errArray) ||
                errArray.ValueKind != JsonValueKind.Array ||
                errArray.GetArrayLength() == 0)
            {
                return false;
            }
            var parts = new List<string>();
            foreach (var e in errArray.EnumerateArray())
                parts.Add(e.ToString());
            errors = string.Join("; ", parts);
            return parts.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string IndentLines(string text, int spaces)
    {
        var pad = new string(' ', spaces);
        return string.Join("\n", text.Split('\n').Select(l => pad + l));
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + $"… (+{s.Length - max} chars)";
}
