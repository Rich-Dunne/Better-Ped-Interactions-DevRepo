using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BetterPedInteractions.Utils;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using static BetterPedInteractions.Settings;

namespace BetterPedInteractions
{
    class ResponseManager
    {
        private static List<ParentCategory> ParentCategories { get; } = new List<ParentCategory>();

        internal static void AddParentCategories(List<ParentCategory> parentCategories) => ParentCategories.AddRange(parentCategories);

        internal static void FindMatchingPrompt(string prompt)
        {
            var allMenuItems = GetAllMenuItems();
            MenuItem matchingPrompt = GetMatchingPrompt();
            if(matchingPrompt == null)
            {
                Game.LogTrivial($"No matching prompt found.");
                return;
            }

            UpdateDialogueOptionsFromPrompt(matchingPrompt);
            ChoosePedResponse(matchingPrompt);

            MenuItem GetMatchingPrompt()
            {
                var match = allMenuItems.FirstOrDefault(x => (x.MenuPrompt?.Value == prompt || x.AudioPrompts.Contains(prompt)) && !x.BelongsToSubCategory);
                if (match == null)
                {
                    match = allMenuItems.FirstOrDefault(x => (x.MenuPrompt?.Value == prompt || x.AudioPrompts.Contains(prompt)) && x.BelongsToSubCategory);
                }
                return match != null ? match : null;
            }
        }

        private static void UpdateDialogueOptionsFromPrompt(MenuItem matchingPrompt)
        {
            IncrementCategoryLevel();

            EnableDialoguePathFromPrompt();

            EnableCategoryFromPrompt();

            void IncrementCategoryLevel()
            {
                if(matchingPrompt.BelongsToSubCategory && matchingPrompt.Level == matchingPrompt.SubCategory.Level)
                {
                    matchingPrompt.SubCategory.Level++;
                }
                else if (matchingPrompt.Level == matchingPrompt.ParentCategory.Level)
                {
                    matchingPrompt.ParentCategory.Level++;
                }
            }

            void EnableDialoguePathFromPrompt()
            {
                if (matchingPrompt.MenuPrompt.Attribute("enablesDialoguePath") != null)
                {
                    //Game.LogTrivial($"This prompt should unlock a menu");
                    // Check for matching main category
                    var menuItems = GetAllMenuItems();
                    var menuItemsWithMatchingDialoguePath = menuItems.Where(x => x.MenuPrompt.Attribute("dialoguePath") != null && x.MenuPrompt.Attribute("dialoguePath").Value == matchingPrompt.MenuPrompt.Attribute("enablesDialoguePath").Value);

                    if (menuItemsWithMatchingDialoguePath.Count() > 0)
                    {
                        foreach (MenuItem menuItem in menuItemsWithMatchingDialoguePath)
                        {
                            menuItem.Enabled = true;
                        }
                    }
                    else
                    {
                        Game.LogTrivial($"No matching menu items found with dialogue path: {matchingPrompt.MenuPrompt.Attribute("enablesDialoguePath").Value}");
                    }
                }
            }

            void EnableCategoryFromPrompt()
            {
                if (matchingPrompt.MenuPrompt.Attribute("enablesCategory") != null)
                {
                    //Game.LogTrivial($"This prompt should unlock a menu");
                    // Check for matching main category
                    var matchingCategoryToEnable = ParentCategories.FirstOrDefault(x => x.Name.Value == matchingPrompt.MenuPrompt.Attribute("enablesCategory").Value);

                    // Check for matching sub category
                    if (matchingCategoryToEnable != null)
                    {
                        matchingCategoryToEnable.Enabled = true;
                    }
                    else
                    {
                        var matchingSubCategoryToEnable = ParentCategories.SelectMany(x => x.SubCategories).FirstOrDefault(x => x.Name.Value == matchingPrompt.MenuPrompt.Attribute("enablesCategory").Value);
                        if (matchingSubCategoryToEnable != null)
                        {
                            matchingSubCategoryToEnable.Enabled = true;
                        }
                        else
                        {
                            Game.LogTrivial($"No matching category found: {matchingPrompt.MenuPrompt.Attribute("enablesCategory").Value}");
                        }
                    }
                }
            }
        }

        private static void ChoosePedResponse(MenuItem prompt)
        {
            if(prompt.Action != null)
            {
                PerformPedAction();
                return;
            }

            var focusedPed = PedHandler.FocusedPed;
            Game.LogTrivial($"Focused ped: {focusedPed.Ped.Model.Name}");
            XElement response = null;

            GetResponse();
            SetPedResponseHonesty();
            DisplayResponse();
            AddQuestionResponsePairToUsedBank();
            UpdateDialogueOptionsFromResponse(prompt, response);

            if (focusedPed.Group == Settings.Group.Civilian && EnableAgitation)
            {
                AdjustPedAgitation();
            }

            if (prompt.ParentCategory.Menu == MenuManager.CivMenu)
            {
                MenuManager.PopulateMenu(prompt.ParentCategory.Menu, MenuManager.CivParentCategoryScroller);
            }
            else if (prompt.ParentCategory.Menu == MenuManager.CopMenu)
            {
                MenuManager.PopulateMenu(prompt.ParentCategory.Menu, MenuManager.CopParentCategoryScroller);
            }

            void PerformPedAction()
            {
                // TODO: This currently only works for Follow action, needs to work for every checkbox action
                if (prompt.Action.GetType() == typeof(UIMenuCheckboxItem))
                {
                    var actionMenuItem = prompt.ParentCategory.Menu.MenuItems.FirstOrDefault(x => x.Text == prompt.Action.Text) as UIMenuCheckboxItem;
                    actionMenuItem.Checked = !actionMenuItem.Checked;
                    if (actionMenuItem.Checked)
                    {
                        PedHandler.FocusedPed.FollowMe();
                    }
                    else
                    {
                        PedHandler.FocusedPed.StopFollowing();
                    }
                }
                Game.LogTrivial($"Prompt is a ped action.  We don't need a response.");
            }

            void GetResponse()
            {
                // Assign a response (75% chance to match previous response truth/lie attribute) and add it to _usedResponses
                // If this is the ped's first response OR if this is not the first response and they meet the 25% chance requirement to divert from their initial response type, choose a random response
                if (focusedPed.ResponseHonesty == ResponseHonesty.Unspecified || (focusedPed.ResponseHonesty != ResponseHonesty.Unspecified && GetResponseChance() == 3))
                {
                    Game.LogTrivial($"First, deviated, or unspecified honesty response");
                    response = prompt.Responses[GetRandomResponseValue()];
                }
                // If this is not the ped's first response, choose a response that matches their initial response's type
                else if (focusedPed.ResponseHonesty != ResponseHonesty.Unspecified && GetResponseChance() < 3)
                {
                    Game.LogTrivial($"Follow-up response");
                    // Response is null when the ped's ResponseHonesty is defined, but there are no responses without a honesty attribute
                    response = prompt.Responses.FirstOrDefault(x => x.Attribute("honesty")?.Value.ToLower() == focusedPed.ResponseHonesty.ToString().ToLower());
                    if (response == null)
                    {
                        response = prompt.Responses[GetRandomResponseValue()];
                    }
                }
            }

            int GetResponseChance() => new Random().Next(0, 4);

            int GetRandomResponseValue() => new Random().Next(prompt.Responses.Count);

            void SetPedResponseHonesty()
            {
                if (response.Attribute("honesty") != null)
                {
                    var honestyAttribute = response.Attribute("honesty").Value;
                    //Game.LogTrivial($"Response type: {honestyAttribute}");
                    honestyAttribute = char.ToUpper(honestyAttribute[0]) + honestyAttribute.Substring(1);
                    //Game.LogTrivial($"Response type after char.ToUpper: {honestyAttribute}");
                    var parsed = Enum.TryParse(honestyAttribute, out ResponseHonesty responseHonesty);
                    //Game.LogTrivial($"Response type parsed: {parsed}, Response type: {responseHonesty}");
                    if (parsed)
                    {
                        focusedPed.ResponseHonesty = responseHonesty;
                    }
                }
            }

            void DisplayResponse()
            {
                if (!focusedPed.StoppedTalking)
                {
                    if(focusedPed.Group == Settings.Group.Civilian)
                    {
                        Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~{response.Value}");
                    }
                    else if (focusedPed.Group == Settings.Group.Cop)
                    {
                        Game.DisplaySubtitle($"~y~Officer: ~w~{response.Value}");
                    }
                    PlayLipAnimation();
                    return;
                }
            }

            void PlayLipAnimation()
            {
                focusedPed.Ped.Tasks.PlayAnimation("mp_facial", "mic_chatter", 1.0f, AnimationFlags.None);
                GameFiber.StartNew(() =>
                {
                    var numberOfWords = response.Value.Split();
                    //Game.LogTrivial($"Response: {response.Value}, number of words: {numberOfWords.Length}");
                    var timer = 0;
                    GameFiber.Sleep(500);
                    while (timer < (numberOfWords.Length * 10))
                    {
                        timer += 1;
                        GameFiber.Yield();
                    }
                    //Game.DisplayHelp("Stop animation");
                    if (focusedPed.Ped && focusedPed.Ped.IsAlive)
                    {
                        focusedPed.Ped.Tasks.Clear();
                    }
                });
            }

            void AddQuestionResponsePairToUsedBank()
            {
                // Add the question and response pair to a list of already-asked questions.
                if (!focusedPed.UsedQuestions.ContainsKey(prompt.MenuPrompt))
                {
                    focusedPed.UsedQuestions.Add(prompt.MenuPrompt, response);
                }
            }

            void AdjustPedAgitation()
            {
                if (prompt.MenuPrompt.Attributes().Any(x => x.Name == "type"))
                {
                    if (prompt.MenuPrompt.Attribute("type").Value == "interview")
                    {
                        focusedPed.DecreaseAgitation();
                    }
                    else if (prompt.MenuPrompt.Attribute("type").Value == "interrogation")
                    {
                        focusedPed.IncreaseAgitation();
                    }
                }
            }
        }

        private static void UpdateDialogueOptionsFromResponse(MenuItem prompt, XElement response)
        {
            var recentlyEnabledCategories = new List<Category>();
            EnableCategory();
            EnableDialoguePath();

            void EnableCategory()
            {
                if (prompt.Element.Attribute("enablesCategory") != null)
                {
                    var allMenuItems = GetAllMenuItems();
                    var menuItemsWithCategoriesToEnable = allMenuItems.Where(x => (x.ParentCategory.Name.Value == prompt.Element.Attribute("enablesCategory").Value || x.SubCategory?.Name.Value == prompt.Element.Attribute("enablesCategory").Value) && x.ParentCategory.File == prompt.ParentCategory.File).Distinct();
                    var parentCategoriesToEnable = menuItemsWithCategoriesToEnable.Where(x => !x.ParentCategory.Enabled).Select(x => x.ParentCategory).ToList();
                    var subCategoriesToEnable = menuItemsWithCategoriesToEnable.Where(x => x.SubCategory != null && !x.SubCategory.Enabled).Select(x => x.SubCategory).ToList();
                    //Game.LogTrivial($"Categories to be enabled: {categoriesToBeEnabled.Count()}");
                    foreach (ParentCategory parentCategory in parentCategoriesToEnable)
                    {
                        //Game.LogTrivial($"Category: {category.Name.Value}, File: {category.File}");
                        recentlyEnabledCategories.Add(parentCategory);
                        parentCategory.Enabled = true;
                        Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nCategory unlocked with new dialogue options:\nMenu: ~b~{parentCategory.Menu.TitleText.Split(' ').First()}\n~w~Category: ~y~{parentCategory.Name.Value}");
                    }
                    foreach (SubCategory subCategory in subCategoriesToEnable)
                    {
                        recentlyEnabledCategories.Add(subCategory);
                        subCategory.Enabled = true;
                        Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nSub Category unlocked with new dialogue options:\nMenu: ~b~{subCategory.ParentCategory.Menu.TitleText.Split(' ').First()}\n~w~Category: ~y~{subCategory.ParentCategory.Name.Value}\n~w~Sub Category: ~g~{subCategory.Name.Value}");
                    }
                }
            }

            void EnableDialoguePath()
            {
                var newDialogueOptions = new List<MenuItem>();
                var allMenuItems = GetAllMenuItems();
                if (response.Attribute("enablesDialoguePath") != null)
                {
                    Game.LogTrivial($"Enabling dialogue path {response.Attribute("enablesDialoguePath").Value}");                    
                    var itemsWithPathToBeEnabled = allMenuItems.Where(x => (x.Element.Attribute("dialoguePath") != null && response.Attribute("enablesDialoguePath") != null && (x.Element.Attribute("dialoguePath").Value == response.Attribute("enablesDialoguePath").Value) || prompt.MenuPrompt.Attribute("enablesDialoguePath") != null && x.Element.Attribute("dialoguePath").Value == prompt.MenuPrompt.Attribute("enablesDialoguePath").Value));
                    
                    foreach (MenuItem menuItem in itemsWithPathToBeEnabled)
                    {
                        Game.LogTrivial($"Enabling {menuItem.MenuPrompt.Value}");
                        newDialogueOptions.Add(menuItem);
                        menuItem.Enabled = true;
                    }
                }
                else if (prompt.Element.Attribute("enablesDialoguePath") != null)
                {
                    Game.LogTrivial($"Enabling dialogue path {prompt.Element.Attribute("enablesDialoguePath").Value}");
                    var itemsWithPathToBeEnabled = allMenuItems.Where(x => x.Element.Attribute("dialoguePath") != null && x.Element.Attribute("dialoguePath").Value == prompt.Element.Attribute("enablesDialoguePath").Value);

                    foreach (MenuItem menuItem in itemsWithPathToBeEnabled)
                    {
                        Game.LogTrivial($"Enabling {menuItem.MenuPrompt.Value}");
                        newDialogueOptions.Add(menuItem);
                        menuItem.Enabled = true;
                    }
                }

                NotifyPlayerOfNewDialoguePathOptions(newDialogueOptions);
            }

            void NotifyPlayerOfNewDialoguePathOptions(List<MenuItem> newDialogueOptions)
            {
                var updatedParentCategories = newDialogueOptions.Select(x => x.ParentCategory).Distinct();
                var updatedSubCategories = newDialogueOptions.Select(x => x.SubCategory).Distinct();

                foreach (SubCategory subCategory in updatedSubCategories.SkipWhile(x => recentlyEnabledCategories.Contains(x)))
                {
                    Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nNew dialogue options unlocked:\nMenu: ~b~{subCategory.ParentCategory.Menu.TitleText.Split(' ').First()}\n~w~Category: ~y~{subCategory.ParentCategory.Name.Value}\n~w~Sub Category: ~g~{subCategory.Name.Value}");
                }

                foreach (ParentCategory parentCategory in updatedParentCategories.SkipWhile(x => x.HasSubCategory || recentlyEnabledCategories.Contains(x)))
                {
                    Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nNew dialogue options unlocked:\nMenu: ~b~{parentCategory.Menu.TitleText.Split(' ').First()}\n~w~Category: ~y~{parentCategory.Name.Value}");
                }
            }
        }

        private static List<MenuItem> GetAllMenuItems()
        {
            var allMenuItems = ParentCategories.SelectMany(x => x.MenuItems).ToList();
            var subCategories = ParentCategories.SelectMany(x => x.SubCategories);
            var subCategoryMenuItems = subCategories.SelectMany(x => x.MenuItems).ToList();
            allMenuItems.AddRange(subCategoryMenuItems);
            //Game.LogTrivial($"Menu items: {allMenuItems.Count()}");
            return allMenuItems;
        }
    }
}
