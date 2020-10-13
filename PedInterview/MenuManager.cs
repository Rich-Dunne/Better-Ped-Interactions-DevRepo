﻿using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Drawing;

namespace PedInterview
{
    class MenuManager
    {
        public static MenuPool menuPool = new MenuPool();
        private static UIMenu civMainMenu, copMainMenu;
        internal static UIMenuItem questionItem, rollWindowDown, exitVehicle, turnOffEngine, dismiss;
        private static UIMenuCheckboxItem followMe;
        private static UIMenuListScrollerItem<string> civQuestionCategories, copQuestionCategories;

        internal static UIMenu BuildCivMenu(Dictionary<string, Dictionary<XElement, List<XElement>>> civQuestionsAndAnswers)
        {
            civMainMenu = new UIMenu("Civilian Ped Interview", "");
            menuPool.Add(civMainMenu);

            civMainMenu.AddItem(civQuestionCategories = new UIMenuListScrollerItem<string>("Category", "The category of the questions", civQuestionsAndAnswers.Keys));
            populateCivMenu();
            civMainMenu.RefreshIndex();

            civMainMenu.Width = SetMenuWidth(civMainMenu);

            civMainMenu.MouseControlsEnabled = false;
            civMainMenu.AllowCameraMovement = true;

            civMainMenu.OnItemSelect += CivInteract_OnItemSelected;
            civMainMenu.OnCheckboxChange += CivInteract_OnCheckboxChanged;
            civMainMenu.OnScrollerChange += CivInteract_OnScrollerChanged;

            return civMainMenu;

            void CivInteract_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
            {
                Ped p = null;
                if (EntryPoint.focusedPed && EntryPoint.focusedPed.IsAlive)
                {
                    p = EntryPoint.focusedPed;
                }
                else
                {
                    Game.LogTrivial($"The focused ped is invalid or dead.");
                    return;
                }
                var collectedPed = EntryPoint.collectedPeds.Where(cp => cp.Ped == p).FirstOrDefault();

                if (checkboxItem == followMe)
                {
                    if (followMe.Checked)
                    {
                        collectedPed.FollowMe();
                    }
                    else
                    {
                        collectedPed.StopFollowing();
                    }
                }
            }

            void CivInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
            {
                Ped p = null;
                if (EntryPoint.focusedPed && EntryPoint.focusedPed.IsAlive)
                {
                    p = EntryPoint.focusedPed;
                }
                else
                {
                    Game.LogTrivial($"The focused ped is invalid or dead.");
                    return;
                }
                var collectedPed = EntryPoint.collectedPeds.Where(cp => cp.Ped == p).FirstOrDefault();

                if (selectedItem == dismiss)
                {
                    collectedPed.Dismiss();
                    civMainMenu.Close();
                }

                if(selectedItem == rollWindowDown && collectedPed.Ped.CurrentVehicle && collectedPed.Ped.CurrentVehicle.IsCar)
                {
                    collectedPed.RollDownWindow();
                }

                if(selectedItem == exitVehicle && collectedPed.Ped.CurrentVehicle)
                {
                    collectedPed.ExitVehicle();
                }

                if(selectedItem == turnOffEngine && p.CurrentVehicle && collectedPed.Ped.CurrentVehicle.Driver == collectedPed.Ped)
                {
                    collectedPed.TurnOffEngine();
                }

                if(civQuestionCategories.SelectedItem != "Ped Actions")
                {
                    FindMatchingQuestion(civQuestionsAndAnswers, civQuestionCategories, selectedItem);
                }
            }

            void CivInteract_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
            {
                while (civMainMenu.MenuItems.Count > 1)
                {
                    civMainMenu.RemoveItemAt(1);
                }

                populateCivMenu();
                civMainMenu.Width = SetMenuWidth(civMainMenu);
            }

            void populateCivMenu()
            {
                foreach (KeyValuePair<string, Dictionary<XElement, List<XElement>>> questionCategory in civQuestionsAndAnswers)
                {
                    if (questionCategory.Key == civQuestionCategories.SelectedItem)
                    {
                        if(civQuestionCategories.SelectedItem == "Ped Actions")
                        {
                            civMainMenu.AddItem(rollWindowDown = new UIMenuItem("Roll down window", "Rolls down the ped's window"));
                            rollWindowDown.ForeColor = Color.Gold;
                            civMainMenu.AddItem(turnOffEngine = new UIMenuItem("Turn engine off", "Makes ped turn off the engine"));
                            turnOffEngine.ForeColor = Color.Gold;
                            civMainMenu.AddItem(exitVehicle = new UIMenuItem("Exit vehicle", "Makes ped exit the vehicle"));
                            exitVehicle.ForeColor = Color.Gold;
                            civMainMenu.AddItem(followMe = new UIMenuCheckboxItem("Follow me", false, "Makes ped follow the player"));
                            followMe.ForeColor = Color.Gold;
                            civMainMenu.AddItem(dismiss = new UIMenuItem("Dismiss ped"));
                            dismiss.ForeColor = Color.Gold;
                        }
                        foreach (KeyValuePair<XElement, List<XElement>> questionResponsePair in questionCategory.Value)
                        {
                            civMainMenu.AddItem(questionItem = new UIMenuItem(questionResponsePair.Key.Attribute("question").Value));
                            var attribute = questionResponsePair.Key.Attributes().Where(x => x.Name == "type").FirstOrDefault();
                            if (attribute != null)
                            {
                                if (attribute.Value.ToLower() == "interview")
                                {
                                    questionItem.ForeColor = Color.LimeGreen;
                                }
                                else if (attribute.Value.ToLower() == "interrogation")
                                {
                                    questionItem.ForeColor = Color.IndianRed;
                                }
                                else if(attribute.Value.ToLower() == "action")
                                {
                                    questionItem.ForeColor = Color.Gold;
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static UIMenu BuildCopMenu(Dictionary<string, Dictionary<XElement, List<XElement>>> copQuestionsAndAnswers)
        {
            copMainMenu = new UIMenu("Cop Ped Interview", "");
            menuPool.Add(copMainMenu);

            copMainMenu.AddItem(copQuestionCategories = new UIMenuListScrollerItem<string>("Category", "The category of the questions", copQuestionsAndAnswers.Keys));
            populateCopMenu();
            copMainMenu.RefreshIndex();

            copMainMenu.Width = SetMenuWidth(copMainMenu);

            copMainMenu.MouseControlsEnabled = false;
            copMainMenu.AllowCameraMovement = true;

            copMainMenu.OnItemSelect += CopInteract_OnItemSelected;
            copMainMenu.OnScrollerChange += CopInteract_OnScrollerChanged;

            return copMainMenu;

            void CopInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
            {
                FindMatchingQuestion(copQuestionsAndAnswers, copQuestionCategories, selectedItem);
            }

            void CopInteract_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
            {
                while (copMainMenu.MenuItems.Count > 1)
                {
                    copMainMenu.RemoveItemAt(1);
                }

                populateCopMenu();
                copMainMenu.Width = SetMenuWidth(copMainMenu);
            }

            void populateCopMenu()
            {
                foreach (KeyValuePair<string, Dictionary<XElement, List<XElement>>> category in copQuestionsAndAnswers)
                {
                    if (category.Key == copQuestionCategories.SelectedItem)
                    {
                        foreach (KeyValuePair<XElement, List<XElement>> question in category.Value)
                        {
                            copMainMenu.AddItem(questionItem = new UIMenuItem(question.Key.Attribute("question").Value));
                        }
                    }
                }
            }
        }

        private static void FindMatchingQuestion(Dictionary<string, Dictionary<XElement, List<XElement>>> questionsAndAnswers, UIMenuListScrollerItem<string> questionCategories, UIMenuItem selectedItem)
        {
            foreach (KeyValuePair<string, Dictionary<XElement, List<XElement>>> questionCategory in questionsAndAnswers)
            {
                if (questionCategory.Key == questionCategories.SelectedItem)
                {
                    Game.LogTrivial($"questionCategory.Key: {questionCategory.Key}");
                    Game.LogTrivial($"copQuestionCategories.SelectedItem: {questionCategories.SelectedItem}");

                    foreach (KeyValuePair<XElement, List<XElement>> questionResponsePair in questionCategory.Value)
                    {
                        Game.LogTrivial($"questionResponsePair.Key.Attribute(\"question\").Value: {questionResponsePair.Key.Attribute("question").Value}");
                        Game.LogTrivial($"selectedItem.Text: {selectedItem.Text}");
                        if (questionResponsePair.Key.Attribute("question").Value == selectedItem.Text)
                        {
                            GetPedResponse(questionResponsePair);
                            break;
                        }
                    }
                }
            }

            void GetPedResponse(KeyValuePair<XElement, List<XElement>> questionResponsePair)
            {
                Random r = new Random();
                int i = r.Next(questionResponsePair.Value.Count);
                //Game.DisplaySubtitle($"Count: {kvp2.Value.Count}, Response: {kvp2.Value[i]}");
                Game.DisplaySubtitle($"{questionResponsePair.Value[i]}");
            }
        }

        private static float SetMenuWidth(UIMenu menu)
        {
            float width = 0.25f;
            foreach (var button in menu.MenuItems)
            {
                button.TextStyle.Apply();
                Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(button.Text);
                float textWidth = Rage.Native.NativeFunction.Natives.x85F061DA64ED2F67<float>(true); // _END_TEXT_COMMAND_GET_WIDTH
                float padding = 0.00390625f * 2; // typical padding used in RNUI

                var newWidth = Math.Max(textWidth + padding, UIMenu.DefaultWidth);

                // Minimum width is set to prevent the scroller from clipping the menu item name
                if (newWidth < 0.25)
                {
                    newWidth = 0.25f;
                }
                if(newWidth > width)
                {
                    width = newWidth;
                }
            }
            return width;
        }
    }
}
