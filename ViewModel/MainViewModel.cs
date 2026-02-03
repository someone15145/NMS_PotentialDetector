using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMS_PotentialDetector.Models;
using NMS_PotentialDetector.Services;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NMS_PotentialDetector.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty] private CaptureArea _captureArea = new() { X = 100, Y = 400, Width = 100, Height = 80 }; // Пример: подгони под скрин
        [ObservableProperty] private string _status = "Готов";
        [ObservableProperty] private BitmapImage? _previewImage;
        [ObservableProperty] private bool _isMonitoring;

        private readonly ScreenCaptureService _captureService = new();
        private readonly OcrService _ocrService = new();
        private readonly SoundService _soundService = new();
        private CancellationTokenSource? _cancellationTokenSource;

        [RelayCommand]
        private void SelectArea()
        {
            // Открываем оверлей для выбора области (см. ниже)
            var selectorWindow = new AreaSelectorWindow();
            if (selectorWindow.ShowDialog() == true)
            {
                CaptureArea = selectorWindow.SelectedArea;
                Status = $"Область: {CaptureArea.X:F0},{CaptureArea.Y:F0} {CaptureArea.Width:F0}x{CaptureArea.Height:F0}";
            }
        }

        [RelayCommand]
        private async Task StartMonitoring()
        {
            if (IsMonitoring) return;
            IsMonitoring = true;
            Status = "Мониторинг...";
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(() => MonitorLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        [RelayCommand]
        private async Task StopMonitoring()
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
                    using var processed = _ocrService.Preprocess(bitmap); // Новый: для preview
                    UpdatePreview(bitmap); // Обновляем preview

                    var text = _ocrService.Recognize(bitmap);
                    Debug.WriteLine($"{DateTime.Now.Second}: {text}");
                    if (text == "S")
                    {
                        _soundService.PlayBeep();
                        Status = $"S обнаружен! ({DateTime.Now:HH:mm:ss})";
                    }

                    await Task.Delay(500, cancellationToken); // 2 FPS — баланс CPU/реакция
                }
                catch (Exception ex)
                {
                    Status = $"Ошибка: {ex.Message}";
                }
            }
        }

        private void UpdatePreview(Bitmap bitmap)
        {
            SaveForDebug(bitmap);

            var bi = bitmap.ToBitmapImage(); // Extension ниже
            Application.Current.Dispatcher.Invoke(() => PreviewImage = bi);
        }

        private void SaveForDebug(Bitmap bitmap, string suffix = "")
        {
            bitmap.Save($"debug_{DateTime.Now:yyyyMMdd_HHmmss}{suffix}.png", ImageFormat.Png);
        }

        public void Dispose() => _ocrService.Dispose();
    }

    // Extension для Bitmap -> WPF Image
    public static class BitmapExtensions
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            MemoryStream ms = new();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
    }


}