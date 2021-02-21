using Rage;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BetterPedInteractions
{
    class MenuItem
    {
        internal XElement Element { get; set; }
        internal ParentCategory ParentCategory { get; set; }
        internal SubCategory SubCategory { get; set; }
        internal bool BelongsToSubCategory { get; private set; } = false;
        internal bool Enabled { get; set; } = true;
        internal int Level { get; set; } = 1;
        internal XElement MenuText { get; set; }
        internal List<XElement> Responses { get; set; } = new List<XElement>();
        internal List<string> AudioPrompts { get; set; } = new List<string>();
        internal Settings.Actions Action { get; set; } = Settings.Actions.None;
        internal UIMenuItem UIMenuItem { get; set; }
        internal UIMenuItem.BadgeStyle BadgeStyle { get; set; } = UIMenuItem.BadgeStyle.Star;

        internal MenuItem(XElement element, ParentCategory parentCategory, SubCategory subCategory = null)
        {
            Element = element;
            ParentCategory = subCategory != null ? subCategory.ParentCategory : parentCategory;
            SubCategory = subCategory;
            BelongsToSubCategory = subCategory != null ? true : false;
            MenuText = element.Element("MenuText");

            AssignAction();
            SetEnable();
            AssignLevel();
            AssignMenuTextAsAudioPrompt();
            AssignResponses();
            AssignAudioPrompts();
        }

        internal void Enable()
        {
            Enabled = true;
            Game.LogTrivial($"menuItem.Enabled: {MenuText.Value}");

            if (!ParentCategory.Enabled)
            {
                ParentCategory.Enabled = true;
                Game.LogTrivial($"Enabled ParentCategory {ParentCategory.Name}");
                if (!BelongsToSubCategory)
                {
                    Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nNew dialogue options unlocked:\nMenu: ~b~{ParentCategory.Menu.TitleText.Split(' ').First()}\n~w~Category: ~y~{ParentCategory.Name}");
                }
            }
            if (BelongsToSubCategory && !SubCategory.Enabled)
            {
                SubCategory.Enabled = true;
                Game.LogTrivial($"Enabled SubCategory {SubCategory.Name}");
                Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nNew dialogue options unlocked:\nMenu: ~b~{ParentCategory.Menu.TitleText.Split(' ').First()}\n~w~Category: ~y~{ParentCategory.Name}\n~w~Sub Category: ~g~{SubCategory.Name}");
            }
        }

        private void AssignAction()
        {
            if (Element.Elements("Action").Any() && !string.IsNullOrEmpty(Element.Element("Action").Value))
            {
                Enum.TryParse(Element.Element("Action").Value, out Settings.Actions action);
                Action = action;
            }
        }

        private void SetEnable()
        {
            if (Element.Elements("EnableItemByDefault").Any() && !string.IsNullOrEmpty(Element.Element("EnableItemByDefault").Value))
            {
                bool.TryParse(Element.Element("EnableItemByDefault").Value, out bool result);
                Enabled = result;
            }
        }

        private void AssignLevel()
        {
            if (Element.Elements("Level").Any() && !string.IsNullOrEmpty(Element.Element("Level").Value))
            {
                int.TryParse(Element.Element("Level").Value, out int result);
                Level = result;
            }
        }

        private void AssignMenuTextAsAudioPrompt()
        {
            if (MenuText != null && !VocalInterface.AudioPrompts.Contains(MenuText.Value))
            {
                VocalInterface.AudioPrompts.Add(MenuText.Value);
            }
        }

        private void AssignResponses() => Responses = Element.Elements("Response").ToList();

        private void AssignAudioPrompts()
        {
            var audioPrompts = Element.Elements("AudioPrompt").ToList();
            if (audioPrompts.Count > 0)
            {
                audioPrompts.ForEach(x => AudioPrompts.Add(x.Value));
                audioPrompts.ForEach(x => VocalInterface.AudioPrompts.Add(x.Value));
            }
        }
    }
}
