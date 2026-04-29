using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// A Hotcakes Commerce által használt régi ASP.NET / WCF JSON dátum-formátumot
/// deszerializálja:
///
///   "/Date(1772190902103)/"          → UTC DateTime Unix milliszekundumból
///   "/Date(1772190902103+0200)/"     → ugyanaz (az időzóna offset rögzítve, de
///                                       nincs alkalmazva; a ms érték már abszolút)
///   "/Date(-62135596800000)/"        → nagyon régi dátum (epoch előtti) — kezelve
///   null / ""                        → DateTime.MinValue
///   ISO 8601 karakterlánc            → normál módon parse-olva (visszaeső eset
///                                       újra szerializált értékekhez)
///
/// Használat:
///   Globális regisztráció a JsonSerializerOptions.Converters-ben, VAGY
///   tulajdonságonkénti alkalmazás: [JsonConverter(typeof(DotNetJsonDateConverter))].
/// </summary>
public sealed class DotNetJsonDateConverter : JsonConverter<DateTime>
{
    // /Date(milliseconds)/ formátumra illeszkedik, opcionális ±HHMM időzóna utótaggal
    private static readonly Regex Pattern =
        new(@"^/Date\((-?\d+)([+-]\d{4})?\)/$", RegexOptions.Compiled);

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return DateTime.MinValue;

        var str = reader.GetString();
        if (string.IsNullOrWhiteSpace(str))
            return DateTime.MinValue;

        // Elsődleges: /Date(ms)/ formátum
        var match = Pattern.Match(str);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var ms))
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;

        // Visszaesés: ISO 8601 / RFC 3339 és más szabványos .NET dátum sztringek.
        // Ez kezeli azokat az értékeket, amiket ISO formátumra szerializáltunk
        // vissza (pl. a FromSummary-ben).
        if (DateTime.TryParse(str, null, DateTimeStyles.RoundtripKind, out var parsed))
            return parsed;

        System.Diagnostics.Debug.WriteLine($"[DotNetJsonDateConverter] Nem sikerült értelmezni a dátumot: '{str}'");
        return DateTime.MinValue;
    }

    /// <summary>
    /// A DateTime-okat ISO 8601 ("o" round-trip) formátumban írja ki.
    ///
    /// <para>Furcsa, de élő API-n megerősített tény: a Hotcakes Commerce REST
    /// végpont GET válaszokon „/Date(ms)/" formában <em>küldi</em> a dátumokat,
    /// de a POST body-k input model-binder-e <em>kizárólag</em> ISO 8601-et
    /// fogad el. Ha „/Date(ms)/"-t küldünk vissza, a szerver így válaszol:
    /// <c>{"Code":"EXCEPTION","Description":"/Date(...) is not a valid value
    /// for DateTime."}</c></para>
    ///
    /// <para>Tehát:</para>
    /// <list type="bullet">
    ///   <item>Read elfogad „/Date(ms)/" (elsődleges) és ISO 8601 (fallback) formátumot is.</item>
    ///   <item>Write mindig ISO 8601-et ad ki — ezt tudja értelmezni a szerver.</item>
    /// </list>
    ///
    /// <para>NE módosítsd vissza a Write-ot „/Date(ms)/"-re — minden olyan
    /// POST-ot eltörne, ami DateTime mezőt tartalmaz (lásd a teljes-objektum
    /// fallback-et a <see cref="HotcakesWinFormsApp.Services.OrderStatusUpdateService"/>-ben).</para>
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("o", CultureInfo.InvariantCulture));
}
