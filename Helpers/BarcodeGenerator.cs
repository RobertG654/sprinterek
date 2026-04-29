using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// Valódi CODE_128 vonalkódokat generál a ZXing.Net segítségével.
///
/// Miért CODE_128:
///   • Széles karakterkészlet támogatás (betűk, számjegyek, gyakori szimbólumok)
///     — kezeli a numerikus OrderNumber-eket („1001") és a GUID-stílusú Bvin-eket is.
///   • Tömör kódolás, széles körben támogatott a címkenyomtatók és szkennerek által.
///
/// Kimenet:
///   • PNG byte tömböt ad vissza, amit közvetlenül át lehet adni a QuestPDF
///     .Image(bytes) metódusának.
///   • A core ZXing.Net <see cref="BarcodeWriterPixelData"/>-t használja, így nem
///     függünk a Windows-specifikus binding csomagtól — a pixeleket egy
///     System.Drawing Bitmap-be (WinForms-on keresztül elérhető) másoljuk, majd
///     PNG-ként kódoljuk.
/// </summary>
public static class BarcodeGenerator
{
    /// <summary>
    /// CODE_128 vonalkód PNG-t generál a megadott tartalomhoz.
    /// Null-t ad vissza, ha a tartalom üres, vagy ha a kódoló elutasítja
    /// (ritka, pl. a választott formátumhoz nem megengedett karakterek).
    /// </summary>
    /// <param name="content">A kódolandó karakterlánc — ha van, OrderNumber, különben Bvin.</param>
    /// <param name="width">A vonalkód célszélessége pixelben.</param>
    /// <param name="height">A vonalkód célmagassága pixelben.</param>
    public static byte[]? GeneratePng(string content, int width = 300, int height = 70)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width  = width,
                    Height = height,
                    Margin = 2,
                    // Ne kérjük meg a ZXing-et, hogy a nyers szöveget alá tegye —
                    // a PDF sablon szebb tipográfiával maga rendereli a rendelésszámot.
                    PureBarcode = true
                }
            };

            var pixelData = writer.Write(content);
            return ConvertBgraToPng(pixelData);
        }
        catch
        {
            // Ha a kódolás meghiúsul (nem támogatott karakter, belső hiba stb.),
            // null-t adunk vissza, és a címke szövegre esik vissza.
            return null;
        }
    }

    /// <summary>
    /// Kiválasztja a legjobb ember által olvasható tartalmat a vonalkódhoz:
    /// elsőbbséget élvez az OrderNumber (rövid, nyomtatható), és visszaesik a
    /// Bvin-re (GUID), ha a rendelés még nem véglegesített.
    /// </summary>
    public static string ResolveBarcodeContent(string orderNumber, string bvin)
    {
        if (!string.IsNullOrWhiteSpace(orderNumber)) return orderNumber;
        if (!string.IsNullOrWhiteSpace(bvin))        return bvin;
        return "N/A";
    }

    // ──────────────────────────────────────────────────────────────────────
    // Privát segédmetódusok
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A ZXing BGRA pixeladatokat ad ki (pixelenként 4 byte). Átmásoljuk
    /// egy 32bpp ARGB System.Drawing Bitmap-be, majd memóriából PNG-ként
    /// mentjük el.
    /// </summary>
    private static byte[] ConvertBgraToPng(PixelData pixelData)
    {
        using var bitmap = new Bitmap(
            pixelData.Width, pixelData.Height,
            PixelFormat.Format32bppRgb);

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, pixelData.Width, pixelData.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppRgb);

        try
        {
            Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}
