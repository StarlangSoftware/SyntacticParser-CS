using System.Collections.Generic;
using ParseTree;
using SyntacticParser.ProbabilisticContextFreeGrammar;

namespace SyntacticParser.SyntacticParser
{
    public class PartialParseList
    {
        private List<ParseNode> _partialParses;

        public PartialParseList()
        {
            _partialParses = new List<ParseNode>();
        }

        public void AddPartialParse(ParseNode parseNode)
        {
            _partialParses.Add(parseNode);
        }

        public void UpdatePartialParse(ProbabilisticParseNode parseNode)
        {
            var found = false;
            foreach (ParseNode partialParse in _partialParses){
                if (partialParse.GetData().GetName().Equals(parseNode.GetData().GetName()))
                {
                    if (((ProbabilisticParseNode)partialParse).GetLogProbability() < parseNode.GetLogProbability())
                    {
                        _partialParses.Remove(partialParse);
                        _partialParses.Add(parseNode);
                    }

                    found = true;
                    break;
                }
            }
            if (!found)
            {
                _partialParses.Add(parseNode);
            }
        }

        public ParseNode GetPartialParse(int index)
        {
            return _partialParses[index];
        }

        public int Size()
        {
            return _partialParses.Count;
        }
    }
}