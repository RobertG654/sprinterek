namespace HotcakesWinFormsApp.Configuration;

/// <summary>
/// Gyökér beállítás-objektum, az appsettings.json-ból töltődik be.
/// </summary>
public class AppSettings
{
    public HotcakesSettings Hotcakes { get; set; } = new();
    public DemoLoginSettings DemoLogin { get; set; } = new();

    /// <summary>
    /// Ha igaz, az alkalmazás beépített mintaadatokat használ az élő API helyett.
    /// Az appsettings.json-ban true értékre állítva az UI szerver kapcsolat nélkül
    /// is bemutatható.
    /// </summary>
    public bool UseMockData { get; set; } = false;
}

/// <summary>
/// Hotcakes Commerce API kapcsolati beállítások.
/// </summary>
public class HotcakesSettings
{
    /// <summary>
    /// A DotNetNuke oldal alap URL-je.
    /// Példa: "http://4.231.236.217/"
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost/";

    /// <summary>
    /// A Hotcakes REST API elérési útja az alap URL alatt.
    /// Végső kérés URL = BaseUrl.TrimEnd('/') + "/" + ApiBasePath.Trim('/') + "/" + endpoint
    /// Példa: "DesktopModules/Hotcakes/API/rest/v1/"
    /// </summary>
    public string ApiBasePath { get; set; } = "DesktopModules/Hotcakes/API/rest/v1/";

    /// <summary>
    /// A Hotcakes Admin > Configuration > API alatt létrehozott API kulcs.
    /// Minden kérésnél X-HCC-APIKEY HTTP fejlécben kerül elküldésre.
    /// </summary>
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Demó bejelentkezési adatok (nincs szerveroldali hitelesítés — csak helyi ellenőrzés).
/// </summary>
public class DemoLoginSettings
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
}
