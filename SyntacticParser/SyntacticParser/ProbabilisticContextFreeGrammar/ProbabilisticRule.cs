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

        /// <summary>
        /// Constructor for the probabilistic rule X -> beta. beta is a string of symbols from symbols (non-terminal)
        /// </summary>
        /// <param name="leftHandSide">Non-terminal symbol X.</param>
        /// <param name="rightHandSide">beta. beta is a string of symbols from symbols (non-terminal)</param>
        /// <param name="type">Type of the rule. TERMINAL if the rule is like X -> a, SINGLE_NON_TERMINAL if the rule is like X -> Y,
        ///             TWO_NON_TERMINAL if the rule is like X -> YZ, MULTIPLE_NON_TERMINAL if the rule is like X -> YZT..</param>
        /// <param name="probability">Probability of the rule</param>
        public ProbabilisticRule(Symbol leftHandSide, List<Symbol> rightHandSide, RuleType type, double probability) : base(leftHandSide, rightHandSide, type)
        {
            this._probability = probability;
        }

        /// <summary>
        /// Constructor for the rule X -> beta. beta is a string of symbols from symbols (non-terminal)
        /// </summary>
        /// <param name="leftHandSide">Non-terminal symbol X.</param>
        /// <param name="rightHandSideSymbol">beta. beta is a string of symbols from symbols (non-terminal)</param>
        public ProbabilisticRule(Symbol leftHandSide, List<Symbol> rightHandSideSymbol) : base(leftHandSide, rightHandSideSymbol)
        {
        }

        /// <summary>
        /// Constructor for any probabilistic rule from a string. The string is of the form X -> .... [probability] The
        /// method constructs left hand side symbol and right hand side symbol(s) from the input string.
        /// </summary>
        /// <param name="rule">String containing the rule. The string is of the form X -> .... [probability]</param>
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

        /// <summary>
        /// Accessor for the probability attribute.
        /// </summary>
        /// <returns>Probability attribute.</returns>
        public double GetProbability()
        {
            return _probability;
        }

        /// <summary>
        /// Increments the count attribute.
        /// </summary>
        public void Increment()
        {
            _count++;
        }

        /// <summary>
        /// Calculates the probability from count and the given total value.
        /// </summary>
        /// <param name="total">Value used for calculating the probability.</param>
        public void NormalizeProbability(int total)
        {
            _probability = _count / (total + 0.0);
        }

        /// <summary>
        /// Accessor for the count attribute
        /// </summary>
        /// <returns>Count attribute</returns>
        public int GetCount()
        {
            return _count;
        }

        /// <summary>
        /// Converts the rule to the form X -> ... [probability]
        /// </summary>
        /// <returns>String form of the rule in the form of X -> ... [probability]</returns>
        public override string ToString() 
        {
            return base.ToString() + " [" + _probability + "]";
        }
    }
}