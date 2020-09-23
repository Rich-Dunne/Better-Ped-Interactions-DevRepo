using Rage;
using RAGENativeUI;

[assembly: Rage.Attributes.Plugin("Ped Interview", Author = "Rich", Description = "Dialogue menus to interact with peds.")]

namespace PedInterview
{
    internal class EntryPoint
    {
        public static void Main()
        {
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
                    if (Game.LocalPlayer.Character.IsOnFoot && Game.IsKeyDown(Settings.ToggleKey) && !menuPool.IsAnyMenuOpen())
                    {
                        DisplayPedInteractMenu();
                    }
                }
                else if (Game.LocalPlayer.Character.IsOnFoot && Game.IsKeyDownRightNow(Settings.ModifierKey) && Game.IsKeyDown(Settings.ToggleKey) && !menuPool.IsAnyMenuOpen())
                {
                    DisplayPedInteractMenu();
                }

                // Controller
                if (Settings.ModifierButton == ControllerButtons.None)
                {
                    if (Game.LocalPlayer.Character.IsOnFoot && Game.IsControllerButtonDown(Settings.ToggleButton) && !menuPool.IsAnyMenuOpen())
                    {
                        DisplayPedInteractMenu();
                    }
                }
                else if (Game.LocalPlayer.Character.IsOnFoot && Game.IsControllerButtonDownRightNow(Settings.ModifierButton) && Game.IsControllerButtonDown(Settings.ToggleButton) && !menuPool.IsAnyMenuOpen())
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
                    if (Game.LocalPlayer.Character.IsOnFoot && p != Game.LocalPlayer.Character && p.DistanceTo(Game.LocalPlayer.Character.FrontPosition) <= 2f)
                    {
                        if (p.RelationshipGroup == RelationshipGroup.Cop)
                        {
                            Game.LogTrivial($"Display cop menu");
                            copMainMenu.Visible = !copMainMenu.Visible;
                        }
                        else
                        {
                            Game.LogTrivial($"Display civilian menu");
                            civMainMenu.Visible = !civMainMenu.Visible;
                        }
                        break;
                    }
                }
            }
        }
    }
}
