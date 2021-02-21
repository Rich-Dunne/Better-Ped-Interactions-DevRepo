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
                //Game.DisplaySubtitle($"Capturing input: {VocalInterface.CapturingInput}");
                if (Settings.EnablePTT)
                {
                    if (!VocalInterface.CapturingInput && AudioCaptureKeysPressed())
                    {
                        VocalInterface.CapturingInput = true;
                        VocalInterface.StartRecognition(System.Speech.Recognition.RecognizeMode.Single);
                    }
                    else if (VocalInterface.CapturingInput && !AudioCaptureKeysPressed())
                    {
                        VocalInterface.CapturingInput = false;
                        VocalInterface.EndRecognition();
                    }
                }
                else
                {
                    if (AudioCaptureKeysPressed())
                    {
                        VocalInterface.CapturingInput = !VocalInterface.CapturingInput;
                        if (VocalInterface.CapturingInput)
                        {
                            VocalInterface.StartRecognition(System.Speech.Recognition.RecognizeMode.Multiple);
                        }
                        else
                        {
                            VocalInterface.EndRecognition();
                        }
                    }
                }

                if (MenuKeysPressed())
                {
                    MenuManager.DisplayNearbyPedMenu();
                }
                GameFiber.Yield();
            }
        }

        private static bool AudioCaptureKeysPressed()
        {
            if (VocalInterface.AllowVoiceCapture && ((!Settings.EnablePTT && SpeechKeysPressed()) ||(Settings.EnablePTT && SpeechKeysHeldDown())))
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

            bool SpeechKeysHeldDown()
            {
                if ((Settings.SpeechKeyModifier == Keys.None && Game.IsKeyDownRightNow(Settings.SpeechKey)) ||
                    (Game.IsKeyDownRightNow(Settings.SpeechKeyModifier) && Game.IsKeyDownRightNow(Settings.SpeechKey)) ||
                    (Settings.SpeechButtonModifier == ControllerButtons.None && Game.IsControllerButtonDownRightNow(Settings.SpeechButton)) ||
                    (Game.IsControllerButtonDownRightNow(Settings.SpeechButtonModifier) && Game.IsControllerButtonDownRightNow(Settings.SpeechButton)))
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
                if ((Settings.MenuModifierKey == Keys.None && Game.IsKeyDown(Settings.MenuKey)) ||
                    (Game.IsKeyDownRightNow(Settings.MenuModifierKey) && Game.IsKeyDown(Settings.MenuKey)) ||
                    (Settings.MenuModifierButton == ControllerButtons.None && Game.IsControllerButtonDown(Settings.MenuButton)) ||
                    (Game.IsControllerButtonDownRightNow(Settings.MenuModifierButton) && Game.IsControllerButtonDown(Settings.MenuButton)))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
