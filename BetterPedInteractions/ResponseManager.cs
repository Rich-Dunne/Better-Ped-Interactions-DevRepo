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
        private static List<QuestionResponsePair> _questionsAndResponses = new List<QuestionResponsePair>();

        internal static void AssignQuestionsAndAnswers(List<QuestionResponsePair> questionsAndResponses)
        {
            _questionsAndResponses = questionsAndResponses;
        }

        internal static void GetResponseFromAudio(string question)
        {
            var matchingQuestion = _questionsAndResponses.FirstOrDefault(x => x.Question?.Attribute("question").Value == question);
            Game.LogTrivial($"Matching question: {matchingQuestion.Question.Attribute("question").Value}");
            ChoosePedResponse(matchingQuestion);
        }

        internal static void FindMatchingQuestion(UIMenuListScrollerItem<string> questionCategories, UIMenuItem selectedItem)
        {
            var questionResponsePair = _questionsAndResponses.FirstOrDefault(x => x.Question.Attribute("question").Value == selectedItem.Text);

            ChoosePedResponse(questionResponsePair);          
        }

        private static void ChoosePedResponse(QuestionResponsePair questionResponsePair)
        {
            var focusedPed = EntryPoint.FocusedPed;
            Game.LogTrivial($"Focused ped: {focusedPed.Ped.Model.Name}");
            XElement response = null;

            // If the question was already asked, the ped needs to repeat themselves instead of assigning a new response
            if (focusedPed.UsedQuestions.ContainsKey(questionResponsePair.Question))
            {
                response = focusedPed.UsedQuestions[questionResponsePair.Question];
            }
            // Assign a response (75% chance to match previous response truth/lie attribute) and add it to _usedResponses
            // If this is the ped's first response OR if this is not the first response and they meet the 25% chance requirement to divert from their initial response type, choose a random response
            else if (focusedPed.ResponseType == ResponseType.Unspecified || (focusedPed.ResponseType != ResponseType.Unspecified && GetResponseChance() == 3))
            {
                response = questionResponsePair.Responses[GetRandomResponseValue()];
            }
            // If this is not the ped's first response, choose a response that matches their initial response's type
            else if (focusedPed.ResponseType != ResponseType.Unspecified && GetResponseChance() < 3)
            {
                response = questionResponsePair.Responses.FirstOrDefault(x => x.Attributes().Count() > 0 && x.Attribute("type").Value.ToLower() == focusedPed.ResponseType.ToString().ToLower());
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

            DisplayResponse();
            AddQuestionResponsePairToUsedBank();
            if (focusedPed.Group == Settings.Group.Civilian && EnableAgitation)
            {
                AdjustPedAgitation();
            }

            int GetResponseChance()
            {
                return new Random().Next(0, 4);
            }

            int GetRandomResponseValue()
            {
                Random r = new Random();
                return r.Next(questionResponsePair.Responses.Count);
            }

            void DisplayResponse()
            {

                if (!focusedPed.StoppedTalking)
                {
                    if (focusedPed.UsedQuestions.ContainsKey(questionResponsePair.Question))
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

                //if (focusedPed.Group == Settings.Group.Cop)
                //{
                //    if (focusedPed.UsedQuestions.ContainsKey(questionResponsePair.Question))
                //    {
                //        RepeatResponse();
                //    }
                //    else
                //    {
                //        Game.DisplaySubtitle($"~y~Officer: ~w~{response.Value}");
                //    }
                //    PlayLipAnimation();
                //    return;
                //}
            }

            void RepeatResponse()
            {
                var repeatedResponse = focusedPed.UsedQuestions[questionResponsePair.Question].Value.ToLower();
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
                if (!focusedPed.UsedQuestions.ContainsKey(questionResponsePair.Question))
                {
                    focusedPed.UsedQuestions.Add(questionResponsePair.Question, response);
                }
            }

            void AdjustPedAgitation()
            {
                if (questionResponsePair.Question.Attributes().Any(x => x.Name == "type"))
                {
                    if (questionResponsePair.Question.Attribute("type").Value == "interview")
                    {
                        focusedPed.DecreaseAgitation();
                    }
                    else if (questionResponsePair.Question.Attribute("type").Value == "interrogation")
                    {
                        focusedPed.IncreaseAgitation();
                    }
                }
            }
        }
    }
}
