using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotcakesWinFormsApp.Helpers;

/// API kulcs alaki ellenőrzése.
/// A Hotcakes kulcs egy GUID formátumú string – kötőjelekkel, 36 karakter hosszú. Üres vagy ettől eltérő alakú értékre
/// false-ot ad, és így a hívó réteg meg tudja előzni a felesleges hálózati kérést.
public static class ApiKeyValidator
{
    public static bool IsValid(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return false;
        return Guid.TryParse(apiKey, out _);
    }
}