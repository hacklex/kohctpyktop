namespace Kohctpyktop.Models.Field
{
    public sealed class LayerData
    {
        public LayerData(CellContent[,] cells, LinkContent[,] rightLinks, LinkContent[,] bottomLinks)
        {
            Cells = cells;
            RightLinks = rightLinks;
            BottomLinks = bottomLinks;
        }

        public CellContent[,] Cells { get; }
        public LinkContent[,] RightLinks { get; }
        public LinkContent[,] BottomLinks { get; }
    }
    
    public enum Side { Left, Top, Right, Bottom }

    public enum LinkType { SiliconLink, MetalLink }
    
    public interface ILayer : ISupportsUndoRedo
    {
        int Width { get; }
        int Height { get; }
        
        IReadOnlyMatrix<ILayerCell> Cells { get; }

        bool AddCellSilicon(Position position, SiliconType siliconType);
        bool RemoveCellSilicon(Position position);
        
        bool AddCellMetal(Position position);
        bool RemoveCellMetal(Position position);

        bool AddLink(Position from, Position to, LinkType linkType);
        bool RemoveLink(Position from, Position to, LinkType linkType);

        bool AddVia(Position position);
        bool RemoveVia(Position position);
        bool SetCellPin(Position position, Pin pin);
        
        bool MoveCells(Position from, Position to, int offsetX, int offsetY);

        LayerData ExportLayerData();
    }
}