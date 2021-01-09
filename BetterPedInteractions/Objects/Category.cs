using RAGENativeUI;
using System.Collections.Generic;
using System.Xml.Linq;

namespace BetterPedInteractions
{
    internal class Category
    {
        public string File { get; protected set; }
        public XElement Name { get; protected set; }
        public UIMenu Menu { get; protected set; }
        internal int Level { get; set; } = 1;
        internal bool Enabled { get; set; } = true;
        internal List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

        internal Category() { }
    }

    internal class ParentCategory : Category
    {
        internal Settings.Group Group { get; set; }
        internal List<SubCategory> SubCategories { get; private set; } = new List<SubCategory>();
        internal bool HasSubCategory { get; set; } = false;
        internal ParentCategory(XElement name, Settings.Group group, string file)
        {
            Name = name;
            Group = group;
            if(group == Settings.Group.Civilian)
            {
                Menu = MenuManager.CivMenu;
            }
            else if (group == Settings.Group.Cop)
            {
                Menu = MenuManager.CopMenu;
            }
            File = file;
        }
    }

    internal class SubCategory : Category 
    {
        internal ParentCategory ParentCategory { get; set; }
        internal SubCategory(XElement name, ParentCategory parentCategory)
        {
            Name = name;
            ParentCategory = parentCategory;
            ParentCategory.SubCategories.Add(this);
            ParentCategory.HasSubCategory = true;
        }
    }
}
