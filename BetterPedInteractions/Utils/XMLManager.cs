using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BetterPedInteractions
{
    internal class XMLManager
    {
        private static List<ParentCategory> ParentCategories { get; set; } = new List<ParentCategory>();

        internal static void ReadXMLsFromDirectory(string directory)
        {
            string[] directoryFiles = Directory.GetFiles(directory, "*.xml", SearchOption.AllDirectories);
            foreach (string file in directoryFiles)
            {
                XDocument document = XDocument.Load(file);
                Settings.Group group = ParseDocumentMenuToGroup(document);
                IEnumerable<XElement> parentCategoryElements = GetParentCategories(document);

                // For each menu category in the file
                foreach (XElement parentCategoryElement in parentCategoryElements)
                {
                    // Create new ParentCategory
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    var newParentCategory = new ParentCategory(parentCategoryElement, group, fileName);
                    ParentCategories.Add(newParentCategory);
                }
            }
        }

        internal static void Deserialize(List<ParentCategory> parentCategories)
        {
            foreach (ParentCategory parentCategory in parentCategories)
            {
                foreach (XElement subCategory in parentCategory.Element.Elements("SubCategory"))
                {
                    var newSubCategory = new SubCategory(subCategory, parentCategory);

                    foreach (XElement menuItem in subCategory.Elements("MenuItem"))
                    {
                        newSubCategory.MenuItems.Add(new MenuItem(menuItem, parentCategory, newSubCategory));
                    }
                }
                foreach (XElement menuItem in parentCategory.Element.Elements("MenuItem"))
                {
                    parentCategory.MenuItems.Add(new MenuItem(menuItem, parentCategory));
                }
            }
        }

        private static string GetMenuForFile(XDocument document) => document.Root.Attribute("menu").Value;

        private static IEnumerable<XElement> GetParentCategories(XDocument document) => document.Descendants("MenuCategory");

        private static Settings.Group ParseDocumentMenuToGroup(XDocument document)
        {
            string menu = GetMenuForFile(document);
            Enum.TryParse(menu, out Settings.Group group);
            return group;
        }

        internal static List<ParentCategory> GetParentCategories(Settings.Group group) => ParentCategories.Where(x => x.Group == group).ToList();
    }
}
