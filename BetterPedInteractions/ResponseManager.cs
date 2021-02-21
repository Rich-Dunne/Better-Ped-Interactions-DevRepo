using System.Linq;
using System.Xml.Linq;
using BetterPedInteractions.Utils;
using Rage;

namespace BetterPedInteractions
{
    class ResponseManager
    {
        internal static void FindMatchingPrompt(string prompt)
        {
            MenuItem matchingPrompt = PedHandler.FocusedPed.Menu.AllMenuItems.FirstOrDefault(x => x.MenuText?.Value == prompt || x.AudioPrompts.Contains(prompt));

            if(matchingPrompt == null)
            {
                Game.LogTrivial($"No matching prompt found.");
                return;
            }
            Game.LogTrivial($"Matching prompt: {matchingPrompt.ParentCategory.Menu.TitleText}, {matchingPrompt.ParentCategory.Name}, {matchingPrompt.SubCategory?.Name}, {matchingPrompt.MenuText.Value}");

            UpdateMenuItems(matchingPrompt);

            if (matchingPrompt.Action != Settings.Actions.None)
            {
                PedHandler.FocusedPed.PerformAction(matchingPrompt);
                return;
            }

            HandlePedResponse(matchingPrompt);
        }

        private static void UpdateMenuItems(MenuItem matchingPrompt)
        {
            PedHandler.FocusedPed.Menu.IncreaseCategoryLevel(matchingPrompt);
            PedHandler.FocusedPed.Menu.EnableCategoryFromPrompt(matchingPrompt);
            PedHandler.FocusedPed.Menu.EnableDialoguePathFromPrompt(matchingPrompt);
        }

        private static void HandlePedResponse(MenuItem prompt)
        {
            XElement response = PedHandler.FocusedPed.ChooseResponse(prompt);
            if(response == null)
            {
                return;
            }
            PedHandler.FocusedPed.Menu.EnableDialoguePathFromResponse(response);
            PedHandler.FocusedPed.Menu.AddPromptToUsedMenuItems(prompt);
            if (PedHandler.FocusedPed.Group == Settings.Group.Civilian && Settings.EnableAgitation)
            {
                PedHandler.FocusedPed.AdjustAdgitationFromPrompt(prompt);
            }
            PedHandler.FocusedPed.SetHonesty(response);
            DisplayResponse(response);
            PedHandler.CollectedPeds.ForEach(x => MenuManager.PopulateMenu(x));
        }

        private static void DisplayResponse(XElement response)
        {
            if (PedHandler.FocusedPed.StoppedTalking)
            {
                Game.LogTrivial($"FocusedPed refused to talk.");
                return;
            }

            if (PedHandler.FocusedPed.Group == Settings.Group.Civilian)
            {
                Game.LogTrivial($"~y~Unidentified {PedHandler.FocusedPed.Gender}: ~w~{response.Value}");
                Game.DisplaySubtitle($"~y~Unidentified {PedHandler.FocusedPed.Gender}: ~w~{response.Value}");
            }
            else if (PedHandler.FocusedPed.Group == Settings.Group.Cop)
            {
                Game.LogTrivial($"~y~Officer: ~w~{response.Value}");
                Game.DisplaySubtitle($"~y~Officer: ~w~{response.Value}");
            }
            PedHandler.FocusedPed.PlayLipAnimation(response);
            return;
        }
    }
}
