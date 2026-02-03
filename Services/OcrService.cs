using System.Drawing; // Для Bitmap, Color, Graphics, Rectangle
using System.Drawing.Imaging; // Для ColorMatrix, ImageAttributes, GraphicsUnit
using Tesseract; // Для TesseractEngine, Pix, EngineMode

namespace NMS_PotentialDetector.Services
{
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine _engine;

        public OcrService()
        {
            _engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
            //_engine.SetVariable("tessedit_char_whitelist", "SABC");
            _engine.SetVariable("tessedit_pageseg_mode", "10"); // Single char
        }

        public string Recognize(Bitmap bitmap)
        {
            using var processed = Preprocess(bitmap);
            using var img = PixConverter.ToPix(processed);
            using var page = _engine.Process(img);
            return page.GetText().Trim().ToUpper();
        }

        public Bitmap Preprocess(Bitmap original)
        {
            // Шаг 1: Grayscale (без изменений)
            var grayscale = new Bitmap(original.Width, original.Height);
            using (var g = Graphics.FromImage(grayscale))
            {
                var colorMatrix = new ColorMatrix(new float[][]
                {
                    new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                    new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                    new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
                var attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            // Шаг 2: Threshold (снижаем до 100 для твоей картинки — светлая "S")
            var threshold = 100; // Подгони: 80-120 для NMS glow
            for (int y = 0; y < grayscale.Height; y++)
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    var pixel = grayscale.GetPixel(x, y);
                    int brightness = pixel.R;
                    grayscale.SetPixel(x, y, brightness > threshold ? Color.White : Color.Black);
                }
            }

            // Шаг 3: Invert (без изменений)
            for (int y = 0; y < grayscale.Height; y++)
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    var pixel = grayscale.GetPixel(x, y);
                    grayscale.SetPixel(x, y, Color.FromArgb(255 - pixel.R, 255 - pixel.G, 255 - pixel.B));
                }
            }

            // Новый Шаг 4: Upscale x3 для Tesseract (улучшает распознавание маленьких символов)
            const float scaleFactor = 3.0f; // x3 — баланс: x2 мало, x4 тяжело для CPU
            var resized = new Bitmap((int)(grayscale.Width * scaleFactor), (int)(grayscale.Height * scaleFactor));
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear; // Сглаживание для anti-pixelation
                g.DrawImage(grayscale, new Rectangle(0, 0, resized.Width, resized.Height), 0, 0, grayscale.Width, grayscale.Height, GraphicsUnit.Pixel);
            }
            grayscale.Dispose(); // Освобождаем старый
            return resized;
        }

        public void Dispose() => _engine?.Dispose();
    }
}