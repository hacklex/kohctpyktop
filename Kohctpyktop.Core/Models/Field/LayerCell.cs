using System;

namespace Kohctpyktop.Models.Field
{
    public class StubSet : IReadOnlyDirectionalSet<ICellLink>
    {
        public ICellLink this[int side] => null;

        public ICellLink this[Side side] => null;
    }

    public class LayerCell : ILayerCell
    {
        private readonly Layer _layer;

        public LayerCell(Layer layer, int row, int column)
        {
            _layer = layer;
            Row = row;
            Column = column;
            
            IsValidCell = true;
        }

        public bool IsValidCell { get; }
        
        public int Row { get; }
        public int Column { get; }
        public SiliconTypes Silicon { get; private set; }
        public bool HasMetal { get; private set; }

        public IReadOnlyDirectionalSet<ICellLink> Links { get; } = new StubSet();
        public IReadOnlyDirectionalSet<ILayerCell> Neighbors => throw new NotImplementedException();

        public LayerCellMatrix.CellContent SaveState()
        {
            return new LayerCellMatrix.CellContent(Silicon, HasMetal);
        }
        
        public void Apply(LayerCellMatrix.CellContent content)
        {
            Silicon = content.Silicon;
            HasMetal = content.HasMetal;
        }
    }
}