using System.Collections.Generic;

namespace SyntacticParser.ContextFreeGrammar
{
    public class RuleComparator : Comparer<Rule>
    {
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