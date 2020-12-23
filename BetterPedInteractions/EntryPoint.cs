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
        internal static List<CollectedPed> CollectedPeds = new List<CollectedPed>();
        internal static CollectedPed FocusedPed = null;

        public static void Main()
        {
            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
            Settings.LoadSettings();
            XMLReader.ReadXMLs();
            VocalInterface.Initialize();
            GetAssemblyVersion();
            VocalInterface.CaptureUserInput();
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

                //if ((Settings.SpeechKeyModifier == Keys.None && Game.IsKeyDown(Settings.SpeechKey)) ||
                //    (Game.IsKeyDownRightNow(Settings.SpeechKeyModifier) && Game.IsKeyDown(Settings.SpeechKey)) ||
                //    (Settings.SpeechButtonModifier == ControllerButtons.None && Game.IsControllerButtonDown(Settings.SpeechButton)) ||
                //    (Game.IsControllerButtonDownRightNow(Settings.SpeechButtonModifier) && Game.IsControllerButtonDown(Settings.SpeechButton)))
                //{
                //    if(GetNearbyPed() && !VocalInterface.CapturingInput)
                //    {
                //        VocalInterface.CaptureUserInput();
                //    }
                //}
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
                    if (FocusedPed.Group == Settings.Group.Civilian)
                    {
                        civMenu.Visible = !civMenu.Visible;
                    }
                    else if (FocusedPed.Group == Settings.Group.Cop)
                    {
                        copMenu.Visible = !copMenu.Visible;
                    }
                }
            }

            void CloseMenuIfPlayerTooFar()
            {
                if (menuPool.IsAnyMenuOpen() && FocusedPed != null && FocusedPed.Ped && Game.LocalPlayer.Character.DistanceTo2D(FocusedPed.Ped) > Settings.InteractDistance && !FocusedPed.Following || !Game.LocalPlayer.Character || !Game.LocalPlayer.Character.IsAlive)
                {
                    FocusedPed = null;
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
                if(FocusedPed == null)
                {
                    //Game.LogTrivial($"focusedPed is null.");
                    return;
                }

                if(FocusedPed.Ped && FocusedPed.Following && Game.LocalPlayer.Character.DistanceTo2D(FocusedPed.Ped) > Settings.InteractDistance)
                {
                    foreach(UIMenuItem item in civMenu.MenuItems)
                    {
                        if(item.Text != "Follow me")
                        {
                            item.Enabled = false;
                        }
                    }
                }
                else if(Game.LocalPlayer.Character && Game.LocalPlayer.Character.DistanceTo2D(FocusedPed.Ped) <= Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in civMenu.MenuItems)
                    {
                        item.Enabled = true;
                    }
                }

                if (FocusedPed.Ped && !FocusedPed.Ped.CurrentVehicle)
                {
                    MenuManager.rollWindowDown.Enabled = false;
                    MenuManager.turnOffEngine.Enabled = false;
                    MenuManager.exitVehicle.Enabled = false;
                }
                else
                {
                    MenuManager.rollWindowDown.Enabled = true;
                    MenuManager.exitVehicle.Enabled = true;
                    if (FocusedPed?.Ped && FocusedPed.Ped.CurrentVehicle.Driver == FocusedPed.Ped)
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

            var collectedPed = CollectedPeds.FirstOrDefault(cp => cp.Ped == nearbyPed);
            if (collectedPed == null && (nearbyPed.RelationshipGroup == RelationshipGroup.Cop || nearbyPed.RelationshipGroup == "UBCOP" || nearbyPed.Model.Name == "MP_M_FREEMODE_01" || nearbyPed.Model.Name.Contains("COP")))
            {
                Game.LogTrivial($"CollectedPed is null, collecting nearby COP and assigning as focusedPed.");
                FocusedPed = CollectPed(nearbyPed, Settings.Group.Cop);
            }
            else if (collectedPed == null)
            {
                Game.LogTrivial($"CollectedPed is null, collecting nearby CIV and assigning as focusedPed.");
                FocusedPed = CollectPed(nearbyPed, Settings.Group.Civilian);
            }
            else
            {
                Game.LogTrivial($"CollectedPed was found in our collection.");
                FocusedPed = collectedPed;
            }

            MakeCollectedPedFacePlayer();

            CollectedPed CollectPed(Ped p, Settings.Group group)
            {
                var newCollectedPed = new CollectedPed(p, group);
                CollectedPeds.Add(newCollectedPed);
                Game.LogTrivial($"{p.Model.Name} collected.");

                FocusedPed = newCollectedPed;
                return newCollectedPed;
            }

            void MakeCollectedPedFacePlayer()
            {
                if (FocusedPed.Ped.IsOnFoot && !FocusedPed.Following && !FocusedPed.FleeingOrAttacking)
                {
                    FocusedPed.FacePlayer();
                    Game.LogTrivial($"Ped should be facing player.");
                }
            }
        }

        private static void MyTerminationHandler(object sender, EventArgs e)
        {
            VocalInterface.EndSRE();
            for(int i = CollectedPeds.Count()-1; i >= 0; i--)
            {
                CollectedPeds[i].Dismiss();
            }
        }
    }
}
