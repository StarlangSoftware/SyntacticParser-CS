using System.Collections.Generic;
using ParseTree;

namespace SyntacticParser.ContextFreeGrammar
{
    public class Rule
    {
        protected Symbol LeftHandSide;
        protected List<Symbol> RightHandSide;
        public RuleType Type;

        public Rule()
        {
        }

        public Rule(Symbol leftHandSide, Symbol rightHandSideSymbol)
        {
            this.LeftHandSide = leftHandSide;
            RightHandSide = new List<Symbol>();
            RightHandSide.Add(rightHandSideSymbol);
        }

        public Rule(Symbol leftHandSide, Symbol rightHandSideSymbol1, Symbol rightHandSideSymbol2) : this(leftHandSide, rightHandSideSymbol1)
        {
            this.RightHandSide.Add(rightHandSideSymbol2);
        }

        public Rule(Symbol leftHandSide, List<Symbol> rightHandSide)
        {
            this.LeftHandSide = leftHandSide;
            this.RightHandSide = rightHandSide;
        }

        public Rule(Symbol leftHandSide, List<Symbol> rightHandSide, RuleType type) : this(leftHandSide, rightHandSide)
        {
            this.Type = type;
        }

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

        public bool LeftRecursive()
        {
            return RightHandSide[0].Equals(LeftHandSide) && Type == RuleType.SINGLE_NON_TERMINAL;
        }

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

        public RuleType GetRuleType()
        {
            return Type;
        }

        public Symbol GetLeftHandSide()
        {
            return LeftHandSide;
        }

        public List<Symbol> GetRightHandSide()
        {
            return RightHandSide;
        }

        public int GetRightHandSideSize()
        {
            return RightHandSide.Count;
        }

        public Symbol GetRightHandSideAt(int index)
        {
            return RightHandSide[index];
        }

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