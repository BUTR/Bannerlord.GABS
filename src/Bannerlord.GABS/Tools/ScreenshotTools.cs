// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using TaleWorlds.Engine;

using Path = System.IO.Path;

namespace Bannerlord.GABS.Tools;

public partial class ScreenshotTools
{
    #region GDI+ P/Invoke (for BMP-to-JPEG conversion)

    [DllImport("gdiplus.dll", CharSet = CharSet.Unicode)]
    private static extern int GdiplusStartup(out IntPtr token, ref GdiplusStartupInput input, IntPtr output);

    [DllImport("gdiplus.dll")]
    private static extern void GdiplusShutdown(IntPtr token);

    [DllImport("gdiplus.dll", CharSet = CharSet.Unicode)]
    private static extern int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);

    [DllImport("gdiplus.dll")]
    private static extern int GdipDisposeImage(IntPtr image);

    [DllImport("gdiplus.dll", CharSet = CharSet.Unicode)]
    private static extern int GdipSaveImageToFile(IntPtr image, string filename,
        ref Guid clsidEncoder, IntPtr encoderParams);

    [DllImport("gdiplus.dll")]
    private static extern int GdipGetImageWidth(IntPtr image, out uint width);

    [DllImport("gdiplus.dll")]
    private static extern int GdipGetImageHeight(IntPtr image, out uint height);

    [StructLayout(LayoutKind.Sequential)]
    private struct GdiplusStartupInput
    {
        public uint GdiplusVersion;
        public IntPtr DebugEventCallback;
        public int SuppressBackgroundThread;
        public int SuppressExternalCodecs;
    }

    // JPEG encoder CLSID: {557CF401-1A04-11D3-9A73-0000F81EF32E}
    private static Guid JpegEncoderClsid = new Guid("557CF401-1A04-11D3-9A73-0000F81EF32E");

    #endregion

    private static readonly string ScreenshotDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Mount and Blade II Bannerlord", "Screenshots", "GABS");

    private static void ConvertBmpToJpeg(string bmpPath, string jpegPath)
    {
        var startupInput = new GdiplusStartupInput { GdiplusVersion = 1 };
        int status = GdiplusStartup(out IntPtr gdipToken, ref startupInput, IntPtr.Zero);
        if (status != 0)
            throw new Exception($"GDI+ startup failed with status {status}");

        try
        {
            status = GdipCreateBitmapFromFile(bmpPath, out var gpBitmap);
            if (status != 0)
                throw new Exception($"GdipCreateBitmapFromFile failed with status {status}");

            try
            {
                status = GdipSaveImageToFile(gpBitmap, jpegPath, ref JpegEncoderClsid, IntPtr.Zero);
                if (status != 0)
                    throw new Exception($"GdipSaveImageToFile failed with status {status}");
            }
            finally
            {
                GdipDisposeImage(gpBitmap);
            }
        }
        finally
        {
            GdiplusShutdown(gdipToken);
        }
    }

    private static (uint width, uint height) GetImageDimensions(string bmpPath)
    {
        var startupInput = new GdiplusStartupInput { GdiplusVersion = 1 };
        GdiplusStartup(out var gdipToken, ref startupInput, IntPtr.Zero);
        try
        {
            GdipCreateBitmapFromFile(bmpPath, out var gpBitmap);
            try
            {
                GdipGetImageWidth(gpBitmap, out var w);
                GdipGetImageHeight(gpBitmap, out var h);
                return (w, h);
            }
            finally
            {
                GdipDisposeImage(gpBitmap);
            }
        }
        finally
        {
            GdiplusShutdown(gdipToken);
        }
    }

    [Tool("ui/take_screenshot", Description = "Take a screenshot of the game window. Saves as JPEG and returns the file path. Use the Read tool on the returned path to view the image.")]
    public partial Task<object> TakeScreenshot()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            try
            {
                Directory.CreateDirectory(ScreenshotDir);

                // Use engine API to capture the rendered frame as BMP (works regardless of focus)
                var tempBmp = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.bmp");
                Utilities.TakeScreenshot(tempBmp);

                if (!File.Exists(tempBmp))
                    return new { error = "Engine screenshot failed — file was not created" };

                // Get dimensions from the BMP, then convert to JPEG using GDI+
                var (width, height) = GetImageDimensions(tempBmp);

                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine(ScreenshotDir, fileName);
                ConvertBmpToJpeg(tempBmp, filePath);

                // Clean up temp BMP
                try { File.Delete(tempBmp); } catch { }

                var fileInfo = new FileInfo(filePath);

                // Clean up old screenshots (keep last 10)
                try
                {
                    var files = new DirectoryInfo(ScreenshotDir).GetFiles("screenshot_*.jpg");
                    if (files.Length > 10)
                    {
                        Array.Sort(files, (a, b) => a.CreationTime.CompareTo(b.CreationTime));
                        for (int i = 0; i < files.Length - 10; i++)
                            files[i].Delete();
                    }
                }
                catch { }

                return new
                {
                    /// Absolute path to the saved JPEG file
                    filePath,
                    /// File size in kilobytes
                    fileSizeKB = fileInfo.Length / 1024,
                    /// Image width in pixels
                    width,
                    /// Image height in pixels
                    height,
                    /// Image format (always 'jpeg')
                    format = "jpeg",
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Screenshot failed: {ex.Message}" };
            }
        });
    }
}