using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace NMS_PotentialDetector.Services;

/// <summary>
/// Handles screen region capturing.
/// </summary>
public class ScreenCaptureService
{
    /// <summary>
    /// Captures a specified screen area.
    /// </summary>
    public Bitmap Capture(Rect area)
    {
        Bitmap bitmap = new((int)area.Width, (int)area.Height, PixelFormat.Format32bppArgb);

        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen((int)area.X, (int)area.Y, 0, 0, new System.Drawing.Size((int)area.Width, (int)area.Height));

        return bitmap;
    }
}