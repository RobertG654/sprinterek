using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Configuration;

/// <summary>
/// User-editable API connection settings, persisted to
/// <c>%APPDATA%\HotcakesWinFormsApp\apisettings.json</c> in the user profile.
///
/// This is the runtime replacement for the old "demo login" concept: instead of
/// a username/password, the user now supplies an API key.
///
/// ── WHY %APPDATA% AND NOT THE PROJECT FOLDER? ───────────────────────
/// The API key is a secret. Storing it inside the cloned/built project tree
/// is risky — it's easy to accidentally commit, share via screenshot, or zip
/// up the folder for someone else. By writing to the user's roaming profile
/// (e.g. C:\Users\Alice\AppData\Roaming\HotcakesWinFormsApp\) the key never
/// touches the source/output directory and a `git add .` from the project
/// folder cannot pick it up. It also persists across rebuilds, so wiping
/// bin/ no longer loses the key.
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

    /// <summary>
    /// Absolute path to the apisettings.json file in the user's roaming profile
    /// (e.g. <c>C:\Users\Alice\AppData\Roaming\HotcakesWinFormsApp\apisettings.json</c>).
    /// The directory is created on demand. This path is OUTSIDE the project tree
    /// — accidental commits from the repo are impossible.
    /// </summary>
    [JsonIgnore]
    public static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HotcakesWinFormsApp");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "apisettings.json");
        }
    }

    /// <summary>
    /// Legacy storage location (next to the executable, inside bin\…). Earlier
    /// versions of the app wrote here; <see cref="Load"/> migrates the file to
    /// <see cref="FilePath"/> on first run after the upgrade.
    /// </summary>
    [JsonIgnore]
    private static string LegacyFilePath =>
        Path.Combine(AppContext.BaseDirectory, "apisettings.json");

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
    ///
    /// Performs a one-time migration from <see cref="LegacyFilePath"/> (bin\…)
    /// to the new <see cref="FilePath"/> in %APPDATA% so users who upgrade
    /// don't have to re-enter their key.
    /// </summary>
    public static ApiSettingsStore Load()
    {
        try
        {
            MigrateLegacyFileIfNeeded();
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

    /// <summary>
    /// One-time copy of the legacy bin\apisettings.json into %APPDATA%, then
    /// deletes the legacy file so the secret no longer lives in the build tree.
    /// Best-effort: any IO failure is swallowed (the user can re-enter the key
    /// via <c>ApiSettingsForm</c> if migration fails).
    /// </summary>
    private static void MigrateLegacyFileIfNeeded()
    {
        try
        {
            var legacy = LegacyFilePath;
            if (!File.Exists(legacy)) return;
            if (!File.Exists(FilePath))
                File.Copy(legacy, FilePath, overwrite: false);
            // Either way, scrub the legacy copy so the secret doesn't linger
            // inside the project's bin folder.
            File.Delete(legacy);
        }
        catch
        {
            // Swallow — migration is best-effort. The form will prompt the user
            // for the key if migration didn't produce a usable file.
        }
    }
}
