using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// A Hotcakes Commerce által visszaadott számlázási vagy szállítási címet
/// reprezentálja.
/// Null-biztos: minden string tulajdonság alapértelmezetten "".
/// A FullName és FullAddress üres karakterláncot ad vissza, ha nincs adat —
/// a hívóknak IsNullOrWhiteSpace-szel kell ellenőrizniük megjelenítés előtt.
/// </summary>
public class AddressInfo
{
    /// <summary>A cím rekordhoz tartozó belső Hotcakes GUID. Lehet üres.</summary>
    [JsonPropertyName("Bvin")]
    public string Bvin { get; set; } = "";

    /// <summary>A cím rekord egész számos bolt-azonosítója. Az API számként küldi.</summary>
    [JsonPropertyName("StoreId")]
    public int StoreId { get; set; }

    /// <summary>Cím típus enum (pl. 0=Általános, 1=Számlázási, 2=Szállítási). Az API számként küldi.</summary>
    [JsonPropertyName("AddressType")]
    public int AddressType { get; set; }

    [JsonPropertyName("FirstName")]
    public string FirstName { get; set; } = "";

    [JsonPropertyName("LastName")]
    public string LastName { get; set; } = "";

    /// <summary>Cégnév. Magánszemélyeknél lehet üres.</summary>
    [JsonPropertyName("Company")]
    public string Company { get; set; } = "";

    [JsonPropertyName("Line1")]
    public string Line1 { get; set; } = "";

    [JsonPropertyName("Line2")]
    public string Line2 { get; set; } = "";

    [JsonPropertyName("City")]
    public string City { get; set; } = "";

    [JsonPropertyName("RegionName")]
    public string RegionName { get; set; } = "";

    [JsonPropertyName("PostalCode")]
    public string PostalCode { get; set; } = "";

    [JsonPropertyName("CountryName")]
    public string CountryName { get; set; } = "";

    [JsonPropertyName("Phone")]
    public string Phone { get; set; } = "";

    /// <summary>
    /// Egyesített kereszt- és vezetéknév. Üres karakterláncot ad vissza, ha
    /// mindkettő üres. A hívóknak IsNullOrWhiteSpace-szel kell ellenőrizniük,
    /// és más mezőre kell visszaesniük.
    /// </summary>
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Több soros formátumú cím. Üres karakterláncot ad vissza, ha minden
    /// cím-elem üres.
    /// PDF címkékhez és számla megjelenítéshez alkalmas.
    /// </summary>
    [JsonIgnore]
    public string FullAddress
    {
        get
        {
            var cityLine = $"{PostalCode} {City}".Trim();
            var parts = new[] { Line1, Line2, cityLine, RegionName, CountryName }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join("\n", parts);
        }
    }
}
