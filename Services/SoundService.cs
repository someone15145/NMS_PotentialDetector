using System.Diagnostics;
using System.Media;

namespace NMS_PotentialDetector.Services
{
    public class SoundService
    {
        public void PlayBeep()
        {
            Debug.WriteLine("BEEEEEEEEEEEEEEEEEEEEP");
            SystemSounds.Beep.Play(); // Стандартный Windows beep — просто, без файлов
            // Альтернатива: SystemSounds.Asterisk.Play(); — другой тон
        }
    }
}