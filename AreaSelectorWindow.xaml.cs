using NMS_PotentialDetector.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace NMS_PotentialDetector
{
    public partial class AreaSelectorWindow : Window
    {
        private bool _isSelecting;
        private Point _startPoint;
        public CaptureArea? SelectedArea { get; private set; }

        public AreaSelectorWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = 0; Top = 0;
        }

        private void SelectionCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(SelectionCanvas);
            _isSelecting = true;
            SelectionCanvas.CaptureMouse();
            SelectionRect.Visibility = Visibility.Visible;
        }

        private void SelectionCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;
            var current = e.GetPosition(SelectionCanvas);
            SelectionRect.Width = Math.Abs(current.X - _startPoint.X);
            SelectionRect.Height = Math.Abs(current.Y - _startPoint.Y);
            Canvas.SetLeft(SelectionRect, Math.Min(current.X, _startPoint.X));
            Canvas.SetTop(SelectionRect, Math.Min(current.Y, _startPoint.Y));
        }

        private void SelectionCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting) return;
            _isSelecting = false;
            SelectionCanvas.ReleaseMouseCapture();

            var endPoint = e.GetPosition(SelectionCanvas);

            // Выделенная область почему-то смещается
            endPoint.X -= 8;
            endPoint.Y -= 8;

            SelectedArea = new CaptureArea
            {
                X = Math.Min(_startPoint.X, endPoint.X),
                Y = Math.Min(_startPoint.Y, endPoint.Y),
                Width = Math.Abs(endPoint.X - _startPoint.X),
                Height = Math.Abs(endPoint.Y - _startPoint.Y)
            };

            // Новое: Конвертация DIPs в physical pixels для DPI scaling
            var source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                var dpiX = source.CompositionTarget.TransformToDevice.M11; // Scaling factor по X (DPI/96)
                var dpiY = source.CompositionTarget.TransformToDevice.M22; // По Y (обычно = dpiX)
                SelectedArea.X *= dpiX;
                SelectedArea.Y *= dpiY;
                SelectedArea.Width *= dpiX;
                SelectedArea.Height *= dpiY;
            }

            SelectionRect.Visibility = Visibility.Collapsed;
            DialogResult = true;
            Close();
        }
    }
}