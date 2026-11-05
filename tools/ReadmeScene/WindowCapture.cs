using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace Source191.Torni.ReadmeScene;

/// <summary>
/// Captures the scene's window with <c>PrintWindow</c> and <c>PW_RENDERFULLCONTENT</c>, which draws Windows Terminal's
/// content even while the window is covered or the workstation is locked.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowCapture
{
    public static void ToPng(nint window, string file)
    {
        using Bitmap bitmap = Capture(window);
        bitmap.Save(file, ImageFormat.Png);
    }

    public static Bitmap Capture(nint window)
    {
        NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.PerMonitorAwareV2);
        if (!NativeMethods.GetWindowRect(window, out NativeMethods.Rectangle bounds))
        {
            throw new InvalidOperationException("The scene's window has gone.");
        }

        Bitmap bitmap = new(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        nint deviceContext = graphics.GetHdc();
        try
        {
            NativeMethods.PrintWindow(window, deviceContext, NativeMethods.PrintWindowRenderFullContent);
        }
        finally
        {
            graphics.ReleaseHdc(deviceContext);
        }

        return bitmap;
    }
}