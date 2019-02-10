using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;

namespace Kohctpyktop.Models.Field
{
    public class InvalidLink : ICellLink
    {
        private InvalidLink() {}
        
        public static InvalidLink Instance { get; } = new InvalidLink();
        
        public bool IsValidLink => false;
        public ILayerCell SourceCell => InvalidCell.Instance;
        public ILayerCell TargetCell => InvalidCell.Instance;
        public SiliconLink SiliconLink => SiliconLink.None;
        public bool HasMetalLink => false;
        public ICellLink Inverted => this;
    }
    
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
        public IReadOnlyDirectionalSet<ICellLink> Links { get; }
        public IReadOnlyDirectionalSet<ILayerCell> Neighbors { get; }
    }

    public class LayerCellLinkSet : IReadOnlyDirectionalSet<ICellLink>
    {
        private class Link : ICellLink
        {
            private readonly Link _invLink;
            private SiliconLink _siliconLink;
            private bool _hasMetalLink;

            public Link() => _invLink = new Link(this);

            private Link(Link invLink) => _invLink = invLink;
            
            public bool IsValidLink => true;
            
            public ILayerCell SourceCell => throw new NotImplementedException();
            public ILayerCell TargetCell => throw new NotImplementedException();
            
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
            
            _rlink = new Link();
            _blink = new Link();
        }

        public ICellLink this[int side] => this[(Side) side];

        public ICellLink this[Side side]
        {
            get
            {
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

        public void Apply(Side side, LayerCellMatrix.LinkContent content)
        {
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

        public IReadOnlyDirectionalSet<ICellLink> Links { get; }
        public IReadOnlyDirectionalSet<ILayerCell> Neighbors { get; }

        public LayerCellMatrix.CellContent SaveState()
        {
            return new LayerCellMatrix.CellContent(Silicon, HasMetal);
        }
        
        public void Apply(LayerCellMatrix.CellContent content)
        {
            Silicon = content.Silicon;
            HasMetal = content.HasMetal;
        }
        
        public (LayerCellMatrix.LinkContent, LayerCellMatrix.LinkContent) SaveLinkState()
        {
            var rlink = Links[Side.Right];
            var blink = Links[Side.Bottom];
            return (new LayerCellMatrix.LinkContent(rlink.SiliconLink, rlink.HasMetalLink),
                new LayerCellMatrix.LinkContent(blink.SiliconLink, blink.HasMetalLink));
        }
        
        public void ApplyLink(Side side, LayerCellMatrix.LinkContent content)
        {
            _links.Apply(side, content);
        }
    }
}