using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotcakesWinFormsApp.Configuration;
using HotcakesWinFormsApp.Helpers;
using HotcakesWinFormsApp.Models;

namespace HotcakesWinFormsApp.Services;

/// <summary>
/// A Hotcakes rendelés-állapot (StatusCode + StatusName) frissítését végzi
/// a REST API-n keresztül, és ellenőrzi, hogy a változás valóban perzisztálódott.
///
/// ── VÉGPONT VISELKEDÉS (élő telepítésen megerősítve) ────────────────
/// Frissítés   →  POST /orders/{bvin}?key={apiKey}&amp;recalculateOrder=false
///                 a TELJES OrderDTO body-val, amit úgy módosítunk, hogy
///                 a kívánt StatusCode + StatusName (és egy Instructions
///                 megjegyzés) szerepeljen benne.
///                 Megjegyzések:
///                   • A PUT HTTP 500-at ad.
///                   • A POST a /orders-ra (bvin nélkül az URL-ben) a
///                     CREATE-NEW útvonal — frissítő payload-ot oda küldve
///                     500 lesz a vég.
///                   • A /orders/{bvin}-re küldött minimális body-jú POST
///                     HTTP 200-at ad, de NEM perzisztálja a változást.
///                     A végpont a rendelést azzal helyettesíti, amit
///                     küldünk; a hiányzó mezőket alapértékre állítja
///                     megőrzés helyett. Ezért mindig a GET-tel visszaolvasott
///                     teljes snapshot-et küldjük el.
///                   • A DateTime mezőknek ISO 8601 formátumban KELL lenniük
///                     a body-ban — a szerver explicit így válaszol, ha WCF
///                     formátumot küldünk: "/Date(...) is not a valid value
///                     for DateTime."
///                     A <see cref="DotNetJsonDateConverter"/> ISO 8601-ben ír.
/// Verifikáció →  GET /orders/{bvin}?key={apiKey}, kb. 500 ms múlva.
///                 Összehasonlítjuk a StatusCode-ot a kért értékkel — a
///                 Hotcakes néha 200 OK-t ad anélkül, hogy perzisztálná.
///
/// ── INTEGRÁCIÓ ──────────────────────────────────────────────────────
/// A MainForm.GenerateInvoiceAsync-ből hívd, miután a PDF elmentődött:
///   var result = await statusService.UpdateOrderStatusAsync(order.Bvin);
///   if (!result.Success) { ... hibaüzenet ... }
/// </summary>
public class OrderStatusUpdateService
{
    private const string OrdersEndpoint = "orders";
    private const string OrderByBvinTemplate = "orders/{0}";
    private const string ApiKeyQueryParam = "key";

    /// <summary>Mennyit várjunk a frissítő POST és a verifikációs GET között.</summary>
    private static readonly TimeSpan VerificationDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>A szerviz alapértelmezett célállapota. Jelenleg csak a „Complete" használt.</summary>
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
    /// Szépen formázott (indented) — a naplókhoz és a fallback POST-hoz is,
    /// hogy aki a hálózati trace-eket vizsgálja, pontosan lássa, mit küldtünk.
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
    // Publikus belépési pont
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// A rendelés állapotát a szerviz alapértelmezett <see cref="TargetStatus"/>-ára
    /// állítja (általában <c>Complete</c>) és ellenőrzi a változást.
    /// Kényelmi overload a számla utáni gyakori esetre.
    /// </summary>
    public Task<StatusUpdateResult> UpdateOrderStatusAsync(string bvin)
        => UpdateOrderStatusAsync(bvin, TargetStatus);

    /// <summary>
    /// A rendelés állapotát a <paramref name="targetStatus"/>-ra állítja és
    /// ellenőrzi a változást.
    ///
    /// Egystratégiás folyamat (a minimális body-jú POST bizonyítottan csendben
    /// adatot veszít — lásd az osztály-szintű összefoglalót):
    ///   1. GET /orders/{bvin}    — a jelenlegi teljes snapshot lekérése.
    ///   2. StatusCode + StatusName + Instructions átírása a snapshoton.
    ///   3. POST /orders/{bvin}   — a módosított snapshot visszaküldése.
    ///   4. <see cref="VerificationDelay"/> várakozás.
    ///   5. GET /orders/{bvin}    — ellenőrizzük, hogy az új StatusCode perzisztálódott.
    ///
    /// Az explicit <paramref name="targetStatus"/> overload teszi lehetővé, hogy
    /// ugyanaz a szerviz példány <c>Complete</c>-re tolja a rendelést számla
    /// generálás után, ÉS visszaállítsa <c>Received</c>-re a főablak
    /// kézi „visszaállítás" gombjáról.
    /// </summary>
    public async Task<StatusUpdateResult> UpdateOrderStatusAsync(string bvin, OrderStatus targetStatus)
    {
        var log = new StringBuilder();
        log.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] UpdateOrderStatusAsync(bvin='{bvin}') target='{targetStatus.StatusName}'");

        if (string.IsNullOrWhiteSpace(bvin))
        {
            log.AppendLine("  ✘ A Bvin üres — nem hívjuk az API-t.");
            return StatusUpdateResult.Fail(
                "Hiányzik a rendelés azonosítója (Bvin) — az állapot frissítése nem küldhető el.",
                log.ToString());
        }

        return await TryFullUpdateAsync(bvin, targetStatus, log);
    }

    // ──────────────────────────────────────────────────────────────────
    // Teljes GET → mutáció → teljes POST → verifikáció
    //
    // Ez az egyetlen stratégia, ami valóban működik ezen a Hotcakes
    // telepítésen. Parciális body küldésekor a meg nem adott mezők csendben
    // elvesznek.
    // ──────────────────────────────────────────────────────────────────

    private async Task<StatusUpdateResult> TryFullUpdateAsync(string bvin, OrderStatus targetStatus, StringBuilder log)
    {
        // ── 1. A jelenlegi snapshot lekérése GET-tel ────────────────────
        log.AppendLine("  1. lépés: GET a rendelés jelenlegi snapshot-jához.");
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

        // Már a kért állapotban van? Kihagyjuk a kerülőutat és sikert jelentünk.
        if (StatusMatches(full, targetStatus))
        {
            log.AppendLine($"  ✔ A rendelés már a célállapotban van: '{targetStatus.StatusName}' — POST nem szükséges.");
            return StatusUpdateResult.Ok(full, log.ToString());
        }

        // ── 2. Status mezők átírása a snapshoton ────────────────────────
        var previousStatusCode = full.StatusCode;
        var previousStatusName = full.StatusName;
        full.StatusCode = targetStatus.StatusCode;
        full.StatusName = targetStatus.StatusName;

        var noteSuffix = $"Status changed to '{targetStatus.StatusName}': {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        full.Instructions = string.IsNullOrWhiteSpace(full.Instructions)
            ? noteSuffix
            : $"{full.Instructions} | {noteSuffix}";

        log.AppendLine($"  2. lépés: status mutáció '{previousStatusName}' ({previousStatusCode}) → " +
                       $"'{targetStatus.StatusName}' ({targetStatus.StatusCode}).");

        // ── 3. A teljes objektum visszaküldése POST-tal ────────────────
        var url = BuildPostUrl(bvin);
        var body = JsonSerializer.Serialize(full, JsonWriteOptions);

        log.AppendLine($"  3. lépés: POST {url}  ({body.Length} karakter)");

        var (postOk, postError) = await SendPostAsync(url, body, log);
        if (!postOk)
        {
            return StatusUpdateResult.Fail(
                postError ?? "Ismeretlen hiba a teljes rendelés visszaküldése közben.",
                log.ToString());
        }

        // ── 4. Várakozás, majd 5. verifikáció ──────────────────────────
        await Task.Delay(VerificationDelay);

        log.AppendLine("  5. lépés: verifikációs GET.");
        var (verified, verifyError) = await FetchOrderAsync(bvin, log);
        if (verifyError != null)
        {
            return StatusUpdateResult.Fail(
                $"A POST után a visszaolvasás sikertelen: {verifyError}",
                log.ToString());
        }

        if (StatusMatches(verified, targetStatus))
        {
            log.AppendLine($"  ✔ Verifikálva: a szerver szerint StatusCode='{verified!.StatusCode}', StatusName='{verified.StatusName}'.");
            return StatusUpdateResult.Ok(verified, log.ToString());
        }

        log.AppendLine($"  ✘ Verifikációs eltérés: várt '{targetStatus.StatusCode}/{targetStatus.StatusName}', " +
                       $"kapott '{verified?.StatusCode}/{verified?.StatusName}'.");
        return StatusUpdateResult.Fail(
            "A POST után az állapot nem változott meg a szerveren.",
            log.ToString(),
            verified);
    }

    // ──────────────────────────────────────────────────────────────────
    // HTTP segédek
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Felépíti a POST /orders/{bvin}?key=...&amp;recalculateOrder=false URL-t.
    ///
    /// <para>A bvin azért van az URL-ben, mert ez a Hotcakes Commerce REST
    /// controller UPDATE útvonala. A /orders-ra postolva (bvin nélkül az
    /// URL-ben) a CREATE-NEW útvonalat találnánk el, ami szerveroldalon
    /// szétdurranna.</para>
    ///
    /// <para>A recalculateOrder=false elkerüli az árazási szabályok újrafutását
    /// — csak az állapotot változtatjuk, nem szeretnénk hogy az összegek
    /// elcsússzanak.</para>
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
    /// Elküldi a JSON body-t POST-tal, és (true, null)-t ad vissza 2xx esetén
    /// vagy (false, mappedError)-t hálózati hibáknál / 4xx / 5xx esetén.
    /// Tömör request / response infót ad a naplóhoz.
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
            log.AppendLine("  Válasz:");
            log.AppendLine(IndentLines(Truncate(respBody, 800), 4));

            if (!response.IsSuccessStatusCode)
            {
                return (false, ApiExceptionMapper.MapHttpStatus(
                    (int)response.StatusCode, response.ReasonPhrase));
            }

            // Még 200-on is lehet { "Errors": [...] } a wrapperben.
            // Ezt hibaként tálaljuk, hogy a verifikációs lépés tudjon róla.
            if (TryExtractEnvelopeErrors(respBody, out var envErrors))
            {
                log.AppendLine($"  ! A wrapper hibákat jelzett: {envErrors}");
                return (false, ApiExceptionMapper.MapApiErrors(envErrors));
            }

            return (true, null);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            log.AppendLine("  ✘ A POST 30 mp után időtúllépésbe futott.");
            return (false, ApiExceptionMapper.MapTimeout());
        }
        catch (HttpRequestException ex)
        {
            log.AppendLine($"  ✘ HttpRequestException: {ex.Message}");
            return (false, ApiExceptionMapper.MapHttpRequest(ex));
        }
        catch (Exception ex)
        {
            log.AppendLine($"  ✘ Váratlan: {ex.Message}");
            return (false, ApiExceptionMapper.MapUnexpected(ex));
        }
    }

    /// <summary>GET /orders/{bvin} — a rendelést úgy adja vissza, ahogy a Hotcakes jelenleg tárolja.</summary>
    private async Task<(OrderDetail? Order, string? Error)> FetchOrderAsync(string bvin, StringBuilder log)
    {
        var url = BuildGetUrl(bvin);
        log.AppendLine($"  GET {url}");

        try
        {
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            log.AppendLine($"  ← HTTP {(int)response.StatusCode} ({json.Length} karakter)");

            if (!response.IsSuccessStatusCode)
                return (null, ApiExceptionMapper.MapHttpStatus((int)response.StatusCode, response.ReasonPhrase));

            // A Hotcakes a válaszokat így csomagolja: { "Errors": [...], "Content": { ... } }
            var wrapped = JsonSerializer.Deserialize<HotcakesApiResponse<OrderDetail>>(json, JsonReadOptions);
            if (wrapped == null)
                return (null, ApiExceptionMapper.MapDeserialization());

            if (wrapped.HasErrors)
                return (null, ApiExceptionMapper.MapApiErrors(wrapped.ErrorMessage));

            return (wrapped.Content, null);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            log.AppendLine("  ✘ A GET 30 mp után időtúllépésbe futott.");
            return (null, ApiExceptionMapper.MapTimeout());
        }
        catch (HttpRequestException ex)
        {
            log.AppendLine($"  ✘ HttpRequestException: {ex.Message}");
            return (null, ApiExceptionMapper.MapHttpRequest(ex));
        }
        catch (JsonException ex)
        {
            log.AppendLine($"  ✘ JSON parse hiba: {ex.Message}");
            return (null, ApiExceptionMapper.MapDeserialization());
        }
        catch (Exception ex)
        {
            log.AppendLine($"  ✘ Váratlan: {ex.Message}");
            return (null, ApiExceptionMapper.MapUnexpected(ex));
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Tiszta segédfüggvények
    // ──────────────────────────────────────────────────────────────────

    private static bool StatusMatches(OrderDetail? order, OrderStatus target)
    {
        if (order == null) return false;
        return string.Equals(order.StatusCode, target.StatusCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(order.StatusName, target.StatusName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Best-effort kísérlet, hogy az „Errors" tömböt kinyerjük egy
    /// tetszőleges Hotcakes response envelope-ból, anélkül hogy konkrét
    /// Content alakhoz kötnénk.
    /// false-t ad vissza, ha a body-ban nincs hiba vagy nem parse-olható.
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
        s.Length <= max ? s : s[..max] + $"… (+{s.Length - max} karakter)";
}
