using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace NMS_PotentialDetector.Helpers;

/// <summary>
/// Bitmap conversion helpers.
/// </summary>
public static class BitmapExtensions
{
    /// <summary>
    /// Converts Bitmap to BitmapImage.
    /// </summary>
    public static BitmapImage ToBitmapImage(this Bitmap bitmap)
    {
        using MemoryStream stream = new();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        return image;
    }
}