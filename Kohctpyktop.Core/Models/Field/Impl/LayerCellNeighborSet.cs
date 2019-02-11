using System;
using System.Collections;
using System.Collections.Generic;

namespace Kohctpyktop.Models.Field
{
    public class LayerCellNeighborSet : IReadOnlyDirectionalSet<ILayerCell>
    {
        private readonly Layer _layer;
        private readonly int _row;
        private readonly int _column;

        public LayerCellNeighborSet(Layer layer, int row, int column)
        {
            _layer = layer;
            _row = row;
            _column = column;
        }

        public ILayerCell this[int side] => this[(Side) side];

        public ILayerCell this[Side side]
        {
            get
            {
                switch (side)
                {
                    case Side.Left: return _layer.Cells[_row, _column - 1];
                    case Side.Top: return _layer.Cells[_row - 1, _column];
                    case Side.Right: return _layer.Cells[_row, _column + 1];
                    case Side.Bottom: return _layer.Cells[_row + 1, _column];;
                    default: throw new ArgumentException(nameof(side));
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public IEnumerator<ILayerCell> GetEnumerator()
        {
            IEnumerable<ILayerCell> Enumerable()
            {
                for (var i = 0; i < 4; i++) yield return this[i];
            }

            return Enumerable().GetEnumerator();
        }
    }
}