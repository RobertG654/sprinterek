using System.Text.Json.Serialization;

namespace HotcakesWinFormsApp.Models;

/// <summary>
/// Represents a billing or shipping address as returned by Hotcakes Commerce.
/// Null-safe: all string properties default to "".
/// FullName and FullAddress return "" when the data is absent —
/// callers should check IsNullOrWhiteSpace before displaying.
/// </summary>
public class AddressInfo
{
    /// <summary>Hotcakes internal GUID for this address record. May be empty.</summary>
    [JsonPropertyName("Bvin")]
    public string Bvin { get; set; } = "";

    /// <summary>Integer store ID on the address record. API sends as number.</summary>
    [JsonPropertyName("StoreId")]
    public int StoreId { get; set; }

    /// <summary>Address type enum (e.g. 0=General, 1=Billing, 2=Shipping). API sends as number.</summary>
    [JsonPropertyName("AddressType")]
    public int AddressType { get; set; }

    [JsonPropertyName("FirstName")]
    public string FirstName { get; set; } = "";

    [JsonPropertyName("LastName")]
    public string LastName { get; set; } = "";

    /// <summary>Company name. May be empty for private individuals.</summary>
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
    /// Combined first + last name. Returns "" if both are empty.
    /// Callers should check IsNullOrWhiteSpace and fall back to a different field.
    /// </summary>
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Multi-line formatted address. Returns "" if all address parts are empty.
    /// Suitable for PDF labels and invoice display.
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
