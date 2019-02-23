using Kohctpyktop.Models.Topology;

namespace Kohctpyktop.Models.Simulation
{
    public struct GateState
    {
        public GateState(SchemeGate gate, bool isOpen)
        {
            Gate = gate;
            IsOpen = isOpen;
        }

        public SchemeGate Gate { get; }
        public bool IsOpen { get; }
    }
}