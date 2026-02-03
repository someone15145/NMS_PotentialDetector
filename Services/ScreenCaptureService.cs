using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;

namespace NMS_PotentialDetector.Services
{
    public class ScreenCaptureService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        private const int Srccopy = 0x00CC0020;

        public Bitmap Capture(Rect area)
        {
            var bitmap = new Bitmap((int)area.Width, (int)area.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            var dc1 = GetDC(IntPtr.Zero); // Весь экран
            var dc2 = graphics.GetHdc();
            BitBlt(dc2, 0, 0, (int)area.Width, (int)area.Height, dc1, (int)area.X, (int)area.Y, Srccopy);
            graphics.ReleaseHdc(dc2);
            ReleaseDC(IntPtr.Zero, dc1);
            return bitmap;
        }
    }
}