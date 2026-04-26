using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Configuration;

/// <summary>
/// User-editable API connection settings, persisted to <c>apisettings.json</c>
/// next to the executable.
///
/// This is the runtime replacement for the old "demo login" concept: instead of
/// a username/password, the user now supplies an API key.
///
/// On application start:
///   • If the file is missing or the ApiKey is empty → show <c>ApiSettingsForm</c>
///   • If the file exists and ApiKey is set → proceed to the main form
///
/// BaseUrl and ApiBasePath are seeded from appsettings.json when the file is
/// first created, but are persisted here afterwards so the user can change
/// them without editing appsettings.json.
/// </summary>
public class ApiSettingsStore
{
    /// <summary>Base URL of the DotNetNuke / Hotcakes site.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Relative path to the Hotcakes REST API (e.g. "DesktopModules/Hotcakes/API/rest/v1/").</summary>
    public string ApiBasePath { get; set; } = "";

    /// <summary>
    /// The API key the user entered in the settings form.
    /// Stored locally as plain text — Hotcakes itself also treats this as a
    /// long-lived bearer token, so encrypting it here would add little value.
    /// </summary>
    public string ApiKey { get; set; } = "";

    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Absolute path to the apisettings.json file, next to the executable.</summary>
    [JsonIgnore]
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "apisettings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    /// <summary>True when an API key has been supplied by the user.</summary>
    [JsonIgnore]
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Loads the user's API settings from disk, or returns a new empty instance
    /// (without writing the file) when none exists yet. Callers should check
    /// <see cref="HasApiKey"/> to decide whether to open the settings form.
    /// </summary>
    public static ApiSettingsStore Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new ApiSettingsStore();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ApiSettingsStore>(json, JsonOptions)
                   ?? new ApiSettingsStore();
        }
        catch
        {
            return new ApiSettingsStore();
        }
    }

    /// <summary>Writes settings to <see cref="FilePath"/>.</summary>
    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
