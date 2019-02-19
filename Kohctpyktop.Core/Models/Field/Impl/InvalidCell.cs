namespace Kohctpyktop.Models.Field
{
    public class InvalidCell : ILayerCell
    {
        private InvalidCell()
        {
            Links = new LayerCellLinkSet(null, -2, -2);
            Neighbors = new LayerCellNeighborSet(null, -2, -2);
        }

        public static InvalidCell Instance { get; } = new InvalidCell();

        public bool IsValidCell => false;
        public int Row => -1;
        public int Column => -1;
        public SiliconTypes Silicon => SiliconTypes.None;
        public bool HasMetal => false;
        public bool IsLocked => true;
        public Pin Pin { get; } = null;
        public string Name => null;
        public IReadOnlyDirectionalSet<ICellLink> Links { get; }
        public IReadOnlyDirectionalSet<ILayerCell> Neighbors { get; }
    }
}