using BetterPedInteractions.Utils;
using Rage;
using RAGENativeUI;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BetterPedInteractions.Objects
{
    internal class DialogueMenu : UIMenu
    {
        internal Settings.Group Group { get; private set; }
        internal List<ParentCategory> ParentCategories { get; private set; } = new List<ParentCategory>();
        internal List<SubCategory> SubCategories { get; private set; } = new List<SubCategory>();
        internal List<MenuItem> AllMenuItems { get; private set; } = new List<MenuItem>();
        internal List<MenuItem> UsedMenuItems { get; private set; } = new List<MenuItem>();

        internal DialogueMenu(string title, string subtitle, Settings.Group group) : base("", "")
        {
            TitleText = title;
            SubtitleText = subtitle;
            Group = group;
            GetParentCategories();
            DeserializeParentCategories();
            AssignMenuToParentCategories();
            GetSubCategories();
            CompileAllMenuItems();
        }

        private void GetParentCategories() => XMLManager.GetParentCategories(Group).ForEach(x => ParentCategories.Add(x.DeepCopy()));

        private void DeserializeParentCategories() => XMLManager.Deserialize(ParentCategories);
        
        private void AssignMenuToParentCategories() => ParentCategories.ForEach(x => x.SetMenu(this));

        private void GetSubCategories() => SubCategories.AddRange(ParentCategories.SelectMany(x => x.SubCategories));

        private void CompileAllMenuItems()
        {
            AllMenuItems.AddRange(ParentCategories.SelectMany(x => x.MenuItems));
            AllMenuItems.AddRange(ParentCategories.SelectMany(x => x.SubCategories).SelectMany(y => y.MenuItems));
        }

        internal void IncreaseCategoryLevel(MenuItem matchingPrompt)
        {
            if (matchingPrompt.BelongsToSubCategory && matchingPrompt.Level == matchingPrompt.SubCategory.Level)
            {
                matchingPrompt.SubCategory.Level++;
            }
            else if (!matchingPrompt.BelongsToSubCategory && matchingPrompt.Level == matchingPrompt.ParentCategory.Level)
            {
                matchingPrompt.ParentCategory.Level++;
            }
        }

        internal void EnableDialoguePathFromPrompt(MenuItem menuItem)
        {
            if (!menuItem.IsMenuItemElementDefined("DialoguePathToEnableWhenSelected"))
            {
                Game.LogTrivial($"Element not defined for 'DialoguePathToEnableWhenSelected'");
                return;
            }

            string dialoguePathToEnable = menuItem.MenuText.Parent.Element("DialoguePathToEnableWhenSelected").Value;
            Game.LogTrivial($"DialoguePathToEnableWhenSelected: {dialoguePathToEnable}");
            
            var menuItemsWithMatchingDialoguePath = AllMenuItems.Where(x => x.IsMenuItemElementDefined("DialoguePath") && x.Element.Element("DialoguePath").Value == dialoguePathToEnable).ToList();       
            if (menuItemsWithMatchingDialoguePath.Count() <= 0)
            {
                Game.LogTrivial($"No matching menu items found with dialogue path: {menuItem.MenuText.Parent.Element("DialoguePathToEnableWhenSelected").Value}");
                return;
            }

            if (menuItem.IsAttributeDefined("DialoguePathToEnableWhenSelected", "enableGlobally") && bool.Parse(menuItem.Element.Element("DialoguePathToEnableWhenSelected").Attribute("enableGlobally").Value))
            {
                Game.LogTrivial($"Enabling dialogue path globally");
                var collectedPedsOfSameGroup = PedHandler.CollectedPeds.Where(x => x.Group == menuItem.ParentCategory.Group).ToList();
                List<MenuItem> matchingMenuItems = new List<MenuItem>();
                collectedPedsOfSameGroup.ForEach(x => matchingMenuItems.AddRange(x.Menu.AllMenuItems.Where(y => menuItemsWithMatchingDialoguePath.Contains(y))));
                matchingMenuItems.ForEach(x => x.Enable());
            }
            else
            {
                Game.LogTrivial($"Enabling dialogue path locally");
                menuItemsWithMatchingDialoguePath.ForEach(x => { if (!x.Enabled) x.Enable(); });
            }
        }

        internal void EnableCategoryFromPrompt(MenuItem menuItem)
        {
            if (!menuItem.IsMenuItemElementDefined("CategoryToEnableWhenSelected"))
            {
                Game.LogTrivial($"Element not defined for 'CategoryToEnableWhenSelected'");
                return;
            }

            string categoryToEnable = menuItem.Element.Element("CategoryToEnableWhenSelected").Value;
            Game.LogTrivial($"CategoryToEnableWhenSelected: {categoryToEnable}");

            var menuItemsWithMatchingCategory = AllMenuItems.FirstOrDefault(x => x.ParentCategory.Name == categoryToEnable);
            if(menuItemsWithMatchingCategory == null)
            {
                menuItemsWithMatchingCategory = AllMenuItems.FirstOrDefault(x => x.SubCategory.Name == categoryToEnable);
            }
            if (menuItemsWithMatchingCategory == null)
            {
                Game.LogTrivial($"No matching category found: {categoryToEnable}");
                return;
            }

            if (menuItem.IsAttributeDefined("CategoryToEnableWhenSelected", "enableGlobally") && bool.Parse(menuItem.Element.Element("CategoryToEnableWhenSelected").Attribute("enableGlobally").Value))
            {
                Game.LogTrivial($"Enabling category globally");
                var collectedPedsOfSameGroup = PedHandler.CollectedPeds.Where(x => x.Group == menuItem.ParentCategory.Group).ToList();
                collectedPedsOfSameGroup.ForEach(x => x.Menu.AllMenuItems.FirstOrDefault(y => y == menuItemsWithMatchingCategory).Enable());
            }
            else
            {
                Game.LogTrivial($"Enabling category locally");
                var menuItemWithMatchingCategory = AllMenuItems.FirstOrDefault(x => x == menuItemsWithMatchingCategory);
                if(menuItemWithMatchingCategory.ParentCategory.Name == categoryToEnable && !menuItemWithMatchingCategory.ParentCategory.Enabled)
                {
                    menuItemWithMatchingCategory.ParentCategory.Enabled = true;
                    Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nNew dialogue options unlocked:\nMenu: ~b~{menuItemWithMatchingCategory.ParentCategory.Menu.TitleText.Split(' ').First()}\n~w~Category: ~y~{menuItemWithMatchingCategory.ParentCategory.Name}");
                }
                if(menuItemWithMatchingCategory.SubCategory.Name == categoryToEnable && !menuItemWithMatchingCategory.SubCategory.Enabled)
                {
                    menuItemWithMatchingCategory.SubCategory.Enabled = true;
                    Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nNew dialogue options unlocked:\nMenu: ~b~{menuItemWithMatchingCategory.SubCategory.ParentCategory.Menu.TitleText.Split(' ').First()}\n~w~Category: ~y~{menuItemWithMatchingCategory.SubCategory.ParentCategory.Name}\n~w~Sub Category: ~g~{menuItemWithMatchingCategory.SubCategory.Name}");
                }
            }
        }

        internal void EnableDialoguePathFromResponse(XElement response)
        {
            if (response.Attribute("dialoguePathToEnable") == null || string.IsNullOrEmpty(response.Attribute("dialoguePathToEnable").Value))
            {
                Game.LogTrivial($"Response dialoguePathToEnable attribute is not defined.");
                return;
            }
            string dialoguePath = response.Attribute("dialoguePathToEnable").Value;
            Game.LogTrivial($"Response dialoguePathToEnable: {dialoguePath}");

            var menuItemsWithMatchingDialoguePath = AllMenuItems.Where(x => x.Element.Element("DialoguePath")?.Value == dialoguePath).ToList();
            if (menuItemsWithMatchingDialoguePath.Count() <= 0)
            {
                Game.LogTrivial($"No matching menu items found with dialogue path: {dialoguePath}");
                return;
            }

            if (response.Attribute("enableDialoguePathGlobally") != null || bool.Parse(response.Attribute("enableDialoguePathGlobally").Value))
            {
                Game.LogTrivial($"Enabling dialogue path globally from response");
                var collectedPedsOfSameGroup = PedHandler.CollectedPeds.Where(x => x.Group == PedHandler.FocusedPed.Group).ToList();
                List<MenuItem> matchingMenuItems = new List<MenuItem>();
                collectedPedsOfSameGroup.ForEach(x => matchingMenuItems.AddRange(x.Menu.AllMenuItems.Where(y => menuItemsWithMatchingDialoguePath.Contains(y))));
                matchingMenuItems.ForEach(x => x.Enable());
            }
            else
            {
                menuItemsWithMatchingDialoguePath.ForEach(x => x.Enable());
            }
        }

        internal void AddPromptToUsedMenuItems(MenuItem prompt)
        {
            if (!UsedMenuItems.Contains(prompt))
            {
                UsedMenuItems.Add(prompt);
            }
        }
    }
}
