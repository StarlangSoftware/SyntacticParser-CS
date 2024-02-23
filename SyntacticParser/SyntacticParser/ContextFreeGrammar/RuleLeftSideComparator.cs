using System.Collections.Generic;

namespace SyntacticParser.ContextFreeGrammar
{
    public class RuleLeftSideComparator : Comparer<Rule>
    {
        public override int Compare(Rule ruleA, Rule ruleB)
        {
            return ruleA.GetLeftHandSide().GetName().CompareTo(ruleB.GetLeftHandSide().GetName());
        }
    }
}