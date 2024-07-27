using System.Collections.Generic;
using ParseTree;

namespace SyntacticParser.ContextFreeGrammar
{
    public class Rule
    {
        protected Symbol LeftHandSide;
        protected List<Symbol> RightHandSide;
        public RuleType Type;

        /// <summary>
        /// Empty constructor for the rule class.
        /// </summary>
        public Rule()
        {
        }

        /// <summary>
        /// Constructor for the rule X -> Y.
        /// </summary>
        /// <param name="leftHandSide">Non-terminal symbol X</param>
        /// <param name="rightHandSideSymbol">Symbol Y (terminal or non-terminal)</param>
        public Rule(Symbol leftHandSide, Symbol rightHandSideSymbol)
        {
            this.LeftHandSide = leftHandSide;
            RightHandSide = new List<Symbol>();
            RightHandSide.Add(rightHandSideSymbol);
        }

        /// <summary>
        /// Constructor for the rule X -> YZ.
        /// </summary>
        /// <param name="leftHandSide">Non-terminal symbol X.</param>
        /// <param name="rightHandSideSymbol1">Symbol Y (non-terminal).</param>
        /// <param name="rightHandSideSymbol2">Symbol Z (non-terminal).</param>
        public Rule(Symbol leftHandSide, Symbol rightHandSideSymbol1, Symbol rightHandSideSymbol2) : this(leftHandSide, rightHandSideSymbol1)
        {
            this.RightHandSide.Add(rightHandSideSymbol2);
        }

        /// <summary>
        /// Constructor for the rule X -> beta. beta is a string of symbols from symbols (non-terminal)
        /// </summary>
        /// <param name="leftHandSide">Non-terminal symbol X.</param>
        /// <param name="rightHandSide">beta. beta is a string of symbols from symbols (non-terminal)</param>
        public Rule(Symbol leftHandSide, List<Symbol> rightHandSide)
        {
            this.LeftHandSide = leftHandSide;
            this.RightHandSide = rightHandSide;
        }

        /// <summary>
        /// Constructor for the rule X -> beta. beta is a string of symbols from symbols (non-terminal)
        /// </summary>
        /// <param name="leftHandSide">Non-terminal symbol X.</param>
        /// <param name="rightHandSide">beta. beta is a string of symbols from symbols (non-terminal)</param>
        /// <param name="type">Type of the rule. TERMINAL if the rule is like X -> a, SINGLE_NON_TERMINAL if the rule is like X -> Y,
        ///             TWO_NON_TERMINAL if the rule is like X -> YZ, MULTIPLE_NON_TERMINAL if the rule is like X -> YZT..</param>
        public Rule(Symbol leftHandSide, List<Symbol> rightHandSide, RuleType type) : this(leftHandSide, rightHandSide)
        {
            this.Type = type;
        }

        /// <summary>
        /// Constructor for any rule from a string. The string is of the form X -> .... The method constructs left hand
        /// side symbol and right hand side symbol(s) from the input string.
        /// </summary>
        /// <param name="rule">String containing the rule. The string is of the form X -> ....</param>
        public Rule(string rule)
        {
            int i;
            var left = rule.Substring(0, rule.IndexOf("->")).Trim();
            var right = rule.Substring(rule.IndexOf("->") + 2).Trim();
            LeftHandSide = new Symbol(left);
            var rightSide = right.Split(" ");
            RightHandSide = new List<Symbol>();
            for (i = 0; i < rightSide.Length; i++)
            {
                RightHandSide.Add(new Symbol(rightSide[i]));
            }
        }

        /// <summary>
        /// Checks if the rule is left recursive or not. A rule is left recursive if it is of the form X -> X..., so its
        /// first symbol of the right side is the symbol on the left side.
        /// </summary>
        /// <returns>True, if the rule is left recursive; false otherwise.</returns>
        public bool LeftRecursive()
        {
            return RightHandSide[0].Equals(LeftHandSide) && Type == RuleType.SINGLE_NON_TERMINAL;
        }

        /// <summary>
        /// In conversion to Chomsky Normal Form, rules like A -> BC... are replaced with A -> X1... and X1 -> BC. This
        /// method replaces B and C non-terminals on the right hand side with X1.
        /// </summary>
        /// <param name="first">Non-terminal symbol B.</param>
        /// <param name="second">Non-terminal symbol C.</param>
        /// <param name="with">Non-terminal symbol X1.</param>
        /// <returns>True, if any replacements has been made; false otherwise.</returns>
        public bool UpdateMultipleNonTerminal(Symbol first, Symbol second, Symbol with)
        {
            int i;
            for (i = 0; i < RightHandSide.Count - 1; i++)
            {
                if (RightHandSide[i].Equals(first) && RightHandSide[i + 1].Equals(second))
                {
                    RightHandSide.RemoveAt(i + 1);
                    RightHandSide.RemoveAt(i);
                    RightHandSide.Insert(i, with);
                    if (RightHandSide.Count == 2)
                    {
                        Type = RuleType.TWO_NON_TERMINAL;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Accessor for the rule type.
        /// </summary>
        /// <returns>Rule type.</returns>
        public RuleType GetRuleType()
        {
            return Type;
        }

        /// <summary>
        /// Accessor for the left hand side.
        /// </summary>
        /// <returns>Left hand side.</returns>
        public Symbol GetLeftHandSide()
        {
            return LeftHandSide;
        }

        /// <summary>
        /// Accessor for the right hand side.
        /// </summary>
        /// <returns>Right hand side.</returns>
        public List<Symbol> GetRightHandSide()
        {
            return RightHandSide;
        }

        /// <summary>
        /// Returns number of symbols on the right hand side.
        /// </summary>
        /// <returns>Number of symbols on the right hand side.</returns>
        public int GetRightHandSideSize()
        {
            return RightHandSide.Count;
        }

        /// <summary>
        /// Returns symbol at position index on the right hand side.
        /// </summary>
        /// <param name="index">Position of the symbol</param>
        /// <returns>Symbol at position index on the right hand side.</returns>
        public Symbol GetRightHandSideAt(int index)
        {
            return RightHandSide[index];
        }

        /// <summary>
        /// Converts the rule to the form X -> ...
        /// </summary>
        /// <returns>String form of the rule in the form of X -> ...</returns>
        public override string ToString()
        {
            var result = LeftHandSide + " -> ";
            foreach (Symbol symbol in RightHandSide){
                result = result + " " + symbol;
            }
            return result;
        }
    }
}