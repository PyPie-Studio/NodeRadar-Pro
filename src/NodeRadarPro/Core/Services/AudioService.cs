using System;
using System.Media;

namespace NodeRadarPro.Core
{
    public static class AudioService
    {
        private static Action<bool>? _customPlayerForTesting;

        public static bool Enabled { get; set; } = true;

        internal static void SetSoundPlayerForTesting(Action<bool>? player)
        {
            _customPlayerForTesting = player;
        }

        public static void PlayAlert(bool critical)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                if (_customPlayerForTesting != null)
                {
                    _customPlayerForTesting(critical);
                    return;
                }

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
                Logger.Log(LogLevel.Error, "AudioService", $"Failed to play sound alert. {ex.Message}");
            }
            catch (PlatformNotSupportedException ex)
            {
                // Prevent crashes if audio is unavailable
                Logger.Log(LogLevel.Error, "AudioService", $"Failed to play sound alert. {ex.Message}");
            }
        }
    }
}
