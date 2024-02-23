using System;
using System.Collections.Generic;
using Corpus;
using Dictionary.Dictionary;
using ParseTree;
using SyntacticParser.ContextFreeGrammar;
using SyntacticParser.SyntacticParser;

namespace SyntacticParser.ProbabilisticContextFreeGrammar
{
    public class ProbabilisticCYKParser : ProbabilisticParser.ProbabilisticParser
    {
        public List<ParseTree.ParseTree> Parse(ProbabilisticContextFreeGrammar pCfg, Sentence sentence)
        {
            int i, j, k, x, y;
            PartialParseList[][] table;
            ProbabilisticParseNode leftNode, rightNode;
            double bestProbability, probability;
            List<Rule> candidates;
            var parseTrees = new List<ParseTree.ParseTree>();
            var backUp = new Sentence();
            for (i = 0; i < sentence.WordCount(); i++)
            {
                backUp.AddWord(new Word(sentence.GetWord(i).GetName()));
            }

            pCfg.RemoveExceptionalWordsFromSentence(sentence);
            table = new PartialParseList[sentence.WordCount()][];
            for (i = 0; i < sentence.WordCount(); i++)
            {
                table[i] = new PartialParseList[sentence.WordCount()];
                for (j = i; j < sentence.WordCount(); j++)
                    table[i][j] = new PartialParseList();
            }

            for (i = 0; i < sentence.WordCount(); i++)
            {
                candidates = pCfg.GetTerminalRulesWithRightSideX(new Symbol(sentence.GetWord(i).GetName()));
                foreach (var candidate in candidates)
                {
                    table[i][i].AddPartialParse(new ProbabilisticParseNode(
                        new ParseNode(new Symbol(sentence.GetWord(i).GetName())), candidate.GetLeftHandSide(),
                        System.Math.Log(((ProbabilisticRule)candidate).GetProbability())));
                }
            }

            for (j = 1; j < sentence.WordCount(); j++)
            {
                for (i = j - 1; i >= 0; i--)
                {
                    for (k = i; k < j; k++)
                    {
                        for (x = 0; x < table[i][k].Size(); x++)
                        {
                            for (y = 0; y < table[k + 1][j].Size(); y++)
                            {
                                leftNode = (ProbabilisticParseNode)table[i][k].GetPartialParse(x);
                                rightNode = (ProbabilisticParseNode)table[k + 1][j].GetPartialParse(y);
                                candidates =
                                    pCfg.GetRulesWithTwoNonTerminalsOnRightSide(leftNode.GetData(),
                                        rightNode.GetData());
                                foreach (var candidate in candidates){
                                    probability = System.Math.Log(((ProbabilisticRule)candidate).GetProbability()) +
                                                  leftNode.GetLogProbability() + rightNode.GetLogProbability();
                                    table[i][j].UpdatePartialParse(new ProbabilisticParseNode(leftNode, rightNode,
                                        candidate.GetLeftHandSide(), probability));
                                }
                            }
                        }
                    }
                }
            }

            bestProbability = Double.MinValue;
            for (i = 0; i < table[0][sentence.WordCount() - 1].Size(); i++)
            {
                if (table[0][sentence.WordCount() - 1].GetPartialParse(i).GetData().GetName().Equals("S") &&
                    ((ProbabilisticParseNode)table[0][sentence.WordCount() - 1].GetPartialParse(i))
                    .GetLogProbability() > bestProbability)
                {
                    bestProbability = ((ProbabilisticParseNode)table[0][sentence.WordCount() - 1].GetPartialParse(i))
                        .GetLogProbability();
                }
            }

            for (i = 0; i < table[0][sentence.WordCount() - 1].Size(); i++)
            {
                if (table[0][sentence.WordCount() - 1].GetPartialParse(i).GetData().GetName().Equals("S") &&
                    ((ProbabilisticParseNode)table[0][sentence.WordCount() - 1].GetPartialParse(i))
                    .GetLogProbability() == bestProbability)
                {
                    var parseTree = new ParseTree.ParseTree(table[0][sentence.WordCount() - 1].GetPartialParse(i));
                    parseTree.CorrectParents();
                    parseTree.RemoveXNodes();
                    parseTrees.Add(parseTree);
                }
            }

            foreach (var parseTree in parseTrees){
                pCfg.ReinsertExceptionalWordsFromSentence(parseTree, backUp);
            }
            return parseTrees;
        }
    }
}