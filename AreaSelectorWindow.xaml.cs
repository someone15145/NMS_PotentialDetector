using NMS_PotentialDetector.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NMS_PotentialDetector;

/// <summary>
/// Окно выбора области захвата.
/// </summary>
public partial class AreaSelectorWindow : Window
{
    /// <summary>
    /// Флаг процесса выделения.
    /// </summary>
    private bool _isSelecting;

    /// <summary>
    /// Начальная точка выделения.
    /// </summary>
    private Point _startPoint;

    /// <summary>
    /// Выбранная пользователем область.
    /// </summary>
    public CaptureArea? SelectedArea { get; private set; }

    /// <summary>
    /// Конструктор окна.
    /// </summary>
    public AreaSelectorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Начало выделения.
    /// </summary>
    private void SelectionCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(SelectionCanvas);
        _isSelecting = true;
        SelectionCanvas.CaptureMouse();
        SelectionRect.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Изменение размера области.
    /// </summary>
    private void SelectionCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting)
            return;

        Point currentPoint = e.GetPosition(SelectionCanvas);

        double x = Math.Min(currentPoint.X, _startPoint.X);
        double y = Math.Min(currentPoint.Y, _startPoint.Y);
        double width = Math.Abs(currentPoint.X - _startPoint.X);
        double height = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = width;
        SelectionRect.Height = height;
    }

    /// <summary>
    /// Завершение выделения.
    /// </summary>
    private void SelectionCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting)
            return;

        _isSelecting = false;
        SelectionCanvas.ReleaseMouseCapture();

        Point endPoint = e.GetPosition(SelectionCanvas);

        SelectedArea = new CaptureArea
        {
            X = Math.Min(_startPoint.X, endPoint.X),
            Y = Math.Min(_startPoint.Y, endPoint.Y),
            Width = Math.Abs(endPoint.X - _startPoint.X),
            Height = Math.Abs(endPoint.Y - _startPoint.Y)
        };

        PresentationSource? source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            double dpiX = source.CompositionTarget.TransformToDevice.M11;
            double dpiY = source.CompositionTarget.TransformToDevice.M22;

            SelectedArea.X *= dpiX;
            SelectedArea.Y *= dpiY;
            SelectedArea.Width *= dpiX;
            SelectedArea.Height *= dpiY;
        }

        DialogResult = true;
        Close();
    }
}