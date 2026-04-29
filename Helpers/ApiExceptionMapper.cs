using System.Net;

namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// Műszaki API / hálózati kivételeket alakít át felhasználóbarát magyar
/// üzenetekké.
///
/// Ezt az osztályt a szerviz metódusokban használd (nem a form-okban), hogy
/// az UI kód tiszta maradjon, és csak honosított, ember által olvasható
/// szövegeket lásson.
///
/// Példa használat:
///   catch (TaskCanceledException) { return (empty, ApiExceptionMapper.MapTimeout()); }
///   catch (HttpRequestException ex) { return (empty, ApiExceptionMapper.MapHttpRequest(ex)); }
/// </summary>
public static class ApiExceptionMapper
{
    /// <summary>Időtúllépés leképezése (TaskCanceledException explicit cancellation nélkül).</summary>
    public static string MapTimeout() =>
        "Az API nem válaszol időben. Kérjük, próbálja újra később.\n" +
        "Lehetséges, hogy a szerver átmenetileg nem elérhető.";

    /// <summary>Felhasználó által kezdeményezett megszakítás leképezése.</summary>
    public static string MapCancelled() =>
        "A kérés megszakadt.";

    /// <summary>
    /// HttpRequestException leképezése — lefedi a hálózati szintű hibákat
    /// (státuszkód nélkül) és a HTTP hibaválaszokat is (4xx/5xx, az
    /// ex.StatusCode-ban).
    /// </summary>
    public static string MapHttpRequest(HttpRequestException ex)
    {
        if (ex.StatusCode.HasValue)
            return MapHttpStatus((int)ex.StatusCode, ex.Message);

        // Nincs HTTP státuszkód → hálózati rétegbeli hiba (DNS, refused, unreachable).
        return "Nem sikerült kapcsolódni a szerverhez.\n" +
               "Ellenőrizze az internetkapcsolatot, a szerver elérhetőségét és az API URL-t.";
    }

    /// <summary>Nyers HTTP státuszkód leképezése (response.IsSuccessStatusCode ellenőrzés után használandó).</summary>
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
    /// Az az eset, amikor a HTTP hívás sikeres volt, de a JSON-t nem sikerült
    /// deszerializálni. Minden JSON parse-kísérlet meghiúsult.
    /// </summary>
    public static string MapDeserialization() =>
        "A szerver válasza beérkezett, de az adatok feldolgozása nem sikerült.\n" +
        "Valószínűleg a válasz formátuma eltér a várttól.\n\n" +
        "A nyers JSON a Debug kimenetben látható ([HotcakesAPI] előtag).";

    /// <summary>
    /// Amikor az API a response envelope-ban hibákat adott vissza.
    /// </summary>
    public static string MapApiErrors(string errors) =>
        $"Az API hibaüzenetet küldött vissza:\n{errors}";

    /// <summary>Végső gyűjtő-eset valóban váratlan kivételekhez.</summary>
    public static string MapUnexpected(Exception ex) =>
        $"Váratlan hiba történt: {ex.Message}";
}
