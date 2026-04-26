using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace HotcakesWinFormsApp.Helpers;

/// <summary>
/// Generates real CODE_128 barcodes using ZXing.Net.
///
/// Why CODE_128:
///   • Broad character support (letters, digits, common symbols) — handles
///     both numeric OrderNumbers like "1001" and GUID-style Bvins.
///   • Compact encoding, widely supported by label printers and scanners.
///
/// Output shape:
///   • Returns a PNG byte array ready to hand to QuestPDF's .Image(bytes).
///   • Uses <see cref="BarcodeWriterPixelData"/> from core ZXing.Net so we
///     don't depend on the Windows-specific bindings package — pixels are
///     turned into a System.Drawing Bitmap (available via WinForms) and
///     then encoded as PNG.
/// </summary>
public static class BarcodeGenerator
{
    /// <summary>
    /// Generates a CODE_128 barcode PNG for the supplied content.
    /// Returns null if content is empty or the encoder rejects it (rare, e.g.
    /// illegal characters for the chosen format).
    /// </summary>
    /// <param name="content">String to encode — use OrderNumber when available, Bvin otherwise.</param>
    /// <param name="width">Target barcode width in pixels.</param>
    /// <param name="height">Target barcode height in pixels.</param>
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
                    // Don't ask ZXing to pad the barcode with the raw text underneath —
                    // the PDF template renders the order number itself with nicer typography.
                    PureBarcode = true
                }
            };

            var pixelData = writer.Write(content);
            return ConvertBgraToPng(pixelData);
        }
        catch
        {
            // If encoding fails (unsupported character, internal error, etc.)
            // we return null and the label falls back to text-only rendering.
            return null;
        }
    }

    /// <summary>
    /// Picks the best human-readable content for a barcode: prefers OrderNumber
    /// (short, printable) and falls back to Bvin (GUID) when the order has not
    /// been finalised yet.
    /// </summary>
    public static string ResolveBarcodeContent(string orderNumber, string bvin)
    {
        if (!string.IsNullOrWhiteSpace(orderNumber)) return orderNumber;
        if (!string.IsNullOrWhiteSpace(bvin))        return bvin;
        return "N/A";
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ZXing produces BGRA pixel data (4 bytes per pixel). We copy it into a
    /// 32bpp ARGB System.Drawing Bitmap and then save as PNG in memory.
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
