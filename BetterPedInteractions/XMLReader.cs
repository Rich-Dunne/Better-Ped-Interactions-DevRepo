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
            var civQuestionsAndAnswers = new Dictionary<string, Dictionary<XElement, List<XElement>>>();
            var copQuestionsAndAnswers = new Dictionary<string, Dictionary<XElement, List<XElement>>>();
            var defaultDirectory = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions\\Default";
            var customDirectory = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions\\Custom";
            // Iterate over all XML files in all subdirectories
            LoadXMLs(defaultDirectory);
            LoadXMLs(customDirectory);

            Game.LogTrivial($"Building civ menu");
            MenuManager.BuildCivMenu(civQuestionsAndAnswers);
            Game.LogTrivial($"Building cop menu");
            MenuManager.BuildCopMenu(copQuestionsAndAnswers);

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
                        var localQuestions = new Dictionary<XElement, List<XElement>>();
                        foreach (XElement question in questions.Where(x => x.Parent == category.Parent))
                        {
                            //Game.LogTrivial($"Question: {question.Attribute("question").Value}");
                            var localResponses = new List<XElement>();
                            foreach (XElement response in responses.Where(r => r.Parent.Attribute("question").Value == question.Attribute("question").Value))
                            {
                                localResponses.Add(response);
                                //Game.LogTrivial($"Response: {response.Value}");
                            }
                            localQuestions.Add(question, localResponses);
                        }

                        if (menu == "civilian" && !civQuestionsAndAnswers.ContainsKey(category.Value))
                        {
                            civQuestionsAndAnswers.Add(category.Value, localQuestions);
                        }
                        else if(menu == "cop" && !copQuestionsAndAnswers.ContainsKey(category.Value))
                        {
                            copQuestionsAndAnswers.Add(category.Value, localQuestions);
                        }
                    }
                }
            }
        }

        //internal static Dictionary<string, Dictionary<XElement, List<XElement>>> ReadXML(string filename)
        //{
        //    var questionsAndAnswers = new Dictionary<string,Dictionary<XElement, List<XElement>>>();
        //    var currentDirectory = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions";
        //    var xmlFilepath = Path.Combine(currentDirectory, filename);

        //    XElement file = XElement.Load(xmlFilepath);
        //    var questionCategories = file.Descendants("QuestionGroup").Attributes();
        //    var questions = file.Descendants("Question");
        //    var responses = file.Descendants("Response");

        //    foreach (XAttribute category in questionCategories)
        //    {
        //        var localQuestions = new Dictionary<XElement, List<XElement>>();
        //        foreach (XElement question in questions.Where(x => x.Parent == category.Parent))
        //        {
        //            //Game.LogTrivial($"Question: {question.Attribute("question").Value}");
        //            var localResponses = new List<XElement>();
        //            foreach (XElement response in responses.Where(r => r.Parent.Attribute("question").Value == question.Attribute("question").Value))
        //            {
        //                localResponses.Add(response);
        //                //Game.LogTrivial($"Response: {response.Value}");
        //            }
        //            localQuestions.Add(question, localResponses);
        //        }
        //        questionsAndAnswers.Add(category.Value, localQuestions);
        //    }

        //    return questionsAndAnswers;
        //}
    }
}
