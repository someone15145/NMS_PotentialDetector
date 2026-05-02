using System.Windows;

namespace NMS_PotentialDetector.Models;

/// <summary>
/// Описывает область захвата экрана для анализа.
/// </summary>
public class CaptureArea
{
    /// <summary>
    /// Координата X верхнего левого угла.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Координата Y верхнего левого угла.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Ширина области.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Высота области.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Преобразует область в Rect.
    /// </summary>
    public Rect ToRect() => new(X, Y, Width, Height);
}