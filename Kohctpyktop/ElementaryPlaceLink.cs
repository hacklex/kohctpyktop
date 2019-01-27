namespace Kohctpyktop
{
    public struct ElementaryPlaceLink
    {
        public Cell Cell { get; set; }
        public bool IsMetalLayer { get; set; }

        public ElementaryPlaceLink(Cell cell, bool isMetalLayer)
        {
            Cell = cell;
            IsMetalLayer = isMetalLayer;
        }
    }
}