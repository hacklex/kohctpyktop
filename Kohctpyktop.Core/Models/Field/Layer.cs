using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Kohctpyktop.Models.Field
{
    public class LayerCellMatrix : IReadOnlyMatrix<ILayerCell>
    {
        private const long MaxUndoRedoDepth = 10;

        public struct CellContent
        {
            public CellContent(SiliconTypes silicon, bool hasMetal)
            {
                Silicon = silicon;
                HasMetal = hasMetal;
            }

            public SiliconTypes Silicon { get; }
            public bool HasMetal { get; }
        }
        
        public struct LinkContent
        {
            public LinkContent(SiliconLink siliconLink, bool hasMetalLink)
            {
                SiliconLink = siliconLink;
                HasMetalLink = hasMetalLink;
            }
            
            public SiliconLink SiliconLink { get; }
            public bool HasMetalLink { get; }
        }
        
        private class CellNode
        {
            private CellContent _savedCellContent;

            public CellNode(Layer layer, int row, int column)
            {
                HostedCell = new LayerCell(layer, row, column);
            }
            
            public Dictionary<int, CellContent> SavedStates { get; } = new Dictionary<int, CellContent>();
            public LayerCell HostedCell { get; }

            public void CommitChanges(int transactionId)
            {
                SavedStates[transactionId] = _savedCellContent;
                _savedCellContent = HostedCell.SaveState();
            }

            public void RejectChanges()
            {
                HostedCell.Apply(_savedCellContent);
            }

            public void Update(CellContent cellContent)
            {
                HostedCell.Apply(cellContent);
            }

            public void Restore(int transactionId)
            {
                if (SavedStates.TryGetValue(transactionId, out var savedState))
                {
                    SavedStates.Remove(transactionId);
                    
                    _savedCellContent = savedState;
                    RejectChanges();
                }
            }
        }
        
        private class LinkNode
        {
            private LinkContent _savedRightLinkContent, _savedBottomLinkContent;

            public LinkNode(LayerCell hostedCell)
            {
                HostedCell = hostedCell;
            }
            
            public Dictionary<int, (LinkContent, LinkContent)> SavedStates { get; } = new Dictionary<int, (LinkContent, LinkContent)>();
            public LayerCell HostedCell { get; }

            public void CommitChanges(int transactionId)
            {
                SavedStates[transactionId] = (_savedRightLinkContent, _savedBottomLinkContent);
                (_savedRightLinkContent, _savedBottomLinkContent) = HostedCell.SaveLinkState();
            }

            public void RejectChanges()
            {
                HostedCell.ApplyLink(Side.Right, _savedRightLinkContent);
                HostedCell.ApplyLink(Side.Bottom, _savedBottomLinkContent);
            }

            public void Update(Side side, LinkContent linkContent)
            {
                HostedCell.ApplyLink(side, linkContent);
            }

            public void Restore(int transactionId)
            {
                if (SavedStates.TryGetValue(transactionId, out var savedState))
                {
                    SavedStates.Remove(transactionId);

                    _savedRightLinkContent = savedState.Item1;
                    _savedBottomLinkContent = savedState.Item2;
                    RejectChanges();
                }
            }
        }
        
        private readonly Layer _layer;
        private readonly CellNode[,] _cellNodes;
        private readonly LinkNode[,] _linkNodes;

        private int _transactionId;

        public LayerCellMatrix(Layer layer)
        {
            _layer = layer;
            _cellNodes = new CellNode[RowCount, ColumnCount];
            _linkNodes = new LinkNode[RowCount, ColumnCount];
            
            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
            {
                _cellNodes[i, j] = new CellNode(_layer, i, j);
                _linkNodes[i, j] = new LinkNode(_cellNodes[i, j].HostedCell);
            }
        }

        public int RowCount => _layer.Height;
        public int ColumnCount => _layer.Width;

        public ILayerCell this[int row, int column] =>
            row < 0 || column < 0 || row >= RowCount || column >= ColumnCount
                ? (ILayerCell) InvalidCell.Instance
                : _cellNodes[row, column].HostedCell;

        public ILayerCell this[Position position] => this[position.Row, position.Col];

        public void CommitChanges()
        {
            if (!HasUncommitedChanges) return;
            
            _transactionId++;
            
            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
            {
                _cellNodes[i, j].CommitChanges(_transactionId);
                _linkNodes[i, j].CommitChanges(_transactionId);
            }

            HasUncommitedChanges = false;
        }

        public void RejectChanges()
        {
            if (!HasUncommitedChanges) return;
            
            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
            {
                _cellNodes[i, j].RejectChanges();
                _linkNodes[i, j].RejectChanges();
            }

            HasUncommitedChanges = false;
        }
        
        public void UpdateCellContent(Position position, CellContent cellContent)
        {
            _cellNodes[position.Row, position.Col].Update(cellContent);
            HasUncommitedChanges = true;
        }

        private static LinkContent Invert(LinkContent content)
        {
            return new LinkContent(content.SiliconLink.Invert(), content.HasMetalLink);
        }

        private static (Position, Side, LinkContent) NormalizeLinkPosition(Position position, Side side, LinkContent content)
        {
            switch (side)
            {
                case Side.Right:
                case Side.Bottom:
                    return (position, side, content);
                case Side.Left:
                    return (position.Offset(-1, 0), Side.Right, Invert(content));
                case Side.Top:
                    return (position.Offset(0, -1), Side.Bottom, Invert(content));
                default: throw new ArgumentException(nameof(side));
            }
        }
        
        public void UpdateLinkContent(Position position, Side side, LinkContent linkContent)
        {
            (position, side, linkContent) = NormalizeLinkPosition(position, side, linkContent);
            
            _linkNodes[position.Row, position.Col].Update(side, linkContent);
            HasUncommitedChanges = true;
        }

        public void Undo()
        {
            if (HasUncommitedChanges)
            {
                RejectChanges();
                return;
            }

            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
            {
                _cellNodes[i, j].Restore(_transactionId);
                _linkNodes[i, j].Restore(_transactionId);
            }

            _transactionId--;
        }

        public bool HasUncommitedChanges { get; private set; }
    }
    
    public class Layer : ILayer
    {
        private readonly LayerCellMatrix _cellMatrix;
        
        public Layer(int width, int height)
        {
            Width = width;
            Height = height;
            
            _cellMatrix = new LayerCellMatrix(this);
        }
        
        public int Width { get; }
        public int Height { get; }

        public IReadOnlyMatrix<ILayerCell> Cells => _cellMatrix;

        public bool HasUncommitedChanges => _cellMatrix.HasUncommitedChanges;
        
        public void CommitChanges() => _cellMatrix.CommitChanges();
        public void RejectChanges() => _cellMatrix.RejectChanges();

        public int MaxUndoDepth { get; set; }
        public int MaxRedoDepth { get; set; }

        public bool CanUndo => true;
        
        public void Undo()
        {
            _cellMatrix.Undo();
        }

        public bool CanRedo { get; }
        public void Redo()
        {
            throw new System.NotImplementedException();
        }

        private void RemoveCellLinks(Position position, LinkType type)
        {
            RemoveLink(position, Side.Left, type);
            RemoveLink(position, Side.Top, type);
            RemoveLink(position, Side.Right, type);
            RemoveLink(position, Side.Bottom, type);
        }
        
        public bool AddCellSilicon(Position position, SiliconType siliconType)
        {
            var cell = _cellMatrix[position];
            if (cell.Silicon != SiliconTypes.None) return false;

            if (!cell.IsValidCell) return false;

            var slcType = siliconType == SiliconType.NType ? SiliconTypes.NType : SiliconTypes.PType;
            
            _cellMatrix.UpdateCellContent(position, new LayerCellMatrix.CellContent(slcType, cell.HasMetal));
            return true;
        }

        public bool RemoveCellSilicon(Position position)
        {
            var cell = _cellMatrix[position];
            if (cell.Silicon == SiliconTypes.None) return false;
            
            if (!cell.IsValidCell) return false;

            _cellMatrix.UpdateCellContent(position, new LayerCellMatrix.CellContent(SiliconTypes.None, cell.HasMetal));
            RemoveCellLinks(position, LinkType.SiliconLink);
            
            return true;
        }

        public bool AddCellMetal(Position position)
        {
            var cell = _cellMatrix[position];
            if (cell.HasMetal) return false;
            
            if (!cell.IsValidCell) return false;

            _cellMatrix.UpdateCellContent(position, new LayerCellMatrix.CellContent(cell.Silicon, true));
            return true;
        }

        public bool RemoveCellMetal(Position position)
        {
            var cell = _cellMatrix[position];
            if (!cell.HasMetal) return false;
            
            if (!cell.IsValidCell) return false;

            _cellMatrix.UpdateCellContent(position, new LayerCellMatrix.CellContent(cell.Silicon, false));
            RemoveCellLinks(position, LinkType.MetalLink);
            
            return true;
        }

        private (bool, SiliconLink, SiliconTypes) CheckSiliconLink(ILayerCell fromCell, ILayerCell toCell, Side side)
        {
            var fromBase = fromCell.IsBaseN() ? SiliconType.NType : SiliconType.PType;
            var toBase = toCell.IsBaseN() ? SiliconType.NType : SiliconType.PType;

            if (fromBase != toBase)
            {
                var (p1, p2) = side.GetPerpendicularSides();

                return toCell.Links[p1].SiliconLink == SiliconLink.BiDirectional &&
                       toCell.Links[p2].SiliconLink == SiliconLink.BiDirectional &&
                       !toCell.HasVia()
                    ? (true, SiliconLink.Master, toCell.HasGate() ? toCell.Silicon : toBase.ConvertToGate(!side.IsVertical()))
                    : (false, SiliconLink.None, SiliconTypes.None);
            }
            else
            {
                return fromCell.HasGate() || toCell.HasGate()
                    ? (false, SiliconLink.None, SiliconTypes.None)
                    : (true, SiliconLink.BiDirectional, toCell.Silicon);
            }
        }
        
        public bool AddLink(Position from, Position to, LinkType linkType)
        {
            if (!from.IsAdjacent(to))
                return false;
            
            var fromCell = _cellMatrix[from];
            var toCell = _cellMatrix[to];
            var side = from.GetAdjacentSide(to);
            
            var existingLink = fromCell.Links[side];
            
            switch (linkType)
            {
                case LinkType.SiliconLink:
                    if (existingLink.SiliconLink != SiliconLink.None ||
                        !fromCell.HasSilicon() ||
                        !toCell.HasSilicon()) return false;

                    var (canPlace, newLinkType, targetSlc) = CheckSiliconLink(fromCell, toCell, side);
                    if (canPlace)
                    {
                        _cellMatrix.UpdateCellContent(to, new LayerCellMatrix.CellContent(targetSlc, toCell.HasMetal));
                        _cellMatrix.UpdateLinkContent(from, side, new LayerCellMatrix.LinkContent(newLinkType, existingLink.HasMetalLink));
                        return true;
                    }

                    return false;

                case LinkType.MetalLink:
                    if (existingLink.HasMetalLink ||
                        !fromCell.HasMetal ||
                        !toCell.HasMetal) return false;
                    
                    _cellMatrix.UpdateLinkContent(from, side, new LayerCellMatrix.LinkContent(existingLink.SiliconLink, true));
                    return true;
                default:
                    throw new ArgumentException(nameof(linkType));
            }
        }

        public bool RemoveLink(Position from, Position to, LinkType linkType)
        {
            if (!from.IsAdjacent(to))
                return false;
            
            var fromCell = _cellMatrix[from];
            var toCell = _cellMatrix[to];
            var side = from.GetAdjacentSide(to);
            
            var existingLink = fromCell.Links[side];
            
            switch (linkType)
            {
                case LinkType.SiliconLink:
                    if (existingLink.SiliconLink == SiliconLink.None) return false;
                    
                    _cellMatrix.UpdateLinkContent(from, side, new LayerCellMatrix.LinkContent(SiliconLink.None, existingLink.HasMetalLink));
                    
                    if (fromCell.HasGate()) // todo: double gates 
                        _cellMatrix.UpdateCellContent(from, new LayerCellMatrix.CellContent(fromCell.Silicon.RemoveGate(), fromCell.HasMetal));
                    if (toCell.HasGate()) 
                        _cellMatrix.UpdateCellContent(to, new LayerCellMatrix.CellContent(toCell.Silicon.RemoveGate(), toCell.HasMetal));
                        
                    return true;
                case LinkType.MetalLink:
                    if (!existingLink.HasMetalLink) return false;
                    
                    _cellMatrix.UpdateLinkContent(from, side, new LayerCellMatrix.LinkContent(existingLink.SiliconLink, false));
                    return true;
                default:
                    throw new ArgumentException(nameof(linkType));
            }
        }

        public bool RemoveLink(Position from, Side side, LinkType linkType)
        {
            return RemoveLink(from, from.Shift(side), linkType);
        }
    }
}