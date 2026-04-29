using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DNNHello.Tests.Helpers
{
    // Teszthez modellezett FileNameHelper.
    // Az eredeti DNNHello/Controllers/GalleryApiController.cs Upload() metódusában lévő fájlnév-generáló logikát tükrözi:
    // var uniqueName = DateTime.UtcNow.Ticks + "_" + fileName
    public static class FileNameHelper
    {
        public static string GenerateUniqueFileName(string originalFileName)
        {
            return DateTime.UtcNow.Ticks + "_" + originalFileName;
        }
    }

    [TestClass]
    public class FileNameHelperTests
    {
        // UT-MOD-02
        // Cél: a generált fájlnév
        //   1) tartalmazza az időbélyeget (Ticks)
        //   2) ne legyenek benne tiltott karakterek

        [TestMethod]
        public void GenerateUniqueFileName_ContainsTimestamp()
        {
            var result = FileNameHelper.GenerateUniqueFileName("kep.jpg");

            // A Ticks egy hosszú szám – aláhúzás előtt kell lennie
            string ticksPart = result.Split('_')[0];

            Assert.IsTrue(long.TryParse(ticksPart, out _),
                "A fájlnév eleje nem érvényes időbélyeg.");
        }

        [TestMethod]
        public void GenerateUniqueFileName_NoInvalidFileSystemChars()
        {
            var result = FileNameHelper.GenerateUniqueFileName("kep.jpg");

            char[] invalidChars = Path.GetInvalidFileNameChars();

            Assert.IsFalse(result.Any(c => invalidChars.Contains(c)),
                "A generált fájlnév tiltott karaktert tartalmaz.");
        }
    }
}
