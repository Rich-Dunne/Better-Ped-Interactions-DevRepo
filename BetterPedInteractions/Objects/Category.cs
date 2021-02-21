using Rage;
using RAGENativeUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BetterPedInteractions
{
    internal class Category
    {
        public string File { get; protected set; }
        public XElement Element { get; protected set; }
        public string Name { get; protected set; }
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
        internal ParentCategory(XElement element, Settings.Group group, string file)
        {
            Element = element;
            Name = element.Element("CategoryName").Value;
            Group = group;
            File = file;
        }

        internal ParentCategory(string file, XElement element, string name, UIMenu menu, int level, bool enabled, Settings.Group group, List<SubCategory> subCategories, bool hasSubCategory, List<MenuItem> menuItems)
        {
            File = file;
            Element = element;
            Name = name;
            Menu = menu;
            Level = level;
            Enabled = enabled;
            Group = group;
            SubCategories = new List<SubCategory>();
            HasSubCategory = hasSubCategory;
            MenuItems = new List<MenuItem>();
        }

        internal void SetMenu(UIMenu menu) => Menu = menu;

        internal ParentCategory DeepCopy()
        {
            ParentCategory parentCategoryCopy = new ParentCategory(File, Element, Name, Menu, Level, Enabled, Group, SubCategories, HasSubCategory, MenuItems);
            return parentCategoryCopy;
        }
    }

    internal class SubCategory : Category
    {
        internal ParentCategory ParentCategory { get; set; }
        internal SubCategory(XElement element, ParentCategory parentCategory)
        {
            Element = element;
            Name = element.Element("CategoryName").Value;
            if (Element.Elements("EnableCategoryByDefault").Any())
            {
                bool.TryParse(Element.Element("EnableCategoryByDefault").Value.ToLower(), out bool result);
                Enabled = result;
            }
            ParentCategory = parentCategory;
            ParentCategory.SubCategories.Add(this);
            ParentCategory.HasSubCategory = true;
        }
    }
}
