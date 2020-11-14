using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: Rage.Attributes.Plugin("Ped Interview", Author = "Rich", Description = "Dialogue menus to interact with peds.", PrefersSingleInstance = true)]

namespace PedInterview
{
    internal class EntryPoint
    {
        internal static List<CollectedPed> collectedPeds = new List<CollectedPed>();
        internal static CollectedPed focusedPed = null;

        public static void Main()
        {
            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
            Settings.LoadSettings();
            var civQuestionsAndAnswers = XMLReader.ReadXML("PedInterview.xml");
            var copQuestionsAndAnswers = XMLReader.ReadXML("CopInterview.xml");
            var civMainMenu = MenuManager.BuildCivMenu(civQuestionsAndAnswers);
            var copMainMenu = MenuManager.BuildCopMenu(copQuestionsAndAnswers);
            GetAssemblyVersion();
            LoopForUserInput(civMainMenu, copMainMenu);

            void GetAssemblyVersion()
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Game.LogTrivial($"V{version} is ready.");
            }
        }

        private static void LoopForUserInput(UIMenu civMainMenu, UIMenu copMainMenu)
        {
            var menuPool = MenuManager.menuPool;
            while (true)
            {
                if (Game.LocalPlayer.Character.IsOnFoot)
                {
                    // Keyboard
                    if ((Settings.ModifierKey == System.Windows.Forms.Keys.None && Game.IsKeyDown(Settings.ToggleKey)) || 
                        (Game.IsKeyDownRightNow(Settings.ModifierKey) && Game.IsKeyDown(Settings.ToggleKey)) || 
                        (Settings.ModifierButton == ControllerButtons.None && Game.IsControllerButtonDown(Settings.ToggleButton)) || 
                        (Game.IsControllerButtonDownRightNow(Settings.ModifierButton) && Game.IsControllerButtonDown(Settings.ToggleButton)))
                    {
                        DisplayPedInteractMenu();
                    }
                }
                MenuManager.menuPool.ProcessMenus();

                CloseMenuIfPlayerTooFar();
                DisableMenuItems();
                GameFiber.Yield();

                void CloseMenuIfPlayerTooFar()
                {
                    if (focusedPed != null && focusedPed.Ped && Game.LocalPlayer.Character.DistanceTo2D(focusedPed.Ped) > Settings.InteractDistance && !focusedPed.Following || !Game.LocalPlayer.Character || !Game.LocalPlayer.Character.IsAlive)
                    {
                        menuPool.CloseAllMenus();
                    }
                }
            }

            void DisplayPedInteractMenu()
            {
                var nearbyPed = Game.LocalPlayer.Character.GetNearbyPeds(16).Where(p => p && p != Game.LocalPlayer.Character && p.IsAlive && p.DistanceTo2D(Game.LocalPlayer.Character) <= Settings.InteractDistance).OrderBy(p => p.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
                if(nearbyPed && (nearbyPed.RelationshipGroup == RelationshipGroup.Cop || nearbyPed.Model.Name == "MP_M_FREEMODE_01" || nearbyPed.Model.Name.Contains("COP")))
                {
                    focusedPed = null;
                    copMainMenu.Visible = !copMainMenu.Visible;
                }
                else if(nearbyPed)
                {
                    var collectedPed = collectedPeds.Where(cp => cp.Ped == nearbyPed).FirstOrDefault();
                    if (collectedPed == null)
                    {
                        Game.LogTrivial($"collectedPed is null, collecting nearbyPed and assigning as focusedPed.");
                        focusedPed = CollectCivPed(nearbyPed);
                    }
                    else
                    {
                        focusedPed = collectedPed;
                    }

                    if (focusedPed.Ped.IsOnFoot && !focusedPed.Following && !focusedPed.FleeingOrAttacking)
                    {
                        focusedPed.FacePlayer();
                    }
                    civMainMenu.Visible = !civMainMenu.Visible;
                }

                CollectedPed CollectCivPed(Ped p)
                {
                    var collectedPed = new CollectedPed(p);
                    collectedPeds.Add(collectedPed);
                    Game.LogTrivial($"{p.Model.Name} collected.");

                    focusedPed = collectedPed;
                    return collectedPed;
                }
            }

            void DisableMenuItems()
            {
                if (!Game.LocalPlayer.Character)
                {
                    Game.LogTrivial($"Player character is null.");
                    return;
                }
                if(focusedPed == null)
                {
                    Game.LogTrivial($"focusedPed is null.");
                    return;
                }

                if(focusedPed.Ped && focusedPed.Following && Game.LocalPlayer.Character.DistanceTo2D(focusedPed.Ped) > Settings.InteractDistance)
                {
                    foreach(UIMenuItem item in civMainMenu.MenuItems)
                    {
                        if(item.Text != "Follow me")
                        {
                            item.Enabled = false;
                        }
                    }
                }
                else if(Game.LocalPlayer.Character && Game.LocalPlayer.Character.DistanceTo2D(focusedPed.Ped) <= Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in civMainMenu.MenuItems)
                    {
                        item.Enabled = true;
                    }
                }

                if (focusedPed.Ped && !focusedPed.Ped.CurrentVehicle)
                {
                    MenuManager.rollWindowDown.Enabled = false;
                    MenuManager.turnOffEngine.Enabled = false;
                    MenuManager.exitVehicle.Enabled = false;
                }
                else
                {
                    MenuManager.rollWindowDown.Enabled = true;
                    MenuManager.exitVehicle.Enabled = true;
                    if(focusedPed?.Ped && focusedPed.Ped.CurrentVehicle.Driver == focusedPed.Ped)
                    {
                        MenuManager.turnOffEngine.Enabled = true;
                    }
                    else
                    {
                        MenuManager.turnOffEngine.Enabled = false;
                    }
                }
            }
        }

        private static void MyTerminationHandler(object sender, EventArgs e)
        {
            for(int i = collectedPeds.Count()-1; i >= 0; i--)
            {
                collectedPeds[i].Dismiss();
            }
        }
    }
}
