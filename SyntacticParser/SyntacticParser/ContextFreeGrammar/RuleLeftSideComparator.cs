using System.Collections.Generic;

namespace SyntacticParser.ContextFreeGrammar
{
    public class RuleLeftSideComparator : Comparer<Rule>
    {
        /// <summary>
        /// Compares two rules based on their left sides lexicographically.
        /// </summary>
        /// <param name="ruleA">the first rule to be compared.</param>
        /// <param name="ruleB">the second rule to be compared.</param>
        /// <returns>-1 if the first rule is less than the second rule lexicographically, 1 if the first rule is larger than
        ///          the second rule lexicographically, 0 if they are the same rule.</returns>
        public override int Compare(Rule ruleA, Rule ruleB)
        {
            return ruleA.GetLeftHandSide().GetName().CompareTo(ruleB.GetLeftHandSide().GetName());
        }
    }
}