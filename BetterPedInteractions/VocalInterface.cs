using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using Rage;

namespace BetterPedInteractions
{
    internal static class VocalInterface
    {
        internal static SpeechRecognitionEngine SRE = new SpeechRecognitionEngine(new System.Globalization.CultureInfo(Settings.SpeechLanguage));
        internal static Choices Phrases = new Choices();
        internal static List<string> AudioPrompts = new List<string>();
        private static SubsetMatchingMode MatchingMode = SubsetMatchingMode.OrderedSubsetContentRequired;
        internal static bool AllowVoiceCapture { get; set; } = false;
        internal static bool CapturingInput { get; set; } = false;
        private static bool SpeechDetected { get; set; } = false;
        private static bool SpeechRecognized { get; set; } = false;
        private static bool PlayerTalking { get; set; } = false;
        private static string RecentlyCapturedPhrase { get; set; } = null;

        internal static void Initialize()
        {
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
            AllowVoiceCapture = true;
            var grammarBuilder = new DictationGrammar();
            //var grammarBuilder = new GrammarBuilder(Phrases);
            //var grammar = new Grammar(grammarBuilder);
            //Game.LogTrivial($"{grammarBuilder.DebugShowPhrases}");

            Game.LogTrivial($"Initializing voice recognition.");
            SRE.LoadGrammar(grammarBuilder);

            SRE.BabbleTimeout = TimeSpan.FromSeconds(1);
            SRE.InitialSilenceTimeout = TimeSpan.FromSeconds(1);
            SRE.EndSilenceTimeout = TimeSpan.FromSeconds(1);
            SRE.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1);
            SRE.SpeechDetected += SRE_SpeechDetected;
            SRE.SpeechRecognized += SRE_SpeechRecognized;
            SRE.RecognizeCompleted += SRE_RecognizeCompleted;
            SRE.SpeechRecognitionRejected += SRE_SpeechRecognitionRejected;
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
                Game.LogTrivial($"Speech heard: {e.Result.Text}");
                RecentlyCapturedPhrase = e.Result.Text;
                // Compare phrase to everything in AudioPrompts
                var possibleMatches = new List<int>();
                Game.LogTrivial($"Searching {AudioPrompts.Count()} possible matches.");
                foreach (string phrase in AudioPrompts)
                {
                    possibleMatches.Add(DamerauLevensteinMetric.LevenshteinDistance(phrase, RecentlyCapturedPhrase));
                }

                if(possibleMatches.Count > 0)
                {
                    string match = AudioPrompts.ElementAt(possibleMatches.IndexOf(possibleMatches.Min()));
                    Game.LogTrivial($"Best match: {match}");
                    RecentlyCapturedPhrase = match;
                    SpeechDetected = false;
                    SpeechRecognized = true;
                    //Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nSpeech ~g~recognized:~w~ {match}");
                }
                else
                {
                    Game.LogTrivial($"No matching prompts found.");
                    //Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\n~r~No matching prompts found~w~.");
                }

            }
            catch (Exception ex)
            {
                Game.LogTrivial($"Something went wrong");
                Game.LogTrivial($"Exception: {ex}");
                SpeechDetected = false;
                Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nAudio capture ~r~error");
            }
        }

        private static void SRE_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            Game.LogTrivial($"Recognition completed.");
        }

        private static void SRE_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Game.LogTrivial($"Could not recognize speech.");
            Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nSpeech ~r~not recognized~w~.");
            SpeechDetected = false;
        }

        internal static void StartRecognition()
        {
            SRE.RecognizeAsync(RecognizeMode.Multiple);
            GameFiber.StartNew(() =>
            {
                while (true)
                {
                    if (CapturingInput)
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
                                ResponseManager.FindMatchingPromptFromAudio(RecentlyCapturedPhrase);
                            }
                            SpeechRecognized = false;
                        }
                    }
                    GameFiber.Yield();
                }
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

        internal static void EndRecognition()
        {
            SRE.RecognizeAsyncCancel();
        }
    }
}
