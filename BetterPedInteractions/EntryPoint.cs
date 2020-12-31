using Rage;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

[assembly: Rage.Attributes.Plugin("Better Ped Interactions", Author = "Rich", Description = "Custom dialogue menus for better ped interactions, among other features to enhance interaction experiences.", PrefersSingleInstance = true)]

namespace BetterPedInteractions
{
    [Obfuscation(Exclude = false, Feature = "-rename", ApplyToMembers = true)]
    internal class EntryPoint
    {
        internal static List<CollectedPed> CollectedPeds = new List<CollectedPed>();
        internal static CollectedPed FocusedPed = null;
        [Obfuscation(Exclude = false, Feature = "-rename")]
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
            var menuPool = MenuManager.MenuPool;
            var copMenu = MenuManager.CopMenu;
            var civMenu = MenuManager.CivMenu;

            while (true)
            {
                CloseMenuIfPlayerTooFar();
                DisableMenuItems();
                ToggleAudioCapture();
                ToggleMenu();

                MenuManager.MenuPool.ProcessMenus();

                GameFiber.Yield();
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
                if (FocusedPed == null)
                {
                    //Game.LogTrivial($"focusedPed is null.");
                    return;
                }

                if (FocusedPed.Ped && FocusedPed.Following && Game.LocalPlayer.Character.DistanceTo2D(FocusedPed.Ped) > Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in civMenu.MenuItems)
                    {
                        if (item.Text != "Follow me")
                        {
                            item.Enabled = false;
                            item.BackColor = Color.Gray;
                        }
                    }
                }
                else if (Game.LocalPlayer.Character && Game.LocalPlayer.Character.DistanceTo2D(FocusedPed.Ped) <= Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in civMenu.MenuItems)
                    {
                        item.Enabled = true;
                    }
                }

                foreach (MenuItem action in MenuManager.Actions.Where(x => x.MenuPrompt.Attribute("action").Value != "follow" && x.MenuPrompt.Attribute("action").Value != "dismiss"))
                {
                    if (FocusedPed.Ped && !FocusedPed.Ped.CurrentVehicle && action.SubCategory.Name.Value == "On Foot")
                    {
                        action.Action.Enabled = true;
                    }
                    else if (FocusedPed.Ped && FocusedPed.Ped.CurrentVehicle && action.SubCategory.Name.Value == "On Foot")
                    {
                        action.Action.Enabled = false;
                    }

                    if (FocusedPed.Ped && FocusedPed.Ped.CurrentVehicle && action.SubCategory.Name.Value == "In Vehicle")
                    {
                        action.Action.Enabled = true;
                    }
                    else if (FocusedPed.Ped && !FocusedPed.Ped.CurrentVehicle && action.SubCategory.Name.Value == "In Vehicle")
                    {
                        action.Action.Enabled = false;
                    }

                    if (!action.Action.Enabled)
                    {
                        action.Action.HighlightedBackColor = Color.White;
                    }
                    else
                    {
                        action.Action.HighlightedBackColor = action.Action.ForeColor;
                    }
                }
            }

            void ToggleAudioCapture()
            {
                // If button pressed, enable or disable persistent audio capture
                if (VocalInterface.AllowVoiceCapture &&
                    (Settings.SpeechKeyModifier == Keys.None && Game.IsKeyDown(Settings.SpeechKey)) ||
                    (Game.IsKeyDownRightNow(Settings.SpeechKeyModifier) && Game.IsKeyDown(Settings.SpeechKey)) ||
                    (Settings.SpeechButtonModifier == ControllerButtons.None && Game.IsControllerButtonDown(Settings.SpeechButton)) ||
                    (Game.IsControllerButtonDownRightNow(Settings.SpeechButtonModifier) && Game.IsControllerButtonDown(Settings.SpeechButton)))
                {
                    VocalInterface.CapturingInput = !VocalInterface.CapturingInput;
                    if (VocalInterface.CapturingInput)
                    {
                        VocalInterface.StartRecognition();
                        Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nAudio capture ~b~started~w~.");
                    }
                    else
                    {
                        VocalInterface.EndRecognition();
                        Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nAudio capture ~o~ended~w~.");
                    }
                }
            }

            void ToggleMenu()
            {
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
            }

            void DisplayMenuForNearbyPed()
            {
                var nearbyPed = GetNearbyPed();
                if (nearbyPed)
                {
                    CollectOrFocusNearbyPed(nearbyPed);
                    if (FocusedPed.Group == Settings.Group.Civilian)
                    {
                        MenuManager.PopulateMenu(civMenu, MenuManager.CivParentCategoryScroller);
                        civMenu.Visible = !civMenu.Visible;
                    }
                    else if (FocusedPed.Group == Settings.Group.Cop)
                    {
                        MenuManager.PopulateMenu(copMenu, MenuManager.CopParentCategoryScroller);
                        copMenu.Visible = !copMenu.Visible;
                    }
                }
            }

            Ped GetNearbyPed()
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
                //nearbyPed = new CollectedPed(); Preparing to use inheritence for CollectedPed
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
            VocalInterface.EndRecognition();
            for(int i = CollectedPeds.Count()-1; i >= 0; i--)
            {
                CollectedPeds[i].Dismiss();
            }
        }
    }
}
