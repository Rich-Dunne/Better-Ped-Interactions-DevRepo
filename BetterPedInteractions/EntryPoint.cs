using Rage;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

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
            VocalInterface.Initialize();
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

                if ((Settings.SpeechKeyModifier == Keys.None && Game.IsKeyDown(Settings.SpeechKey)) ||
                    (Game.IsKeyDownRightNow(Settings.SpeechKeyModifier) && Game.IsKeyDown(Settings.SpeechKey)) ||
                    (Settings.SpeechButtonModifier == ControllerButtons.None && Game.IsControllerButtonDown(Settings.SpeechButton)) ||
                    (Game.IsControllerButtonDownRightNow(Settings.SpeechButtonModifier) && Game.IsControllerButtonDown(Settings.SpeechButton)))
                {
                    if(GetNearbyPed() && !VocalInterface.CapturingInput)
                    {
                        VocalInterface.CaptureUserInput();
                    }
                }
                //if(GetNearbyPed() && (Game.IsKeyDown(Settings.SpeechKey) || Game.IsControllerButtonDown(Settings.SpeechButton)) && !VocalInterface.CapturingInput)
                //{
                //    VocalInterface.CaptureUserInput();
                //}

                if (Game.LocalPlayer.Character.IsOnFoot)
                {
                    if ((Settings.ModifierKey == Keys.None && Game.IsKeyDown(Settings.ToggleKey)) || 
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

            void DisplayMenuForNearbyPed()
            {
                var nearbyPed = GetNearbyPed();
                if (nearbyPed)
                {
                    CollectOrFocusNearbyPed(nearbyPed);
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

            void CloseMenuIfPlayerTooFar()
            {
                if (focusedPed != null && focusedPed.Ped && Game.LocalPlayer.Character.DistanceTo2D(focusedPed.Ped) > Settings.InteractDistance && !focusedPed.Following || !Game.LocalPlayer.Character || !Game.LocalPlayer.Character.IsAlive)
                {
                    focusedPed = null;
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

        internal static Ped GetNearbyPed()
        {
            var nearbyPed = Game.LocalPlayer.Character.GetNearbyPeds(16).Where(p => p && p != Game.LocalPlayer.Character && p.IsAlive && p.DistanceTo2D(Game.LocalPlayer.Character) <= Settings.InteractDistance).OrderBy(p => p.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
            if (!nearbyPed)
            {
                //Game.LogTrivial($"nearbyPed is null.");
                return null;
            }
            else
            {
                return nearbyPed;
            }
        }

        internal static void CollectOrFocusNearbyPed(Ped nearbyPed)
        {
            if (!nearbyPed)
            {
                Game.LogTrivial($"Nearby ped is null.");
                return;
            }

            var collectedPed = collectedPeds.FirstOrDefault(cp => cp.Ped == nearbyPed);
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

            MakeCollectedPedFacePlayer();

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
        }

        private static void MyTerminationHandler(object sender, EventArgs e)
        {
            VocalInterface.EndSRE();
            for(int i = collectedPeds.Count()-1; i >= 0; i--)
            {
                collectedPeds[i].Dismiss();
            }
        }
    }
}
