using System.Collections.Generic;
using Corpus;

namespace SyntacticParser.ProbabilisticParser
{
    public interface ProbabilisticParser
    {
        List<ParseTree.ParseTree> Parse(ProbabilisticContextFreeGrammar.ProbabilisticContextFreeGrammar pCfg,
            Sentence sentence);
    }
}