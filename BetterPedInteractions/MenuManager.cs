using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Drawing;

namespace BetterPedInteractions
{
    class MenuManager
    {
        public static MenuPool menuPool = new MenuPool();
        private static UIMenu civMenu, copMenu;
        internal static UIMenuItem questionItem, rollWindowDown, exitVehicle, turnOffEngine, dismiss;
        private static UIMenuCheckboxItem followMe;
        private static UIMenuListScrollerItem<string> civQuestionCategories, copQuestionCategories;
        private static Dictionary<XElement, XElement> usedQuestionResponsePairs = new Dictionary<XElement, XElement>();
        private static List<XElement> usedResponses = new List<XElement>();
        private static string responseType = null;
        private static Random r = new Random();

        internal static void BuildCivMenu(Dictionary<string, Dictionary<XElement, List<XElement>>> civQuestionsAndAnswers)
        {
            if(civMenu == null)
            {
                civMenu = new UIMenu("Civilian Interaction Menu", "");
                menuPool.Add(civMenu);

                civMenu.AddItem(civQuestionCategories = new UIMenuListScrollerItem<string>("Category", "The category of the questions", civQuestionsAndAnswers.Keys));
            }

            populateCivMenu();
            civMenu.RefreshIndex();

            civMenu.Width = SetMenuWidth(civMenu);

            civMenu.MouseControlsEnabled = false;
            civMenu.AllowCameraMovement = true;

            civMenu.OnItemSelect += CivInteract_OnItemSelected;
            civMenu.OnCheckboxChange += CivInteract_OnCheckboxChanged;
            civMenu.OnScrollerChange += CivInteract_OnScrollerChanged;

            void CivInteract_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
            {
                Ped p = null;
                if (EntryPoint.focusedPed.Ped && EntryPoint.focusedPed.Ped.IsAlive)
                {
                    p = EntryPoint.focusedPed.Ped;
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
                if (EntryPoint.focusedPed.Ped && EntryPoint.focusedPed.Ped.IsAlive)
                {
                    p = EntryPoint.focusedPed.Ped;
                }
                else
                {
                    Game.LogTrivial($"The focused ped is invalid or dead.");
                    return;
                }
                var focusedPed = EntryPoint.collectedPeds.Where(cp => cp.Ped == p).FirstOrDefault();

                if (selectedItem == dismiss)
                {
                    focusedPed.Dismiss();
                    civMenu.Close();
                }

                if(selectedItem == rollWindowDown && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.IsCar)
                {
                    focusedPed.RollDownWindow();
                }

                if(selectedItem == exitVehicle && focusedPed.Ped.CurrentVehicle)
                {
                    focusedPed.ExitVehicle();
                }

                if(selectedItem == turnOffEngine && p.CurrentVehicle && focusedPed.Ped.CurrentVehicle.Driver == focusedPed.Ped)
                {
                    focusedPed.TurnOffEngine();
                }

                if(civQuestionCategories.SelectedItem != "Ped Actions")
                {
                    FindMatchingQuestion(civQuestionsAndAnswers, civQuestionCategories, selectedItem);
                }
            }

            void CivInteract_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
            {
                while (civMenu.MenuItems.Count > 1)
                {
                    civMenu.RemoveItemAt(1);
                }

                populateCivMenu();
                civMenu.Width = SetMenuWidth(civMenu);
            }

            void populateCivMenu()
            {
                foreach (KeyValuePair<string, Dictionary<XElement, List<XElement>>> questionCategory in civQuestionsAndAnswers)
                {
                    if (questionCategory.Key == civQuestionCategories.SelectedItem)
                    {
                        if (civQuestionCategories.SelectedItem == "Ped Actions")
                        {
                            civMenu.AddItem(rollWindowDown = new UIMenuItem("Roll down window", "Rolls down the ped's window"));
                            rollWindowDown.ForeColor = Color.Gold;
                            civMenu.AddItem(turnOffEngine = new UIMenuItem("Turn engine off", "Makes ped turn off the engine"));
                            turnOffEngine.ForeColor = Color.Gold;
                            civMenu.AddItem(exitVehicle = new UIMenuItem("Exit vehicle", "Makes ped exit the vehicle"));
                            exitVehicle.ForeColor = Color.Gold;
                            civMenu.AddItem(followMe = new UIMenuCheckboxItem("Follow me", false, "Makes ped follow the player"));
                            followMe.ForeColor = Color.Gold;
                            if (EntryPoint.focusedPed != null && EntryPoint.focusedPed.Following)
                            {
                                followMe.Checked = true;
                            }
                            civMenu.AddItem(dismiss = new UIMenuItem("Dismiss ped"));
                            dismiss.ForeColor = Color.Gold;
                        }
                        foreach (KeyValuePair<XElement, List<XElement>> questionResponsePair in questionCategory.Value)
                        {
                            civMenu.AddItem(questionItem = new UIMenuItem(questionResponsePair.Key.Attribute("question").Value));
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

        internal static void BuildCopMenu(Dictionary<string, Dictionary<XElement, List<XElement>>> copQuestionsAndAnswers)
        {
            if(copMenu == null)
            {
                copMenu = new UIMenu("Cop Interaction Menu", "");
                menuPool.Add(copMenu);

                copMenu.AddItem(copQuestionCategories = new UIMenuListScrollerItem<string>("Category", "The category of the questions", copQuestionsAndAnswers.Keys));
            }

            populateCopMenu();
            copMenu.RefreshIndex();

            copMenu.Width = SetMenuWidth(copMenu);

            copMenu.MouseControlsEnabled = false;
            copMenu.AllowCameraMovement = true;

            copMenu.OnItemSelect += CopInteract_OnItemSelected;
            copMenu.OnScrollerChange += CopInteract_OnScrollerChanged;

            void CopInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
            {
                FindMatchingQuestion(copQuestionsAndAnswers, copQuestionCategories, selectedItem);
            }

            void CopInteract_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
            {
                while (copMenu.MenuItems.Count > 1)
                {
                    copMenu.RemoveItemAt(1);
                }

                populateCopMenu();
                copMenu.Width = SetMenuWidth(copMenu);
            }

            void populateCopMenu()
            {
                foreach (KeyValuePair<string, Dictionary<XElement, List<XElement>>> category in copQuestionsAndAnswers)
                {
                    if (category.Key == copQuestionCategories.SelectedItem)
                    {
                        foreach (KeyValuePair<XElement, List<XElement>> question in category.Value)
                        {
                            copMenu.AddItem(questionItem = new UIMenuItem(question.Key.Attribute("question").Value));
                        }
                    }
                }
            }
        }

        // Consider a separate class to handle question/response stuff
        private static void FindMatchingQuestion(Dictionary<string, Dictionary<XElement, List<XElement>>> questionsAndAnswers, UIMenuListScrollerItem<string> questionCategories, UIMenuItem selectedItem)
        {
            var matchingCategory = questionsAndAnswers.Where(x => x.Key == questionCategories.SelectedItem).FirstOrDefault();
            var questionResponsePair = matchingCategory.Value.Where(x => x.Key.Attribute("question").Value == selectedItem.Text).FirstOrDefault();
            GetPedResponse();

            void GetPedResponse()
            {
                var focusedPed = EntryPoint.focusedPed;
                if (usedQuestionResponsePairs.ContainsKey(questionResponsePair.Key))
                {
                    if (!focusedPed.StoppedTalking)
                    {
                        RepeatResponse();
                    }
                    if (Settings.EnableAgitation)
                    {
                        focusedPed.IncreaseAgitation(true);
                    }
                    return;
                }

                // Get question type, adjust Agitation
                if (Settings.EnableAgitation)
                {
                    if (questionResponsePair.Key.Attributes().Any(x => x.Name == "type"))
                    {
                        if (questionResponsePair.Key.Attribute("type").Value == "interview")
                        {
                            focusedPed.DecreaseAgitation();
                        }
                        else if (questionResponsePair.Key.Attribute("type").Value == "interrogation")
                        {
                            focusedPed.IncreaseAgitation();
                        }
                    }
                }

                XElement response = null;
                if(responseType == null || responseType != null && GetResponseChance() == 3)
                {
                    response = questionResponsePair.Value[GetRandomValue()];
                    usedResponses.Add(response);
                    Game.LogTrivial($"Response added: {response}");
                }
                else if (responseType != null && GetResponseChance() < 3)
                {
                    response = questionResponsePair.Value.Where(x => x.Attributes().Count() > 0 && x.Attribute("type").Value == responseType).FirstOrDefault();
                    usedResponses.Add(response);
                    Game.LogTrivial($"Response added: {response}");
                }
                usedQuestionResponsePairs.Add(questionResponsePair.Key, response);
                if (!focusedPed.StoppedTalking)
                {
                    Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~{response}");
                }

                if (response.HasAttributes)
                {
                    var responseAttributes = response.Attributes();
                    foreach (XAttribute attribute in responseAttributes)
                    {
                        Game.LogTrivial($"Response attribute: {attribute.Value}");
                        responseType = attribute.Value;
                    }
                }

                void RepeatResponse()
                {
                    var repeatedResponse = usedQuestionResponsePairs[questionResponsePair.Key].Value.ToLower();
                    Game.LogTrivial($"This response was already used");

                    if (MathHelper.GetChance(2))
                    {
                        Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~I already told you, {repeatedResponse}");
                    }
                    else
                    {
                        Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~Did I s-s-stutter?");
                    }
                }

                int GetResponseChance()
                {
                    return r.Next(0, 4);
                }

                int GetRandomValue()
                {
                    Random r = new Random();
                    return r.Next(questionResponsePair.Value.Count);
                }
            }
        }

        private static float SetMenuWidth(UIMenu menu)
        {
            float width = 0.25f;

            civQuestionCategories.TextStyle.Apply();
            Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
            if(menu.TitleText == "Civilian Interaction Menu")
            {
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(civQuestionCategories.SelectedItem);
            }
            else if (menu.TitleText == "Cop Interaction Menu")
            {
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(copQuestionCategories.SelectedItem);
            }
            float scrollerTextWidth = Rage.Native.NativeFunction.Natives.x85F061DA64ED2F67<float>(true); // _END_TEXT_COMMAND_GET_WIDTH
            float padding = 0.00390625f * 2; // typical padding used in RNUI

            var scrollerItemWidth = scrollerTextWidth + padding;
            //Game.LogTrivial($"Scroller item width: {scrollerItemWidth}");

            foreach (var menuItem in menu.MenuItems)
            {
                menuItem.TextStyle.Apply();
                Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(menuItem.Text);
                float textWidth = Rage.Native.NativeFunction.Natives.x85F061DA64ED2F67<float>(true); // _END_TEXT_COMMAND_GET_WIDTH
                //float padding = 0.00390625f * 2; // typical padding used in RNUI

                var newWidth = Math.Max(textWidth + padding, UIMenu.DefaultWidth);
                //Game.LogTrivial($"Menu item width: {newWidth}");
                
                // Minimum width is set to prevent the scroller from clipping the menu item name
                if (newWidth < 0.25)
                {
                    newWidth = 0.25f;
                }
                if (newWidth > width)
                {
                    width = newWidth;
                }
                if(scrollerItemWidth > 0.15)
                {
                    width = scrollerItemWidth + 0.08f;
                }
            }
            return width;
        }
    }
}
