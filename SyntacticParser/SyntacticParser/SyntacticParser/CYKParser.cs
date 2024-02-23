using System.Collections.Generic;
using Corpus;
using Dictionary.Dictionary;
using ParseTree;
using SyntacticParser.ContextFreeGrammar;

namespace SyntacticParser.SyntacticParser
{
    public class CYKParser : SyntacticParser
    {
        public List<ParseTree.ParseTree> Parse(ContextFreeGrammar.ContextFreeGrammar cfg, Sentence sentence)
        {
            int i, j, k, x, y;
            PartialParseList[][] table;
            ParseNode leftNode, rightNode;
            List<Rule> candidates;
            var parseTrees = new List<ParseTree.ParseTree>();
            Sentence backUp = new Sentence();
            for (i = 0; i < sentence.WordCount(); i++)
            {
                backUp.AddWord(new Word(sentence.GetWord(i).GetName()));
            }

            cfg.RemoveExceptionalWordsFromSentence(sentence);
            table = new PartialParseList[sentence.WordCount()][];
            for (i = 0; i < sentence.WordCount(); i++)
            {
                table[i] = new PartialParseList[sentence.WordCount()];
                for (j = i; j < sentence.WordCount(); j++)
                    table[i][j] = new PartialParseList();
            }

            for (i = 0; i < sentence.WordCount(); i++)
            {
                candidates = cfg.GetTerminalRulesWithRightSideX(new Symbol(sentence.GetWord(i).GetName()));
                foreach (Rule candidate in candidates)
                {
                    table[i][i].AddPartialParse(new ParseNode(new ParseNode(new Symbol(sentence.GetWord(i).GetName())),
                        candidate.GetLeftHandSide()));
                }
            }

            for (j = 1; j < sentence.WordCount(); j++)
            {
                for (i = j - 1; i >= 0; i--)
                {
                    for (k = i; k < j; k++)
                    {
                        for (x = 0; x < table[i][k].Size(); x++)
                        for (y = 0; y < table[k + 1][j].Size(); y++)
                        {
                            leftNode = table[i][k].GetPartialParse(x);
                            rightNode = table[k + 1][j].GetPartialParse(y);
                            candidates =
                                cfg.GetRulesWithTwoNonTerminalsOnRightSide(leftNode.GetData(), rightNode.GetData());
                            foreach (var candidate in candidates)
                            {
                                table[i][j].AddPartialParse(new ParseNode(leftNode, rightNode,
                                    candidate.GetLeftHandSide()));
                            }
                        }
                    }
                }
            }

            for (i = 0; i < table[0][sentence.WordCount() - 1].Size(); i++)
            {
                if (table[0][sentence.WordCount() - 1].GetPartialParse(i).GetData().GetName().Equals("S"))
                {
                    var parseTree = new ParseTree.ParseTree(table[0][sentence.WordCount() - 1].GetPartialParse(i));
                    parseTree.CorrectParents();
                    parseTree.RemoveXNodes();
                    parseTrees.Add(parseTree);
                }
            }

            foreach (var parseTree in parseTrees){
                cfg.ReinsertExceptionalWordsFromSentence(parseTree, backUp);
            }
            return parseTrees;
        }
    }
}