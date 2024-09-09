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

        /// <summary>
        /// Empty constructor for the ContextFreeGrammar class.
        /// </summary>
        public ContextFreeGrammar()
        {
        }

        /// <summary>
        /// Reads the lexicon for the grammar. Each line consists of two items, the terminal symbol and the frequency of
        /// that symbol. The method fills the dictionary counter hash map according to this data.
        /// </summary>
        /// <param name="dictionaryFileName">File name of the lexicon.</param>
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

        /// <summary>
        /// Constructor for the ContextFreeGrammar class. Reads the rules from the rule file, lexicon rules from the
        /// dictionary file and sets the minimum frequency parameter.
        /// </summary>
        /// <param name="ruleFileName">File name for the rule file.</param>
        /// <param name="dictionaryFileName">File name for the lexicon file.</param>
        /// <param name="minCount">Minimum frequency parameter.</param>
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

        /// <summary>
        /// Another constructor for the ContextFreeGrammar class. Constructs the lexicon from the leaf nodes of the trees
        /// in the given treebank. Extracts rules from the non-leaf nodes of the trees in the given treebank. Also sets the
        /// minimum frequency parameter.
        /// </summary>
        /// <param name="treeBank">Treebank containing the constituency trees.</param>
        /// <param name="minCount">Minimum frequency parameter.</param>
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

        /// <summary>
        /// Constructs the lexicon from the given treebank. Reads each tree and for each leaf node in each tree puts the
        /// symbol in the dictionary.
        /// </summary>
        /// <param name="treeBank">Treebank containing the constituency trees.</param>
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

        /// <summary>
        /// Updates the exceptional symbols of the leaf nodes in the trees. Constituency trees consists of rare symbols and
        /// numbers, which are usually useless in creating constituency grammars. This is due to the fact that, numbers may
        /// not occur exactly the same both in the train and/or test set, although they have the same meaning in general.
        /// Similarly, when a symbol occurs in the test set but not in the training set, there will not be any rule covering
        /// that symbol and therefore no parse tree will be generated. For those reasons, the leaf nodes containing numerals
        /// are converted to the same terminal symbol, i.e. _num_; the leaf nodes containing rare symbols are converted to
        /// the same terminal symbol, i.e. _rare_.
        /// </summary>
        /// <param name="parseTree">Parse tree to be updated.</param>
        /// <param name="minCount">Minimum frequency for the terminal symbols to be considered as rare.</param>
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

        /// <summary>
        /// Updates the exceptional words in the sentences for which constituency parse trees will be generated. Constituency
        /// trees consist of rare symbols and numbers, which are usually useless in creating constituency grammars. This is
        /// due to the fact that, numbers may not occur exactly the same both in the train and/or test set, although they have
        /// the same meaning in general. Similarly, when a symbol occurs in the test set but not in the training set, there
        /// will not be any rule covering that symbol and therefore no parse tree will be generated. For those reasons, the
        /// words containing numerals are converted to the same terminal symbol, i.e. _num_; thewords containing rare symbols
        /// are converted to the same terminal symbol, i.e. _rare_.
        /// </summary>
        /// <param name="sentence">Sentence to be updated.</param>
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

        /// <summary>
        /// After constructing the constituency tree with a parser for a sentence, it contains exceptional words such as
        /// rare words and numbers, which are represented as _rare_ and _num_ symbols in the tree. Those words should be
        /// converted to their original forms. This method replaces the exceptional symbols to their original forms by
        /// replacing _rare_ and _num_ symbols.
        /// </summary>
        /// <param name="parseTree">Parse tree to be updated.</param>
        /// <param name="sentence">Original sentence for which constituency tree is generated.</param>
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

        /// <summary>
        /// Updates the types of the rules according to the number of symbols on the right hand side. Rule type is TERMINAL
        /// if the rule is like X -> a, SINGLE_NON_TERMINAL if the rule is like X -> Y, TWO_NON_TERMINAL if the rule is like
        /// X -> YZ, MULTIPLE_NON_TERMINAL if the rule is like X -> YZT...
        /// </summary>
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

        /// <summary>
        /// Converts a parse node in a tree to a rule. The symbol in the parse node will be the symbol on the leaf side of the
        /// rule, the symbols in the child nodes will be the symbols on the right hand side of the rule.
        /// </summary>
        /// <param name="parseNode">Parse node for which a rule will be created.</param>
        /// <param name="trim">If true, the tags will be trimmed. If the symbol's data contains '-' or '=', this method trims all
        ///             characters after those characters.</param>
        /// <returns>A new rule constructed from a parse node and its children.</returns>
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

        /// <summary>
        /// Recursive method to generate all rules from a subtree rooted at the given node.
        /// </summary>
        /// <param name="parseNode">Root node of the subtree.</param>
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
        
        /// <summary>
        /// Inserts a new rule into the correct position in the sorted rules and rulesRightSorted array lists.
        /// </summary>
        /// <param name="newRule">Rule to be inserted into the sorted array lists.</param>
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

        /// <summary>
        /// Removes a given rule from the sorted rules and rulesRightSorted array lists.
        /// </summary>
        /// <param name="rule">Rule to be removed from the sorted array lists.</param>
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

        /// <summary>
        /// Returns rules formed as X -> ... Since there can be more than one rule, which have X on the left side, the method
        /// first binary searches the rule to obtain the position of such a rule, then goes up and down to obtain others
        /// having X on the left side.
        /// </summary>
        /// <param name="x">Left side of the rule</param>
        /// <returns>Rules of the form X -> ...</returns>
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

        /// <summary>
        /// Returns all symbols X from terminal rules such as X -> a.
        /// </summary>
        /// <returns>All symbols X from terminal rules such as X -> a.</returns>
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

        /// <summary>
        /// Returns all symbols X from all rules such as X -> ...
        /// </summary>
        /// <returns>All symbols X from all rules such as X -> ...</returns>
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

        /// <summary>
        /// Returns all rules with the given terminal symbol on the right hand side, that is it returns all terminal rules
        /// such as X -> s
        /// </summary>
        /// <param name="s">Terminal symbol on the right hand side.</param>
        /// <returns>All rules with the given terminal symbol on the right hand side</returns>
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

        /// <summary>
        /// Returns all rules with the given non-terminal symbol on the right hand side, that is it returns all non-terminal
        /// rules such as X -> S
        /// </summary>
        /// <param name="s">Non-terminal symbol on the right hand side.</param>
        /// <returns>All rules with the given non-terminal symbol on the right hand side</returns>
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

        /// <summary>
        /// Returns all rules with the given two non-terminal symbols on the right hand side, that is it returns all
        /// non-terminal rules such as X -> AB.
        /// </summary>
        /// <param name="a">First non-terminal symbol on the right hand side.</param>
        /// <param name="b">Second non-terminal symbol on the right hand side.</param>
        /// <returns>All rules with the given two non-terminal symbols on the right hand side</returns>
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

        /// <summary>
        /// Returns the symbol on the right side of the first rule with one non-terminal symbol on the right hand side, that
        /// is it returns S of the first rule such as X -> S. S should also not be in the given removed list.
        /// </summary>
        /// <param name="removedList">Discarded list for symbol S.</param>
        /// <returns>The symbol on the right side of the first rule with one non-terminal symbol on the right hand side. The
        /// symbol to be returned should also not be in the given discarded list.</returns>
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

        /// <summary>
        /// Returns all rules with more than two non-terminal symbols on the right hand side, that is it returns all
        /// non-terminal rules such as X -> ABC...
        /// </summary>
        /// <returns>All rules with more than two non-terminal symbols on the right hand side.</returns>
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

        /// <summary>
        /// In conversion to Chomsky Normal Form, rules like X -> Y are removed and new rules for every rule as Y -> beta are
        /// replaced with X -> beta. The method first identifies all X -> Y rules. For every such rule, all rules Y -> beta
        /// are identified. For every such rule, the method adds a new rule X -> beta. Every Y -> beta rule is then deleted.
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
                        AddRule(new Rule(rule.GetLeftHandSide(), new List<Symbol>(candidate.GetRightHandSide()),
                            candidate.Type));
                    }
                    RemoveRule(rule);
                }
                nonTerminalList.Add(removeCandidate);
                removeCandidate = GetSingleNonTerminalCandidateToRemove(nonTerminalList);
            }
        }

        /// <summary>
        /// In conversion to Chomsky Normal Form, rules like A -> BC... are replaced with A -> X1... and X1 -> BC. This
        /// method replaces B and C non-terminals on the right hand side with X1 for all rules in the grammar.
        /// </summary>
        /// <param name="first">Non-terminal symbol B.</param>
        /// <param name="second">Non-terminal symbol C.</param>
        /// <param name="with">Non-terminal symbol X1.</param>
        protected void UpdateAllMultipleNonTerminalWithNewRule(Symbol first, Symbol second, Symbol with)
        {
            foreach (var rule in Rules) {
                if (rule.Type == RuleType.MULTIPLE_NON_TERMINAL)
                {
                    rule.UpdateMultipleNonTerminal(first, second, with);
                }
            }
        }

        /// <summary>
        /// In conversion to Chomsky Normal Form, rules like A -> BC... are replaced with A -> X1... and X1 -> BC. This
        /// method determines such rules and for every such rule, it adds new rule X1->BC and updates rule A->BC to A->X1.
        /// </summary>
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

        /// <summary>
        /// The method converts the grammar into Chomsky normal form. First, rules like X -> Y are removed and new rules for
        /// every rule as Y -> beta are replaced with X -> beta. Second, rules like A -> BC... are replaced with A -> X1...
        /// and X1 -> BC.
        /// </summary>
        public void ConvertToChomskyNormalForm()
        {
            RemoveSingleNonTerminalFromRightHandSide();
            UpdateMultipleNonTerminalFromRightHandSide();
            Comparer<Rule> comparator = new RuleComparator();
            Rules.Sort(comparator);
            Comparer<Rule> rightComparator = new RuleRightSideComparator();
            RulesRightSorted.Sort(rightComparator);
        }

        /// <summary>
        /// Searches a given rule in the grammar.
        /// </summary>
        /// <param name="rule">Rule to be searched.</param>
        /// <returns>Rule if found, null otherwise.</returns>
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

        /// <summary>
        /// Returns number of rules in the grammar.
        /// </summary>
        /// <returns>Number of rules in the Context Free Grammar.</returns>
        public int Size()
        {
            return Rules.Count;
        }
    }
}