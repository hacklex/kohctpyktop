using Kohctpyktop.Input;
using Kohctpyktop.Models.Topology;

namespace Kohctpyktop.Rendering
{
    public struct RenderOpts
    {
        public CellAssignments[,] Assignments { get; }
        public SchemeNode HoveredNode { get; }
        public SelectionState SelectionState { get; }
        public Selection Selection { get; }

        public RenderOpts(SelectionState selectionState, Selection selection, CellAssignments[,] assignments, SchemeNode hoveredNode)
        {
            SelectionState = selectionState;
            Selection = selection;
            Assignments = assignments;
            HoveredNode = hoveredNode;
        }
    }
}