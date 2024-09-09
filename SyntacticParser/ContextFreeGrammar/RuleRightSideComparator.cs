using System.Collections.Generic;

namespace SyntacticParser.ContextFreeGrammar
{
    public class RuleRightSideComparator : Comparer<Rule>
    {
        /// <summary>
        /// Compares two rules based on their right sides lexicographically.
        /// </summary>
        /// <param name="ruleA">the first rule to be compared.</param>
        /// <param name="ruleB">the second rule to be compared.</param>
        /// <returns>-1 if the first rule is less than the second rule lexicographically, 1 if the first rule is larger than
        ///          the second rule lexicographically, 0 if they are the same rule.</returns>
        public override int Compare(Rule ruleA, Rule ruleB)
        {
            var i = 0;
            while (i < ruleA.GetRightHandSideSize() && i < ruleB.GetRightHandSideSize()){
                if (ruleA.GetRightHandSideAt(i).GetName().Equals(ruleB.GetRightHandSideAt(i).GetName())){
                    i++;
                } else {
                    return ruleA.GetRightHandSideAt(i).GetName().CompareTo(ruleB.GetRightHandSideAt(i).GetName());
                }
            }
            if (ruleA.GetRightHandSideSize() < ruleB.GetRightHandSideSize()){
                return -1;
            } else {
                if (ruleA.GetRightHandSideSize() > ruleB.GetRightHandSideSize()){
                    return 1;
                } else {
                    return 0;
                }
            }
        }
    }
}