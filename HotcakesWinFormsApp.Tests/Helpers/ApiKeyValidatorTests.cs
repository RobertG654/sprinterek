using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotcakesWinFormsApp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HotcakesWinFormsApp.Tests.Helpers;

[TestClass]
public class ApiKeyValidatorTests
{
    // UT-KLIENS-01
    // Cél: üres vagy hibás formátumú kulcsra false-t adjon, hogy a hívó réteg ne is indítson hálózati kérést.

    [TestMethod]
    public void IsValid_EmptyKey_ReturnsFalse()
    {
        Assert.IsFalse(ApiKeyValidator.IsValid(""));
        Assert.IsFalse(ApiKeyValidator.IsValid("   "));
        Assert.IsFalse(ApiKeyValidator.IsValid(null));
    }

    [TestMethod]
    public void IsValid_MalformedKey_ReturnsFalse()
    {
        // Nem GUID alakú strings
        Assert.IsFalse(ApiKeyValidator.IsValid("abc123"));
        Assert.IsFalse(ApiKeyValidator.IsValid("not-a-guid"));
    }

    [TestMethod]
    public void IsValid_ValidGuidKey_ReturnsTrue()
    {
        // Egy érvényes GUID alakú kulcs (kontroll eset)
        Assert.IsTrue(ApiKeyValidator.IsValid("12345678-1234-1234-1234-123456789012"));
    }
}
