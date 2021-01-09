using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BetterPedInteractions
{
    internal class XMLReader
    {
        internal static void ReadFromDirectory(string directory)
        {
            var parentCategories = new List<ParentCategory>();
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
                    var newParentCategory = new ParentCategory(parentCategoryElement.Element("CategoryName"), group, fileName);

                    // Check for submenus
                    if (parentCategoryElement.Elements("SubCategory").Any())
                    {
                        CreateSubCategories(parentCategoryElement, newParentCategory);
                    }
                    else
                    {
                        CreateMenuItems(parentCategoryElement, newParentCategory);
                    }
                    parentCategories.Add(newParentCategory);
                }
            }

            MenuManager.AddParentCategories(parentCategories);
            MenuManager.InitializeActionMenuItems();
            ResponseManager.AddParentCategories(parentCategories);
        }

        private static string GetMenuForFile(XDocument document) => document.Root.Attribute("menu").Value;

        private static IEnumerable<XElement> GetParentCategories(XDocument document) => document.Descendants("MenuCategory");

        private static Settings.Group ParseDocumentMenuToGroup(XDocument document)
        {
            string menu = GetMenuForFile(document);
            Enum.TryParse(menu, out Settings.Group group);
            return group;
        }

        private static void CreateSubCategories(XElement parentCategoryElement, ParentCategory newParentCategory)
        {
            IEnumerable<XElement> subCategories = parentCategoryElement.Elements("SubCategory");

            foreach (XElement subCategory in subCategories)
            {
                var newSubCategory = new SubCategory(subCategory.Element("CategoryName"), newParentCategory);
                if (subCategory.Attribute("enableByDefault") != null)
                {
                    newSubCategory.Enabled = bool.Parse(subCategory.Attribute("enableByDefault").Value);
                }

                CreateMenuItems(subCategory, newParentCategory, newSubCategory);
            }
        }

        private static void CreateMenuItems(XElement category, ParentCategory parentCategory = null, SubCategory subCategory = null)
        {
            var menuItems = category.Elements("MenuItem");

            foreach (XElement menuItem in menuItems)
            {
                MenuItem newMenuItem;
                if(subCategory != null)
                {
                    newMenuItem = new MenuItem(menuItem, subCategory.ParentCategory, subCategory);
                    newMenuItem.MenuPrompt = menuItem.Element("MenuPrompt");
                    subCategory.MenuItems.Add(newMenuItem);
                }
                else
                {
                    newMenuItem = new MenuItem(menuItem, parentCategory);
                    newMenuItem.MenuPrompt = menuItem.Element("MenuPrompt");
                    parentCategory.MenuItems.Add(newMenuItem);
                }
                if (menuItem.Attribute("enableByDefault") != null)
                {
                    newMenuItem.Enabled = bool.Parse(menuItem.Attribute("enableByDefault").Value);
                }
                if (menuItem.Element("Level") != null)
                {
                    newMenuItem.Level = int.Parse(menuItem.Element("Level").Value);
                }
                if(newMenuItem.MenuPrompt != null)
                {
                    VocalInterface.AudioPrompts.Add(newMenuItem.MenuPrompt.Value);
                }
                newMenuItem.Responses = menuItem.Elements("Response").ToList();
                var audioPrompts = menuItem.Elements("AudioPrompt").ToList();
                if(audioPrompts.Count > 0)
                {
                    audioPrompts.ForEach(x => newMenuItem.AudioPrompts.Add(x.Value));
                    audioPrompts.ForEach(x => VocalInterface.AudioPrompts.Add(x.Value));
                }
            }
        }
    }
}
