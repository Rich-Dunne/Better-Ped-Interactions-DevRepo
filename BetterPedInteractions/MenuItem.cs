using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Xml.Linq;

namespace BetterPedInteractions
{
    class MenuItem
    {
        internal XElement Element { get; set; }
        internal Category Category { get; set; }
        internal bool Enabled { get; set; } = true;
        internal int Level { get; set; } = 1;
        internal XElement MenuPrompt { get; set; }
        internal List<XElement> Responses { get; set; } = new List<XElement>();
        internal List<string> AudioPrompts { get; set; } = new List<string>();
        internal UIMenuItem Action { get; set; } = null;

        internal MenuItem(XElement element)
        {
            Element = element;
        }

        internal bool BelongsTo(Category category)
        {
            if (category.MenuItems.Contains(this))
            {
                return true;
            }
            return false;
        }
    }
}
