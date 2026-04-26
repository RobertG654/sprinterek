using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// Standard Hotcakes Commerce REST API response envelope.
/// All API responses are wrapped in this structure:
/// {
///   "Errors": [],
///   "Content": { ... } or [ ... ]
/// }
/// </summary>
public class HotcakesApiResponse<T>
{
    [JsonPropertyName("Errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("Content")]
    public T? Content { get; set; }

    public bool HasErrors => Errors?.Count > 0;
    public string ErrorMessage => Errors?.Count > 0 ? string.Join("; ", Errors) : "";
}

// OrderListContent was removed — the confirmed /orders response wraps orders
// as a direct array: { "Errors": [], "Content": [ {...}, {...} ] }
// No intermediate Items wrapper object exists in the real API.
