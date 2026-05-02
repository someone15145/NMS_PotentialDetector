using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMS_PotentialDetector.Helpers;
using NMS_PotentialDetector.Models;
using NMS_PotentialDetector.Services;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NMS_PotentialDetector.ViewModels;

/// <summary>
/// Main application logic.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ScreenCaptureService _screenCaptureService = new();
    private readonly TemplateMatchingService _templateMatchingService = new();
    private readonly SoundService _soundService = new();

    private CancellationTokenSource? _monitoringTokenSource;

    [ObservableProperty]
    private CaptureArea captureArea = new() { X = 390, Y = 645, Width = 50, Height = 60 };

    [ObservableProperty]
    private string status = "Ready";

    [ObservableProperty]
    private BitmapImage? previewImage;

    [ObservableProperty]
    private bool isMonitoring;

    [ObservableProperty]
    private BitmapImage? templateImage;

    public MainViewModel()
    {
        TemplateImage = _templateMatchingService.TemplatePreview;
    }

    [RelayCommand]
    private void SelectArea()
    {
        AreaSelectorWindow selector = new();

        if (selector.ShowDialog() == true && selector.SelectedArea is not null)
        {
            CaptureArea = selector.SelectedArea;
            Status = $"Area Selected: {CaptureArea.Width:F0}x{CaptureArea.Height:F0}";
        }
    }

    [RelayCommand]
    private async Task StartMonitoring()
    {
        if (IsMonitoring)
            return;

        IsMonitoring = true;
        Status = "Monitoring...";
        _monitoringTokenSource = new();

        await Task.Run(() => MonitorAsync(_monitoringTokenSource.Token));
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        _monitoringTokenSource?.Cancel();
        IsMonitoring = false;
        Status = "Stopped";
    }

    private async Task MonitorAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var bitmap = _screenCaptureService.Capture(CaptureArea.ToRect());

                Application.Current.Dispatcher.Invoke(() =>
                {
                    PreviewImage = bitmap.ToBitmapImage();
                });

                if (_templateMatchingService.IsSDetected(bitmap))
                {
                    _soundService.PlayBeep();
                    Status = $"S-Class detected at {DateTime.Now:T}";
                }

                await Task.Delay(500, token);
            }
            catch { }
        }
    }

    public void Dispose()
    {
        _monitoringTokenSource?.Cancel();
        _templateMatchingService.Dispose();
    }
}