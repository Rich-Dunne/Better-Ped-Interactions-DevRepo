using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BetterPedInteractions
{ 
    class QuestionResponsePair
    {
        internal Settings.Group Group { get; set; }

        internal XAttribute Category { get; set; }

        internal XElement Question { get; set; }

        internal List<XElement> Responses { get; set; } = new List<XElement>();

        internal QuestionResponsePair(XAttribute category)//(XAttribute category, XElement question, List<XElement> responses)
        {
            Category = category;
            //Question = question;
            //Responses = responses;
        }
    }
}
