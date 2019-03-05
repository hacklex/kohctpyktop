using System.Collections.Generic;
using System.Linq;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Topology
{
    /// <summary>
    /// Handles groups of gates directly following each other.
    /// A singular gate is counted as a group of one.
    /// <!-- todo: make object immutable --> 
    /// </summary>
    public class SchemeGateTemplate
    {
        /// <summary>
        /// Arrays of one or two inputs OR'ed for each consecutive gate
        /// </summary>
        public List<SchemeNodeTemplate[]> GateInputs { get; set; } = new List<SchemeNodeTemplate[]>();
        /// <summary>
        /// The exactly 2 nodes connected or disconnected by the gate
        /// </summary>
        public List<SchemeNodeTemplate> GatePowerNodes { get; set; } = new List<SchemeNodeTemplate>();
        /// <summary>
        /// true if the gate is open if and only if the input is low (PNP)
        /// otherwise the gate is open if and only if the input is high (NPN)
        /// </summary>
        public bool IsInversionGate { get; set; }

        public List<ILayerCell> GateCells { get; } = new List<ILayerCell>();
        
        public SchemeGate Freeze(IReadOnlyDictionary<SchemeNodeTemplate, SchemeNode> frozenNodes) => new SchemeGate(
            GateInputs.Select(x => (IReadOnlyList<SchemeNode>) x.Select(y => frozenNodes[y]).ToArray()).ToArray(),
            GatePowerNodes.Select(x => frozenNodes[x]).ToArray(),
            IsInversionGate,
            GateCells.ToArray());
    }
    
    public class SchemeGate
    {
        public SchemeGate(
            IReadOnlyList<IReadOnlyList<SchemeNode>> gateInputs,
            IReadOnlyList<SchemeNode> gatePowerNodes,
            bool isInversionGate,
            IReadOnlyList<ILayerCell> gateCells)
        {
            GateInputs = gateInputs;
            GatePowerNodes = gatePowerNodes;
            IsInversionGate = isInversionGate;
            GateCells = gateCells;
        }

        /// <summary>
        /// Arrays of one or two inputs OR'ed for each consecutive gate
        /// </summary>
        public IReadOnlyList<IReadOnlyList<SchemeNode>> GateInputs { get; }
        /// <summary>
        /// The exactly 2 nodes connected or disconnected by the gate
        /// </summary>
        public IReadOnlyList<SchemeNode> GatePowerNodes { get; }
        /// <summary>
        /// true if the gate is open if and only if the input is low (PNP)
        /// otherwise the gate is open if and only if the input is high (NPN)
        /// </summary>
        public bool IsInversionGate { get; }

        public IReadOnlyList<ILayerCell> GateCells { get; }
    }
}