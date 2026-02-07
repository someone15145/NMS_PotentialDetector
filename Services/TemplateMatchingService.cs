using OpenCvSharp; // Mat, Cv2 (vision API)
using System.Drawing; // Bitmap
using System.IO; // File

namespace NMS_PotentialDetector.Services
{
    public class TemplateMatchingService : IDisposable
    {
        private Mat? _fullSTemplate;

        public TemplateMatchingService()
        {
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            const string fullSPath = "templates/full_s_pattern.png"; // Бинаризованный шаблон всего паттерна с S

            if (!File.Exists(fullSPath))
                throw new FileNotFoundException("Шаблон не найден! Создай templates/full_s_pattern.png из игры.");

            _fullSTemplate = Cv2.ImRead(fullSPath, ImreadModes.Grayscale); // Grayscale для matching
        }

        public bool IsSDetected(Bitmap capturedBitmap)
        {
            using var srcMat = OpenCvSharp.Extensions.BitmapConverter.ToMat(capturedBitmap); // Bitmap → Mat
            using var gray = new Mat();
            Cv2.CvtColor(srcMat, gray, ColorConversionCodes.BGRA2GRAY); // Grayscale

            using var binary = new Mat();
            Cv2.Threshold(gray, binary, 100, 255, ThresholdTypes.Binary); // Бинаризация, подгони threshold

            double matchScore = MatchTemplate(binary, _fullSTemplate);
            return matchScore > 0.9; // Порог — тестируй на реальных скринах
        }

        private double MatchTemplate(Mat source, Mat template)
        {
            if (template.Empty() || source.Empty()) return 0;

            // Resize шаблона к размеру source, если нужно (для устойчивости к scale)
            Cv2.Resize(template, template, source.Size(), 0, 0, InterpolationFlags.Linear);

            using var result = new Mat();
            Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed); // Нормализованный match
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal;
        }

        public void Dispose() => _fullSTemplate?.Dispose();
    }
}