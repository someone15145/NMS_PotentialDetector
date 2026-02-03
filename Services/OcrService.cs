using System;
using System.Drawing; // Для Bitmap, Color, Graphics
using System.Drawing.Imaging; // Для ColorMatrix, ImageAttributes, ImageFormat
using System.IO; // Для MemoryStream (если TIFF)
using Tesseract; // Для TesseractEngine, Pix, EngineMode

namespace NMS_PotentialDetector.Services
{
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine _engine;

        public OcrService()
        {
            _engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
            _engine.SetVariable("tessedit_char_whitelist", "SABC"); // Ограничение для скорости/точности
            _engine.SetVariable("tessedit_pageseg_mode", "10"); // Изменено: PSM_SINGLE_CHAR для одиночных символов
        }

        public string Recognize(Bitmap bitmap)
        {
            using var processed = Preprocess(bitmap); // Интеграция: всегда prep перед OCR
            using var img = PixConverter.ToPix(processed); // Изменено: Прямой ToPix (эффективнее TIFF)
            using var page = _engine.Process(img);
            return page.GetText().Trim().ToUpper();
        }

        // Preprocess: Обработка для HUD (grayscale + threshold + invert)
        public Bitmap Preprocess(Bitmap original)
        {
            // Шаг 1: Grayscale — фокус на яркости (ColorMatrix для скорости)
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

            // Шаг 2: Threshold — бинаризация (подгони под NMS: светлый текст)
            var threshold = 120; // Снижено: 120 для дебаг-картинки (экспериментируй 80-150)
            for (int y = 0; y < grayscale.Height; y++)
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    var pixel = grayscale.GetPixel(x, y);
                    int brightness = pixel.R; // Grayscale: R=G=B
                    grayscale.SetPixel(x, y, brightness > threshold ? Color.White : Color.Black);
                }
            }

            // Шаг 3: Invert — для светлого текста на тёмном (стандарт для Tesseract: чёрный на белом)
            for (int y = 0; y < grayscale.Height; y++)
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    var pixel = grayscale.GetPixel(x, y);
                    grayscale.SetPixel(x, y, Color.FromArgb(255 - pixel.R, 255 - pixel.G, 255 - pixel.B));
                }
            }

            return grayscale;
        }

        public void Dispose() => _engine?.Dispose();
    }
}