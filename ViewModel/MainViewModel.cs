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
        [ObservableProperty] private CaptureArea _captureArea = new() { X = 390, Y = 645, Width = 50, Height = 60 }; // Положение индикатора
        [ObservableProperty] private string _status = "Готов";
        [ObservableProperty] private BitmapImage? _previewImage;
        [ObservableProperty] private bool _isMonitoring; 
        [ObservableProperty] private bool _alwaysShowPreview = true;
        [ObservableProperty] private BitmapImage? _templatePreviewImage; 
        [ObservableProperty] private double _matchThreshold = 0.9;
        [ObservableProperty] private double _currentMatchScore;   // 0.0 .. 1.0

        private readonly ScreenCaptureService _captureService = new();
        private readonly TemplateMatchingService _templateService = new();
        private readonly SoundService _soundService = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationTokenSource? _previewCts;
        public MainViewModel()
        {
            LoadTemplatePreview();
            if (AlwaysShowPreview)
                StartPreviewLoop();
        }

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
            while (!cancellationToken.IsCancellationRequested || AlwaysShowPreview)
            {
                try
                {
                    using var bitmap = _captureService.Capture(CaptureArea.ToRect());
                    UpdatePreview(bitmap); // Показываем оригинал для debug

                    CurrentMatchScore = _templateService.GetMatchScore(bitmap);

                    if (cancellationToken.IsCancellationRequested) continue;

                    bool isDetected = _templateService.IsSDetected(bitmap, MatchThreshold);
                    if (IsMonitoring && isDetected)
                    {
                        _soundService.PlayBeep();
                        Status = $"S обнаружен! ({DateTime.Now:HH:mm:ss}) Соответствие: {CurrentMatchScore:P1}";
                    }
                    else if (IsMonitoring)
                    {
                        Status = $"Мониторинг... Текущее соответствие: {CurrentMatchScore:P1}";
                    }

                    SaveForDebug(bitmap, isDetected); // Сохраняем для анализа

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
            var bi = bitmap.ToBitmapImage();
            Application.Current.Dispatcher.Invoke(() => PreviewImage = bi);
        }

        private void SaveForDebug(Bitmap bitmap, bool isDetected)
        {
            string folder = isDetected ? "_detected" : "_noDetected";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            bitmap.Save($"{folder}/{DateTime.Now:yyyyMMdd_HHmmss}.png", ImageFormat.Png);
        }
        private void LoadTemplatePreview()
        {
            const string fullSPath = "templates/full_s_pattern.png";
            if (File.Exists(fullSPath))
            {
                using var bitmap = new Bitmap(fullSPath);
                TemplatePreviewImage = bitmap.ToBitmapImage();
            }
        }

        private async Task PreviewLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var bitmap = _captureService.Capture(CaptureArea.ToRect());
                    UpdatePreview(bitmap);
                    await Task.Delay(500, cancellationToken);
                }
                catch { }
            }
        }

        private void StartPreviewLoop()
        {
            if (_previewCts != null) return;
            _previewCts = new CancellationTokenSource();
            _ = Task.Run(() => PreviewLoopAsync(_previewCts.Token));
        }

        private void StopPreviewLoop()
        {
            _previewCts?.Cancel();
            _previewCts = null;
        }
        partial void OnAlwaysShowPreviewChanged(bool value)
        {
            if (value && !IsMonitoring)
                StartPreviewLoop();
            else if (!value && !IsMonitoring)
                StopPreviewLoop();
        }

        partial void OnIsMonitoringChanged(bool oldValue, bool newValue)
        {
            if (newValue)
                StopPreviewLoop();           // чтобы не было двух потоков превью
            else if (AlwaysShowPreview)
                StartPreviewLoop();
        }

        public void Dispose()
        {
            _templateService.Dispose(); 
            StopPreviewLoop();
        }
    }

    public static class BitmapExtensions // Extension для удобства
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad; // Фикс: Загружает data сразу, перед dispose stream
            using MemoryStream ms = new();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze(); // Для thread-safety
            return bi;
        }
    }
}