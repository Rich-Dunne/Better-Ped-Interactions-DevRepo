using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using BetterPedInteractions.Utils;
using BetterPedInteractions.Objects;

namespace BetterPedInteractions
{
    class MenuManager
    {
        private static MenuPool MenuPool { get; } = new MenuPool();
        private static List<ParentCategory> ParentCategories { get; } = new List<ParentCategory>();
        private static List<MenuItem> Actions { get; } = new List<MenuItem>();
        private static List<string> SubCategoryNames { get; set; } = new List<string>();
        private static UIMenuListScrollerItem<string> SubMenuScroller { get; set; } = new UIMenuListScrollerItem<string>("Sub Category", "", SubCategoryNames);
        private static int SavedSubMenuIndex { get; set; } = 0;
        private static UIMenuItem MenuItem { get; set; }
        private static UIMenuItem RollDownWindowAction { get; set; }
        private static UIMenuItem ExitVehicleAction { get; set; }
        private static UIMenuItem TurnOffEngineAction { get; set; }
        private static UIMenuItem DismissAction { get; set; }
        private static UIMenuCheckboxItem FollowMeAction { get; set; }

        internal static DialogueMenu InitializeMenu(Settings.Group group)
        {
            DialogueMenu menu;
            if(group == Settings.Group.Civilian)
            {
                menu = new DialogueMenu("Civilian Interaction Menu", "", group);
            }
            else
            {
                menu = new DialogueMenu("Cop Interaction Menu", "", group);
            }

            MenuPool.Add(menu);
            menu.MouseControlsEnabled = false;
            menu.AllowCameraMovement = true;
            menu.OnCheckboxChange += MenuItem_OnCheckboxChanged;
            menu.OnItemSelect += MenuItem_OnItemSelected;
            menu.OnScrollerChange += MenuItem_OnScrollerChanged;

            return menu;
        }

        internal static void DisplayNearbyPedMenu()
        {
            Ped nearbyPed = PedHandler.NearbyPed;
            if (!nearbyPed)
            {
                return;
            }

            PedHandler.CollectOrFocusNearbyPed(nearbyPed);
            PopulateMenu(PedHandler.FocusedPed);
            PedHandler.FocusedPed.Menu.Visible = !PedHandler.FocusedPed.Menu.Visible;
            GameFiber.StartNew(() => HighlightTracker(PedHandler.FocusedPed.Menu), "Menu Item Highlight Tracker Fiber");
            GameFiber.StartNew(() => CloseMenuIfPlayerTooFar(PedHandler.FocusedPed.Menu), "Auto Close Menu Fiber");
        }

        private static void HighlightTracker(UIMenu menu)
        {
            while (menu.Visible)
            {
                foreach (UIMenuItem menuItem in menu.MenuItems)
                {
                    if (menuItem.Selected && menuItem.LeftBadge != UIMenuItem.BadgeStyle.None)
                    {
                        var selectedItem = PedHandler.FocusedPed.Menu.ParentCategories.SelectMany(x => x.MenuItems).FirstOrDefault(x => x.MenuText.Value == menuItem.Text);
                        if(selectedItem == null)
                        {
                            selectedItem = PedHandler.FocusedPed.Menu.ParentCategories.SelectMany(x => x.SubCategories).SelectMany(y => y.MenuItems).FirstOrDefault(z => z.MenuText.Value == menuItem.Text);
                        }
                        selectedItem.BadgeStyle = UIMenuItem.BadgeStyle.None;
                        menuItem.LeftBadge = selectedItem.BadgeStyle;
                    }
                }
                GameFiber.Yield();
            }
        }

        internal static void CloseMenuIfPlayerTooFar(UIMenu menu)
        {
            while (true)
            {
                MenuPool.ProcessMenus();
                if (!menu.Visible || (MenuPool.IsAnyMenuOpen() && PedHandler.FocusedPed != null && PedHandler.FocusedPed && Game.LocalPlayer.Character.DistanceTo2D(PedHandler.FocusedPed) > Settings.InteractDistance && !PedHandler.FocusedPed.Following || !Game.LocalPlayer.Character || !Game.LocalPlayer.Character.IsAlive))
                {
                    PedHandler.FocusedPed = null;
                    MenuPool.CloseAllMenus();
                    MenuPool.RefreshIndex();
                    break;
                }
                GameFiber.Yield();
            }
        }

        internal static void PopulateMenu(CollectedPed ped, int removeItemIndex = 1)
        {
            UIMenu menu = ped.Menu;
            if (menu == null)
            {
                Game.LogTrivial($"Focused ped's menu is null.");
                return;
            }

            var categories = ped.Menu.ParentCategories.Select(x => x.Name).ToList();
            menu.AddItem(new UIMenuListScrollerItem<string>("Category", "The category of the prompts", categories));
            var categoryScroller = (UIMenuListScrollerItem<string>)menu.MenuItems[0];

            UpdateMenuDescription();

            // First I need to clear the menu because this method will be used for refreshing menu items
            while (menu.MenuItems.Count > removeItemIndex)
            {
                menu.RemoveItemAt(removeItemIndex);
            }

            // Next, I need to get the current parent category
            var parentCategory = ped.Menu.ParentCategories.FirstOrDefault(x => x.Name == categoryScroller.SelectedItem);
            if (parentCategory == null)
            {
                Game.LogTrivial($"Parent category is null.");
                return;
            }

            // Then, I need to check if the parent category has sub categories
            // If there are any sub categories, I need to create a sub-category scroller menu item
            var subCategories = parentCategory.SubCategories;
            var enabledSubCategories = subCategories.Where(x => x.Enabled).ToList();

            UIMenuListScrollerItem<string> subCategoryScroller = null;
            if (subCategories.Count > 0 && menu.MenuItems.Count == 1)
            {           
                CreateSubCategoryScroller();
            }
            subCategoryScroller = (UIMenuListScrollerItem<string>)menu.MenuItems[1];
            var subCategory = ped.Menu.ParentCategories.SelectMany(x => x.SubCategories).FirstOrDefault(x => x.Name == subCategoryScroller.OptionText);

            // Next, I need to populate the menu with prompts which either match the currently selected parent category or sub category
            if (categoryScroller.SelectedItem == "Ped Actions")
            {
                var actions = ped.Menu.AllMenuItems.Where(x => x.Enabled && x.SubCategory.Name == subCategoryScroller?.OptionText).ToList();
                if(actions.Count > 0)
                {
                    AddPedActionsToMenu(actions);
                    DisableIrrelevantActions();
                }
            }
            else
            {
                AddPromptsToMenu(); 
            }

            // Finally, set the menu width based on the longest menu item
            SetMenuWidth(menu);

            void UpdateMenuDescription()
            {
                var scroller = (UIMenuListScrollerItem<string>)PedHandler.FocusedPed.Menu.MenuItems[0];
                menu.MenuItems[0].Description = $"From file: ~b~test";
                //menu.MenuItems[0].Description = $"From file: ~b~{PedHandler.FocusedPed.Menu.ParentCategories.FirstOrDefault(x => x.Name == scroller.OptionText).File}";
            }

            void CreateSubCategoryScroller()
            {
                SubCategoryNames.Clear();
                SubCategoryNames = subCategories.Where(x => x.Enabled && x.MenuItems.Any(y => y.Enabled)).Select(z => z.Name).Distinct().ToList();
                SubMenuScroller.Items.Clear();
                SubMenuScroller.Items = SubCategoryNames;
                menu.AddItem(SubMenuScroller, 1);
                SubMenuScroller.ForeColor = Color.SkyBlue;
                SubMenuScroller.HighlightedBackColor = Color.SkyBlue;
                if (SavedSubMenuIndex < SubMenuScroller.OptionCount)
                {
                    SubMenuScroller.Index = SavedSubMenuIndex;
                }
                else
                {
                    SubMenuScroller.Index = 0;
                }
            }

            void AddPedActionsToMenu(IEnumerable<MenuItem> actions)
            {
                Actions.Clear();
                foreach (MenuItem menuItem in actions)
                {
                    if (menuItem.MenuText.Parent.Element("Action").Value == "Follow")
                    {
                        bool followingActionBool = false;
                        if(PedHandler.FocusedPed != null)
                        {
                            followingActionBool = PedHandler.FocusedPed.Following;
                        }
                        FollowMeAction = new UIMenuCheckboxItem(menuItem.MenuText.Value, followingActionBool, "Makes the ped follow the player");
                        menuItem.Action = Settings.Actions.Follow;
                        menuItem.UIMenuItem = FollowMeAction;
                        menu.AddItem(FollowMeAction);
                    }
                    if (menuItem.MenuText.Parent.Element("Action").Value == "Dismiss")
                    {
                        DismissAction = new UIMenuItem(menuItem.MenuText.Value, "Dismisses the focused ped.");
                        menuItem.Action = Settings.Actions.Dismiss;
                        menuItem.UIMenuItem = DismissAction;
                        menu.AddItem(DismissAction);
                    }
                    if (menuItem.MenuText.Parent.Element("Action").Value == "RollWindowDown")
                    {
                        RollDownWindowAction = new UIMenuItem(menuItem.MenuText.Value, "Makes the ped roll down their window");
                        menuItem.Action = Settings.Actions.RollWindowDown;
                        menuItem.UIMenuItem = RollDownWindowAction;
                        menu.AddItem(RollDownWindowAction);
                    }
                    if (menuItem.MenuText.Parent.Element("Action").Value == "TurnOffEngine")
                    {
                        TurnOffEngineAction = new UIMenuItem(menuItem.MenuText.Value, "Makes ped turn off the engine");
                        menuItem.Action = Settings.Actions.TurnOffEngine;
                        menuItem.UIMenuItem = TurnOffEngineAction;
                        menu.AddItem(TurnOffEngineAction);
                    }
                    if (menuItem.MenuText.Parent.Element("Action").Value == "ExitVehicle")
                    {
                        ExitVehicleAction = new UIMenuItem(menuItem.MenuText.Value, "Makes ped exit the vehicle");
                        menuItem.Action = Settings.Actions.ExitVehicle;
                        menuItem.UIMenuItem = ExitVehicleAction;
                        menu.AddItem(ExitVehicleAction);
                    }
                    Actions.Add(menuItem);
                    AssignFontColorFromAttribute(menuItem, menuItem.UIMenuItem);
                }
                //Game.LogTrivial($"Actions assigned: {Actions.Count()}");
            }

            void DisableIrrelevantActions()
            {
                if (!Game.LocalPlayer.Character)
                {
                    Game.LogTrivial($"Player character is null.");
                    return;
                }
                if (PedHandler.FocusedPed == null)
                {
                    Game.LogTrivial($"focusedPed is null.");
                    return;
                }

                if (PedHandler.FocusedPed && PedHandler.FocusedPed.Following && Game.LocalPlayer.Character.DistanceTo2D(PedHandler.FocusedPed) > Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in PedHandler.FocusedPed.Menu.MenuItems)
                    {
                        if (item.Text != "Follow me")
                        {
                            item.Enabled = false;
                            item.BackColor = Color.Gray;
                        }
                    }
                }
                else if (Game.LocalPlayer.Character && Game.LocalPlayer.Character.DistanceTo2D(PedHandler.FocusedPed) <= Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in PedHandler.FocusedPed.Menu.MenuItems)
                    {
                        item.Enabled = true;
                    }
                }

                foreach (MenuItem action in Actions.Where(x => x.MenuText.Parent.Element("Action").Value != "Follow" && x.MenuText.Parent.Element("Action").Value != "Dismiss"))
                {
                    if (PedHandler.FocusedPed && !PedHandler.FocusedPed.CurrentVehicle && action.SubCategory.Name == "On Foot")
                    {
                        action.UIMenuItem.Enabled = true;
                    }
                    else if (PedHandler.FocusedPed && PedHandler.FocusedPed.CurrentVehicle && action.SubCategory.Name == "On Foot")
                    {
                        action.UIMenuItem.Enabled = false;
                    }

                    if (PedHandler.FocusedPed && PedHandler.FocusedPed.CurrentVehicle && action.SubCategory.Name == "In Vehicle")
                    {
                        Game.LogTrivial($"{action.MenuText.Value} is enabled.");
                        action.UIMenuItem.Enabled = true;
                    }
                    else if (PedHandler.FocusedPed && !PedHandler.FocusedPed.CurrentVehicle && action.SubCategory.Name == "In Vehicle")
                    {
                        action.UIMenuItem.Enabled = false;
                    }

                    // This is a different reason to change
                    if (!action.UIMenuItem.Enabled)
                    {
                        action.UIMenuItem.HighlightedBackColor = Color.White;
                    }
                    else
                    {
                        action.UIMenuItem.HighlightedBackColor = action.UIMenuItem.ForeColor;
                    }
                }
            }

            void AddPromptsToMenu()
            {
                List<MenuItem> prompts = GetPromptsMatchingCategoryLevel();
                foreach (MenuItem menuItem in prompts?.Where(x => x.Enabled && x.MenuText != null))
                {
                    if (PedHandler.FocusedPed != null && !PedHandler.FocusedPed.Menu.UsedMenuItems.Contains(menuItem))
                    {
                        //Game.LogTrivial($"[{menuItem.ParentCategory.Menu.TitleText.ToUpper()} ADD] Parent category: {menuItem.ParentCategory.Name}({menuItem.ParentCategory.Level}), Sub-Category: {menuItem.SubCategory.Name}({menuItem.SubCategory.Level}), Prompt: {menuItem.MenuPrompt.Value}");
                        menu.AddItem(MenuItem = new UIMenuItem(menuItem.MenuText.Value));
                        MenuItem.LeftBadge = menuItem.BadgeStyle;
                        AssignFontColorFromAttribute(menuItem, MenuItem);
                    }
                }
            }

            List<MenuItem> GetPromptsMatchingCategoryLevel()
            {
                if (menu.MenuItems.Count > 1 && subCategories.Count > 0 && SubMenuScroller.OptionCount > 0)
                {
                    //Game.LogTrivial($"Subscroller selected item: {_subMenuScroller.SelectedItem}");
                    var matchingSubCategory = subCategories.FirstOrDefault(x => SubMenuScroller.SelectedItem == x.Name);
                    //Game.LogTrivial($"Possible prompts: {promptsMatchingReadLevel.Count()}");
                    if (matchingSubCategory == null)
                    {
                        Game.LogTrivial($"Matching sub category is null.");
                        return null;
                    }
                    return PedHandler.FocusedPed.Menu.ParentCategories.SelectMany(x => x.SubCategories).SelectMany(y => y.MenuItems).Where(z => z.SubCategory.Name == SubMenuScroller.SelectedItem && z.SubCategory.ParentCategory.Name == parentCategory.Name && z.Level <= matchingSubCategory.Level).ToList();
                }
                else
                {
                    var parentCategoryScroller = menu.MenuItems[0] as UIMenuListScrollerItem<string>;
                    return PedHandler.FocusedPed.Menu.ParentCategories.SelectMany(x => x.MenuItems).Where(y => y.ParentCategory.Name == parentCategoryScroller.SelectedItem && y.Level <= parentCategory.Level).ToList();
                }
            }

            void AssignFontColorFromAttribute(MenuItem menuItem, UIMenuItem uiMenuItem)
            {
                if (menuItem.MenuText.Parent.Elements("PromptType").Any())
                {
                    if (menuItem.MenuText.Parent.Element("PromptType").Value.ToLower() == "interview")
                    {
                        uiMenuItem.ForeColor = Color.LimeGreen;
                    }
                    else if (menuItem.MenuText.Parent.Element("PromptType").Value.ToLower() == "interrogation")
                    {
                        uiMenuItem.ForeColor = Color.IndianRed;
                    }
                }
                if (menuItem.MenuText.Parent.Elements("Action").Any())
                {
                    uiMenuItem.ForeColor = Color.Gold;
                }
                if (menuItem.Enabled)
                {
                    uiMenuItem.HighlightedBackColor = uiMenuItem.ForeColor;
                }
            }
        }

        private static void SetMenuWidth(UIMenu menu)
        {
            float MINIMUM_WIDTH = 0.25f;
            float padding = 0.00390625f * 2; // typical padding used in RNUI
            float widthToAssign = 0.25f;
            float scrollerItemWidth = GetParentCategoryScrollerTextWidth() + padding;
            float subScrollerItemWidth = 0;
            //Game.LogTrivial($"Scroller item width: {scrollerItemWidth}");

            foreach (var menuItem in menu.MenuItems)
            {
                float menuItemTextWidth = GetMenuItemTextWidth(menuItem);
                float newWidth = Math.Max(menuItemTextWidth + padding, UIMenu.DefaultWidth);
                
                // If menu item has a LeftBadge, add some width for padding
                if (menuItem.LeftBadge != UIMenuItem.BadgeStyle.None)
                {
                    newWidth += 0.02f;
                }
                //Game.LogTrivial($"Menu item width: {newWidth}");

                if (menuItem.GetType() == typeof(UIMenuListScrollerItem<string>))
                {
                    subScrollerItemWidth = menuItemTextWidth;
                    //Game.LogTrivial($"Subscroller width: {subScrollerItemWidth}");
                }

                // Minimum width is set to prevent the scroller from clipping the menu item name
                if (newWidth < MINIMUM_WIDTH)
                {
                    newWidth = MINIMUM_WIDTH;
                }
                if (newWidth > widthToAssign)
                {
                    widthToAssign = newWidth;
                }
            }

            // If either scroll item is wide enough, multiple by 0.1f to keep it from overlapping the text
            if (scrollerItemWidth > 0.15 || subScrollerItemWidth > 0.15)
            {
                widthToAssign += widthToAssign * 0.1f;
            }

            // When the menu consists of only scroller items
            if (menu.MenuItems.Count() == 2 && menu.MenuItems[0].GetType() == typeof(UIMenuListScrollerItem<string>) && menu.MenuItems[1].GetType() == menu.MenuItems[0].GetType())
            {
                widthToAssign = Math.Max(scrollerItemWidth, subScrollerItemWidth);
                widthToAssign += 0.5f * widthToAssign;
                if (widthToAssign < MINIMUM_WIDTH)
                {
                    widthToAssign = MINIMUM_WIDTH;
                }
            }
            menu.Width = widthToAssign;

            float GetParentCategoryScrollerTextWidth()
            {
                var parentCategoryScroller = (UIMenuListScrollerItem<string>)menu.MenuItems[0];
                //if (menu == CivMenu)
                //{
                //    CivParentCategoryScroller.TextStyle.Apply();
                //}
                //else
                //{
                //    CopParentCategoryScroller.TextStyle.Apply();
                //}
                parentCategoryScroller.TextStyle.Apply();

                Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
                //if (menu == CivMenu)
                //{
                //    Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(CivParentCategoryScroller.SelectedItem);
                //}
                //else if (menu == CopMenu)
                //{
                //    Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(CopParentCategoryScroller.SelectedItem);
                //}
                Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(parentCategoryScroller.SelectedItem);

                return Rage.Native.NativeFunction.Natives.x85F061DA64ED2F67<float>(true); // _END_TEXT_COMMAND_GET_WIDTH
            }

            float GetMenuItemTextWidth(UIMenuItem menuItem)
            {
                menuItem.TextStyle.Apply();
                Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
                if (menuItem.GetType() == typeof(UIMenuListScrollerItem<string>))
                {
                    var scrollerMenuItem = menuItem as UIMenuListScrollerItem<string>;
                    Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(scrollerMenuItem.OptionText);
                }
                else
                {
                    Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(menuItem.Text);
                }
                return Rage.Native.NativeFunction.Natives.x85F061DA64ED2F67<float>(true); // _END_TEXT_COMMAND_GET_WIDTH
            }
        }

        private static void MenuItem_OnCheckboxChanged(UIMenu menu, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            var focusedPed = PedHandler.FocusedPed;

            if (checkboxItem == FollowMeAction)
            {
                if (FollowMeAction.Checked)
                {
                    focusedPed.FollowMe();
                }
                else
                {
                    focusedPed.StopFollowing();
                }
            }
        }

        private static void MenuItem_OnItemSelected(UIMenu menu, UIMenuItem selectedItem, int index)
        {
            var focusedPed = PedHandler.FocusedPed;

            if (selectedItem == DismissAction)
            {
                focusedPed.Dismiss();
                menu.Close();
                return;
            }

            if (selectedItem == RollDownWindowAction && focusedPed.CurrentVehicle && focusedPed.CurrentVehicle.IsCar)
            {
                focusedPed.RollDownWindow();
                return;
            }

            if (selectedItem == ExitVehicleAction && focusedPed.CurrentVehicle)
            {
                focusedPed.ExitVehicle();
                return;
            }

            if (selectedItem == TurnOffEngineAction && focusedPed.CurrentVehicle && focusedPed.CurrentVehicle.Driver == focusedPed)
            {
                focusedPed.TurnOffEngine();
                return;
            }

            if (selectedItem.GetType() != typeof(UIMenuListScrollerItem<string>) && selectedItem.GetType() != typeof(UIMenuCheckboxItem))
            {
                ResponseManager.FindMatchingPrompt(selectedItem.Text);
                return;
            }
        }

        private static void MenuItem_OnScrollerChanged(UIMenu menu, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
        {
            var categoryScroller = (UIMenuListScrollerItem<string>)menu.MenuItems[0];

            if (scroller == menu.MenuItems[0])
            {
                PopulateMenu(PedHandler.FocusedPed);
            }

            if (menu.MenuItems.Count > 1 && scroller == menu.MenuItems[1])
            {
                ScrollSubMenu();
            }

            void ScrollSubMenu()
            {
                SavedSubMenuIndex = SubMenuScroller.Index;
                while (menu.MenuItems.Count > 2)
                {
                    menu.RemoveItemAt(2);
                }
                var parentCategory = ParentCategories.FirstOrDefault(x => x.Name == categoryScroller.OptionText && x.Menu == menu);
                PopulateMenu(PedHandler.FocusedPed, 2);
            }
        }
    }
}
