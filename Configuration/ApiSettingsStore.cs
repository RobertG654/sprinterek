using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Configuration;

/// <summary>
/// Felhasználó által szerkeszthető API kapcsolati beállítások, a felhasználói
/// profilban tárolva: <c>%APPDATA%\HotcakesWinFormsApp\apisettings.json</c>.
///
/// Ez a futásidejű utódja a régi „demo bejelentkezés" koncepciónak: felhasználónév
/// és jelszó helyett a felhasználó most egy API kulcsot ad meg.
///
/// ── MIÉRT %APPDATA% ÉS NEM A PROJEKT MAPPA? ─────────────────────────
/// Az API kulcs egy titok. Ha a klónozott / fordított projekt fa alatt tárolnánk,
/// könnyen véletlenül commit-olható, képernyőkép-ben megosztható, vagy bezippelt
/// mappában elküldhető lenne. A roaming profilba írással
/// (pl. C:\Users\Alice\AppData\Roaming\HotcakesWinFormsApp\) a kulcs sosem
/// kerül a forrás- vagy kimeneti könyvtárba, és a `git add .` a projekt
/// mappából nem tudja felvenni. Bónuszként a build-ek között is megőrződik,
/// így a bin/ kitörlése nem szünteti meg a kulcsot.
///
/// Az alkalmazás induláskor:
///   • Ha a fájl nincs vagy az ApiKey üres → megjeleníti az <c>ApiSettingsForm</c>-ot.
///   • Ha a fájl létezik és van ApiKey → továbbmegy a főablakra.
///
/// A BaseUrl és ApiBasePath értékét az appsettings.json-ból tölti be a fájl
/// első létrehozásakor, de utána már itt tartja, hogy a felhasználó az
/// appsettings.json érintése nélkül tudja módosítani.
/// </summary>
public class ApiSettingsStore
{
    /// <summary>A DotNetNuke / Hotcakes oldal alap URL-je.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>A Hotcakes REST API relatív elérési útja (pl. "DesktopModules/Hotcakes/API/rest/v1/").</summary>
    public string ApiBasePath { get; set; } = "";

    /// <summary>
    /// A felhasználó által a beállító ablakban megadott API kulcs.
    /// Helyben, sima szövegként tárolva — a Hotcakes maga is hosszú élettartamú
    /// bearer tokenként kezeli, így a helyi titkosítás kevés értéket adna hozzá.
    /// </summary>
    public string ApiKey { get; set; } = "";

    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Az apisettings.json abszolút útvonala a felhasználó roaming profiljában
    /// (pl. <c>C:\Users\Alice\AppData\Roaming\HotcakesWinFormsApp\apisettings.json</c>).
    /// A könyvtár igény szerint létrejön. Ez az útvonal a projekt FA-N KÍVÜL van —
    /// repo-ból véletlenül nem lehet kommitolni.
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
    /// Régi tárolási hely (a futtatható mellett, a bin\… alatt). A korábbi
    /// verziók ide írtak; a <see cref="Load"/> az upgrade utáni első induláskor
    /// átköltözteti a fájlt a <see cref="FilePath"/> alá.
    /// </summary>
    [JsonIgnore]
    private static string LegacyFilePath =>
        Path.Combine(AppContext.BaseDirectory, "apisettings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    /// <summary>Igaz, ha a felhasználó megadott API kulcsot.</summary>
    [JsonIgnore]
    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Betölti a felhasználó API beállításait a lemezről, vagy egy új üres
    /// példányt ad vissza (a fájl írása nélkül), ha még nem létezik. A hívóknak
    /// a <see cref="HasApiKey"/> alapján kell eldönteniük, hogy meg kell-e
    /// nyitni a beállító ablakot.
    ///
    /// Egyszeri migrációt végez a <see cref="LegacyFilePath"/> (bin\…) helyről
    /// az új <see cref="FilePath"/>-re a %APPDATA%-ban, hogy az upgrade-elő
    /// felhasználóknak ne kelljen újra megadniuk a kulcsukat.
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

    /// <summary>A beállításokat a <see cref="FilePath"/> útvonalra írja.</summary>
    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// Egyszeri másolás a régi bin\apisettings.json-ból a %APPDATA%-ba, majd a
    /// régi fájl törlése, hogy a titok ne maradjon a build fában. Best-effort:
    /// bármilyen IO hiba elnyelődik (a felhasználó újra megadhatja a kulcsot az
    /// <c>ApiSettingsForm</c>-ban, ha a migráció nem sikerült).
    /// </summary>
    private static void MigrateLegacyFileIfNeeded()
    {
        try
        {
            var legacy = LegacyFilePath;
            if (!File.Exists(legacy)) return;
            if (!File.Exists(FilePath))
                File.Copy(legacy, FilePath, overwrite: false);
            // Mindenképpen takarítsuk ki a régi másolatot, hogy a titok ne
            // ácsorogjon a projekt bin mappájában.
            File.Delete(legacy);
        }
        catch
        {
            // Elnyeljük — a migráció best-effort. A form prompt-olja a
            // felhasználót a kulcsért, ha a migráció nem hozott használható fájlt.
        }
    }
}
