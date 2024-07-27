using System.Collections.Generic;

namespace SyntacticParser.ContextFreeGrammar
{
    public class RuleComparator : Comparer<Rule>
    {
        /// <summary>
        /// Compares two rules based on first their left hand side and their right hand side lexicographically.
        /// </summary>
        /// <param name="ruleA">the first rule to be compared.</param>
        /// <param name="ruleB">the second rule to be compared.</param>
        /// <returns>-1 if the first rule is less than the second rule lexicographically, 1 if the first rule is larger than
        /// the second rule lexicographically, 0 if they are the same rule.</returns>
        public override int Compare(Rule ruleA, Rule ruleB)
        {
            if (ruleA.GetLeftHandSide().Equals(ruleB.GetLeftHandSide())){
                Comparer<Rule> rightComparator = new RuleRightSideComparator();
                return rightComparator.Compare(ruleA, ruleB);
            } else {
                Comparer<Rule> leftComparator = new RuleLeftSideComparator();
                return leftComparator.Compare(ruleA, ruleB);
            }
        }
    }
}