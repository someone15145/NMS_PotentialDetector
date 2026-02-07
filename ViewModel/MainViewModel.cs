using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMS_PotentialDetector.Models;
using NMS_PotentialDetector.Services;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NMS_PotentialDetector.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty] private CaptureArea _captureArea = new() { X = 100, Y = 400, Width = 100, Height = 120 }; // Увеличь Height для 4 рядов
        [ObservableProperty] private string _status = "Готов";
        [ObservableProperty] private BitmapImage? _previewImage;
        [ObservableProperty] private bool _isMonitoring;

        private readonly ScreenCaptureService _captureService = new();
        private readonly TemplateMatchingService _templateService = new();
        private readonly SoundService _soundService = new();
        private CancellationTokenSource? _cancellationTokenSource;

        [RelayCommand]
        private void SelectArea() // Оставляем как есть
        {
            var selectorWindow = new AreaSelectorWindow();
            if (selectorWindow.ShowDialog() == true)
            {
                CaptureArea = selectorWindow.SelectedArea;
                Status = $"Область: {CaptureArea.X:F0},{CaptureArea.Y:F0} {CaptureArea.Width:F0}x{CaptureArea.Height:F0}";
            }
        }

        [RelayCommand]
        private async Task StartMonitoring() // Оставляем
        {
            if (IsMonitoring) return;
            IsMonitoring = true;
            Status = "Мониторинг...";
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(() => MonitorLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        [RelayCommand]
        private async Task StopMonitoring() // Оставляем
        {
            IsMonitoring = false;
            _cancellationTokenSource?.Cancel();
            Status = "Остановлен";
        }

        private async Task MonitorLoopAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var bitmap = _captureService.Capture(CaptureArea.ToRect());
                    UpdatePreview(bitmap); // Показываем оригинал для debug

                    if (_templateService.IsSDetected(bitmap))
                    {
                        _soundService.PlayBeep();
                        Status = $"S обнаружен! ({DateTime.Now:HH:mm:ss})";
                    }

                    await Task.Delay(500, cancellationToken); // 500ms — баланс: не слишком часто для CPU
                }
                catch (Exception ex)
                {
                    Status = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void UpdatePreview(Bitmap bitmap)
        {
            SaveForDebug(bitmap); // Сохраняем для анализа

            var bi = bitmap.ToBitmapImage();
            Application.Current.Dispatcher.Invoke(() => PreviewImage = bi);
        }

        private void SaveForDebug(Bitmap bitmap, string suffix = "")
        {
            if (!Directory.Exists("debug"))
                Directory.CreateDirectory("debug");
            bitmap.Save($"debug/{DateTime.Now:yyyyMMdd_HHmmss}{suffix}.png", ImageFormat.Png);
        }

        public void Dispose()
        {
            _templateService.Dispose();
        }
    }

    public static class BitmapExtensions // Оставляем extension для удобства
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            using MemoryStream ms = new();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
    }
}