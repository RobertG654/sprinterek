using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// A Hotcakes Commerce REST API szabványos response envelope-ja.
/// Az összes API válasz ebben a struktúrában van becsomagolva:
/// {
///   "Errors": [],
///   "Content": { ... } vagy [ ... ]
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

// Az OrderListContent eltávolítva — a megerősített /orders válasz közvetlenül
// tömbbe csomagolja a rendeléseket: { "Errors": [], "Content": [ {...}, {...} ] }
// A valódi API-ban nincs köztes Items wrapper objektum.
