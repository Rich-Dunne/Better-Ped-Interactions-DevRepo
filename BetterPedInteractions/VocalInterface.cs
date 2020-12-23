using System;
using System.Linq;
using System.Speech.Recognition;
using Rage;

namespace BetterPedInteractions
{
    internal static class VocalInterface
    {
        private static SpeechRecognitionEngine SRE = new SpeechRecognitionEngine(new System.Globalization.CultureInfo(Settings.SpeechLanguage));
        internal static Choices Phrases = new Choices();
        internal static bool CapturingInput { get; private set; } = false;
        private static bool SpeechDetected { get; set; } = false;
        private static bool SpeechRecognized { get; set; } = false;
        private static bool PlayerTalking { get; set; } = false;
        private static string RecentlyCapturedPhrase { get; set; } = null;
        private static Ped NearbyPed { get; set; } = null;

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
            SRE.SpeechRecognitionRejected += SRE_SpeechRecognitionRejected;
        }

        internal static void CaptureUserInput()
        {
            //CapturingInput = true;
            Game.DisplayNotification($"Audio capture ~b~started");
            SRE.RecognizeAsync(RecognizeMode.Multiple);
            GameFiber.StartNew(() =>
            {
                //while (CapturingInput)
                while(true)
                {
                    ManagePlayerLipAnimation();
                    if (SpeechRecognized)
                    {
                        Game.LogTrivial($"Searching for nearby ped.");
                        var nearbyPed = Game.LocalPlayer.Character.GetNearbyPeds(16).Where(p => p && p != Game.LocalPlayer.Character && p.IsAlive && p.DistanceTo2D(Game.LocalPlayer.Character) <= Settings.InteractDistance).OrderBy(p => p.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
                        if (nearbyPed)
                        {
                            Game.LogTrivial($"Interacting with nearby ped.");
                            EntryPoint.CollectOrFocusNearbyPed(nearbyPed);
                            GameFiber.Sleep(1000);
                            ResponseManager.GetResponseFromAudio(RecentlyCapturedPhrase);
                        }
                        SpeechRecognized = false;
                    }
                    GameFiber.Yield();
                }
                //Game.LocalPlayer.Character.Tasks.ClearSecondary();
                //if(RecentlyCapturedPhrase != null)
                //{
                //    var nearbyPed = EntryPoint.GetNearbyPed();
                //    if(nearbyPed && EntryPoint.focusedPed?.Ped != nearbyPed)
                //    {
                //        EntryPoint.CollectOrFocusNearbyPed(EntryPoint.GetNearbyPed());
                //    }
                //    ResponseManager.GetResponseFromAudio(RecentlyCapturedPhrase);
                //}
            });

            void ManagePlayerLipAnimation()
            {
                if (SpeechDetected && !PlayerTalking)
                {
                    Game.LocalPlayer.Character.Tasks.PlayAnimation("mp_facial", "mic_chatter", 1.0f, AnimationFlags.SecondaryTask);
                    PlayerTalking = true;
                }
                if (!SpeechDetected && PlayerTalking)
                {
                    Game.LocalPlayer.Character.Tasks.ClearSecondary();
                    PlayerTalking = false;
                }
            }
        }

        private static void SRE_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            Game.LogTrivial($"Speech detected.");
            SpeechDetected = true;
        }


        private static void SRE_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                Game.LogTrivial($"Speech recognized: {e.Result.Text}");
                RecentlyCapturedPhrase = e.Result.Text;
                ////CapturingInput = false;
                SpeechDetected = false;
                SpeechRecognized = true;
                ////PlayerTalking = false;
                Game.DisplayNotification($"~g~Audio capture complete.");
            }
            catch (Exception ex)
            {
                Game.LogTrivial($"Something went wrong");
                Game.LogTrivial($"Exception: {ex}");
                //CapturingInput = false;
                SpeechDetected = false;
                //PlayerTalking = false;
                Game.DisplayNotification($"~r~Audio capture error");
            }
        }

        private static void SRE_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            Game.LogTrivial($"Recognition completed.");
            Game.DisplayNotification($"~y~Audio capture ended.");
            //CapturingInput = false;
        }

        private static void SRE_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Game.LogTrivial($"Could not recognize speech..");
            Game.DisplayNotification($"~r~Could not recognize speech.");
            SpeechDetected = false;
        }

        internal static void EndSRE()
        {
            SRE.RecognizeAsyncCancel();
        }
    }
}
