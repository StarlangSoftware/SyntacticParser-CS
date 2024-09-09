using ParseTree;

namespace SyntacticParser.ProbabilisticContextFreeGrammar
{
    public class ProbabilisticParseNode : ParseNode
    {
        private double _logProbability;

        /// <summary>
        /// Constructor for the ProbabilisticParseNode class. Extends the parse node with a probability.
        /// </summary>
        /// <param name="left">Left child of this node.</param>
        /// <param name="right">Right child of this node.</param>
        /// <param name="data">Data for this node.</param>
        /// <param name="logProbability">Logarithm of the probability of the node.</param>
        public ProbabilisticParseNode(ParseNode left, ParseNode right, Symbol data, double logProbability) : base(left, right, data){
            this._logProbability = logProbability;
        }

        /// <summary>
        /// Another constructor for the ProbabilisticParseNode class.
        /// </summary>
        /// <param name="left">Left child of this node.</param>
        /// <param name="data">Data for this node.</param>
        /// <param name="logProbability">Logarithm of the probability of the node.</param>
        public ProbabilisticParseNode(ParseNode left, Symbol data, double logProbability) : base(left, data){
            this._logProbability = logProbability;
        }

        /// <summary>
        /// Another constructor for the ProbabilisticParseNode class.
        /// </summary>
        /// <param name="data">Data for this node.</param>
        /// <param name="logProbability">Logarithm of the probability of the node.</param>
        public ProbabilisticParseNode(Symbol data, double logProbability) : base(data){
            this._logProbability = logProbability;
        }

        /// <summary>
        /// Accessor for the logProbability attribute.
        /// </summary>
        /// <returns>logProbability attribute.</returns>
        public double GetLogProbability(){
            return _logProbability;
        }
    }
}