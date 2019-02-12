using System;
using System.Collections;
using System.Collections.Generic;

namespace Kohctpyktop.Models.Field
{
    public class LayerCellLinkSet : IReadOnlyDirectionalSet<ICellLink>
    {
        private class Link : ICellLink
        {
            private readonly Layer _layer;
            private readonly int _sourceRow;
            private readonly int _sourceColumn;
            private readonly int _targetRow;
            private readonly int _targetColumn;
            
            private readonly Link _invLink;
            private SiliconLink _siliconLink;
            private bool _hasMetalLink;

            public Link(Layer layer, int sourceRow, int sourceColumn, int targetRow, int targetColumn)
            {
                _layer = layer;
                _sourceRow = sourceRow;
                _sourceColumn = sourceColumn;
                _targetRow = targetRow;
                _targetColumn = targetColumn;
                _invLink = new Link(this, layer, targetRow, targetColumn, sourceRow, sourceColumn);
            }

            private Link(Link invLink, Layer layer, int sourceRow, int sourceColumn, int targetRow, int targetColumn)
            {
                _layer = layer;
                _sourceRow = sourceRow;
                _sourceColumn = sourceColumn;
                _targetRow = targetRow;
                _targetColumn = targetColumn;
                _invLink = invLink;
            }

            public bool IsValidLink => SourceCell.IsValidCell && TargetCell.IsValidCell;

            public ILayerCell SourceCell => _layer.Cells[_sourceRow, _sourceColumn];
            public ILayerCell TargetCell => _layer.Cells[_targetRow, _targetColumn];
            
            public SiliconLink SiliconLink
            {
                get => _siliconLink;
                set
                {
                    _siliconLink = value;
                    _invLink._siliconLink = value.Invert();
                }
            }

            public bool HasMetalLink
            {
                get => _hasMetalLink;
                set
                {
                    _hasMetalLink = value;
                    _invLink._hasMetalLink = value;
                }
            }

            public ICellLink Inverted => _invLink;
        }

        private readonly Layer _layer;
        private readonly int _row;
        private readonly int _column;
        private readonly Link _rlink, _blink;

        public LayerCellLinkSet(Layer layer, int row, int column)
        {
            _layer = layer;
            _row = row;
            _column = column;

            if (_layer != null)
            {
                _rlink = new Link(_layer, row, column, row, column + 1);
                _blink = new Link(_layer, row, column, row + 1, column);
            }
        }

        public ICellLink this[int side] => this[(Side) side];

        public ICellLink this[Side side]
        {
            get
            {
                if (_layer == null) return InvalidLink.Instance;
                
                switch (side)
                {
                    case Side.Right: return _rlink;
                    case Side.Bottom: return _blink;
                    case Side.Left: return _layer.Cells[_row, _column - 1].Links[Side.Right].Inverted;
                    case Side.Top: return _layer.Cells[_row - 1, _column].Links[Side.Bottom].Inverted;
                    default: throw new ArgumentException(nameof(side));
                }
            }
        }

        public void Apply(Side side, LinkContent content)
        {
            if (_layer == null) return;
            
            switch (side)
            {
                case Side.Right:
                    _rlink.SiliconLink = content.SiliconLink;
                    _rlink.HasMetalLink = content.HasMetalLink;
                    break;
                case Side.Bottom:
                    _blink.SiliconLink = content.SiliconLink;
                    _blink.HasMetalLink = content.HasMetalLink;
                    break;
                default: throw new ArgumentException(nameof(side));
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public IEnumerator<ICellLink> GetEnumerator()
        {
            IEnumerable<ICellLink> Enumerable()
            {
                for (var i = 0; i < 4; i++) yield return this[i];
            }

            return Enumerable().GetEnumerator();
        }
    }
}