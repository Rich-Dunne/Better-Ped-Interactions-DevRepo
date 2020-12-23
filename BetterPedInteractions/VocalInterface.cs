using System;
using System.Speech.Recognition;
using Rage;

namespace BetterPedInteractions
{
    internal static class VocalInterface
    {
        private static SpeechRecognitionEngine SRE = new SpeechRecognitionEngine(new System.Globalization.CultureInfo(Settings.SpeechLanguage));
        internal static Choices Phrases = new Choices();
        internal static bool CapturingInput { get; private set; } = false;
        internal static bool SpeechDetected { get; private set; } = false;
        internal static string RecentlyCapturedPhrase { get; private set; } = null;
        internal static void Initialize()
        {
            GrammarBuilder gb = new GrammarBuilder(Phrases);
            Game.LogTrivial($"Initializing voice recognition.");

            Grammar gr = new Grammar(gb);
            SRE.LoadGrammar(gr);
            try
            {
                SRE.SetInputToDefaultAudioDevice();
                Game.LogTrivial($"Input device found.");
            }
            catch
            {
                Game.DisplayHelp("~o~[Better Ped Interactions] ~r~ERROR~w~\nYour default input device was not detected.");
                return;
            }
            SRE.BabbleTimeout = TimeSpan.FromSeconds(1);
            SRE.InitialSilenceTimeout = TimeSpan.FromSeconds(1);
            SRE.EndSilenceTimeout = TimeSpan.FromSeconds(1);
            SRE.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1);
            SRE.SpeechDetected += SRE_SpeechDetected;
            SRE.SpeechRecognized += SRE_SpeechRecognized;
            SRE.RecognizeCompleted += SRE_RecognizeCompleted;
        }

        internal static void CaptureUserInput()
        {
            CapturingInput = true;
            Game.DisplayNotification($"Audio capture ~b~started");
            SRE.RecognizeAsync();
            GameFiber.StartNew(() =>
            {
                bool playerTalking = false;
                while (CapturingInput)
                {
                    if (SpeechDetected && !playerTalking)
                    {
                        Game.LocalPlayer.Character.Tasks.PlayAnimation("mp_facial", "mic_chatter", 1.0f, AnimationFlags.SecondaryTask);
                        playerTalking = true;
                    }
                    GameFiber.Yield();
                }
                Game.LocalPlayer.Character.Tasks.ClearSecondary();
                if(RecentlyCapturedPhrase != null)
                {
                    var nearbyPed = EntryPoint.GetNearbyPed();
                    if(nearbyPed && EntryPoint.focusedPed?.Ped != nearbyPed)
                    {
                        EntryPoint.CollectOrFocusNearbyPed(EntryPoint.GetNearbyPed());
                    }
                    ResponseManager.GetResponseFromAudio(RecentlyCapturedPhrase);
                }
            });
        }

        private static void SRE_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            SpeechDetected = true;
        }


        private static void SRE_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                Game.LogTrivial($"Result: {e.Result.Text}");
                RecentlyCapturedPhrase = e.Result.Text;
                CapturingInput = false;
                SpeechDetected = false;
                Game.DisplayNotification($"~g~Audio capture complete.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"Something went wrong");
                Game.LogTrivial($"Exception: {ex}");
                CapturingInput = false;
                SpeechDetected = false;
                Game.DisplayNotification($"~r~Audio capture error");
            }
        }

        private static void SRE_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            Game.DisplayNotification($"~y~Audio capture ended.");
            CapturingInput = false;
        }

        internal static void EndSRE()
        {
            SRE.RecognizeAsyncCancel();
        }
    }
}
