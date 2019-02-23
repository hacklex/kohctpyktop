using Kohctpyktop.Models.Topology;

namespace Kohctpyktop.Models.Simulation
{
    // should we store SchemeNode there?
    public class NodeState
    {
        public NodeState(SchemeNode node, bool isHigh)
        {
            Node = node;
            IsHigh = isHigh;
        }

        public SchemeNode Node { get; }
        public bool IsHigh { get; }
    }
}