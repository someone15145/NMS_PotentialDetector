using NMS_PotentialDetector.Helpers;
using OpenCvSharp;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace NMS_PotentialDetector.Services;

/// <summary>
/// Выполняет поиск шаблона S-класса на изображении.
/// </summary>
public sealed class TemplateMatchingService : IDisposable
{
    /// <summary>
    /// Загруженный шаблон для сравнения.
    /// </summary>
    private readonly Mat _template;

    /// <summary>
    /// Превью шаблона для интерфейса.
    /// </summary>
    public BitmapImage TemplatePreview { get; }

    /// <summary>
    /// Инициализация сервиса и загрузка шаблона.
    /// </summary>
    public TemplateMatchingService()
    {
        const string templatePath = "templates/full_s_pattern.png";

        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Не найден шаблон templates/full_s_pattern.png");

        _template = Cv2.ImRead(templatePath, ImreadModes.Grayscale);

        using Bitmap bitmap = new(templatePath);
        TemplatePreview = bitmap.ToBitmapImage();
    }

    /// <summary>
    /// Проверяет наличие шаблона на захваченном изображении.
    /// </summary>
    public bool IsSDetected(Bitmap bitmap)
    {
        using Mat source = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
        using Mat gray = new();
        using Mat binary = new();
        using Mat resizedTemplate = new();
        using Mat result = new();

        Cv2.CvtColor(source, gray, ColorConversionCodes.BGRA2GRAY);
        Cv2.Threshold(gray, binary, 100, 255, ThresholdTypes.Binary);
        Cv2.Resize(_template, resizedTemplate, binary.Size());

        Cv2.MatchTemplate(binary, resizedTemplate, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out double maxValue);

        return maxValue > 0.90;
    }

    /// <summary>
    /// Освобождает ресурсы OpenCV.
    /// </summary>
    public void Dispose() => _template.Dispose();
}