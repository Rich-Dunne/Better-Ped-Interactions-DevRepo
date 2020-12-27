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
        internal static MenuPool MenuPool = new MenuPool();
        private static UIMenu _civMenu, _copMenu;
        private static UIMenuItem _DialogueItem, _RollWindowDown, _ExitVehicle, _TurnOffEngine;
        internal static List<MenuItem> Actions = new List<MenuItem>();
        private static UIMenuItem _Dismiss = new UIMenuItem("Dismiss ped", "Dismisses the focused ped.");
        private static UIMenuCheckboxItem _FollowMe = new UIMenuCheckboxItem("Follow me", false, "Makes the ped follow the player");
        internal static UIMenuListScrollerItem<string> CivParentCategoryScroller, CopParentCategoryScroller;
        private static List<string> _subCategoryNames = new List<string>();
        private static UIMenuListScrollerItem<string> _subMenuScroller = new UIMenuListScrollerItem<string>("Sub Category","", _subCategoryNames);
        private static List<ParentCategory> _parentCategories;
        private static int _savedSubMenuIndex = 0;

        internal static void BuildMenus(List<ParentCategory> menuCategoryObjects)
        {
            _parentCategories = menuCategoryObjects;
            ResponseManager.AssignInteractions(_parentCategories);

            // Currently, scrolling a menu removes current menu items, then re-adds ones with matching category properties
            BuildCivMenu();
            BuildCopMenu();

            void BuildCivMenu()
            {
                _civMenu = new UIMenu("Civilian Interaction Menu", "");
                MenuPool.Add(_civMenu);

                var civCategories = _parentCategories.Where(x => x.Menu == Settings.Group.Civilian).Select(x => x.Name).Distinct().ToList();
                var civCategoriesList = civCategories.Select(x => x.Value).ToList();
                //Game.LogTrivial($"Civ Categories: {civCategories.Count()}");
                _civMenu.AddItem(CivParentCategoryScroller = new UIMenuListScrollerItem<string>("Category", "The category of the questions", civCategoriesList));

                PopulateMenu(_civMenu, CivParentCategoryScroller);
                _civMenu.RefreshIndex();

                _civMenu.MouseControlsEnabled = false;
                _civMenu.AllowCameraMovement = true;

                _civMenu.OnCheckboxChange += CivInteract_OnCheckboxChanged;
                _civMenu.OnItemSelect += CivInteract_OnItemSelected;
                _civMenu.OnScrollerChange += Menu_OnScrollerChanged;

                void CivInteract_OnCheckboxChanged(UIMenu menu, UIMenuCheckboxItem checkboxItem, bool @checked)
                {
                    Game.LogTrivial($"Checkbox item toggled");
                    var focusedPed = EntryPoint.FocusedPed;

                    if (checkboxItem == _FollowMe)
                    {
                        if (_FollowMe.Checked)
                        {
                            focusedPed.FollowMe();
                        }
                        else
                        {
                            focusedPed.StopFollowing();
                        }
                    }
                }

                void CivInteract_OnItemSelected(UIMenu menu, UIMenuItem selectedItem, int index)
                {
                    var focusedPed = EntryPoint.FocusedPed;

                    if (selectedItem == _Dismiss)
                    {
                        focusedPed.Dismiss();
                        menu.Close();
                        Game.LogTrivial($"Dismiss");
                        return;
                    }

                    if (selectedItem == _RollWindowDown && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.IsCar)
                    {
                        focusedPed.RollDownWindow();
                        return;
                    }

                    if (selectedItem == _ExitVehicle && focusedPed.Ped.CurrentVehicle)
                    {
                        focusedPed.ExitVehicle();
                        return;
                    }

                    if (selectedItem == _TurnOffEngine && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.Driver == focusedPed.Ped)
                    {
                        focusedPed.TurnOffEngine();
                        return;
                    }

                    if (selectedItem.GetType() != typeof(UIMenuListScrollerItem<string>) && selectedItem.GetType() != typeof(UIMenuCheckboxItem))
                    {
                        ResponseManager.FindMatchingPromptFromMenu(menu, selectedItem);
                        PopulateMenu(menu, CivParentCategoryScroller);
                        return;
                    }
                }
            }

            void BuildCopMenu()
            {
                _copMenu = new UIMenu("Cop Interaction Menu", "");
                MenuPool.Add(_copMenu);

                var copCategories = _parentCategories.Where(x => x.Menu == Settings.Group.Cop).Select(x => x.Name).Distinct().ToList();
                //var copSubCategories = _menuCategoryObjects.SelectMany(x => x.SubCategories.Where(y => y.ParentCategory.Menu == Settings.Group.Cop));
                var civCategoriesList = copCategories.Select(x => x.Value).ToList();
                _copMenu.AddItem(CopParentCategoryScroller = new UIMenuListScrollerItem<string>("Category", "The category of the questions", civCategoriesList));

                PopulateMenu(_copMenu, CopParentCategoryScroller);
                SetMenuWidth(_copMenu);
                _copMenu.RefreshIndex();

                _copMenu.MouseControlsEnabled = false;
                _copMenu.AllowCameraMovement = true;

                _copMenu.OnCheckboxChange += CopInteract_OnCheckboxChanged;
                _copMenu.OnItemSelect += CopInteract_OnItemSelected;
                _copMenu.OnScrollerChange += Menu_OnScrollerChanged;

                void CopInteract_OnCheckboxChanged(UIMenu menu, UIMenuCheckboxItem checkboxItem, bool @checked)
                {
                    var focusedPed = EntryPoint.FocusedPed;

                    if (checkboxItem == _FollowMe)
                    {
                        if (_FollowMe.Checked)
                        {
                            focusedPed.FollowMe();
                        }
                        else
                        {
                            focusedPed.StopFollowing();
                        }
                    }
                }

                void CopInteract_OnItemSelected(UIMenu menu, UIMenuItem selectedItem, int index)
                {
                    var focusedPed = EntryPoint.FocusedPed;

                    if (selectedItem == _Dismiss)
                    {
                        focusedPed.Dismiss();
                        menu.Close();
                        return;
                    }

                    if (selectedItem.GetType() != typeof(UIMenuListScrollerItem<string>) && selectedItem.GetType() != typeof(UIMenuCheckboxItem))
                    {
                        ResponseManager.FindMatchingPromptFromMenu(menu, selectedItem);
                        PopulateMenu(menu, CopParentCategoryScroller);
                        return;
                    }
                }
            }
        }

        internal static void PopulateMenu(UIMenu menu, UIMenuListScrollerItem<string> categoryScroller, int removeItemIndex = 1)
        {
            // First I need to clear the menu because this method will be used for refreshing menu items
            while (menu.MenuItems.Count > removeItemIndex)
            {
                menu.RemoveItemAt(removeItemIndex);
            }

            // Next, I need to get the current parent category
            var parentCategory = _parentCategories.FirstOrDefault(x => x.Name.Value == categoryScroller.SelectedItem);
            if (parentCategory == null)
            {
                Game.LogTrivial($"Parent category is null.");
                return;
            }
            //Game.LogTrivial($"Parent category: {parentCategory.Name.Value}");
            // Then, I need to check if the parent category has sub categories
            // If there are any sub categories, I need to add their names to the submenu scroller menu item
            var subCategories = parentCategory.SubCategories;
            if(subCategories.Count > 0 && menu.MenuItems.Count == 1)
            {
                CreateSubCategoryScroller();
            }

            // Next, I need to populate the menu with prompts which either match the currently selected sub category, or the currently selected parent category
            if (categoryScroller.SelectedItem == "Ped Actions")
            {
                AddPedActionsToMenu();
            }
            else
            {
                IEnumerable<MenuItem> promptsMatchingReadLevel;

                if (menu.MenuItems.Count > 1 && subCategories.Count > 0 && _subMenuScroller.OptionCount > 0)
                {
                    //Game.LogTrivial($"Subscroller selected item: {_subMenuScroller.SelectedItem}");
                    var matchingSubCategory = subCategories.FirstOrDefault(x => _subMenuScroller.SelectedItem == x.Name.Value);
                    //Game.LogTrivial($"Possible prompts: {promptsMatchingReadLevel.Count()}");
                    if (matchingSubCategory == null)
                    {
                        Game.LogTrivial($"Matching sub category is null.");
                        return;
                    }
                    promptsMatchingReadLevel = matchingSubCategory.MenuItems.Where(x => x.Level <= matchingSubCategory.Level);
                }
                else
                {
                    promptsMatchingReadLevel = parentCategory.MenuItems.Where(x => x.Level <= parentCategory.Level);
                }

                foreach (MenuItem menuItem in promptsMatchingReadLevel.Where(x => x.Enabled && x.MenuPrompt != null))
                {
                    if (EntryPoint.FocusedPed == null || (EntryPoint.FocusedPed != null && !EntryPoint.FocusedPed.UsedQuestions.ContainsKey(menuItem.MenuPrompt)))
                    {
                        //Game.LogTrivial($"[MENU ADD] Menu: {menuCategoryObject.Menu}, Category: {menuCategoryObject.Name.Value}, Prompt: {menuItem.MenuPrompt.Value}");
                        menu.AddItem(_DialogueItem = new UIMenuItem(menuItem.MenuPrompt.Value));
                        //_DialogueItem.HighlightedBackColor = _DialogueItem.ForeColor;
                        AssignFontColorFromAttribute(menuItem, _DialogueItem);
                    }
                }
            }
            

            SetMenuWidth(menu);

            void AddPedActionsToMenu()
            {
                IEnumerable<MenuItem> matchingPrompts;
                if (menu.MenuItems.Count > 1 && subCategories.Count > 0 && _subMenuScroller.OptionCount > 0)
                {
                    //Game.LogTrivial($"Subscroller selected item: {_subMenuScroller.SelectedItem}");
                    var matchingSubCategory = subCategories.FirstOrDefault(x => _subMenuScroller.SelectedItem == x.Name.Value);
                    //Game.LogTrivial($"Possible prompts: {promptsMatchingReadLevel.Count()}");
                    if (matchingSubCategory == null)
                    {
                        Game.LogTrivial($"Matching sub category is null.");
                        return;
                    }
                    matchingPrompts = matchingSubCategory.MenuItems.Where(x => x.MenuPrompt.Attribute("action") != null);
                }
                else
                {
                    matchingPrompts = parentCategory.MenuItems.Where(x => x.MenuPrompt.Attribute("action") != null);
                }

                foreach (MenuItem menuItem in matchingPrompts)
                {
                    if(menuItem.MenuPrompt.Attribute("action").Value == "follow")
                    {
                        menu.AddItem(_FollowMe);
                        menuItem.Action = _FollowMe;
                        Actions.Add(menuItem);
                    }
                    if(menuItem.MenuPrompt.Attribute("action").Value == "dismiss")
                    {
                        menu.AddItem(_Dismiss);
                        menuItem.Action = _Dismiss;
                        Actions.Add(menuItem);
                    }
                    if (menuItem.MenuPrompt.Attribute("action").Value == "rollWindowDown")
                    {
                        _RollWindowDown = new UIMenuCheckboxItem(menuItem.MenuPrompt.Value, false, "Rolls down the ped's window");
                        menu.AddItem(_RollWindowDown);
                        menuItem.Action = _RollWindowDown;
                        Actions.Add(menuItem);
                    }
                    if(menuItem.MenuPrompt.Attribute("action").Value == "turnOffEngine")
                    {
                        _TurnOffEngine = new UIMenuCheckboxItem(menuItem.MenuPrompt.Value, false, "Makes ped turn off the engine");
                        menu.AddItem(_TurnOffEngine);
                        menuItem.Action = _TurnOffEngine;
                        Actions.Add(menuItem);
                    }
                    if (menuItem.MenuPrompt.Attribute("action").Value == "exitVehicle")
                    {
                        _ExitVehicle = new UIMenuCheckboxItem(menuItem.MenuPrompt.Value, false, "Makes ped exit the vehicle");
                        menu.AddItem(_ExitVehicle);
                        menuItem.Action = _ExitVehicle;
                        Actions.Add(menuItem);
                    }
                    AssignFontColorFromAttribute(menuItem, menuItem.Action);
                }
            }

            void CreateSubCategoryScroller()
            {
                _subCategoryNames.Clear();
                _subCategoryNames = subCategories.Where(x => x.Enabled && x.MenuItems.Any(y => y.Enabled)).Select(z => z.Name.Value).ToList();
                _subMenuScroller.Items.Clear();
                _subMenuScroller.Items = _subCategoryNames;
                menu.AddItem(_subMenuScroller, 1);
                _subMenuScroller.ForeColor = Color.SkyBlue;
                _subMenuScroller.HighlightedBackColor = Color.SkyBlue;
                if (_savedSubMenuIndex < _subMenuScroller.OptionCount)
                {
                    _subMenuScroller.Index = _savedSubMenuIndex;
                }
                else
                {
                    _subMenuScroller.Index = 0;
                }
            }

            void AssignFontColorFromAttribute(MenuItem menuItem, UIMenuItem uiMenuItem)
            {
                var attribute = menuItem.MenuPrompt.Attribute("type");
                if (menuItem.MenuPrompt.Attribute("type") != null)
                {
                    if (attribute.Value.ToLower() == "interview")
                    {
                        uiMenuItem.ForeColor = Color.LimeGreen;
                    }
                    else if (attribute.Value.ToLower() == "interrogation")
                    {
                        uiMenuItem.ForeColor = Color.IndianRed;
                    }
                }
                if (menuItem.MenuPrompt.Attribute("action") != null)
                {
                    uiMenuItem.ForeColor = Color.Gold;
                }
                if (menuItem.Enabled)
                {
                    uiMenuItem.HighlightedBackColor = uiMenuItem.ForeColor;
                }
            }
        }
    
        private static void Menu_OnScrollerChanged(UIMenu menu, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
        {
            var categoryScroller = (UIMenuListScrollerItem<string>)menu.MenuItems[0];
            AssignScrollerCategory();

            if (scroller == CopParentCategoryScroller || scroller == CivParentCategoryScroller)
            {
                ScrollParentCategory();
            }

            if(scroller == menu.MenuItems[1])
            {
                ScrollSubMenu();
            }
            SetMenuWidth(menu);

            void AssignScrollerCategory()
            {
                if (scroller == CopParentCategoryScroller)
                {
                    categoryScroller = CopParentCategoryScroller;
                }
                else if (scroller == CivParentCategoryScroller)
                {
                    categoryScroller = CivParentCategoryScroller;
                }
            }

            void ScrollParentCategory()
            {
                while (menu.MenuItems.Count > 1)
                {
                    menu.RemoveItemAt(1);
                }

                if (menu.TitleText.Contains("Civilian"))
                {
                    PopulateMenu(menu, CivParentCategoryScroller);
                }
                else if (menu.TitleText.Contains("Cop"))
                {
                    PopulateMenu(menu, CopParentCategoryScroller);
                }
            }

            void ScrollSubMenu()
            {
                _savedSubMenuIndex = _subMenuScroller.Index;
                while (menu.MenuItems.Count > 2)
                {
                    menu.RemoveItemAt(2);
                }

                var parentCategory = _parentCategories.FirstOrDefault(x => x.Name.Value == categoryScroller.OptionText);
                if (MatchingMenuOpen(menu, parentCategory))
                {
                    PopulateMenu(menu, categoryScroller, 2);
                }
            }
        }

        private static void SetMenuWidth(UIMenu menu)
        {
            float MINIMUM_WIDTH = 0.25f;
            float width = 0.25f;

            CivParentCategoryScroller.TextStyle.Apply();
            Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
            if (menu.TitleText == "Civilian Interaction Menu")
            {
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(CivParentCategoryScroller.SelectedItem);
            }
            else if (menu.TitleText == "Cop Interaction Menu")
            {
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(CopParentCategoryScroller.SelectedItem);
            }
            float scrollerTextWidth = Rage.Native.NativeFunction.Natives.x85F061DA64ED2F67<float>(true); // _END_TEXT_COMMAND_GET_WIDTH
            float padding = 0.00390625f * 2; // typical padding used in RNUI

            var scrollerItemWidth = scrollerTextWidth + padding;
            //Game.LogTrivial($"Scroller item width: {scrollerItemWidth}");
            float subScrollerItemWidth = 0;

            foreach (var menuItem in menu.MenuItems)
            {
                menuItem.TextStyle.Apply();
                Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
                if(menuItem.GetType() == typeof(UIMenuListScrollerItem<string>))
                {
                    var scrollerMenuItem = menuItem as UIMenuListScrollerItem<string>;
                    Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(scrollerMenuItem.OptionText);
                }
                else
                {
                    Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(menuItem.Text);
                }

                float textWidth = Rage.Native.NativeFunction.Natives.x85F061DA64ED2F67<float>(true); // _END_TEXT_COMMAND_GET_WIDTH
                //Game.LogTrivial($"Menu item width: {textWidth}");
                var newWidth = Math.Max(textWidth + padding, UIMenu.DefaultWidth);
                //Game.LogTrivial($"Menu item width: {newWidth}");

                if(menuItem.GetType() == typeof(UIMenuListScrollerItem<string>))
                {
                    subScrollerItemWidth = textWidth;
                    //Game.LogTrivial($"Subscroller width: {subScrollerItemWidth}");
                }

                // Minimum width is set to prevent the scroller from clipping the menu item name
                if (newWidth < MINIMUM_WIDTH)
                {
                    newWidth = MINIMUM_WIDTH;
                }
                if (newWidth > width)
                {
                    width = newWidth;
                }
            }
            // If either scroll item is wide enough, multiple by 0.1f to keep it from overlapping the text
            if (scrollerItemWidth > 0.15 || subScrollerItemWidth > 0.15)
            {
                //width += 0.025f;
                width += width * 0.1f;
            }
            // When the menu consists of only scroller items
            if (menu.MenuItems.Count() == 2 && menu.MenuItems[0].GetType() == typeof(UIMenuListScrollerItem<string>) && menu.MenuItems[1].GetType() == typeof(UIMenuListScrollerItem<string>))
            {
                width = Math.Max(scrollerItemWidth, subScrollerItemWidth);
                width += 0.5f * width;
                if(width < MINIMUM_WIDTH)
                {
                    width = MINIMUM_WIDTH;
                }
            }
            menu.Width = width;
        }

        private static bool MatchingMenuOpen(UIMenu menu, ParentCategory parentCategory)
        {
            if ((parentCategory.Menu == Settings.Group.Civilian && menu.TitleText.Contains("Civilian")) || (parentCategory.Menu == Settings.Group.Cop && menu.TitleText.Contains("Cop")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
