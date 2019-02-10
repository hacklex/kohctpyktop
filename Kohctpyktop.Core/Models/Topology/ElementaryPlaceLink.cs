using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Topology
{
    public struct ElementaryPlaceLink
    {
        public ILayerCell Cell { get; set; }
        public bool IsMetalLayer { get; set; }

        public bool IsSameAs(ElementaryPlaceLink other) =>
            Cell.Row == other.Cell.Row && Cell.Column == other.Cell.Column && IsMetalLayer == other.IsMetalLayer;

        public ElementaryPlaceLink(ILayerCell cell, bool isMetalLayer)
        {
            Cell = cell;
            IsMetalLayer = isMetalLayer;
        }
    }
}