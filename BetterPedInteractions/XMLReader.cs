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
        internal static void ReadXMLs()
        {
            var menuCategoryObjects = new List<ParentCategory>();
            var defaultDirectory = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions\\Default";
            var customDirectory = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions\\Custom";
            
            LoadXMLs(defaultDirectory);
            LoadXMLs(customDirectory);
            //Game.LogTrivial($"MenuCategory objects: {menuCategoryObjects.Count()}");
            MenuManager.BuildMenus(menuCategoryObjects);

            void LoadXMLs(string directory)
            {
                foreach (string file in Directory.GetFiles(directory, "*.xml", SearchOption.AllDirectories))
                {
                    //Game.LogTrivial($"File: {Path.GetFileNameWithoutExtension(file)}");
                    XDocument document = XDocument.Load(file);
                    var menu = document.Root.Attribute("menu").Value;
                    //Game.LogTrivial($"Menu: {menu}");
                    var parsed = Enum.TryParse(menu, out Settings.Group group);
                    //Game.LogTrivial($"Parsed: {parsed}, Parsed menu: {group}");
                    var menuCategories = document.Descendants("MenuCategory");
                    //Game.LogTrivial($"Categories: {menuCategories.Count()}");

                    // For each menu category in the file
                    foreach(XElement category in menuCategories)
                    {
                        // Create new MenuCategory object
                        var newMenuCategory = new ParentCategory(category.Element("CategoryName"), group, Path.GetFileNameWithoutExtension(file));
                        //Game.LogTrivial($"Parent category file: {newMenuCategory.File}");
                        // Check for submenus
                        if (category.Elements("SubCategory").Any())
                        {
                            //Game.LogTrivial($"Sub categories: {category.Elements("SubCategory").Count()}");
                            foreach (XElement categoryType in category.Elements("SubCategory"))
                            {
                                var newSubCategory = new SubCategory(categoryType.Element("CategoryName"), newMenuCategory, Path.GetFileNameWithoutExtension(file));
                                //Game.LogTrivial($"Sub Category file: {newSubCategory.File}");
                                if(categoryType.Attribute("enableByDefault") != null)
                                {
                                    newSubCategory.Enabled = bool.Parse(categoryType.Attribute("enableByDefault").Value);
                                }
                                //Game.LogTrivial($"--Sub category: {newSubCategory.Name.Value}");
                                newSubCategory.MenuItems = CompileMenuItems(newSubCategory, categoryType);
                                newMenuCategory.SubCategories.Add(newSubCategory);
                            }
                        }
                        else
                        {
                            newMenuCategory.MenuItems = CompileMenuItems(newMenuCategory, category);
                        }
                        menuCategoryObjects.Add(newMenuCategory);
                    }
                }
            }
        }

        private static List<MenuItem> CompileMenuItems(Category category, XElement categoryType)
        {
            IEnumerable<XElement> menuItems;
            //Game.LogTrivial($"Category Type: {categoryType.Name}");
            //Game.LogTrivial($"Category Name: {category.Name.Value}");
            menuItems = categoryType.Elements("MenuItem");

            //Game.LogTrivial($"Menu items: {menuItems.Count()}");
            foreach (XElement menuItem in menuItems)
            {
                var newMenuItem = new MenuItem(menuItem);
                if(menuItem.Attribute("enableByDefault") != null)
                {
                    newMenuItem.Enabled = bool.Parse(menuItem.Attribute("enableByDefault").Value);
                }
                newMenuItem.Category = category;
                if (menuItem.Element("Level") != null)
                {
                    newMenuItem.Level = int.Parse(menuItem.Element("Level").Value);
                }
                newMenuItem.MenuPrompt = menuItem.Element("MenuPrompt");
                newMenuItem.Responses = menuItem.Elements("Response").ToList();
                //Game.LogTrivial($"---Prompt: {newMenuItem.MenuPrompt?.Value}, Level: {newMenuItem.Level}, Responses: {newMenuItem.Responses?.Count()}");
                category.MenuItems.Add(newMenuItem);

                if(newMenuItem.MenuPrompt != null)
                {
                    VocalInterface.Phrases.Add(newMenuItem.MenuPrompt.Value);
                    //VocalInterface.AudioPrompts.Add(newMenuItem.MenuPrompt.Value);
                    //Game.LogTrivial($"Phrase from menu prompt: {newMenuItem.MenuPrompt.Value}");
                    var audioPrompts = menuItem.Elements("AudioPrompt").Select(x => x.Value).ToList();
                    //VocalInterface.AudioPrompts.AddRange(audioPrompts);
                    foreach (string audioPrompt in audioPrompts)
                    {
                        //Game.LogTrivial($"Audio prompt: {audioPrompt}");
                        VocalInterface.Phrases.Add(audioPrompt);
                    }
                }
            }
            return category.MenuItems;
        }
    }
}
