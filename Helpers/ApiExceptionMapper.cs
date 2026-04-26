using System.Net;

namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// Converts technical API/network exceptions into user-friendly Hungarian messages.
///
/// Use this class inside service methods (not in forms) so that UI code stays clean
/// and only sees localized, human-readable strings.
///
/// Usage example:
///   catch (TaskCanceledException) { return (empty, ApiExceptionMapper.MapTimeout()); }
///   catch (HttpRequestException ex) { return (empty, ApiExceptionMapper.MapHttpRequest(ex)); }
/// </summary>
public static class ApiExceptionMapper
{
    /// <summary>Maps a request timeout (TaskCanceledException without explicit cancellation).</summary>
    public static string MapTimeout() =>
        "Az API nem válaszol időben. Kérjük, próbálja újra később.\n" +
        "Lehetséges, hogy a szerver átmenetileg nem elérhető.";

    /// <summary>Maps a user-initiated cancellation.</summary>
    public static string MapCancelled() =>
        "A kérés megszakadt.";

    /// <summary>
    /// Maps an HttpRequestException — covers both network-level errors (no status code)
    /// and HTTP error responses (4xx/5xx captured in ex.StatusCode).
    /// </summary>
    public static string MapHttpRequest(HttpRequestException ex)
    {
        if (ex.StatusCode.HasValue)
            return MapHttpStatus((int)ex.StatusCode, ex.Message);

        // No HTTP status code → network-layer failure (DNS, refused, unreachable)
        return "Nem sikerült kapcsolódni a szerverhez.\n" +
               "Ellenőrizze az internetkapcsolatot, a szerver elérhetőségét és az API URL-t.";
    }

    /// <summary>Maps a raw HTTP status code (for use after response.IsSuccessStatusCode check).</summary>
    public static string MapHttpStatus(int statusCode, string? detail = null)
    {
        return statusCode switch
        {
            401 or 403 =>
                "Az API kulcs hiányzik vagy érvénytelen (HTTP " + statusCode + ").\n" +
                "Ellenőrizze az ApiKey értékét az appsettings.json fájlban.",

            404 =>
                "Az API végpont nem található (HTTP 404).\n" +
                "Ellenőrizze az ApiBasePath értékét az appsettings.json fájlban.",

            >= 500 =>
                $"A szerver belső hibát adott vissza (HTTP {statusCode}).\n" +
                "Kérjük, próbálja újra később.",

            _ => string.IsNullOrWhiteSpace(detail)
                ? $"HTTP hiba: {statusCode}"
                : $"HTTP hiba: {statusCode} — {detail}"
        };
    }

    /// <summary>
    /// Maps the case where HTTP was successful but JSON could not be deserialized.
    /// All JSON parse attempts failed.
    /// </summary>
    public static string MapDeserialization() =>
        "A szerver válasza beérkezett, de az adatok feldolgozása nem sikerült.\n" +
        "Valószínűleg a válasz formátuma eltér a várttól.\n\n" +
        "A nyers JSON a Debug kimenetben látható ([HotcakesAPI] előtag).";

    /// <summary>
    /// Maps the case where the API returned Errors in the response envelope.
    /// </summary>
    public static string MapApiErrors(string errors) =>
        $"Az API hibaüzenetet küldött vissza:\n{errors}";

    /// <summary>Catch-all for truly unexpected exceptions.</summary>
    public static string MapUnexpected(Exception ex) =>
        $"Váratlan hiba történt: {ex.Message}";
}
