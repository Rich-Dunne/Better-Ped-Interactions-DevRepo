using Rage;
using RAGENativeUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

[assembly: Rage.Attributes.Plugin("Ped Interview", Author = "Rich", Description = "Dialogue menus to interact with peds.", PrefersSingleInstance = true)]

namespace PedInterview
{
    internal class EntryPoint
    {
        internal static List<CollectedPed> collectedPeds = new List<CollectedPed>();
        internal static Ped focusedPed = null;

        public static void Main()
        {
            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
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
                    if (focusedPed && Game.LocalPlayer.Character.DistanceTo2D(focusedPed) > Settings.InteractDistance)
                    {
                        menuPool.CloseAllMenus();
                    }
                }
            }

            void DisplayPedInteractMenu()
            {
                var nearbyPed = Game.LocalPlayer.Character.GetNearbyPeds(16).Where(p => p && p != Game.LocalPlayer.Character && p.IsAlive && p.DistanceTo2D(Game.LocalPlayer.Character) <= Settings.InteractDistance).OrderBy(p => p.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
                if(nearbyPed && nearbyPed.RelationshipGroup == RelationshipGroup.Cop)
                {
                    focusedPed = null;
                    copMainMenu.Visible = !copMainMenu.Visible;
                }
                else if(nearbyPed)
                {
                    var collectedPed = collectedPeds.Where(cp => cp.Ped == nearbyPed).FirstOrDefault();
                    if (collectedPed == null)
                    {
                        collectedPed = CollectCivPed(nearbyPed);
                    }
                    focusedPed = collectedPed.Ped;

                    if (collectedPed.Ped.IsOnFoot && !collectedPed.Following)
                    {
                        collectedPed.FacePlayer();
                    }
                    civMainMenu.Visible = !civMainMenu.Visible;
                }

                CollectedPed CollectCivPed(Ped p)
                {
                    // Collect ped
                    var collectedPed = new CollectedPed(p);
                    collectedPeds.Add(collectedPed);
                    Game.LogTrivial($"{p.Model.Name} collected.");

                    focusedPed = collectedPed.Ped;
                    return collectedPed;
                }
            }

            void DisableMenuItems()
            {
                if (focusedPed && !focusedPed.CurrentVehicle)
                {
                    MenuManager.rollWindowDown.Enabled = false;
                    MenuManager.turnOffEngine.Enabled = false;
                    MenuManager.exitVehicle.Enabled = false;
                }
                else
                {
                    MenuManager.rollWindowDown.Enabled = true;
                    MenuManager.exitVehicle.Enabled = true;
                    if(focusedPed && focusedPed.CurrentVehicle.Driver == focusedPed)
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
