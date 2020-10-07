using Rage;
using RAGENativeUI;

[assembly: Rage.Attributes.Plugin("Ped Interview", Author = "Rich", Description = "Dialogue menus to interact with peds.")]

namespace PedInterview
{
    internal class EntryPoint
    {
        public static void Main()
        {
            Settings.LoadSettings();
            var civQuestionsAndAnswers = XMLReader.ReadXML("PedInterview.xml");
            var copQuestionsAndAnswers = XMLReader.ReadXML("CopInterview.xml");
            var civMainMenu = MenuManager.BuildCivMenu(civQuestionsAndAnswers);
            var copMainMenu = MenuManager.BuildCopMenu(copQuestionsAndAnswers);
            LoopForUserInput(civMainMenu, copMainMenu);
        }

        private static void LoopForUserInput(UIMenu civMainMenu, UIMenu copMainMenu)
        {
            var menuPool = MenuManager.menuPool;
            while (true)
            {
                // Keyboard
                if (Settings.ModifierKey == System.Windows.Forms.Keys.None)
                {
                    if (Game.LocalPlayer.Character.IsOnFoot && Game.IsKeyDown(Settings.ToggleKey))
                    {
                        DisplayPedInteractMenu();
                    }
                }
                else if (Game.LocalPlayer.Character.IsOnFoot && Game.IsKeyDownRightNow(Settings.ModifierKey) && Game.IsKeyDown(Settings.ToggleKey))
                {
                    DisplayPedInteractMenu();
                }

                // Controller
                if (Settings.ModifierButton == ControllerButtons.None)
                {
                    if (Game.LocalPlayer.Character.IsOnFoot && Game.IsControllerButtonDown(Settings.ToggleButton))
                    {
                        DisplayPedInteractMenu();
                    }
                }
                else if (Game.LocalPlayer.Character.IsOnFoot && Game.IsControllerButtonDownRightNow(Settings.ModifierButton) && Game.IsControllerButtonDown(Settings.ToggleButton))
                {
                    DisplayPedInteractMenu();
                }

                MenuManager.menuPool.ProcessMenus();
                GameFiber.Yield();
            }

            void DisplayPedInteractMenu()
            {
                foreach (Ped p in Game.LocalPlayer.Character.GetNearbyPeds(16))
                {
                    if (Game.LocalPlayer.Character.IsOnFoot && p && p != Game.LocalPlayer.Character && p.IsAlive && p.DistanceTo(Game.LocalPlayer.Character.FrontPosition) <= 1.5f)
                    {
                        if (p.RelationshipGroup == RelationshipGroup.Cop)
                        {
                            copMainMenu.Visible = !copMainMenu.Visible;
                        }
                        else
                        {
                            // Get ped's attention
                            // Stop ped
                            p.Tasks.Clear();
                            p.BlockPermanentEvents = true;

                            // Have ped face player
                            if (p.Heading < 180)
                            {
                                p.Tasks.AchieveHeading(Game.LocalPlayer.Character.Heading + 180);
                            }
                            else
                            {
                                p.Tasks.AchieveHeading(Game.LocalPlayer.Character.Heading - 180);
                            }

                            // Collect ped
                            civMainMenu.Visible = !civMainMenu.Visible;
                        }
                        break;
                    }
                }
            }
        }
    }
}
