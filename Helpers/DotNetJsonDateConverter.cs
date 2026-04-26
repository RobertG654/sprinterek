using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// Deserializes the old ASP.NET/WCF JSON date format used by Hotcakes Commerce:
///
///   "/Date(1772190902103)/"          → UTC DateTime from Unix milliseconds
///   "/Date(1772190902103+0200)/"     → same (timezone offset captured but not applied;
///                                       the ms value is already absolute)
///   "/Date(-62135596800000)/"        → very old date (before epoch) — handled correctly
///   null / ""                        → DateTime.MinValue
///   ISO 8601 string                  → parsed normally (fallback for re-serialized values)
///
/// Usage:
///   Register globally in JsonSerializerOptions.Converters, OR
///   Apply per-property with [JsonConverter(typeof(DotNetJsonDateConverter))].
/// </summary>
public sealed class DotNetJsonDateConverter : JsonConverter<DateTime>
{
    // Matches /Date(milliseconds)/ with an optional ±HHMM timezone suffix
    private static readonly Regex Pattern =
        new(@"^/Date\((-?\d+)([+-]\d{4})?\)/$", RegexOptions.Compiled);

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return DateTime.MinValue;

        var str = reader.GetString();
        if (string.IsNullOrWhiteSpace(str))
            return DateTime.MinValue;

        // Primary: /Date(ms)/ format
        var match = Pattern.Match(str);
        if (match.Success && long.TryParse(match.Groups[1].Value, out var ms))
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;

        // Fallback: ISO 8601 / RFC 3339 and other standard .NET date strings.
        // This handles values that were re-serialized back to ISO format (e.g. in FromSummary).
        if (DateTime.TryParse(str, null, DateTimeStyles.RoundtripKind, out var parsed))
            return parsed;

        System.Diagnostics.Debug.WriteLine($"[DotNetJsonDateConverter] Could not parse date: '{str}'");
        return DateTime.MinValue;
    }

    /// <summary>
    /// Writes DateTimes as ISO 8601 ("o" round-trip format).
    ///
    /// <para>Counter-intuitive but confirmed against the live API: the Hotcakes
    /// Commerce REST endpoint <em>emits</em> dates in "/Date(ms)/" form on GET
    /// responses, but its input model-binder only <em>accepts</em> ISO 8601 on
    /// POST bodies. Sending "/Date(ms)/" back results in:
    /// <c>{"Code":"EXCEPTION","Description":"/Date(...) is not a valid value
    /// for DateTime."}</c></para>
    ///
    /// <para>So:</para>
    /// <list type="bullet">
    ///   <item>Read accepts both "/Date(ms)/" (primary) and ISO 8601 (fallback).</item>
    ///   <item>Write always emits ISO 8601 — that's what the server can parse.</item>
    /// </list>
    ///
    /// <para>DO NOT change Write back to "/Date(ms)/" — it will break every
    /// POST that includes DateTime fields (see the full-object fallback in
    /// <see cref="HotcakesWinFormsApp.Services.OrderStatusUpdateService"/>).</para>
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("o", CultureInfo.InvariantCulture));
}
