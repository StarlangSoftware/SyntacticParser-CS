using ParseTree;

namespace SyntacticParser.ProbabilisticContextFreeGrammar
{
    public class ProbabilisticParseNode : ParseNode
    {
        private double _logProbability;

        public ProbabilisticParseNode(ParseNode left, ParseNode right, Symbol data, double logProbability) : base(left, right, data){
            this._logProbability = logProbability;
        }

        public ProbabilisticParseNode(ParseNode left, Symbol data, double logProbability) : base(left, data){
            this._logProbability = logProbability;
        }

        public ProbabilisticParseNode(Symbol data, double logProbability) : base(data){
            this._logProbability = logProbability;
        }

        public double GetLogProbability(){
            return _logProbability;
        }
    }
}