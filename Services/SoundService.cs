using System.Media;

namespace NMS_PotentialDetector.Services;

/// <summary>
/// Plays application notification sounds.
/// </summary>
public class SoundService
{
    /// <summary>
    /// Plays a system beep.
    /// </summary>
    public void PlayBeep() => SystemSounds.Beep.Play();
}