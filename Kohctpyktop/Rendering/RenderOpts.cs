using Kohctpyktop.Input;

namespace Kohctpyktop.Rendering
{
    public struct RenderOpts
    {
        public SelectionState SelectionState { get; }
        public Selection Selection { get; }

        public RenderOpts(SelectionState selectionState, Selection selection)
        {
            SelectionState = selectionState;
            Selection = selection;
        }
    }
}