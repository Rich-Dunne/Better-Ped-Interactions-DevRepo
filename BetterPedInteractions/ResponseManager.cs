using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Rage;
using RAGENativeUI.Elements;
using static BetterPedInteractions.Settings;

namespace BetterPedInteractions
{
    class ResponseManager
    {
        private static Dictionary<XElement, XElement> _usedQuestionResponsePairs = new Dictionary<XElement, XElement>();
        private static List<XElement> _usedResponses = new List<XElement>();

        internal static void FindMatchingQuestion(Dictionary<XAttribute, Dictionary<XElement, List<XElement>>> questionsAndAnswers, UIMenuListScrollerItem<string> questionCategories, UIMenuItem selectedItem)
        {
            var matchingCategory = questionsAndAnswers.FirstOrDefault(x => x.Key.Value == questionCategories.SelectedItem);
            var questionResponsePair = matchingCategory.Value.FirstOrDefault(x => x.Key.Attribute("question").Value == selectedItem.Text);
            var focusedPed = EntryPoint.focusedPed;
            XElement response = null;

            response = ChoosePedResponse();
            DisplayResponse();
            AddQuestionResponsePairToUsedBank();
            if (focusedPed.Group == Settings.Group.Civilian && EnableAgitation)
            {
                AdjustPedAgitation();
            }

            XElement ChoosePedResponse()
            {
                // If the question was already asked, the ped needs to repeat themselves instead of assigning a new response
                if (focusedPed.UsedQuestions.ContainsKey(questionResponsePair.Key))
                {
                    return focusedPed.UsedQuestions[questionResponsePair.Key];
                }

                // Assign a response (75% chance to match previous response truth/lie attribute) and add it to _usedResponses
                // If this is the ped's first response OR if this is not the first response and they meet the 25% chance requirement to divert from their initial response type, choose a random response
                if (focusedPed.ResponseType == ResponseType.Unspecified || (focusedPed.ResponseType != ResponseType.Unspecified && GetResponseChance() == 3))
                {
                    response = questionResponsePair.Value[GetRandomResponseValue()];
                }
                // If this is not the ped's first response, choose a response that matches their initial response's type
                else if (focusedPed.ResponseType != ResponseType.Unspecified && GetResponseChance() < 3)
                {
                    response = questionResponsePair.Value.FirstOrDefault(x => x.Attributes().Count() > 0 && x.Attribute("type").Value.ToLower() == focusedPed.ResponseType.ToString().ToLower());
                }

                // Save the type of response given (truth/lie)
                if (response.HasAttributes)
                {
                    var responseAttributes = response.Attributes();
                    foreach (XAttribute attribute in responseAttributes)
                    {
                        attribute.Value[0].ToString().ToUpper();
                        Game.LogTrivial($"Response attribute: {attribute.Value}");
                        if (Enum.TryParse(attribute.Value, out ResponseType responseType))
                        {
                            focusedPed.ResponseType = responseType;
                        }
                    }
                }

                return response;

                int GetResponseChance()
                {
                    return new Random().Next(0, 4);
                }

                int GetRandomResponseValue()
                {
                    Random r = new Random();
                    return r.Next(questionResponsePair.Value.Count);
                }
            }

            void AddQuestionResponsePairToUsedBank()
            {
                // Add the question and response pair to a list of already-asked questions.
                if (!focusedPed.UsedQuestions.ContainsKey(questionResponsePair.Key))
                {
                    focusedPed.UsedQuestions.Add(questionResponsePair.Key, response);
                }
            }

            void DisplayResponse()
            {
                
                if(focusedPed.Group == Settings.Group.Civilian && !focusedPed.StoppedTalking)
                {
                    if (focusedPed.UsedQuestions.ContainsKey(questionResponsePair.Key))
                    {
                        RepeatResponse();            }
                    else
                    {
                        Game.DisplaySubtitle($"~y~Unidentified {focusedPed.Gender}: ~w~{response.Value}");
                    }
                    PlayLipAnimation();
                    return;
                }

                if(focusedPed.Group == Settings.Group.Cop)
                {
                    if (focusedPed.UsedQuestions.ContainsKey(questionResponsePair.Key))
                    {
                        RepeatResponse();
                    }
                    else
                    {
                        Game.DisplaySubtitle($"~y~Officer: ~w~{response.Value}");
                    }
                    PlayLipAnimation();
                    return;
                }
            }

            void RepeatResponse()
            {
                var repeatedResponse = focusedPed.UsedQuestions[questionResponsePair.Key].Value.ToLower();
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
                    if(focusedPed.Group == Settings.Group.Civilian)
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

            void AdjustPedAgitation()
            {
                if (questionResponsePair.Key.Attributes().Any(x => x.Name == "type"))
                {
                    if (questionResponsePair.Key.Attribute("type").Value == "interview")
                    {
                        focusedPed.DecreaseAgitation();
                    }
                    else if (questionResponsePair.Key.Attribute("type").Value == "interrogation")
                    {
                        focusedPed.IncreaseAgitation();
                    }
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
                    while(timer < numberOfWords.Length * 500)
                    {
                        timer += 1;
                        GameFiber.Yield();
                    }

                    if (focusedPed.Ped && focusedPed.Ped.IsAlive)
                    {
                        focusedPed.Ped.Tasks.Clear();
                    }
                });
            }
        }
    }
}
