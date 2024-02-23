using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Corpus;
using DataStructure;
using Dictionary.Dictionary;
using ParseTree;
using ParseTree.NodeCondition;

namespace SyntacticParser.ContextFreeGrammar
{
    public class ContextFreeGrammar
    {
        protected CounterHashMap<string> Dictionary = new CounterHashMap<string>();
        protected List<Rule> Rules = new List<Rule>();
        protected List<Rule> RulesRightSorted = new List<Rule>();
        protected int MinCount = 1;

        public ContextFreeGrammar()
        {
        }

        protected void ReadDictionary(string dictionaryFileName)
        {
            var br = new StreamReader(dictionaryFileName);
            var line = br.ReadLine();
            while (line != null)
            {
                var items = line.Split(" ");
                Dictionary.PutNTimes(items[0], int.Parse(items[1]));
                line = br.ReadLine();
            }
        }

        public ContextFreeGrammar(string ruleFileName, string dictionaryFileName, int minCount)
        {
            var br = new StreamReader(ruleFileName);
            var line = br.ReadLine();
            while (line != null)
            {
                var newRule = new Rule(line);
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

        public ContextFreeGrammar(TreeBank treeBank, int minCount)
        {
            ConstructDictionary(treeBank);
            for (var i = 0; i < treeBank.Size(); i++)
            {
                var parseTree = treeBank.Get(i);
                UpdateTree(parseTree, minCount);
                AddRules(parseTree.GetRoot());
            }

            UpdateTypes();
            MinCount = minCount;
        }

        protected void ConstructDictionary(TreeBank treeBank)
        {
            for (var i = 0; i < treeBank.Size(); i++)
            {
                var parseTree = treeBank.Get(i);
                var nodeCollector = new NodeCollector(parseTree.GetRoot(), new IsLeaf());
                var leafList = nodeCollector.Collect();
                foreach (var parseNode in leafList){
                    Dictionary.Put(parseNode.GetData().GetName());
                }
            }
        }

        public void UpdateTree(ParseTree.ParseTree parseTree, int minCount)
        {
            var nodeCollector = new NodeCollector(parseTree.GetRoot(), new IsLeaf());
            var leafList = nodeCollector.Collect();
            var pattern1 = new Regex("\\+?\\d+");
            var pattern2 = new Regex("\\+?(\\d+)?\\.\\d*");
            foreach (var parseNode in leafList){
                var data = parseNode.GetData().GetName();
                if (pattern1.IsMatch(data) || (pattern2.IsMatch(data) && !data.Equals(".")))
                {
                    parseNode.SetData(new Symbol("_num_"));
                }
                else
                {
                    if (Dictionary.Count(data) < minCount)
                    {
                        parseNode.SetData(new Symbol("_rare_"));
                    }
                }
            }
        }

        public void RemoveExceptionalWordsFromSentence(Sentence sentence)
        {
            var pattern1 = new Regex("\\+?\\d+");
            var pattern2 = new Regex("\\+?(\\d+)?\\.\\d*");
            for (var i = 0; i < sentence.WordCount(); i++)
            {
                var word = sentence.GetWord(i);
                if (pattern1.IsMatch(word.GetName()) ||
                    (pattern2.IsMatch(word.GetName()) && !word.GetName().Equals(".")))
                {
                    word.SetName("_num_");
                }
                else
                {
                    if (Dictionary.Count(word.GetName()) < MinCount)
                    {
                        word.SetName("_rare_");
                    }
                }
            }
        }

        public void ReinsertExceptionalWordsFromSentence(ParseTree.ParseTree parseTree, Sentence sentence)
        {
            var nodeCollector = new NodeCollector(parseTree.GetRoot(), new IsLeaf());
            var leafList = nodeCollector.Collect();
            for (var i = 0; i < leafList.Count; i++)
            {
                var treeWord = leafList[i].GetData().GetName();
                var sentenceWord = sentence.GetWord(i).GetName();
                if (treeWord.Equals("_rare_") || treeWord.Equals("_num_"))
                {
                    leafList[i].SetData(new Symbol(sentenceWord));
                }
            }
        }

        protected void UpdateTypes()
        {
            var nonTerminals = new HashSet<string>();
            foreach (var rule in Rules){
                nonTerminals.Add(rule.GetLeftHandSide().GetName());
            }
            foreach (var rule in Rules){
                if (rule.GetRightHandSideSize() > 2)
                {
                    rule.Type = RuleType.MULTIPLE_NON_TERMINAL;
                }
                else
                {
                    if (rule.GetRightHandSideSize() == 2)
                    {
                        rule.Type = RuleType.TWO_NON_TERMINAL;
                    }
                    else
                    {
                        if (rule.GetRightHandSideAt(0).IsTerminal() ||
                            Word.IsPunctuation(rule.GetRightHandSideAt(0).GetName()) ||
                            !nonTerminals.Contains(rule.GetRightHandSideAt(0).GetName()))
                        {
                            rule.Type = RuleType.TERMINAL;
                        }
                        else
                        {
                            rule.Type = RuleType.SINGLE_NON_TERMINAL;
                        }
                    }
                }
            }
        }

        public static Rule ToRule(ParseNode parseNode, bool trim)
        {
            Symbol left;
            var right = new List<Symbol>();
            if (trim)
                left = parseNode.GetData().TrimSymbol();
            else
                left = parseNode.GetData();
            for (int i = 0; i < parseNode.NumberOfChildren(); i++)
            {
                ParseNode childNode = parseNode.GetChild(i);
                if (childNode.GetData() != null)
                {
                    if (childNode.GetData().IsTerminal() || !trim)
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

            return new Rule(left, right);
        }

        private void AddRules(ParseNode parseNode)
        {
            Rule newRule;
            newRule = ToRule(parseNode, true);
            if (newRule != null)
            {
                AddRule(newRule);
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
        
        public void AddRule(Rule newRule)
        {
            int pos;
            Comparer<Rule> comparator = new RuleComparator();
            pos = Rules.BinarySearch(newRule, comparator);
            if (pos < 0)
            {
                Rules.Insert(~pos, newRule);
                Comparer<Rule> rightComparator = new RuleRightSideComparator();
                pos = RulesRightSorted.BinarySearch(newRule, rightComparator);
                if (pos >= 0)
                {
                    RulesRightSorted.Insert(pos, newRule);
                }
                else
                {
                    RulesRightSorted.Insert(-pos - 1, newRule);
                }
            }
        }

        public void RemoveRule(Rule rule)
        {
            int pos, posUp, posDown;
            Comparer<Rule> comparator = new RuleComparator();
            pos = Rules.BinarySearch(rule, comparator);
            if (pos >= 0)
            {
                Rules.RemoveAt(pos);
                Comparer<Rule> rightComparator = new RuleRightSideComparator();
                pos = RulesRightSorted.BinarySearch(rule, rightComparator);
                posUp = pos;
                while (posUp >= 0 && rightComparator.Compare(RulesRightSorted[posUp], rule) == 0)
                {
                    if (comparator.Compare(rule, RulesRightSorted[posUp]) == 0)
                    {
                        RulesRightSorted.RemoveAt(posUp);
                        return;
                    }

                    posUp--;
                }

                posDown = pos + 1;
                while (posDown < RulesRightSorted.Count &&
                       rightComparator.Compare(RulesRightSorted[posDown], rule) == 0)
                {
                    if (comparator.Compare(rule, RulesRightSorted[posDown]) == 0)
                    {
                        RulesRightSorted.RemoveAt(posDown);
                        return;
                    }

                    posDown++;
                }
            }
        }

        /*Return Rules such as X -> ... */
        public List<Rule> GetRulesWithLeftSideX(Symbol x)
        {
            int middle, middleUp, middleDown;
            var result = new List<Rule>();
            var dummyRule = new Rule(x, x);
            Comparer<Rule> leftComparator = new RuleLeftSideComparator();
            middle = Rules.BinarySearch(dummyRule, leftComparator);
            if (middle >= 0)
            {
                middleUp = middle;
                while (middleUp >= 0 && Rules[middleUp].GetLeftHandSide().Equals(x))
                {
                    result.Add(Rules[middleUp]);
                    middleUp--;
                }

                middleDown = middle + 1;
                while (middleDown < Rules.Count && Rules[middleDown].GetLeftHandSide().Equals(x))
                {
                    result.Add(Rules[middleDown]);
                    middleDown++;
                }
            }

            return result;
        }

        /*Return symbols X from terminal Rules such as X -> a */
        public List<Symbol> PartOfSpeechTags()
        {
            var result = new List<Symbol>();
            foreach (var rule in Rules) {
                if (rule.Type == RuleType.TERMINAL && !result.Contains(rule.GetLeftHandSide()))
                {
                    result.Add(rule.GetLeftHandSide());
                }
            }
            return result;
        }

        /*Return symbols X from all Rules such as X -> ... */
        public List<Symbol> GetLeftSide()
        {
            var result = new List<Symbol>();
            foreach (var rule in Rules) {
                if (!result.Contains(rule.GetLeftHandSide()))
                {
                    result.Add(rule.GetLeftHandSide());
                }
            }
            return result;
        }

        /*Return terminal Rules such as X -> s*/
        public List<Rule> GetTerminalRulesWithRightSideX(Symbol s)
        {
            int middle, middleUp, middleDown;
            var result = new List<Rule>();
            var dummyRule = new Rule(s, s);
            Comparer<Rule> rightComparator = new RuleRightSideComparator();
            middle = RulesRightSorted.BinarySearch(dummyRule, rightComparator);
            if (middle >= 0)
            {
                middleUp = middle;
                while (middleUp >= 0 && RulesRightSorted[middleUp].GetRightHandSideAt(0).Equals(s))
                {
                    if (RulesRightSorted[middleUp].Type == RuleType.TERMINAL)
                    {
                        result.Add(RulesRightSorted[middleUp]);
                    }

                    middleUp--;
                }

                middleDown = middle + 1;
                while (middleDown < RulesRightSorted.Count &&
                       RulesRightSorted[middleDown].GetRightHandSideAt(0).Equals(s))
                {
                    if (RulesRightSorted[middleDown].Type == RuleType.TERMINAL)
                    {
                        result.Add(RulesRightSorted[middleDown]);
                    }

                    middleDown++;
                }
            }

            return result;
        }

        /*Return terminal Rules such as X -> S*/
        public List<Rule> GetRulesWithRightSideX(Symbol s)
        {
            int pos, posUp, posDown;
            var result = new List<Rule>();
            Rule dummyRule = new Rule(s, s);
            Comparer<Rule> rightComparator = new RuleRightSideComparator();
            pos = RulesRightSorted.BinarySearch(dummyRule, rightComparator);
            if (pos >= 0)
            {
                posUp = pos;
                while (posUp >= 0 && RulesRightSorted[posUp].GetRightHandSideAt(0).Equals(s) &&
                       RulesRightSorted[posUp].GetRightHandSideSize() == 1)
                {
                    result.Add(RulesRightSorted[posUp]);
                    posUp--;
                }

                posDown = pos + 1;
                while (posDown < RulesRightSorted.Count &&
                       RulesRightSorted[posDown].GetRightHandSideAt(0).Equals(s) &&
                       RulesRightSorted[posDown].GetRightHandSideSize() == 1)
                {
                    result.Add(RulesRightSorted[posDown]);
                    posDown++;
                }
            }

            return result;
        }

        /*Return Rules such as X -> AB */
        public List<Rule> GetRulesWithTwoNonTerminalsOnRightSide(Symbol a, Symbol b)
        {
            int pos, posUp, posDown;
            var result = new List<Rule>();
            var dummyRule = new Rule(a, a, b);
            Comparer<Rule> rightComparator = new RuleRightSideComparator();
            pos = RulesRightSorted.BinarySearch(dummyRule, rightComparator);
            if (pos >= 0)
            {
                posUp = pos;
                while (posUp >= 0 && RulesRightSorted[posUp].GetRightHandSideSize() == 2 &&
                       RulesRightSorted[posUp].GetRightHandSideAt(0).Equals(a) &&
                       RulesRightSorted[posUp].GetRightHandSideAt(1).Equals(b))
                {
                    result.Add(RulesRightSorted[posUp]);
                    posUp--;
                }

                posDown = pos + 1;
                while (posDown < RulesRightSorted.Count && RulesRightSorted[posDown].GetRightHandSideSize() == 2 &&
                       RulesRightSorted[posDown].GetRightHandSideAt(0).Equals(a) &&
                       RulesRightSorted[posDown].GetRightHandSideAt(1).Equals(b))
                {
                    result.Add(RulesRightSorted[posDown]);
                    posDown++;
                }
            }

            return result;
        }

        /*Return Y of the first rule such as X -> Y */
        protected Symbol GetSingleNonTerminalCandidateToRemove(List<Symbol> removedList)
        {
            Symbol removeCandidate = null;
            foreach (var rule in Rules) {
                if (rule.Type == RuleType.SINGLE_NON_TERMINAL && !rule.LeftRecursive() &&
                    !removedList.Contains(rule.GetRightHandSideAt(0)))
                {
                    removeCandidate = rule.GetRightHandSideAt(0);
                    break;
                }
            }
            return removeCandidate;
        }

        /*Return the first rule such as X -> ABC... */
        protected Rule GetMultipleNonTerminalCandidateToUpdate()
        {
            Rule removeCandidate = null;
            foreach (var rule in Rules) {
                if (rule.Type == RuleType.MULTIPLE_NON_TERMINAL)
                {
                    removeCandidate = rule;
                    break;
                }
            }
            return removeCandidate;
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
                        AddRule(new Rule(rule.GetLeftHandSide(), new List<Symbol>(candidate.GetRightHandSide()),
                            candidate.Type));
                    }
                    RemoveRule(rule);
                }
                nonTerminalList.Add(removeCandidate);
                removeCandidate = GetSingleNonTerminalCandidateToRemove(nonTerminalList);
            }
        }

        protected void UpdateAllMultipleNonTerminalWithNewRule(Symbol first, Symbol second, Symbol with)
        {
            foreach (var rule in Rules) {
                if (rule.Type == RuleType.MULTIPLE_NON_TERMINAL)
                {
                    rule.UpdateMultipleNonTerminal(first, second, with);
                }
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
                Symbol newSymbol = new Symbol("X" + newVariableCount);
                newRightHandSide.Add(updateCandidate.GetRightHandSide()[0]);
                newRightHandSide.Add(updateCandidate.GetRightHandSide()[1]);
                UpdateAllMultipleNonTerminalWithNewRule(updateCandidate.GetRightHandSide()[0],
                    updateCandidate.GetRightHandSide()[1], newSymbol);
                AddRule(new Rule(newSymbol, newRightHandSide, RuleType.TWO_NON_TERMINAL));
                updateCandidate = GetMultipleNonTerminalCandidateToUpdate();
                newVariableCount++;
            }
        }

        public void ConvertToChomskyNormalForm()
        {
            RemoveSingleNonTerminalFromRightHandSide();
            UpdateMultipleNonTerminalFromRightHandSide();
            Comparer<Rule> comparator = new RuleComparator();
            Rules.Sort(comparator);
            Comparer<Rule> rightComparator = new RuleRightSideComparator();
            RulesRightSorted.Sort(rightComparator);
        }

        public Rule SearchRule(Rule rule)
        {
            int pos;
            Comparer<Rule> comparator = new RuleComparator();
            pos = Rules.BinarySearch(rule, comparator);
            if (pos >= 0)
            {
                return Rules[pos];
            }
            else
            {
                return null;
            }
        }

        public int Size()
        {
            return Rules.Count;
        }
    }
}