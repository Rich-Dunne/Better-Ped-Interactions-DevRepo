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
        internal static MenuPool menuPool = new MenuPool();
        private static UIMenu _civMenu, _copMenu;
        internal static UIMenuItem questionItem, rollWindowDown, exitVehicle, turnOffEngine;
        private static UIMenuItem _dismiss = new UIMenuItem("Dismiss ped", "Dismisses the focused ped.");
        private static UIMenuCheckboxItem _followMe = new UIMenuCheckboxItem("Follow me", false, "Makes the ped follow the player");
        private static UIMenuListScrollerItem<string> _civQuestionCategoryScroller, _copQuestionCategoryScroller, _subMenuScroller;
        private static Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> _civQuestionsAndAnswers, _copQuestionsAndAnswers;
        private static int _savedSubMenuIndex = 0;

        internal static void BuildMenu(Settings.Group group, Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> questionsAndAnswers)
        {
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

                _civMenu.OnCheckboxChange += CivInteract_OnCheckboxChanged;
                _civMenu.OnItemSelect += CivInteract_OnItemSelected;
                _civMenu.OnScrollerChange += Menu_OnScrollerChanged;

                void CivInteract_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
                {
                    var focusedPed = EntryPoint.focusedPed;

                    if (checkboxItem == _followMe)
                    {
                        if (_followMe.Checked)
                        {
                            focusedPed.FollowMe();
                        }
                        else
                        {
                            focusedPed.StopFollowing();
                        }
                    }
                }

                void CivInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
                {
                    var focusedPed = EntryPoint.focusedPed;

                    if (selectedItem == _dismiss)
                    {
                        focusedPed.Dismiss();
                        sender.Close();
                        Game.LogTrivial($"Dismiss");
                        return;
                    }

                    if (selectedItem == rollWindowDown && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.IsCar)
                    {
                        focusedPed.RollDownWindow();
                        return;
                    }

                    if (selectedItem == exitVehicle && focusedPed.Ped.CurrentVehicle)
                    {
                        focusedPed.ExitVehicle();
                        return;
                    }

                    if (selectedItem == turnOffEngine && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.Driver == focusedPed.Ped)
                    {
                        focusedPed.TurnOffEngine();
                        return;
                    }

                    if (selectedItem.GetType() != typeof(UIMenuListScrollerItem<string>) && selectedItem.GetType() != typeof(UIMenuCheckboxItem))
                    {
                        ResponseManager.FindMatchingQuestion(_civQuestionsAndAnswers, _civQuestionCategoryScroller, selectedItem);
                        return;
                    }
                }
            }

            if(group == Settings.Group.Cop)
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

                _copMenu.OnCheckboxChange += CopInteract_OnCheckboxChanged;
                _copMenu.OnItemSelect += CopInteract_OnItemSelected;
                _copMenu.OnScrollerChange += Menu_OnScrollerChanged;

                void CopInteract_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
                {
                    var focusedPed = EntryPoint.focusedPed;

                    if (checkboxItem == _followMe)
                    {
                        if (_followMe.Checked)
                        {
                            focusedPed.FollowMe();
                        }
                        else
                        {
                            focusedPed.StopFollowing();
                        }
                    }
                }

                void CopInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
                {
                    var focusedPed = EntryPoint.focusedPed;

                    if (selectedItem == _dismiss)
                    {
                        focusedPed.Dismiss();
                        sender.Close();
                        return;
                    }

                    if (selectedItem.GetType() != typeof(UIMenuListScrollerItem<string>) && selectedItem.GetType() != typeof(UIMenuCheckboxItem))
                    {
                        ResponseManager.FindMatchingQuestion(_copQuestionsAndAnswers, _copQuestionCategoryScroller, selectedItem);
                        return;
                    }
                }
            }
        }

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
                    else if (menu.TitleText.Contains("Cop") && questionCategories.SelectedItem == "Ped Actions")
                    {
                        AddPedActionsToCopMenu();
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
                menu.AddItem(_followMe);
                _followMe.ForeColor = Color.Gold;
                menu.AddItem(_dismiss);
                _dismiss.ForeColor = Color.Gold;
            }

            void AddPedActionsToCopMenu()
            {
                menu.AddItem(_followMe);
                _followMe.ForeColor = Color.Gold;
                menu.AddItem(_dismiss);
                _dismiss.ForeColor = Color.Gold;
            }

            void GenerateSubMenuScrollerItem(KeyValuePair<XAttribute, Dictionary<XElement, List<XElement>>> questionCategory)
            {
                var categoryHasSubMenus = questionCategory.Key.Parent.Elements().Any(x => x.Name == "SubMenu");
                //Game.LogTrivial($"Category has submenus: {categoryHasSubMenus}");
                var subMenus = questionCategory.Key.Parent.Elements().Where(x => x.Name == "SubMenu").ToList();
                //Game.LogTrivial($"subMenus: {subMenus.Count()}");

                var subMenuCategories = new List<string>();
                if (subMenus.Count() > 0)
                {
                    foreach (XElement subMenu in subMenus)
                    {
                        //Game.LogTrivial($"subMenu Value: {subMenu.FirstAttribute.Value}");
                        subMenuCategories.Add(subMenu.FirstAttribute.Value);
                    }
                }
                if (categoryHasSubMenus && subMenus.Count() > 0 && menu.MenuItems.Count == 1)
                {
                    menu.AddItem(_subMenuScroller = new UIMenuListScrollerItem<string>("Sub Category", "", subMenuCategories), 1);
                    _subMenuScroller.ForeColor = Color.SkyBlue;
                    if(_savedSubMenuIndex < _subMenuScroller.OptionCount)
                    {
                        _subMenuScroller.Index = _savedSubMenuIndex;
                    }
                    else
                    {
                        _subMenuScroller.Index = 0;
                    }

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
