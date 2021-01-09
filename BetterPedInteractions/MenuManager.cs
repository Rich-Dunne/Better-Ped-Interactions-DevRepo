using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using BetterPedInteractions.Utils;

namespace BetterPedInteractions
{
    class MenuManager
    {
        internal static MenuPool MenuPool { get; } = new MenuPool();
        internal static UIMenu CivMenu { get; private set; }
        internal static UIMenu CopMenu { get; private set; }
        private static List<ParentCategory> ParentCategories { get; } = new List<ParentCategory>();
        internal static UIMenuListScrollerItem<string> CivParentCategoryScroller { get; private set; }
        internal static UIMenuListScrollerItem<string> CopParentCategoryScroller { get; private set; }
        internal static List<MenuItem> Actions { get; } = new List<MenuItem>();
        private static List<string> SubCategoryNames { get; set; } = new List<string>();
        private static UIMenuListScrollerItem<string> SubMenuScroller { get; set; } = new UIMenuListScrollerItem<string>("Sub Category", "", SubCategoryNames);
        private static int SavedSubMenuIndex { get; set; } = 0;
        private static UIMenuItem DialogueItem { get; set; }
        internal static UIMenuItem RollWindowDownAction { get; set; }
        internal static UIMenuItem ExitVehicleAction { get; set; }
        internal static UIMenuItem TurnOffEngineAction { get; set; }
        internal static UIMenuItem DismissAction { get; set; }
        internal static UIMenuCheckboxItem FollowMeAction { get; set; }

        internal static void InitializeMenus()
        {
            // Initialize Civilian Menu
            CivMenu = new UIMenu("Civilian Interaction Menu", "");
            MenuPool.Add(CivMenu);

            CivMenu.MouseControlsEnabled = false;
            CivMenu.AllowCameraMovement = true;

            CivMenu.OnCheckboxChange += MenuItem_OnCheckboxChanged;
            CivMenu.OnItemSelect += MenuItem_OnItemSelected;
            CivMenu.OnScrollerChange += MenuItem_OnScrollerChanged;

            // Initialize Cop Menu
            CopMenu = new UIMenu("Cop Interaction Menu", "");
            MenuPool.Add(CopMenu);

            CopMenu.MouseControlsEnabled = false;
            CopMenu.AllowCameraMovement = true;

            CopMenu.OnCheckboxChange += MenuItem_OnCheckboxChanged;
            CopMenu.OnItemSelect += MenuItem_OnItemSelected;
            CopMenu.OnScrollerChange += MenuItem_OnScrollerChanged;
        }

        internal static void AddParentCategories(List<ParentCategory> parentCategories) => ParentCategories.AddRange(parentCategories);

        internal static void InitializeActionMenuItems()
        {
            // ISSUE: Assigning actions to menu items does not seem to be applying to the original menu items in ParentCategories
            foreach (MenuItem menuItem in ParentCategories.SelectMany(x => x.MenuItems.Where(y => y.MenuPrompt != null && y.MenuPrompt.Attribute("action") != null)))
            //foreach (MenuItem menuItem in GetAllMenuItems()?.Where(x => x.MenuPrompt != null && x.MenuPrompt.Attribute("action") != null))
            {
                Game.LogTrivial($"Assigning action for {menuItem.MenuPrompt.Value}");
                if (menuItem.MenuPrompt.Attribute("action").Value == "follow")
                {
                    FollowMeAction = new UIMenuCheckboxItem(menuItem.MenuPrompt.Value, false, "Makes the ped follow the player");
                    Actions.Add(menuItem);
                    menuItem.Action = FollowMeAction;
                    continue;
                }
                if (menuItem.MenuPrompt.Attribute("action").Value == "dismiss")
                {
                    DismissAction = new UIMenuItem(menuItem.MenuPrompt.Value, "Dismisses the focused ped.");
                    Actions.Add(menuItem);
                    menuItem.Action = DismissAction;
                    continue;
                }
                if (menuItem.MenuPrompt.Attribute("action").Value == "rollWindowDown")
                {
                    RollWindowDownAction = new UIMenuItem(menuItem.MenuPrompt.Value, "Makes the ped roll down their window");
                    Actions.Add(menuItem);
                    menuItem.Action = RollWindowDownAction;
                    continue;
                }
                if (menuItem.MenuPrompt.Attribute("action").Value == "turnOffEngine")
                {
                    TurnOffEngineAction = new UIMenuItem(menuItem.MenuPrompt.Value, "Makes ped turn off the engine");
                    Actions.Add(menuItem);
                    menuItem.Action = TurnOffEngineAction;
                    continue;
                }
                if (menuItem.MenuPrompt.Attribute("action").Value == "exitVehicle")
                {
                    ExitVehicleAction = new UIMenuItem(menuItem.MenuPrompt.Value, "Makes ped exit the vehicle");
                    Actions.Add(menuItem);
                    menuItem.Action = ExitVehicleAction;
                    continue;
                }
            }
        }

        internal static void PopulateCategoryScrollers()
        {
            var civCategories = ParentCategories.Where(x => x.Menu == CivMenu).Distinct().Select(x => x.Name.Value).ToList();
            CivMenu.AddItem(CivParentCategoryScroller = new UIMenuListScrollerItem<string>("Category", "The category of the prompts", civCategories));

            var copCategories = ParentCategories.Where(x => x.Menu == CopMenu).Distinct().Select(x => x.Name.Value).ToList();
            CopMenu.AddItem(CopParentCategoryScroller = new UIMenuListScrollerItem<string>("Category", "The category of the prompts", copCategories));
        }

        internal static void InitialMenuPopulation()
        {
            PopulateMenu(CivMenu, CivParentCategoryScroller);
            PopulateMenu(CopMenu, CopParentCategoryScroller);
        }

        internal static void DisplayMenuForNearbyPed()
        {
            Ped nearbyPed = PedHandler.GetNearbyPed();
            UIMenu menu = null;
            if (nearbyPed)
            {
                PedHandler.CollectOrFocusNearbyPed(nearbyPed);
                if (PedHandler.FocusedPed.Group == Settings.Group.Civilian)
                {
                    PopulateMenu(CivMenu, CivParentCategoryScroller);
                    CivMenu.Visible = !CivMenu.Visible;
                    menu = CivMenu;
                }
                else if (PedHandler.FocusedPed.Group == Settings.Group.Cop)
                {
                    PopulateMenu(CopMenu, CopParentCategoryScroller);
                    CopMenu.Visible = !CopMenu.Visible;
                    menu = CopMenu;
                }

                GameFiber.StartNew(() => CloseMenuIfPlayerTooFar(menu));
            }
        }

        internal static void CloseMenuIfPlayerTooFar(UIMenu menu)
        {
            while (true)
            {
                // RefreshIndex anywhere?
                MenuPool.ProcessMenus();
                HighlightTracker();
                if (!menu.Visible || (MenuPool.IsAnyMenuOpen() && PedHandler.FocusedPed != null && PedHandler.FocusedPed.Ped && Game.LocalPlayer.Character.DistanceTo2D(PedHandler.FocusedPed.Ped) > Settings.InteractDistance && !PedHandler.FocusedPed.Following || !Game.LocalPlayer.Character || !Game.LocalPlayer.Character.IsAlive))
                {
                    PedHandler.FocusedPed = null;
                    MenuPool.CloseAllMenus();
                    break;
                }
                GameFiber.Yield();
            }

            void HighlightTracker()
            {
                if (CivMenu.Visible)
                {
                    foreach (UIMenuItem menuItem in CivMenu.MenuItems)
                    {
                        if (menuItem.Selected && menuItem.LeftBadge != UIMenuItem.BadgeStyle.None)
                        {
                            menuItem.LeftBadge = UIMenuItem.BadgeStyle.None;
                            var allMenuItems = GetAllMenuItemsForMenu(CivMenu);
                            var selectedItem = allMenuItems.FirstOrDefault(x => x.MenuPrompt.Value == menuItem.Text);
                            selectedItem.Highlighted = true;
                        }
                    }
                }
                else if (CopMenu.Visible)
                {
                    foreach (UIMenuItem menuItem in CopMenu.MenuItems)
                    {
                        if (menuItem.Selected && menuItem.LeftBadge != UIMenuItem.BadgeStyle.None)
                        {
                            menuItem.LeftBadge = UIMenuItem.BadgeStyle.None;
                            var allMenuItems = GetAllMenuItemsForMenu(CopMenu);
                            var selectedItem = allMenuItems.FirstOrDefault(x => x.MenuPrompt.Value == menuItem.Text);
                            selectedItem.Highlighted = true;
                        }
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
            var parentCategory = ParentCategories.FirstOrDefault(x => x.Name.Value == categoryScroller.SelectedItem);
            if (parentCategory == null)
            {
                Game.LogTrivial($"Parent category is null.");
                return;
            }

            // Then, I need to check if the parent category has sub categories
            // If there are any sub categories, I need to add their names to _subMenuScroller
            var subCategories = parentCategory.SubCategories;
            if (subCategories.Count > 0 && menu.MenuItems.Count == 1)
            {
                CreateSubCategoryScroller();
            }

            // Next, I need to populate the menu with prompts which either match the currently selected parent category or sub category
            IEnumerable<MenuItem> prompts = GetPromptsMatchingCategoryLevel(); ;
            if (categoryScroller.SelectedItem == "Ped Actions")
            {
                AddPedActionsToMenu();
                DisableIrrelevantActions();
            }
            else
            {
                AddPromptsToMenu(); 
            }

            SetMenuWidth(menu);

            void CreateSubCategoryScroller()
            {
                SubCategoryNames.Clear();
                SubCategoryNames = subCategories.Where(x => x.Enabled && x.MenuItems.Any(y => y.Enabled)).Select(z => z.Name.Value).Distinct().ToList();
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

            IEnumerable<MenuItem> GetPromptsMatchingCategoryLevel()
            {
                if (menu.MenuItems.Count > 1 && subCategories.Count > 0 && SubMenuScroller.OptionCount > 0)
                {
                    //Game.LogTrivial($"Subscroller selected item: {_subMenuScroller.SelectedItem}");
                    var matchingSubCategory = subCategories.FirstOrDefault(x => SubMenuScroller.SelectedItem == x.Name.Value);
                    //Game.LogTrivial($"Possible prompts: {promptsMatchingReadLevel.Count()}");
                    if (matchingSubCategory == null)
                    {
                        Game.LogTrivial($"Matching sub category is null.");
                        return null;
                    }
                    return matchingSubCategory.MenuItems.Where(x => x.Level <= matchingSubCategory.Level);
                }
                else
                {
                    return parentCategory.MenuItems.Where(x => x.Level <= parentCategory.Level);
                }
            }

            void AddPedActionsToMenu()
            {
                Actions.Clear();
                foreach (MenuItem menuItem in prompts?.Where(x => x.Enabled && x.MenuPrompt != null && x.MenuPrompt.Attribute("action") != null))
                {
                    if (menuItem.MenuPrompt.Attribute("action").Value == "follow")
                    {
                        bool followingActionBool = false;
                        if(PedHandler.FocusedPed != null)
                        {
                            followingActionBool = PedHandler.FocusedPed.Following;
                        }
                        FollowMeAction = new UIMenuCheckboxItem(menuItem.MenuPrompt.Value, followingActionBool, "Makes the ped follow the player");
                        menu.AddItem(FollowMeAction);
                        menuItem.Action = FollowMeAction;
                    }
                    if (menuItem.MenuPrompt.Attribute("action").Value == "dismiss")
                    {
                        DismissAction = new UIMenuItem(menuItem.MenuPrompt.Value, "Dismisses the focused ped.");
                        menu.AddItem(DismissAction);
                        menuItem.Action = DismissAction;
                    }
                    if (menuItem.MenuPrompt.Attribute("action").Value == "rollWindowDown")
                    {
                        RollWindowDownAction = new UIMenuItem(menuItem.MenuPrompt.Value, "Makes the ped roll down their window");
                        menu.AddItem(RollWindowDownAction);
                        menuItem.Action = RollWindowDownAction;
                    }
                    if (menuItem.MenuPrompt.Attribute("action").Value == "turnOffEngine")
                    {
                        TurnOffEngineAction = new UIMenuItem(menuItem.MenuPrompt.Value, "Makes ped turn off the engine");
                        menu.AddItem(TurnOffEngineAction);
                        menuItem.Action = TurnOffEngineAction;
                    }
                    if (menuItem.MenuPrompt.Attribute("action").Value == "exitVehicle")
                    {
                        ExitVehicleAction = new UIMenuItem(menuItem.MenuPrompt.Value, "Makes ped exit the vehicle");
                        menu.AddItem(ExitVehicleAction);
                        menuItem.Action = ExitVehicleAction;
                    }
                    Actions.Add(menuItem);
                    AssignFontColorFromAttribute(menuItem, menuItem.Action);
                }
            }

            void DisableIrrelevantActions()
            {
                if (!Game.LocalPlayer.Character)
                {
                    //Game.LogTrivial($"Player character is null.");
                    return;
                }
                if (PedHandler.FocusedPed == null)
                {
                    //Game.LogTrivial($"focusedPed is null.");
                    return;
                }

                if (PedHandler.FocusedPed.Ped && PedHandler.FocusedPed.Following && Game.LocalPlayer.Character.DistanceTo2D(PedHandler.FocusedPed.Ped) > Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in CivMenu.MenuItems)
                    {
                        if (item.Text != "Follow me")
                        {
                            item.Enabled = false;
                            item.BackColor = Color.Gray;
                        }
                    }
                }
                else if (Game.LocalPlayer.Character && Game.LocalPlayer.Character.DistanceTo2D(PedHandler.FocusedPed.Ped) <= Settings.InteractDistance)
                {
                    foreach (UIMenuItem item in CivMenu.MenuItems)
                    {
                        item.Enabled = true;
                    }
                }

                foreach (MenuItem action in Actions.Where(x => x.MenuPrompt.Attribute("action").Value != "follow" && x.MenuPrompt.Attribute("action").Value != "dismiss"))
                {
                    if (PedHandler.FocusedPed.Ped && !PedHandler.FocusedPed.Ped.CurrentVehicle && action.SubCategory.Name.Value == "On Foot")
                    {
                        action.Action.Enabled = true;
                    }
                    else if (PedHandler.FocusedPed.Ped && PedHandler.FocusedPed.Ped.CurrentVehicle && action.SubCategory.Name.Value == "On Foot")
                    {
                        action.Action.Enabled = false;
                    }

                    if (PedHandler.FocusedPed.Ped && PedHandler.FocusedPed.Ped.CurrentVehicle && action.SubCategory.Name.Value == "In Vehicle")
                    {
                        Game.LogTrivial($"{action.MenuPrompt.Value} is enabled.");
                        action.Action.Enabled = true;
                    }
                    else if (PedHandler.FocusedPed.Ped && !PedHandler.FocusedPed.Ped.CurrentVehicle && action.SubCategory.Name.Value == "In Vehicle")
                    {
                        action.Action.Enabled = false;
                    }

                    // This is a different reason to change
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

            void AddPromptsToMenu()
            {
                foreach (MenuItem menuItem in prompts?.Where(x => x.Enabled && x.MenuPrompt != null))
                {
                    if (PedHandler.FocusedPed == null || (PedHandler.FocusedPed != null && !PedHandler.FocusedPed.UsedQuestions.ContainsKey(menuItem.MenuPrompt)))
                    {
                        //Game.LogTrivial($"[MENU ADD] Menu: {menuCategoryObject.Menu}, Category: {menuCategoryObject.Name.Value}, Prompt: {menuItem.MenuPrompt.Value}");
                        menu.AddItem(DialogueItem = new UIMenuItem(menuItem.MenuPrompt.Value));
                        // Need to only add the star on meny items recently enabled.  Remove badge when highlighted.
                        if (!menuItem.Highlighted)
                        {
                            DialogueItem.LeftBadge = UIMenuItem.BadgeStyle.Star;
                        }
                        AssignFontColorFromAttribute(menuItem, DialogueItem);
                    }
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
                if (menu == CivMenu)
                {
                    CivParentCategoryScroller.TextStyle.Apply();
                }
                else
                {
                    CopParentCategoryScroller.TextStyle.Apply();
                }

                Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
                if (menu == CivMenu)
                {
                    Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(CivParentCategoryScroller.SelectedItem);
                }
                else if (menu == CopMenu)
                {
                    Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(CopParentCategoryScroller.SelectedItem);
                }

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

        private static List<MenuItem> GetAllMenuItemsForMenu(UIMenu menu)
        {
            var allMenuItems = ParentCategories.Where(x => x.Menu == menu).SelectMany(x => x.MenuItems).ToList();
            var subCategories = ParentCategories.Where(x => x.Menu == menu).SelectMany(x => x.SubCategories);
            var subCategoryMenuItems = subCategories.SelectMany(x => x.MenuItems).ToList();
            allMenuItems.AddRange(subCategoryMenuItems);
            return allMenuItems;
        }

        private static List<MenuItem> GetAllMenuItems()
        {
            var allMenuItems = ParentCategories.SelectMany(x => x.MenuItems).ToList();
            var subCategories = ParentCategories.SelectMany(x => x.SubCategories);
            var subCategoryMenuItems = subCategories.SelectMany(x => x.MenuItems).ToList();
            allMenuItems.AddRange(subCategoryMenuItems);
            return allMenuItems;
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

            if (selectedItem == RollWindowDownAction && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.IsCar)
            {
                focusedPed.RollDownWindow();
                return;
            }

            if (selectedItem == ExitVehicleAction && focusedPed.Ped.CurrentVehicle)
            {
                focusedPed.ExitVehicle();
                return;
            }

            if (selectedItem == TurnOffEngineAction && focusedPed.Ped.CurrentVehicle && focusedPed.Ped.CurrentVehicle.Driver == focusedPed.Ped)
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
            AssignScrollerCategory();

            if (scroller == CopParentCategoryScroller || scroller == CivParentCategoryScroller)
            {
                ScrollParentCategory();
            }

            if (scroller == menu.MenuItems[1])
            {
                //Game.LogTrivial($"Scrolled {scroller.Text}");
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
                SavedSubMenuIndex = SubMenuScroller.Index;
                while (menu.MenuItems.Count > 2)
                {
                    menu.RemoveItemAt(2);
                }
                var parentCategory = ParentCategories.FirstOrDefault(x => x.Name.Value == categoryScroller.OptionText && x.Menu == menu);
                PopulateMenu(menu, categoryScroller, 2);
            }
        }
    }
}
