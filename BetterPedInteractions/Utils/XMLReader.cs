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
                    foreach(XElement parentCategory in menuCategories)
                    {
                        // Create new ParentCategory
                        var newParentCategory = new ParentCategory(parentCategory.Element("CategoryName"), group, Path.GetFileNameWithoutExtension(file));
                        //Game.LogTrivial($"Parent category file: {newMenuCategory.File}");
                        // Check for submenus
                        if (parentCategory.Elements("SubCategory").Any())
                        {
                            //Game.LogTrivial($"Sub categories: {category.Elements("SubCategory").Count()}");
                            foreach (XElement subCategory in parentCategory.Elements("SubCategory"))
                            {
                                var newSubCategory = new SubCategory(subCategory.Element("CategoryName"), newParentCategory);
                                if(subCategory.Attribute("enableByDefault") != null)
                                {
                                    newSubCategory.Enabled = bool.Parse(subCategory.Attribute("enableByDefault").Value);
                                    //Game.LogTrivial($"Sub category [{newSubCategory.Name.Value}] enabled: {newSubCategory.Enabled}");
                                }
                                //Game.LogTrivial($"Sub category: {newSubCategory.Name.Value}");
                                CompileMenuItems(subCategory, newParentCategory, newSubCategory);
                                newParentCategory.SubCategories.Add(newSubCategory);
                            }
                        }
                        else
                        {
                            CompileMenuItems(parentCategory, newParentCategory);
                        }
                        menuCategoryObjects.Add(newParentCategory);
                    }
                }
            }
        }

        private static void CompileMenuItems(XElement category, ParentCategory parentCategory = null, SubCategory subCategory = null)
        {
            //Game.LogTrivial($"Parent category: {parentCategory?.Name.Value}");
            //Game.LogTrivial($"Sub category: {subCategory?.Name.Value}");
            var menuItems = category.Elements("MenuItem");
            //Game.LogTrivial($"Menu items: {menuItems.Count()}");

            foreach (XElement menuItem in menuItems)
            {
                MenuItem newMenuItem;
                if(subCategory != null)
                {
                    newMenuItem = new MenuItem(menuItem, subCategory.ParentCategory, subCategory);
                    newMenuItem.MenuPrompt = menuItem.Element("MenuPrompt");
                    subCategory.MenuItems.Add(newMenuItem);
                    //Game.LogTrivial($"Added {newMenuItem.MenuPrompt.Value} to {newMenuItem.SubCategory.Name.Value}");
                }
                else
                {
                    newMenuItem = new MenuItem(menuItem, parentCategory);
                    newMenuItem.MenuPrompt = menuItem.Element("MenuPrompt");
                    parentCategory.MenuItems.Add(newMenuItem);
                    //Game.LogTrivial($"Added {newMenuItem.MenuPrompt.Value} to {newMenuItem.ParentCategory.Name.Value}");
                }
                if (menuItem.Attribute("enableByDefault") != null)
                {
                    newMenuItem.Enabled = bool.Parse(menuItem.Attribute("enableByDefault").Value);
                }
                //Game.LogTrivial($"newMenuItem Category: {newMenuItem.Category.GetType()}");
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
                //Game.LogTrivial($"---Prompt: {newMenuItem.MenuPrompt?.Value}, Level: {newMenuItem.Level}, Responses: {newMenuItem.Responses?.Count()}");
                //if (subCategory != null)
                //{
                //    subCategory.MenuItems.Add(newMenuItem);
                //}
                //else
                //{
                //    parentCategory.MenuItems.Add(newMenuItem);
                //}
            }
            //return category.MenuItems;
        }
    }
}
