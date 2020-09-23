using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PedInterview
{
    internal class XMLReader
    {
        internal static Dictionary<string, Dictionary<string, List<string>>> ReadXML(string filename)
        {
            var questionsAndAnswers = new Dictionary<string, Dictionary<string, List<string>>>();
            var currentDirectory = Directory.GetCurrentDirectory() + "\\plugins";
            var xmlFilepath = Path.Combine(currentDirectory, filename);

            XElement file = XElement.Load(xmlFilepath);
            var questionCategories = file.Descendants("QuestionGroup").Attributes();
            var questions = file.Descendants("Question");
            var responses = file.Descendants("Response");

            foreach (XAttribute category in questionCategories)
            {
                var localQuestions = new Dictionary<string, List<string>>();
                foreach (XElement question in questions.Where(x => x.Parent == category.Parent))
                {
                    //Game.LogTrivial($"Question: {question.Attribute("question").Value}");
                    var localResponses = new List<string>();
                    foreach (XElement response in responses.Where(r => r.Parent.Attribute("question").Value == question.Attribute("question").Value))
                    {
                        localResponses.Add(response.Value);
                        //Game.LogTrivial($"Response: {response.Value}");
                    }
                    localQuestions.Add(question.Attribute("question").Value, localResponses);
                }
                questionsAndAnswers.Add(category.Value, localQuestions);
            }

            return questionsAndAnswers;
        }
    }
}
