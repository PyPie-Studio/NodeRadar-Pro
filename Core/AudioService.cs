using System;
using System.Media;

namespace NodeRadarPro.Core
{
    public static class AudioService
    {
        public static bool Enabled { get; set; } = true;

        public static void PlayAlert(bool critical)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                if (critical)
                {
                    SystemSounds.Hand.Play();
                }
                else
                {
                    SystemSounds.Asterisk.Play();
                }
            }
            catch (InvalidOperationException ex)
            {
                // Prevent crashes if audio is unavailable
                Console.WriteLine($"AudioService: Failed to play sound alert. {ex.Message}");
            }
            catch (PlatformNotSupportedException ex)
            {
                // Prevent crashes if audio is unavailable
                Console.WriteLine($"AudioService: Failed to play sound alert. {ex.Message}");
            }
        }
    }
}
