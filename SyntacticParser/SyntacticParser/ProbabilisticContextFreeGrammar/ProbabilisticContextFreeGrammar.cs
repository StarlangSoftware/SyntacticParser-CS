using System.Collections.Generic;
using System.IO;
using ParseTree;
using SyntacticParser.ContextFreeGrammar;

namespace SyntacticParser.ProbabilisticContextFreeGrammar
{
    public class ProbabilisticContextFreeGrammar : ContextFreeGrammar.ContextFreeGrammar
    {
        public ProbabilisticContextFreeGrammar()
        {
        }

        public ProbabilisticContextFreeGrammar(string ruleFileName, string dictionaryFileName, int minCount)
        {
            var br = new StreamReader(ruleFileName);
            var line = br.ReadLine();
            while (line != null)
            {
                var newRule = new ProbabilisticRule(line);
                Rules.Add(newRule);
                RulesRightSorted.Add(newRule);
                line = br.ReadLine();
            }

            Comparer<Rule> comparator = new RuleComparator();
            Rules.Sort(comparator);
            Comparer<Rule> rightComparator = new RuleRightSideComparator();
            RulesRightSorted.Sort(rightComparator);

            ReadDictionary(dictionaryFileName);
            UpdateTypes();
            this.MinCount = minCount;
        }

        public ProbabilisticContextFreeGrammar(TreeBank treeBank, int minCount)
        {
            List<Symbol> variables;
            List<Rule> candidates;
            int total;
            ConstructDictionary(treeBank);
            for (var i = 0; i < treeBank.Size(); i++)
            {
                var parseTree = treeBank.Get(i);
                UpdateTree(parseTree, minCount);
                AddRules(parseTree.GetRoot());
            }

            variables = GetLeftSide();
            foreach (var variable in variables){
                candidates = GetRulesWithLeftSideX(variable);
                total = 0;
                foreach (var candidate in candidates){
                    total += ((ProbabilisticRule)candidate).GetCount();
                }
                foreach (var candidate in candidates){
                    ((ProbabilisticRule)candidate).NormalizeProbability(total);
                }
            }
            UpdateTypes();
            this.MinCount = minCount;
        }

        public new static ProbabilisticRule ToRule(ParseNode parseNode, bool trim)
        {
            Symbol left;
            var right = new List<Symbol>();
            if (trim)
                left = parseNode.GetData().TrimSymbol();
            else
                left = parseNode.GetData();
            for (var i = 0; i < parseNode.NumberOfChildren(); i++)
            {
                var childNode = parseNode.GetChild(i);
                if (childNode.GetData() != null)
                {
                    if (childNode.GetData().IsTerminal())
                    {
                        right.Add(childNode.GetData());
                    }
                    else
                    {
                        right.Add(childNode.GetData().TrimSymbol());
                    }
                }
                else
                {
                    return null;
                }
            }

            return new ProbabilisticRule(left, right);
        }

        private void AddRules(ParseNode parseNode)
        {
            Rule existedRule;
            ProbabilisticRule newRule;
            newRule = ToRule(parseNode, true);
            if (newRule != null)
            {
                existedRule = SearchRule(newRule);
                if (existedRule == null)
                {
                    AddRule(newRule);
                    newRule.Increment();
                }
                else
                {
                    ((ProbabilisticRule)existedRule).Increment();
                }
            }

            for (var i = 0; i < parseNode.NumberOfChildren(); i++)
            {
                ParseNode childNode = parseNode.GetChild(i);
                if (childNode.NumberOfChildren() > 0)
                {
                    AddRules(childNode);
                }
            }
        }

        private double Probability(ParseNode parseNode)
        {
            Rule existedRule;
            ProbabilisticRule rule;
            double sum = 0.0;
            if (parseNode.NumberOfChildren() > 0)
            {
                rule = ToRule(parseNode, true);
                existedRule = SearchRule(rule);
                sum = System.Math.Log(((ProbabilisticRule)existedRule).GetProbability());
                if (existedRule.Type != RuleType.TERMINAL)
                {
                    for (var i = 0; i < parseNode.NumberOfChildren(); i++)
                    {
                        var childNode = parseNode.GetChild(i);
                        sum += Probability(childNode);
                    }
                }
            }

            return sum;
        }

        public double Probability(ParseTree.ParseTree parseTree)
        {
            return Probability(parseTree.GetRoot());
        }


        private void RemoveSingleNonTerminalFromRightHandSide()
        {
            List<Symbol> nonTerminalList;
            Symbol removeCandidate;
            List<Rule> ruleList;
            List<Rule> candidateList;
            nonTerminalList = new List<Symbol>();
            removeCandidate = GetSingleNonTerminalCandidateToRemove(nonTerminalList);
            while (removeCandidate != null)
            {
                ruleList = GetRulesWithRightSideX(removeCandidate);
                foreach (var rule in ruleList){
                    candidateList = GetRulesWithLeftSideX(removeCandidate);
                    foreach (var candidate in candidateList){
                        AddRule(new ProbabilisticRule(rule.GetLeftHandSide(),
                            new List<Symbol>(candidate.GetRightHandSide()), candidate.Type,
                            ((ProbabilisticRule)rule).GetProbability() *
                            ((ProbabilisticRule)candidate).GetProbability()));
                    }
                    RemoveRule(rule);
                }
                nonTerminalList.Add(removeCandidate);
                removeCandidate = GetSingleNonTerminalCandidateToRemove(nonTerminalList);
            }
        }

        private void UpdateMultipleNonTerminalFromRightHandSide()
        {
            Rule updateCandidate;
            int newVariableCount = 0;
            updateCandidate = GetMultipleNonTerminalCandidateToUpdate();
            while (updateCandidate != null)
            {
                var newRightHandSide = new List<Symbol>();
                var newSymbol = new Symbol("X" + newVariableCount);
                newRightHandSide.Add(updateCandidate.GetRightHandSide()[0]);
                newRightHandSide.Add(updateCandidate.GetRightHandSide()[1]);
                UpdateAllMultipleNonTerminalWithNewRule(updateCandidate.GetRightHandSide()[0],
                    updateCandidate.GetRightHandSide()[1], newSymbol);
                AddRule(new ProbabilisticRule(newSymbol, newRightHandSide, RuleType.TWO_NON_TERMINAL, 1.0));
                newVariableCount++;
                updateCandidate = GetMultipleNonTerminalCandidateToUpdate();
            }
        }

        public new void ConvertToChomskyNormalForm()
        {
            RemoveSingleNonTerminalFromRightHandSide();
            UpdateMultipleNonTerminalFromRightHandSide();
            Comparer<Rule> comparator = new RuleComparator();
            Rules.Sort(comparator);
            Comparer<Rule> rightComparator = new RuleRightSideComparator();
            RulesRightSorted.Sort(rightComparator);
        }
    }
}