using System.Windows;
using System.Windows.Shapes;

namespace NMS_PotentialDetector.Models
{
    public class CaptureArea
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public Rect ToRect() => new(X, Y, Width, Height);

        // Для preview в UI
        public Rectangle VisualRect { get; set; } = new();
    }
}