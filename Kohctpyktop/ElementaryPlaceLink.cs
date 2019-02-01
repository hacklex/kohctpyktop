namespace Kohctpyktop
{
    public struct ElementaryPlaceLink
    {
        public Cell Cell { get; set; }
        public bool IsMetalLayer { get; set; }

        public bool IsSameAs(ElementaryPlaceLink other) =>
            Cell.Row == other.Cell.Row && Cell.Col == other.Cell.Col && IsMetalLayer == other.IsMetalLayer;

        public ElementaryPlaceLink(Cell cell, bool isMetalLayer)
        {
            Cell = cell;
            IsMetalLayer = isMetalLayer;
        }
    }
}