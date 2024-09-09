using System.Collections.Generic;
using ParseTree;
using SyntacticParser.ProbabilisticContextFreeGrammar;

namespace SyntacticParser.SyntacticParser
{
    public class PartialParseList
    {
        private List<ParseNode> _partialParses;

        /// <summary>
        /// Constructor for the PartialParseList class. Initializes partial parses array list.
        /// </summary>
        public PartialParseList()
        {
            _partialParses = new List<ParseNode>();
        }

        /// <summary>
        /// Adds a new partial parse (actually a parse node representing the root of the subtree of the partial parse)
        /// </summary>
        /// <param name="parseNode">Root of the subtree showing the partial parse.</param>
        public void AddPartialParse(ParseNode parseNode)
        {
            _partialParses.Add(parseNode);
        }

        /// <summary>
        /// Updates the partial parse by removing less probable nodes with the given parse node.
        /// </summary>
        /// <param name="parseNode">Parse node to be added to the partial parse.</param>
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

        /// <summary>
        /// Accessor for the partialParses array list.
        /// </summary>
        /// <param name="index">Position of the parse node.</param>
        /// <returns>Parse node at the given position.</returns>
        public ParseNode GetPartialParse(int index)
        {
            return _partialParses[index];
        }

        /// <summary>
        /// Returns size of the partial parse.
        /// </summary>
        /// <returns>Size of the partial parse.</returns>
        public int Size()
        {
            return _partialParses.Count;
        }
    }
}