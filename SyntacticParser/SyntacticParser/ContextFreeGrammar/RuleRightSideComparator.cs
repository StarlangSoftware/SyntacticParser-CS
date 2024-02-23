using System.Collections.Generic;

namespace SyntacticParser.ContextFreeGrammar
{
    public class RuleRightSideComparator : Comparer<Rule>
    {
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