using RAGENativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BetterPedInteractions
{
    class Category
    {
        public virtual string File { get; set; }
        public XElement Name { get; set; }
        public virtual UIMenu Menu { get; set; }
        internal int Level { get; set; } = 1;
        internal bool Enabled { get; set; } = true;
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

        internal Category() { }
    }

    class ParentCategory : Category
    {
        internal Settings.Group Group { get; set; }
        internal List<SubCategory> SubCategories { get; private set; } = new List<SubCategory>();
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

    class SubCategory : Category 
    {
        internal ParentCategory ParentCategory { get; set; }
        internal SubCategory(XElement name, ParentCategory parentCategory)
        {
            Name = name;
            ParentCategory = parentCategory;
        }
    }
}
