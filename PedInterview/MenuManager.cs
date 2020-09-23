using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;

namespace PedInterview
{
    class MenuManager
    {
        public static MenuPool menuPool = new MenuPool();
        private static UIMenu civMainMenu, copMainMenu;
        private static UIMenuItem questionItem;
        private static UIMenuListScrollerItem<string> civQuestionCategories, copQuestionCategories;

        internal static UIMenu BuildCivMenu(Dictionary<string, Dictionary<string, List<string>>> civQuestionsAndAnswers)
        {
            civMainMenu = new UIMenu("Civilian Ped Interview", "");
            menuPool.Add(civMainMenu);

            civMainMenu.AddItem(civQuestionCategories = new UIMenuListScrollerItem<string>("Category", "The category of the questions", civQuestionsAndAnswers.Keys));
            foreach (KeyValuePair<string, Dictionary<string, List<string>>> kvp in civQuestionsAndAnswers)
            {
                //Game.LogTrivial($"kvp.key: {kvp.Key}");
                //Game.LogTrivial($"civQuestionCategories.SelectedItem: {civQuestionCategories.SelectedItem}");
                if (kvp.Key == civQuestionCategories.SelectedItem)
                {
                    foreach (KeyValuePair<string, List<string>> kvp2 in kvp.Value)
                    {
                        //Game.LogTrivial($"kvp2.key: {kvp2.Key}");
                        civMainMenu.AddItem(questionItem = new UIMenuItem(kvp2.Key));
                    }
                }
            }
            civMainMenu.RefreshIndex();

            civMainMenu.Width = SetMenuWidth(civMainMenu);

            civMainMenu.MouseControlsEnabled = false;
            civMainMenu.AllowCameraMovement = true;

            civMainMenu.OnItemSelect += CivInteract_OnItemSelected;
            civMainMenu.OnScrollerChange += CivInteract_OnScrollerChanged;

            return civMainMenu;

            void CivInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
            {
                foreach (KeyValuePair<string, Dictionary<string, List<string>>> kvp in civQuestionsAndAnswers)
                {
                    foreach (KeyValuePair<string, List<string>> kvp2 in kvp.Value)
                    {
                        if (kvp2.Key == selectedItem.Text)
                        {
                            Random r = new Random();
                            int i = r.Next(kvp2.Value.Count);
                            //Game.DisplaySubtitle($"Count: {kvp2.Value.Count}, Response: {kvp2.Value[i]}");
                            Game.DisplaySubtitle($"{kvp2.Value[i]}");
                        }
                    }
                }
            }

            void CivInteract_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
            {
                while (civMainMenu.MenuItems.Count > 1)
                {
                    civMainMenu.RemoveItemAt(1);
                }

                foreach (KeyValuePair<string, Dictionary<string, List<string>>> kvp in civQuestionsAndAnswers)
                {
                    //Game.LogTrivial($"kvp.key: {kvp.Key}");
                    //Game.LogTrivial($"Scroller text: {scroller.Text}");
                    //Game.LogTrivial($"civQuestionCategories.SelectedItem: {civQuestionCategories.SelectedItem}");
                    if (kvp.Key == civQuestionCategories.SelectedItem)
                    {
                        foreach (KeyValuePair<string, List<string>> kvp2 in kvp.Value)
                        {
                            //Game.LogTrivial($"kvp2.key: {kvp2.Key}");
                            civMainMenu.AddItem(questionItem = new UIMenuItem(kvp2.Key));
                        }
                    }
                }
                civMainMenu.Width = SetMenuWidth(civMainMenu);
            }
        }

        internal static UIMenu BuildCopMenu(Dictionary<string, Dictionary<string, List<string>>> copQuestionsAndAnswers)
        {
            copMainMenu = new UIMenu("Cop Ped Interview", "");
            menuPool.Add(copMainMenu);

            copMainMenu.AddItem(copQuestionCategories = new UIMenuListScrollerItem<string>("Category", "The category of the questions", copQuestionsAndAnswers.Keys));
            foreach (KeyValuePair<string, Dictionary<string, List<string>>> kvp in copQuestionsAndAnswers)
            {
                //Game.LogTrivial($"kvp.key: {kvp.Key}");
                //Game.LogTrivial($"civQuestionCategories.SelectedItem: {civQuestionCategories.SelectedItem}");
                if (kvp.Key == copQuestionCategories.SelectedItem)
                {
                    foreach (KeyValuePair<string, List<string>> kvp2 in kvp.Value)
                    {
                        //Game.LogTrivial($"kvp2.key: {kvp2.Key}");
                        copMainMenu.AddItem(questionItem = new UIMenuItem(kvp2.Key));
                    }
                }
            }
            copMainMenu.RefreshIndex();

            copMainMenu.Width = SetMenuWidth(copMainMenu);

            copMainMenu.MouseControlsEnabled = false;
            copMainMenu.AllowCameraMovement = true;

            copMainMenu.OnItemSelect += CopInteract_OnItemSelected;
            copMainMenu.OnScrollerChange += CopInteract_OnScrollerChanged;

            return copMainMenu;

            void CopInteract_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
            {
                foreach (KeyValuePair<string, Dictionary<string, List<string>>> kvp in copQuestionsAndAnswers)
                {
                    foreach (KeyValuePair<string, List<string>> kvp2 in kvp.Value)
                    {
                        if (kvp2.Key == selectedItem.Text)
                        {
                            Random r = new Random();
                            int i = r.Next(kvp2.Value.Count);
                            //Game.DisplaySubtitle($"Count: {kvp2.Value.Count}, Response: {kvp2.Value[i]}");
                            Game.DisplaySubtitle($"{kvp2.Value[i]}");
                        }
                    }
                }
            }

            void CopInteract_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scroller, int prevIndex, int newIndex)
            {
                while (copMainMenu.MenuItems.Count > 1)
                {
                    copMainMenu.RemoveItemAt(1);
                }

                foreach (KeyValuePair<string, Dictionary<string, List<string>>> kvp in copQuestionsAndAnswers)
                {
                    //Game.LogTrivial($"kvp.key: {kvp.Key}");
                    //Game.LogTrivial($"Scroller text: {scroller.Text}");
                    //Game.LogTrivial($"civQuestionCategories.SelectedItem: {civQuestionCategories.SelectedItem}");
                    if (kvp.Key == copQuestionCategories.SelectedItem)
                    {
                        foreach (KeyValuePair<string, List<string>> kvp2 in kvp.Value)
                        {
                            //Game.LogTrivial($"kvp2.key: {kvp2.Key}");
                            copMainMenu.AddItem(questionItem = new UIMenuItem(kvp2.Key));
                        }
                    }
                }
                copMainMenu.Width = SetMenuWidth(copMainMenu);
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

                width = Math.Max(textWidth + padding, UIMenu.DefaultWidth);
                // Minimum width is set to prevent the scroller from clipping the menu item name
                if (width < 0.25)
                {
                    width = 0.25f;
                }
            }

            return width;
        }
    }
}
