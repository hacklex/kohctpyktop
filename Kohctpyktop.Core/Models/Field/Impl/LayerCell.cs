namespace Kohctpyktop.Models.Field
{
    public class LayerCell : ILayerCell
    {
        private readonly LayerCellLinkSet _links;

        public LayerCell(Layer layer, int row, int column)
        {
            Row = row;
            Column = column;
            
            Links = _links = new LayerCellLinkSet(layer, row, column);
            Neighbors = new LayerCellNeighborSet(layer, row, column);
            
            IsValidCell = true;
        }

        public bool IsValidCell { get; }
        
        public int Row { get; }
        public int Column { get; }
        public SiliconTypes Silicon { get; private set; }
        public bool HasMetal { get; private set; }
        public bool IsLocked { get; private set; }
        public string Name { get; private set; }

        public IReadOnlyDirectionalSet<ICellLink> Links { get; }
        public IReadOnlyDirectionalSet<ILayerCell> Neighbors { get; }
        
        public void Apply(CellContent content)
        {
            Silicon = content.Silicon;
            HasMetal = content.HasMetal;
            IsLocked = content.IsLocked;
            Name = content.Name;
        }
        
        public (LinkContent, LinkContent) SaveLinkState()
        {
            var rlink = Links[Side.Right];
            var blink = Links[Side.Bottom];
            return (new LinkContent(rlink.SiliconLink, rlink.HasMetalLink),
                new LinkContent(blink.SiliconLink, blink.HasMetalLink));
        }
        
        public void ApplyLink(Side side, LinkContent content)
        {
            _links.Apply(side, content);
        }
    }
}