using System;
using System.Diagnostics;
using NAudio.Wave; // Для WaveOut, ISampleProvider
using NAudio.Wave.SampleProviders; // Для SignalGenerator

namespace NMS_PotentialDetector.Services
{
    public class SoundService : IDisposable
    {
        private readonly WaveOut _waveOut;

        public SoundService()
        {
            _waveOut = new WaveOut(); // Инициализируем output device (стандартный speakers)
        }

        public void PlayBeep()
        {
            Debug.WriteLine("BEEEEEEEEEEEEEEEEEEEEP at max volume");

            // Генерируем простой sine wave тон (как beep: 500Hz, 500ms)
            var signal = new SignalGenerator()
            {
                Gain = 1.0, // Амплитуда (volume factor внутри wave)
                Frequency = 500, // Частота (Hz) — подгони для желаемого тона
                Type = SignalGeneratorType.Sin // Sine для чистого beep
            }.Take(TimeSpan.FromMilliseconds(500)); // Длительность

            _waveOut.Volume = 0.25f; // Max volume (0.0f - 1.0f) — фикс тихого звука
            _waveOut.Init(signal);
            _waveOut.Play();

            // Ждём окончания (blocking, но в твоём loop ok; альтернатива: event PlaybackStopped)
            while (_waveOut.PlaybackState == PlaybackState.Playing)
            {
                System.Threading.Thread.Sleep(50); // Не busy-wait, но простой для новичка
            }
        }

        public void Dispose()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
        }
    }
}