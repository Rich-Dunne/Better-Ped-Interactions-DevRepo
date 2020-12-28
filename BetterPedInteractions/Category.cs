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
        public override string File
        {
            get { return base.File; }
            set
            {
                base.File = value;
            }
        }
        public override UIMenu Menu
        {
            get { return base.Menu; }
            set
            {
                base.Menu = value;
            }
        }
        internal Settings.Group Group { get; set; }
        internal List<SubCategory> SubCategories { get; private set; } = new List<SubCategory>();
        //internal new List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        internal ParentCategory(XElement name, Settings.Group menu, string file)
        {
            Name = name;
            Group = menu;
            if(menu == Settings.Group.Civilian)
            {
                Menu = MenuManager.CivMenu;
            }
            else if (menu == Settings.Group.Cop)
            {
                Menu = MenuManager.CopMenu;
            }
            //Menu = menu;
            File = file;
        }
    }

    class SubCategory : Category 
    {
        public override string File
        {
            get { return base.File; }
            set
            {
                base.File = value;
            }
        }
        public override UIMenu Menu
        {
            get { return base.Menu; }
            set
            {
                base.Menu = value;
            }
        }
        internal ParentCategory ParentCategory { get; set; }
        internal SubCategory(XElement name, ParentCategory parentCategory, string file)
        {
            Name = name;
            ParentCategory = parentCategory;
            File = file;
        }
    }
}
