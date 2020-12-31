using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using static BetterPedInteractions.Settings;

namespace BetterPedInteractions
{
    class ResponseManager
    {
        private static List<ParentCategory> ParentCategories = new List<ParentCategory>();

        internal static void AssignInteractions(List<ParentCategory> parentCategories)
        {
            ParentCategories = parentCategories;
        }

        internal static void FindMatchingPromptFromAudio(string heardPrompt)
        {
            var allMenuItems = GetAllMenuItems();
            MenuItem matchingPrompt = GetMatchingPrompt();
            if(matchingPrompt == null)
            {
                Game.LogTrivial($"No matching prompt found.");
                return;
            }

            //Game.LogTrivial($"Matching prompt: {matchingPrompt.MenuPrompt.Value}, Category: {matchingPrompt.Category.Name.Value}");
            UpdateMenus(matchingPrompt);
            ChoosePedResponse(matchingPrompt);

            MenuItem GetMatchingPrompt()
            {
                var prompt = allMenuItems.FirstOrDefault(x => (x.MenuPrompt?.Value == heardPrompt || x.AudioPrompts.Contains(heardPrompt)) && !x.HasSubCategory);
                if (prompt == null)
                {
                    prompt = allMenuItems.FirstOrDefault(x => (x.MenuPrompt?.Value == heardPrompt || x.AudioPrompts.Contains(heardPrompt)) && x.HasSubCategory);
                }
                return prompt != null ? prompt : null;
            }
        }

        internal static void FindMatchingPromptFromMenu(UIMenu menu, UIMenuItem selectedItem)
        {
            //Game.LogTrivial($"Selected item: {selectedItem.Text}");
            var allMenuItems = GetAllMenuItems();
            var matchingPrompt = GetMatchingPrompt();
            if(matchingPrompt == null)
            {
                Game.LogTrivial($"No matching prompt found.");
                return;
            }

            //Game.LogTrivial($"Matching prompt: {matchingPrompt.MenuPrompt.Value}, Category: {matchingPrompt.Category.Name.Value}");
            UpdateMenus(matchingPrompt);
            ChoosePedResponse(matchingPrompt);

            MenuItem GetMatchingPrompt()
            {
                // If the second menu item was not a scroller, then we need to get response from primary category menu items, else we need to get it from subcategory menu items
                if (menu.MenuItems[1].GetType() != typeof(UIMenuListScrollerItem<string>))
                {
                    return allMenuItems.FirstOrDefault(x => x.MenuPrompt?.Value == selectedItem.Text && !x.HasSubCategory);
                }
                else
                {
                    return allMenuItems.FirstOrDefault(x => x.MenuPrompt?.Value == selectedItem.Text && x.HasSubCategory);
                }
            }
        }

        private static void UpdateMenus(MenuItem matchingPrompt)
        {
            IncrementCategoryLevel();

            EnableDialoguePathFromPrompt();

            EnableMenuFromPrompt();

            void IncrementCategoryLevel()
            {
                if(matchingPrompt.HasSubCategory && matchingPrompt.Level == matchingPrompt.SubCategory.Level)
                {
                    //Game.LogTrivial($"Category level being incremented: {matchingPrompt.Category.Name.Value}");
                    //Game.LogTrivial($"Level before: {matchingPrompt.Category.ReadLevel}");
                    matchingPrompt.SubCategory.Level++;
                    //Game.LogTrivial($"Level after: {matchingPrompt.Category.ReadLevel}");
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

            void EnableMenuFromPrompt()
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
                Game.LogTrivial($"Action: {prompt.Action.Text}");
                Game.LogTrivial($"Heard prompt is a ped action.  We don't need a response.");
                return;
            }

            var focusedPed = EntryPoint.FocusedPed;
            Game.LogTrivial($"Focused ped: {focusedPed.Ped.Model.Name}");
            XElement response = null;

            GetResponse();
            SetPedResponseHonesty();
            EnableCategory();
            EnableDialoguePath();
            DisplayResponse();
            AddQuestionResponsePairToUsedBank();
            if (focusedPed.Group == Settings.Group.Civilian && EnableAgitation)
            {
                AdjustPedAgitation();
            }
            // Need to populate menu here or in VocalInterface
            //Game.LogTrivial($"Item is from menu: {prompt.ParentCategory.Menu.TitleText}");
            if (prompt.ParentCategory.Menu == MenuManager.CivMenu)
            {
                MenuManager.PopulateMenu(prompt.ParentCategory.Menu, MenuManager.CivParentCategoryScroller);
            }
            else if (prompt.ParentCategory.Menu == MenuManager.CopMenu)
            {
                MenuManager.PopulateMenu(prompt.ParentCategory.Menu, MenuManager.CopParentCategoryScroller);
            }

            void GetResponse()
            {
                // If the question was already asked, the ped needs to repeat themselves instead of assigning a new response
                if (focusedPed.UsedQuestions.ContainsKey(prompt.MenuPrompt))
                {
                    Game.LogTrivial($"Repeated question");
                    response = focusedPed.UsedQuestions[prompt.MenuPrompt];
                }
                // Assign a response (75% chance to match previous response truth/lie attribute) and add it to _usedResponses
                // If this is the ped's first response OR if this is not the first response and they meet the 25% chance requirement to divert from their initial response type, choose a random response
                else if (focusedPed.ResponseHonesty == ResponseHonesty.Unspecified || (focusedPed.ResponseHonesty != ResponseHonesty.Unspecified && GetResponseChance() == 3))
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

            int GetResponseChance()
            {
                return new Random().Next(0, 4);
            }

            int GetRandomResponseValue()
            {
                Random r = new Random();
                Game.LogTrivial($"Possible responses: {prompt.Responses.Count()}");
                var num = r.Next(prompt.Responses.Count);
                Game.LogTrivial($"Random number: {num}");
                return num;
                //return r.Next(prompt.Responses.Count);
            }

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

            void EnableDialoguePath()
            {
                var newDialogueOptions = new List<MenuItem>();
                if (response.Attribute("enablesDialoguePath") != null)
                {
                    Game.LogTrivial($"Enabling dialogue path {response.Attribute("enablesDialoguePath").Value}");
                    var allMenuItems = GetAllMenuItems();
                    //Game.LogTrivial($"All menu items: {allMenuItems.Count()}");
                    var itemsWithPathToBeEnabled = allMenuItems.Where(x => (x.Element.Attribute("dialoguePath") != null && response.Attribute("enablesDialoguePath") != null && (x.Element.Attribute("dialoguePath").Value == response.Attribute("enablesDialoguePath").Value) || prompt.MenuPrompt.Attribute("enablesDialoguePath") != null && x.Element.Attribute("dialoguePath").Value == prompt.MenuPrompt.Attribute("enablesDialoguePath").Value));
                    //Game.LogTrivial($"To be enabled: {itemsWithPathToBeEnabled.Count()}");
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
                    var allMenuItems = GetAllMenuItems();
                    //Game.LogTrivial($"All menu items: {allMenuItems.Count()}");
                    var itemsWithPathToBeEnabled = allMenuItems.Where(x => x.Element.Attribute("dialoguePath") != null &&  x.Element.Attribute("dialoguePath").Value == prompt.Element.Attribute("enablesDialoguePath").Value);
                    //Game.LogTrivial($"To be enabled: {itemsWithPathToBeEnabled.Count()}");

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
                var updatedCategories = newDialogueOptions.Select(x => x.ParentCategory).Distinct();
                string updatedCategoriesString = "";
                foreach (Category category in updatedCategories)
                {
                    updatedCategoriesString += $"~b~{category.Name.Value} [{category.Menu.TitleText}]~w~, ";
                }
                if(updatedCategoriesString != "")
                {
                    updatedCategoriesString.Trim(' ', ',');
                    Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nNew dialogue options unlocked in category: {updatedCategoriesString.Trim(' ', ',')}~w~.");
                }
            }

            void EnableCategory()
            {
                if (prompt.Element.Attribute("enablesCategory") != null)
                {
                    var allMenuItems = GetAllMenuItems();
                    var menuItemsWithCategoriesToEnable = allMenuItems.Where(x => (x.ParentCategory.Name.Value == prompt.Element.Attribute("enablesCategory").Value || x.SubCategory?.Name.Value == prompt.Element.Attribute("enablesCategory").Value) && x.ParentCategory.File == prompt.ParentCategory.File).Distinct();
                    var parentCategoriesToEnable = menuItemsWithCategoriesToEnable.Where(x => !x.ParentCategory.Enabled).Select(x => x.ParentCategory).ToList();
                    var subCategoriesToEnable = menuItemsWithCategoriesToEnable.Where(x => x.SubCategory != null && !x.SubCategory.Enabled).Select(x => x.SubCategory).ToList();
                    //Game.LogTrivial($"Categories to be enabled: {categoriesToBeEnabled.Count()}");
                    foreach(ParentCategory parentCategory in parentCategoriesToEnable)
                    {
                        //Game.LogTrivial($"Category: {category.Name.Value}, File: {category.File}");
                        parentCategory.Enabled = true;
                        Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nCategory unlocked with new dialogue options: ~b~{parentCategory.Name.Value} [{parentCategory.Menu.TitleText}]~w~.");
                    }
                    foreach(SubCategory subCategory in subCategoriesToEnable)
                    {
                        subCategory.Enabled = true;
                        Game.DisplayNotification($"~o~[Better Ped Interactions]~w~\nSub Category unlocked with new dialogue options: ~b~{subCategory.Name.Value} [{subCategory.ParentCategory.Menu.TitleText}]~w~.");
                    }
                }
            }

            void DisplayResponse()
            {

                if (!focusedPed.StoppedTalking)
                {
                    if (focusedPed.UsedQuestions.ContainsKey(prompt.MenuPrompt))
                    {
                        RepeatResponse();
                    }
                    else
                    {
                        if(focusedPed.Group == Settings.Group.Civilian)
                        {
                            Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~{response.Value}");
                        }
                        else if (focusedPed.Group == Settings.Group.Cop)
                        {
                            Game.DisplaySubtitle($"~y~Officer: ~w~{response.Value}");
                        }
                    }
                    PlayLipAnimation();
                    return;
                }
            }

            void RepeatResponse()
            {
                var repeatedResponse = focusedPed.UsedQuestions[prompt.MenuPrompt].Value.ToLower();
                Game.LogTrivial($"This response was already used");

                if (new Random().Next(2) == 1)
                {
                    if (focusedPed.Group == Settings.Group.Civilian)
                    {
                        Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~I already told you, {repeatedResponse}");
                    }
                    else
                    {
                        Game.DisplaySubtitle($"~y~Officer: ~w~Like I said, {repeatedResponse}");
                    }

                }
                else
                {
                    if (focusedPed.Group == Settings.Group.Civilian)
                    {
                        Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~Did I s-s-stutter?");
                    }
                    else
                    {
                        Game.DisplaySubtitle($"~y~Officer: ~w~Weren't you paying attention the first time you asked?");
                    }
                }

                if (focusedPed.Group == Settings.Group.Civilian && EnableAgitation)
                {
                    focusedPed.IncreaseAgitation(true);
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
