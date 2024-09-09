using System.Collections.Generic;
using Corpus;

namespace SyntacticParser.SyntacticParser
{
    public interface SyntacticParser
    {
        List<ParseTree.ParseTree> Parse(ContextFreeGrammar.ContextFreeGrammar cfg, Sentence sentence);
    }
}