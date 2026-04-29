using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HotcakesWinFormsApp.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HotcakesWinFormsApp.Tests.Helpers;

[TestClass]
public class BarcodeGeneratorTests
{
    // UT-KLIENS-03
    // Cél: érvényes rendelési számra ne legyen null, és a kimenet PNG.

    [TestMethod]
    public void GeneratePng_ValidOrderNumber_ReturnsNonNull()
    {
        byte[]? result = BarcodeGenerator.GeneratePng("1001");

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void GeneratePng_ValidOrderNumber_ReturnsPngFormat()
    {
        // PNG fájl első 4 byte-ja: 0x89 'P' 'N' 'G'
        byte[]? result = BarcodeGenerator.GeneratePng("1001");

        Assert.IsNotNull(result);
        Assert.AreEqual(0x89, result[0]);
        Assert.AreEqual(0x50, result[1]);
        Assert.AreEqual(0x4E, result[2]);
        Assert.AreEqual(0x47, result[3]);
    }
}