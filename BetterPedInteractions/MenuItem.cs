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
        internal List<XElement> Responses { get; set; }
        internal UIMenuItem Action { get; set; }

        internal MenuItem(XElement element)
        {
            Element = element;
        }
    }
}
