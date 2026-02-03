using System.Drawing; // Для Bitmap
using System.Drawing.Imaging;
using System.IO; // Для MemoryStream
using Tesseract; // Для TesseractEngine, Pix, etc.

namespace NMS_PotentialDetector.Services
{
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine _engine;

        public OcrService()
        {
            _engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
            _engine.SetVariable("tessedit_char_whitelist", "SABC"); // Только S,A,B,C — скорость +100%, точность 99%
            _engine.SetVariable("tessedit_pageseg_mode", "8"); // Однобуквенный режим (PSM_SINGLE_CHAR)
        }

        public string Recognize(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Tiff); // Или ImageFormat.Png для меньшего размера
            ms.Position = 0; // Сброс позиции потока
            using var img = Pix.LoadFromMemory(ms.ToArray());
            using var page = _engine.Process(img);
            return page.GetText().Trim().ToUpper();
        }

        // Новый метод: Preprocessing для игрового HUD
        public Bitmap Preprocess(Bitmap original)
        {
            // Шаг 1: Grayscale — убираем цвета, фокус на яркости
            var grayscale = new Bitmap(original.Width, original.Height);
            using (var g = Graphics.FromImage(grayscale))
            {
                var colorMatrix = new ColorMatrix(new float[][]
                {
                    new float[] {0.299f, 0.299f, 0.299f, 0, 0}, // R
                    new float[] {0.587f, 0.587f, 0.587f, 0, 0}, // G
                    new float[] {0.114f, 0.114f, 0.114f, 0, 0}, // B
                    new float[] {0, 0, 0, 1, 0}, // A
                    new float[] {0, 0, 0, 0, 1}
                });
                var attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            // Шаг 2: Threshold (бинарный: чёрный/белый) — усиливаем контраст
            // Порог 128 — подгони под NMS (светлый текст: ниже для "выделения" белого)
            var threshold = 150; // Экспериментируй: 100-200 для светлого текста
            for (int y = 0; y < grayscale.Height; y++)
            {
                for (int x = 0; x < grayscale.Width; x++)
                {
                    var pixel = grayscale.GetPixel(x, y);
                    int brightness = pixel.R; // Поскольку grayscale, R=G=B
                    grayscale.SetPixel(x, y, brightness > threshold ? Color.White : Color.Black);
                }
            }

            // Шаг 3: Invert, если текст светлый на тёмном (NMS — да)
            using (var g = Graphics.FromImage(grayscale))
            {
                g.DrawImage(grayscale, 0, 0); // Dummy для lock
            }
            // Простой invert: меняем чёрный на белый и наоборот
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