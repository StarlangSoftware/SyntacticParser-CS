using System.Collections.Generic;
using System.IO;
using ParseTree;
using SyntacticParser.ContextFreeGrammar;

namespace SyntacticParser.ProbabilisticContextFreeGrammar
{
    public class ProbabilisticContextFreeGrammar : ContextFreeGrammar.ContextFreeGrammar
    {
        /// <summary>
        /// Empty constructor for the ContextFreeGrammar class.
        /// </summary>
        public ProbabilisticContextFreeGrammar()
        {
        }

        /// <summary>
        /// Constructor for the ProbabilisticContextFreeGrammar class. Reads the rules from the rule file, lexicon rules from
        /// the dictionary file and sets the minimum frequency parameter.
        /// </summary>
        /// <param name="ruleFileName">File name for the rule file.</param>
        /// <param name="dictionaryFileName">File name for the lexicon file.</param>
        /// <param name="minCount">Minimum frequency parameter.</param>
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

        /// <summary>
        /// Another constructor for the ProbabilisticContextFreeGrammar class. Constructs the lexicon from the leaf nodes of
        /// the trees in the given treebank. Extracts rules from the non-leaf nodes of the trees in the given treebank. Also
        /// sets the minimum frequency parameter.
        /// </summary>
        /// <param name="treeBank">Treebank containing the constituency trees.</param>
        /// <param name="minCount">Minimum frequency parameter.</param>
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

        /// <summary>
        /// Converts a parse node in a tree to a rule. The symbol in the parse node will be the symbol on the leaf side of the
        /// rule, the symbols in the child nodes will be the symbols on the right hand side of the rule.
        /// </summary>
        /// <param name="parseNode">Parse node for which a rule will be created.</param>
        /// <param name="trim">If true, the tags will be trimmed. If the symbol's data contains '-' or '=', this method trims all
        ///             characters after those characters.</param>
        /// <returns>A new rule constructed from a parse node and its children.</returns>
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

        /// <summary>
        /// Recursive method to generate all rules from a subtree rooted at the given node.
        /// </summary>
        /// <param name="parseNode">Root node of the subtree.</param>
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
                var childNode = parseNode.GetChild(i);
                if (childNode.NumberOfChildren() > 0)
                {
                    AddRules(childNode);
                }
            }
        }

        /// <summary>
        /// Calculates the probability of a parse node.
        /// </summary>
        /// <param name="parseNode">Parse node for which probability is calculated.</param>
        /// <returns>Probability of a parse node.</returns>
        private double Probability(ParseNode parseNode)
        {
            Rule existedRule;
            ProbabilisticRule rule;
            var sum = 0.0;
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

        /// <summary>
        /// Calculates the probability of a parse tree.
        /// </summary>
        /// <param name="parseTree">Parse tree for which probability is calculated.</param>
        /// <returns>Probability of the parse tree.</returns>
        public double Probability(ParseTree.ParseTree parseTree)
        {
            return Probability(parseTree.GetRoot());
        }

        /// <summary>
        /// In conversion to Chomsky Normal Form, rules like X -> Y are removed and new rules for every rule as Y -> beta are
        /// replaced with X -> beta. The method first identifies all X -> Y rules. For every such rule, all rules Y -> beta
        /// are identified. For every such rule, the method adds a new rule X -> beta. Every Y -> beta rule is then deleted.
        /// The method also calculates the probability of the new rules based on the previous rules.
        /// </summary>
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

        /// <summary>
        /// In conversion to Chomsky Normal Form, rules like A -> BC... are replaced with A -> X1... and X1 -> BC. This
        /// method determines such rules and for every such rule, it adds new rule X1->BC and updates rule A->BC to A->X1.
        /// The method sets the probability of the rules X1->BC to 1, and calculates the probability of the rules A -> X1...
        /// </summary>
        private void UpdateMultipleNonTerminalFromRightHandSide()
        {
            Rule updateCandidate;
            var newVariableCount = 0;
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

        /// <summary>
        /// The method converts the grammar into Chomsky normal form. First, rules like X -> Y are removed and new rules for
        /// every rule as Y -> beta are replaced with X -> beta. Second, rules like A -> BC... are replaced with A -> X1...
        /// and X1 -> BC.
        /// </summary>
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