using Rage;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: Rage.Attributes.Plugin("Better Ped Interactions", Author = "Rich", Description = "Custom dialogue menus for better ped interactions, among other features to enhance interaction experiences.", PrefersSingleInstance = true)]

namespace BetterPedInteractions
{
    internal class EntryPoint
    {
        internal static List<CollectedPed> collectedPeds = new List<CollectedPed>();
        internal static CollectedPed focusedPed = null;

        public static void Main()
        {
            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
            Settings.LoadSettings();
            XMLReader.ReadXMLs();
            GetAssemblyVersion();
            LoopForUserInput();

            void GetAssemblyVersion()
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Game.LogTrivial($"V{version} is ready.");
            }
        }

        private static void LoopForUserInput()
        {
            var menuPool = MenuManager.menuPool;
            var copMenu = menuPool.FirstOrDefault(m => m.TitleText == "Cop Interaction Menu");
            var civMenu = menuPool.FirstOrDefault(m => m.TitleText == "Civilian Interaction Menu");

            while (true)
            {
                CloseMenuIfPlayerTooFar();
                DisableMenuItems();

                if (Game.LocalPlayer.Character.IsOnFoot)
                {
                    if ((Settings.ModifierKey == System.Windows.Forms.Keys.None && Game.IsKeyDown(Settings.ToggleKey)) || 
                        (Game.IsKeyDownRightNow(Settings.ModifierKey) && Game.IsKeyDown(Settings.ToggleKey)) || 
                        (Settings.ModifierButton == ControllerButtons.None && Game.IsControllerButtonDown(Settings.ToggleButton)) || 
                        (Game.IsControllerButtonDownRightNow(Settings.ModifierButton) && Game.IsControllerButtonDown(Settings.ToggleButton)))
                    {
                        DisplayMenuForNearbyPed();
                    }
                }
                MenuManager.menuPool.ProcessMenus();

                GameFiber.Yield();
            }

            Ped GetNearbyPed()
            {
                var nearbyPed = Game.LocalPlayer.Character.GetNearbyPeds(16).Where(p => p && p != Game.LocalPlayer.Character && p.IsAlive && p.DistanceTo2D(Game.LocalPlayer.Character) <= Settings.InteractDistance).OrderBy(p => p.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
                if (!nearbyPed)
                {
                    Game.LogTrivial($"nearbyPed is null.");
                    return null;
                }
                else
                {
                    return nearbyPed;
                }
            }

            void DisplayMenuForNearbyPed()
            {
                var nearbyPed = GetNearbyPed();
                if (nearbyPed)
                {
                    var collectedPed = collectedPeds.FirstOrDefault(cp => cp.Ped == nearbyPed);
                    CollectOrFocusNearbyPed(nearbyPed, collectedPed);
                    MakeCollectedPedFacePlayer();
                    if (focusedPed.Group == Settings.Group.Civilian)
                    {
                        civMenu.Visible = !civMenu.Visible;
                    }
                    else if (focusedPed.Group == Settings.Group.Cop)
                    {
                        copMenu.Visible = !copMenu.Visible;
                    }
                }
            }

            void CollectOrFocusNearbyPed(Ped nearbyPed, CollectedPed collectedPed)
            {
                if (collectedPed == null && (nearbyPed.RelationshipGroup == RelationshipGroup.Cop || nearbyPed.RelationshipGroup == "UBCOP" || nearbyPed.Model.Name == "MP_M_FREEMODE_01" || nearbyPed.Model.Name.Contains("COP")))
                {
                    Game.LogTrivial($"collectedPed is null, collecting nearby COP and assigning as focusedPed.");
                    focusedPed = CollectPed(nearbyPed, Settings.Group.Cop);
                }
                else if (collectedPed == null)
                {
                    Game.LogTrivial($"collectedPed is null, collecting nearby CIV and assigning as focusedPed.");
                    focusedPed = CollectPed(nearbyPed, Settings.Group.Civilian);
                }
                else
                {
                    focusedPed = collectedPed;
                }
            }

            CollectedPed CollectPed(Ped p, Settings.Group group)
            {
                var newCollectedPed = new CollectedPed(p, group);
                collectedPeds.Add(newCollectedPed);
                Game.LogTrivial($"{p.Model.Name} collected.");

                focusedPed = newCollectedPed;
                return newCollectedPed;
            }

            void MakeCollectedPedFacePlayer()
            {
                if (focusedPed.Ped.IsOnFoot && !focusedPed.Following && !focusedPed.FleeingOrAttacking)
                {
                    focusedPed.FacePlayer();
                }
            }

            void CloseMenuIfPlayerTooFar()
            {
                if (focusedPed != null && focusedPed.Ped && Game.LocalPlayer.Character.DistanceTo2D(focusedPed.Ped) > Settings.InteractDistance && !focusedPed.Following || !Game.LocalPlayer.Character || !Game.LocalPlayer.Character.IsAlive)
                {
                    menuPool.CloseAllMenus();
                }
            }

            void DisableMenuItems()
            {
                if (!Game.LocalPlayer.Character)
                {
                    //Game.LogTrivial($"Player character is null.");
                    return;
                }
                if(focusedPed == null)
                {
                    //Game.LogTrivial($"focusedPed is null.");
                    return;
                }

                if(focusedPed.Ped && focusedPed.Following && Game.LocalPlayer.Character.DistanceTo2D(focusedPed.Ped) > Settings.InteractDistance)
                {
                    foreach(UIMenuItem item in civMenu.MenuItems)
                    {
                        if(item.Text != "Follow me")
                        {
                            item.Enabled = false;
                        }
                    }
                }
                else if(Game.LocalPlayer.Character && Game.LocalPlayer.Character.DistanceTo2D(focusedPed.Ped) <= Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in civMenu.MenuItems)
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
                    if (focusedPed?.Ped && focusedPed.Ped.CurrentVehicle.Driver == focusedPed.Ped)
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
