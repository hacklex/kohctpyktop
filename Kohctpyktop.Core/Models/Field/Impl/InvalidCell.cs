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
        public SiliconLayerContent Silicon => SiliconLayerContent.None;
        public bool HasMetal => false;
        public Pin Pin { get; } = null;
        public IReadOnlyDirectionalSet<ICellLink> Links { get; }
        public IReadOnlyDirectionalSet<ILayerCell> Neighbors { get; }
    }
}