using Rage;
using System.Windows.Forms;

namespace BetterPedInteractions.Utils
{
    internal class UserInput
    {
        internal static void HandleUserInput()
        {
            while (true)
            {
                if (AudioCaptureKeysPressed())
                {
                    VocalInterface.CapturingInput = !VocalInterface.CapturingInput;
                    if (VocalInterface.CapturingInput)
                    {
                        VocalInterface.StartRecognition();
                    }
                    else
                    {
                        VocalInterface.EndRecognition();
                    }
                }

                if (MenuKeysPressed())
                {
                    MenuManager.DisplayMenuForNearbyPed();
                }
                GameFiber.Yield();
            }
        }

        private static bool AudioCaptureKeysPressed()
        {
            if (VocalInterface.AllowVoiceCapture && SpeechKeysPressed())
            {
                return true;
            }

            return false;

            bool SpeechKeysPressed()
            {
                if ((Settings.SpeechKeyModifier == Keys.None && Game.IsKeyDown(Settings.SpeechKey)) ||
                    (Game.IsKeyDownRightNow(Settings.SpeechKeyModifier) && Game.IsKeyDown(Settings.SpeechKey)) ||
                    (Settings.SpeechButtonModifier == ControllerButtons.None && Game.IsControllerButtonDown(Settings.SpeechButton)) ||
                    (Game.IsControllerButtonDownRightNow(Settings.SpeechButtonModifier) && Game.IsControllerButtonDown(Settings.SpeechButton)))
                {
                    return true;
                }

                return false;
            }
        }

        private static bool MenuKeysPressed()
        {
            if (Game.LocalPlayer.Character.IsOnFoot && MenuKeysPressed())
            {
                return true;
            }

            return false;

            bool MenuKeysPressed()
            {
                if ((Settings.ModifierKey == Keys.None && Game.IsKeyDown(Settings.ToggleKey)) ||
                    (Game.IsKeyDownRightNow(Settings.ModifierKey) && Game.IsKeyDown(Settings.ToggleKey)) ||
                    (Settings.ModifierButton == ControllerButtons.None && Game.IsControllerButtonDown(Settings.ToggleButton)) ||
                    (Game.IsControllerButtonDownRightNow(Settings.ModifierButton) && Game.IsControllerButtonDown(Settings.ToggleButton)))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
