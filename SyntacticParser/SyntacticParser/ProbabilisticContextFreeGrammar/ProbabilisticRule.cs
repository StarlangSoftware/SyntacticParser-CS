using System;
using System.Collections.Generic;
using ParseTree;
using SyntacticParser.ContextFreeGrammar;

namespace SyntacticParser.ProbabilisticContextFreeGrammar
{
    public class ProbabilisticRule : Rule
    {
        private double _probability;
        private int _count;

        public ProbabilisticRule(Symbol leftHandSide, List<Symbol> rightHandSide, RuleType type, double probability) : base(leftHandSide, rightHandSide, type)
        {
            this._probability = probability;
        }

        public ProbabilisticRule(Symbol leftHandSide, List<Symbol> rightHandSideSymbol) : base(leftHandSide, rightHandSideSymbol)
        {
        }

        public ProbabilisticRule(string rule)
        {
            int i;
            String prob = rule.Substring(rule.IndexOf('[') + 1, rule.IndexOf(']') - rule.IndexOf('[') - 1);
            String left = rule.Substring(0, rule.IndexOf("->")).Trim();
            String right = rule.Substring(rule.IndexOf("->") + 2, rule.IndexOf('[') - rule.IndexOf("->") - 2).Trim();
            LeftHandSide = new Symbol(left);
            String[] rightSide = right.Split(" ");
            RightHandSide = new List<Symbol>();
            for (i = 0; i < rightSide.Length; i++)
            {
                RightHandSide.Add(new Symbol(rightSide[i]));
            }

            _probability = double.Parse(prob);
        }

        public double GetProbability()
        {
            return _probability;
        }

        public void Increment()
        {
            _count++;
        }

        public void NormalizeProbability(int total)
        {
            _probability = _count / (total + 0.0);
        }

        public int GetCount()
        {
            return _count;
        }

        public override string ToString() 
        {
            return base.ToString() + " [" + _probability + "]";
        }
    }
}