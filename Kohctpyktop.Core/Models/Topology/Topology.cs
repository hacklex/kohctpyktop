using System.Collections.Generic;
using System.Linq;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Field.ValuesFunctions;

namespace Kohctpyktop.Models.Topology
{
    public class Topology
    {
        public Topology(CellAssignments[,] cellMappings, IEnumerable<SchemeGate> gates,
            IEnumerable<SchemeNode> nodes,
            IReadOnlyCollection<Pin> pins,
            IReadOnlyDictionary<string, ValuesFunction> valuesFunctions)
        {
            CellMappings = cellMappings;
            Nodes = nodes.ToArray();
            Gates = gates.ToArray();
            Pins = pins;
            ValuesFunctions = valuesFunctions;
        }

        public CellAssignments[,] CellMappings { get; }
        public IReadOnlyList<SchemeNode> Nodes { get; }
        public IReadOnlyList<SchemeGate> Gates { get; }
        public IReadOnlyCollection<Pin> Pins { get; }
        public IReadOnlyDictionary<string, ValuesFunction> ValuesFunctions { get; }
    }
}