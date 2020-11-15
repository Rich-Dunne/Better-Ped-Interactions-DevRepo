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
        private static UIMenu _civMenu, _copMenu;
        internal static UIMenuItem questionItem, rollWindowDown, exitVehicle, turnOffEngine, dismiss;
        private static UIMenuCheckboxItem _followMe;
        private static UIMenuListScrollerItem<string> _civQuestionCategoryScroller, _copQuestionCategoryScroller, _subMenuScroller;
        private static Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> _civQuestionsAndAnswers, _copQuestionsAndAnswers;
        private static Dictionary<XElement, XElement> _usedQuestionResponsePairs = new Dictionary<XElement, XElement>();
        private static List<XElement> _usedResponses = new List<XElement>();

        private static Random _randomNumber = new Random();
        private static int _savedSubMenuIndex = 0;

        internal static void BuildMenu(Settings.Group group, Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> questionsAndAnswers)
        {
            //if (_civMenu == null)
            if (group == Settings.Group.Civilian)
            {
                _civQuestionsAndAnswers = new Dictionary<XAttribute, Dictionary<XElement, List<XElement>>>();
                foreach (var QAPair in questionsAndAnswers)
                    _civQuestionsAndAnswers.Add(QAPair.Key, QAPair.Value);

                _civMenu = new UIMenu("Civilian Interaction Menu", "");
                menuPool.Add(_civMenu);
                
                var categories = new List<string>();
                foreach (KeyValuePair<XAttribute, Dictionary<XElement, List<XElement>>> kvp in _civQuestionsAndAnswers)
                {
                    categories.Add(kvp.Key.Value);
                }
                _civMenu.AddItem(_civQuestionCategoryScroller = new UIMenuListScrollerItem<string>("Category", "The category of the questions", categories));

                PopulateMenu(_civMenu, _civQuestionsAndAnswers, _civQuestionCategoryScroller);
                SetMenuWidth(_civMenu);
                _civMenu.RefreshIndex();

                _civMenu.MouseControlsEnabled = false;
                _civMenu.AllowCameraMovement = true;

                _civMenu.OnItemSelect += CivInteract_OnItemSelected;
                _civMenu.OnCheckboxChange += CivInteract_OnCheckboxChanged;
                _civMenu.OnScrollerChange += Menu_OnScrollerChanged;

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

                    if (checkboxItem == _followMe)
                    {
                        if (_followMe.Checked)
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
                        _civMenu.Close();
                    }

                    if (selectedItem == rollWindowDown && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.IsCar)
                    {
                        focusedPed.RollDownWindow();
                    }

                    if (selectedItem == exitVehicle && focusedPed.Ped.CurrentVehicle)
                    {
                        focusedPed.ExitVehicle();
                    }

                    if (selectedItem == turnOffEngine && p.CurrentVehicle && focusedPed.Ped.CurrentVehicle.Driver == focusedPed.Ped)
                    {
                        focusedPed.TurnOffEngine();
                    }

                    if (_civQuestionCategoryScroller.SelectedItem != "Ped Actions")
                    {
                        FindMatchingQuestion(_civQuestionsAndAnswers, _civQuestionCategoryScroller, selectedItem);
                    }
                }
            }

            if(group == Settings.Group.Cop)
            //else if(_copMenu == null)
            {
                _copQuestionsAndAnswers = new Dictionary<XAttribute, Dictionary<XElement, List<XElement>>>();
                foreach (var QAPair in questionsAndAnswers)
                    _copQuestionsAndAnswers.Add(QAPair.Key, QAPair.Value);

                _copMenu = new UIMenu("Cop Interaction Menu", "");
                menuPool.Add(_copMenu);

                var categories = new List<string>();
                foreach (KeyValuePair<XAttribute, Dictionary<XElement, List<XElement>>> kvp in _copQuestionsAndAnswers)
                {
                    categories.Add(kvp.Key.Value);
                }
                _copMenu.AddItem(_copQuestionCategoryScroller = new UIMenuListScrollerItem<string>("Category", "The category of the questions", categories));

                PopulateMenu(_copMenu, _copQuestionsAndAnswers, _copQuestionCategoryScroller);
                SetMenuWidth(_copMenu);
                _copMenu.RefreshIndex();

                _copMenu.MouseControlsEnabled = false;
                _copMenu.AllowCameraMovement = true;

                _copMenu.OnItemSelect += CopInteract_OnItemSelected;
                _copMenu.OnScrollerChange += Menu_OnScrollerChanged;

                void CopInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
                {
                    FindMatchingQuestion(_copQuestionsAndAnswers, _copQuestionCategoryScroller, selectedItem);
                }
            }
        }

        //internal static void BuildCivMenu(Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> civQuestionsAndAnswers)
        //{
        //    if(_civMenu == null)
        //    {
        //        _civMenu = new UIMenu("Civilian Interaction Menu", "");
        //        menuPool.Add(_civMenu);
        //        var categoryStrings = new List<string>();
        //        foreach (KeyValuePair <XAttribute, Dictionary<XElement, List<XElement>>> kvp in civQuestionsAndAnswers)
        //        {
        //            categoryStrings.Add(kvp.Key.Value);
        //        }
        //        _civMenu.AddItem(_civQuestionCategoryScroller = new UIMenuListScrollerItem<string>("Category", "The category of the questions", categoryStrings));
        //    }

        //    PopulateMenu(_civMenu, civQuestionsAndAnswers, _civQuestionCategoryScroller);    
        //    SetMenuWidth(_civMenu);
        //    _civMenu.RefreshIndex();

        //    _civMenu.MouseControlsEnabled = false;
        //    _civMenu.AllowCameraMovement = true;

        //    _civMenu.OnItemSelect += CivInteract_OnItemSelected;
        //    _civMenu.OnCheckboxChange += CivInteract_OnCheckboxChanged;
        //    _civMenu.OnScrollerChange += Menu_OnScrollerChanged;
        //    _civMenu.OnScrollerChange += CivInteract_OnScrollerChanged;

        //    void CivInteract_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        //    {
        //        Ped p = null;
        //        if (EntryPoint.focusedPed.Ped && EntryPoint.focusedPed.Ped.IsAlive)
        //        {
        //            p = EntryPoint.focusedPed.Ped;
        //        }
        //        else
        //        {
        //            Game.LogTrivial($"The focused ped is invalid or dead.");
        //            return;
        //        }
        //        var collectedPed = EntryPoint.collectedPeds.Where(cp => cp.Ped == p).FirstOrDefault();

        //        if (checkboxItem == _followMe)
        //        {
        //            if (_followMe.Checked)
        //            {
        //                collectedPed.FollowMe();
        //            }
        //            else
        //            {
        //                collectedPed.StopFollowing();
        //            }
        //        }
        //    }

        //    void CivInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        //    {
        //        Ped p = null;
        //        if (EntryPoint.focusedPed.Ped && EntryPoint.focusedPed.Ped.IsAlive)
        //        {
        //            p = EntryPoint.focusedPed.Ped;
        //        }
        //        else
        //        {
        //            Game.LogTrivial($"The focused ped is invalid or dead.");
        //            return;
        //        }
        //        var focusedPed = EntryPoint.collectedPeds.Where(cp => cp.Ped == p).FirstOrDefault();

        //        if (selectedItem == dismiss)
        //        {
        //            focusedPed.Dismiss();
        //            _civMenu.Close();
        //        }

        //        if(selectedItem == rollWindowDown && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.IsCar)
        //        {
        //            focusedPed.RollDownWindow();
        //        }

        //        if(selectedItem == exitVehicle && focusedPed.Ped.CurrentVehicle)
        //        {
        //            focusedPed.ExitVehicle();
        //        }

        //        if(selectedItem == turnOffEngine && p.CurrentVehicle && focusedPed.Ped.CurrentVehicle.Driver == focusedPed.Ped)
        //        {
        //            focusedPed.TurnOffEngine();
        //        }

        //        if(_civQuestionCategoryScroller.SelectedItem != "Ped Actions")
        //        {
        //            FindMatchingQuestion(civQuestionsAndAnswers, _civQuestionCategoryScroller, selectedItem);
        //        }
        //    }

        //    void CivInteract_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
        //    {
        //        if (scroller == _civQuestionCategoryScroller)
        //        {
        //            while (_civMenu.MenuItems.Count > 1)
        //            {
        //                _civMenu.RemoveItemAt(1);
        //            }
        //        }

        //        if (scroller == _subMenuScroller)
        //        {
        //            _savedSubMenuIndex = _subMenuScroller.Index;
        //            while (_civMenu.MenuItems.Count > 2)
        //            {
        //                _civMenu.RemoveItemAt(2);
        //            }
        //        }

        //        PopulateMenu(_civMenu, civQuestionsAndAnswers, _civQuestionCategoryScroller);
        //        SetMenuWidth(_civMenu);
        //    }
        //}

        //internal static void BuildCopMenu(Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> copQuestionsAndAnswers)
        //{
        //    if(_copMenu == null)
        //    {
        //        _copMenu = new UIMenu("Cop Interaction Menu", "");
        //        menuPool.Add(_copMenu);

        //        var categoryStrings = new List<string>();
        //        foreach (KeyValuePair<XAttribute, Dictionary<XElement, List<XElement>>> kvp in copQuestionsAndAnswers)
        //        {
        //            categoryStrings.Add(kvp.Key.Value);
        //        }
        //        _copMenu.AddItem(_copQuestionCategoryScroller = new UIMenuListScrollerItem<string>("Category", "The category of the questions", categoryStrings));
        //    }

        //    PopulateMenu(_copMenu, copQuestionsAndAnswers, _copQuestionCategoryScroller);
        //    SetMenuWidth(_copMenu);
        //    _copMenu.RefreshIndex();

        //    _copMenu.MouseControlsEnabled = false;
        //    _copMenu.AllowCameraMovement = true;

        //    _copMenu.OnItemSelect += CopInteract_OnItemSelected;
        //    _copMenu.OnScrollerChange += CopInteract_OnScrollerChanged;

        //    void CopInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        //    {
        //        FindMatchingQuestion(copQuestionsAndAnswers, _copQuestionCategoryScroller, selectedItem);
        //    }

        //    void CopInteract_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
        //    {
        //        if (scroller == _copQuestionCategoryScroller)
        //        {
        //            while (sender.MenuItems.Count > 1)
        //            {
        //                sender.RemoveItemAt(1);
        //            }
        //        }

        //        if (scroller == _subMenuScroller)
        //        {
        //            _savedSubMenuIndex = _subMenuScroller.Index;
        //            while (sender.MenuItems.Count > 2)
        //            {
        //                sender.RemoveItemAt(2);
        //            }
        //        }

        //        PopulateMenu(sender, copQuestionsAndAnswers, _copQuestionCategoryScroller);
        //        SetMenuWidth(sender);
        //    }
        //}

        private static void PopulateMenu(UIMenu menu, Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> questionsAndAnswers, UIMenuListScrollerItem<string> questionCategories)
        {
            foreach (KeyValuePair<XAttribute, Dictionary<XElement, List<XElement>>> questionCategory in questionsAndAnswers)
            {
                if (questionCategory.Key.Value == questionCategories.SelectedItem)
                {
                    if (menu.TitleText.Contains("Civilian") && questionCategories.SelectedItem == "Ped Actions")
                    {
                        AddPedActionsToCivMenu();
                    }

                    GenerateSubMenuScrollerItem(questionCategory);

                    AddQuestionsToMenu(questionCategory);
                }
            }

            void AddPedActionsToCivMenu()
            {
                menu.AddItem(rollWindowDown = new UIMenuItem("Roll down window", "Rolls down the ped's window"));
                rollWindowDown.ForeColor = Color.Gold;
                menu.AddItem(turnOffEngine = new UIMenuItem("Turn engine off", "Makes ped turn off the engine"));
                turnOffEngine.ForeColor = Color.Gold;
                menu.AddItem(exitVehicle = new UIMenuItem("Exit vehicle", "Makes ped exit the vehicle"));
                exitVehicle.ForeColor = Color.Gold;
                menu.AddItem(_followMe = new UIMenuCheckboxItem("Follow me", false, "Makes ped follow the player"));
                _followMe.ForeColor = Color.Gold;
                if (EntryPoint.focusedPed != null && EntryPoint.focusedPed.Following)
                {
                    _followMe.Checked = true;
                }
                menu.AddItem(dismiss = new UIMenuItem("Dismiss ped"));
                dismiss.ForeColor = Color.Gold;
            }

            void GenerateSubMenuScrollerItem(KeyValuePair<XAttribute, Dictionary<XElement, List<XElement>>> questionCategory)
            {
                var categoryHasSubMenus = questionCategory.Key.Parent.Elements().Where(x => x.Name == "SubMenu").Any();
                //Game.LogTrivial($"Category has submenus: {categoryHasSubMenus}");
                var subMenus = questionCategory.Key.Parent.Elements().Where(x => x.Name == "SubMenu").ToList();
                //Game.LogTrivial($"subMenus: {subMenus.Count()}");

                var subMenuValues = new List<string>();
                if (subMenus.Count() > 0)
                {
                    foreach (XElement subMenu in subMenus)
                    {
                        //Game.LogTrivial($"subMenu Value: {subMenu.FirstAttribute.Value}");
                        subMenuValues.Add(subMenu.FirstAttribute.Value);
                    }
                }
                if (categoryHasSubMenus && subMenus.Count() > 0 && menu.MenuItems.Count == 1)
                {
                    menu.AddItem(_subMenuScroller = new UIMenuListScrollerItem<string>("Sub Category", "", subMenuValues), 1);
                    _subMenuScroller.ForeColor = Color.SkyBlue;
                    _subMenuScroller.Index = _savedSubMenuIndex;
                }
            }

            void AddQuestionsToMenu(KeyValuePair<XAttribute, Dictionary<XElement, List<XElement>>> questionCategory)
            {
                foreach (KeyValuePair<XElement, List<XElement>> questionResponsePair in questionCategory.Value)
                {
                    var questionHasSubMenu = questionResponsePair.Key.Attributes().Any(x => x.Name == "submenu");
                    var subMenu = questionResponsePair.Key.Attributes().Where(x => x.Name == "submenu").FirstOrDefault();
                    if (questionHasSubMenu && subMenu != null && _subMenuScroller.SelectedItem == subMenu.Value)
                    {
                        menu.AddItem(questionItem = new UIMenuItem(questionResponsePair.Key.Attribute("question").Value));
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
                            else if (attribute.Value.ToLower() == "action")
                            {
                                questionItem.ForeColor = Color.Gold;
                            }
                        }
                    }
                    else if (!questionHasSubMenu && (menu.MenuItems.Count == 1 || menu.MenuItems.Count > 1 && menu.MenuItems[1].Text != "Sub Category"))
                    {
                        menu.AddItem(questionItem = new UIMenuItem(questionResponsePair.Key.Attribute("question").Value));
                    }
                }
            }
        }

        private static void SetMenuWidth(UIMenu menu)
        {
            float width = 0.25f;

            _civQuestionCategoryScroller.TextStyle.Apply();
            Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
            if (menu.TitleText == "Civilian Interaction Menu")
            {
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(_civQuestionCategoryScroller.SelectedItem);
            }
            else if (menu.TitleText == "Cop Interaction Menu")
            {
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(_copQuestionCategoryScroller.SelectedItem);
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
                if (scrollerItemWidth > 0.16)
                {
                    width += 0.02f;
                }
            }
            menu.Width = width;
        }

        // Consider a separate class to handle question/response stuff
        private static void FindMatchingQuestion(Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> questionsAndAnswers, UIMenuListScrollerItem<string> questionCategories, UIMenuItem selectedItem)
        {
            var matchingCategory = questionsAndAnswers.Where(x => x.Key.Value == questionCategories.SelectedItem).FirstOrDefault();
            var questionResponsePair = matchingCategory.Value.Where(x => x.Key.Attribute("question").Value == selectedItem.Text).FirstOrDefault();
            var focusedPed = EntryPoint.focusedPed;

            GetPedResponse();

            void GetPedResponse()
            {
                string _responseType = null;

                XElement response = null;
                if (_responseType == null || _responseType != null && GetResponseChance() == 3)
                {
                    response = questionResponsePair.Value[GetRandomValue()];
                    if (!_usedResponses.Contains(response))
                    {
                        _usedResponses.Add(response);
                        Game.LogTrivial($"Response added: {response}");
                    }
                }
                else if (_responseType != null && GetResponseChance() < 3)
                {
                    response = questionResponsePair.Value.Where(x => x.Attributes().Count() > 0 && x.Attribute("type").Value == _responseType).FirstOrDefault();
                    if (!_usedResponses.Contains(response))
                    {
                        _usedResponses.Add(response);
                        Game.LogTrivial($"Response added: {response}");
                    }
                }
                if (!_usedQuestionResponsePairs.ContainsKey(questionResponsePair.Key))
                {
                    _usedQuestionResponsePairs.Add(questionResponsePair.Key, response);
                }

                if (response.HasAttributes)
                {
                    var responseAttributes = response.Attributes();
                    foreach (XAttribute attribute in responseAttributes)
                    {
                        Game.LogTrivial($"Response attribute: {attribute.Value}");
                        _responseType = attribute.Value;
                    }
                }

                if (focusedPed != null)
                {
                    if (_usedQuestionResponsePairs.ContainsKey(questionResponsePair.Key))
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

                    if (!focusedPed.StoppedTalking)
                    {
                        Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~{response.Value}");
                    }
                }
                else
                {
                    Game.DisplaySubtitle($"~y~Officer: ~w~{response.Value}");
                }

                void RepeatResponse()
                {
                    var repeatedResponse = _usedQuestionResponsePairs[questionResponsePair.Key].Value.ToLower();
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
                    return _randomNumber.Next(0, 4);
                }

                int GetRandomValue()
                {
                    Random r = new Random();
                    return r.Next(questionResponsePair.Value.Count);
                }
            }
        }
    
        private static void Menu_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
        {
            if (scroller == _copQuestionCategoryScroller || scroller == _civQuestionCategoryScroller)
            {
                ScrollQuestionCategory();
            }

            if (scroller == _subMenuScroller)
            {
                ScrollSubMenu();
            }

            if (sender.TitleText.Contains("Civilian"))
            {
                PopulateMenu(sender, _civQuestionsAndAnswers, _civQuestionCategoryScroller);
            }
            else if (sender.TitleText.Contains("Cop"))
            {
                PopulateMenu(sender, _copQuestionsAndAnswers, _copQuestionCategoryScroller);
            }
            SetMenuWidth(sender);

            void ScrollQuestionCategory()
            {
                while (sender.MenuItems.Count > 1)
                {
                    sender.RemoveItemAt(1);
                }
            }

            void ScrollSubMenu()
            {
                _savedSubMenuIndex = _subMenuScroller.Index;
                while (sender.MenuItems.Count > 2)
                {
                    sender.RemoveItemAt(2);
                }
            }
        }
    }
}
