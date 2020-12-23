using Rage;
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
            var questionsAndAnswers = new List<QuestionResponsePair>();
            var defaultDirectory = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions\\Default";
            var customDirectory = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions\\Custom";
            
            LoadXMLs(defaultDirectory);
            LoadXMLs(customDirectory);
            MenuManager.BuildMenus(questionsAndAnswers);

            void LoadXMLs(string directory)
            {
                foreach (string xmlFile in Directory.GetFiles(directory, "*.xml", SearchOption.AllDirectories))
                {
                    //Game.LogTrivial($"File: {xmlFile}");
                    XDocument document = XDocument.Load(xmlFile);
                    var menu = document.Root.Attribute("menu").Value;
                    //Game.LogTrivial($"Menu attribute: {menu}");
                    var questionCategories = document.Descendants("QuestionGroup").Attributes();
                    var questions = document.Descendants("Question");
                    var responses = document.Descendants("Response");

                    foreach (XAttribute category in questionCategories)
                    {
                        Game.LogTrivial($"Category: {category.Value}");
                        if(category.Value == "Ped Actions")
                        {
                            var questionAnswerPair = new QuestionResponsePair(category);
                            if (menu == "civilian")
                            {
                                questionAnswerPair.Group = Settings.Group.Civilian;
                            }
                            else if (menu == "cop")
                            {
                                questionAnswerPair.Group = Settings.Group.Cop;
                            }
                            questionsAndAnswers.Add(questionAnswerPair);
                        }
                        foreach (XElement question in questions.Where(x => x.Parent == category.Parent))
                        {
                            var questionAnswerPair = new QuestionResponsePair(category);
                            if (menu == "civilian")
                            {
                                questionAnswerPair.Group = Settings.Group.Civilian;
                            }
                            else if (menu == "cop")
                            {
                                questionAnswerPair.Group = Settings.Group.Cop;
                            }
                            //Game.LogTrivial($"Question: {question.Attribute("question").Value}");
                            questionAnswerPair.Question = question;
                            foreach (XElement response in responses.Where(r => r.Parent.Attribute("question").Value == question.Attribute("question").Value))
                            {
                                questionAnswerPair.Responses.Add(response);
                                //Game.LogTrivial($"Response: {response.Value}");
                            }
                            questionsAndAnswers.Add(questionAnswerPair);
                            VocalInterface.Phrases.Add(question.Attribute("question").Value);
                        }
                    }
                }
            }
        }
    }
}
