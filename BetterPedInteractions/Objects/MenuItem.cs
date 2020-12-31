using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Xml.Linq;

namespace BetterPedInteractions
{
    class MenuItem
    {
        internal XElement Element { get; set; }
        internal ParentCategory ParentCategory { get; set; }
        internal SubCategory SubCategory { get; set; }
        internal bool HasSubCategory { get; private set; } = false;
        internal bool Enabled { get; set; } = true;
        internal int Level { get; set; } = 1;
        internal XElement MenuPrompt { get; set; }
        internal List<XElement> Responses { get; set; } = new List<XElement>();
        internal List<string> AudioPrompts { get; set; } = new List<string>();
        internal UIMenuItem Action { get; set; } = null;
        internal bool Highlighted { get; set; } = false;

        internal MenuItem(XElement element, ParentCategory parentCategory, SubCategory subCategory = null)
        {
            Element = element;
            ParentCategory = subCategory != null ? subCategory.ParentCategory : parentCategory;
            SubCategory = subCategory;
            if(subCategory != null)
            {
                HasSubCategory = true;
            }
        }
    }
}
