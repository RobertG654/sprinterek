namespace HotcakesWinFormsApp.Configuration;

/// <summary>
/// Root settings object bound from appsettings.json.
/// </summary>
public class AppSettings
{
    public HotcakesSettings Hotcakes { get; set; } = new();
    public DemoLoginSettings DemoLogin { get; set; } = new();

    /// <summary>
    /// When true, the app uses built-in mock data instead of calling the live API.
    /// Set to true in appsettings.json to demo the UI without a server connection.
    /// </summary>
    public bool UseMockData { get; set; } = false;
}

/// <summary>
/// Hotcakes Commerce API connection settings.
/// </summary>
public class HotcakesSettings
{
    /// <summary>
    /// Base URL of the DotNetNuke site.
    /// Example: "http://4.231.236.217/"
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost/";

    /// <summary>
    /// Path to the Hotcakes REST API under the base URL.
    /// Final request URL = BaseUrl.TrimEnd('/') + "/" + ApiBasePath.Trim('/') + "/" + endpoint
    /// Example: "DesktopModules/Hotcakes/API/rest/v1/"
    /// </summary>
    public string ApiBasePath { get; set; } = "DesktopModules/Hotcakes/API/rest/v1/";

    /// <summary>
    /// API key created in Hotcakes Admin > Configuration > API.
    /// Sent as HTTP header X-HCC-APIKEY on every request.
    /// </summary>
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Demo login credentials (no server authentication — local check only).
/// </summary>
public class DemoLoginSettings
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
}
