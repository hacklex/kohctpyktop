using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace Kohctpyktop.Models.Field
{
    public class LayerCellMatrix : IReadOnlyMatrix<ILayerCell>
    {
        private const long MaxUndoRedoDepth = 10;
        
        private class CellNode
        {
            private CellContent _savedCellContent;

            public CellNode(Layer layer, int row, int column)
            {
                HostedCell = new LayerCell(layer, row, column);
            }
            
            public Dictionary<int, CellContent> SavedStates { get; } = new Dictionary<int, CellContent>();
            public LayerCell HostedCell { get; }

            public void CommitChanges(int transactionId, bool revertable)
            {
                if (revertable) SavedStates[transactionId] = _savedCellContent;
                _savedCellContent = new CellContent(HostedCell);
                
                var maxSavedTransaction = SavedStates.Select(x => x.Key).DefaultIfEmpty(0).Max();
                for (var i = transactionId + 1; i <= maxSavedTransaction; i++) SavedStates.Remove(i);
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
                    SavedStates[transactionId] = _savedCellContent;

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
            
            public void CommitChanges(int transactionId, bool revertable)
            {
                if (revertable) SavedStates[transactionId] = (_savedRightLinkContent, _savedBottomLinkContent);
                (_savedRightLinkContent, _savedBottomLinkContent) = HostedCell.SaveLinkState();

                var maxSavedTransaction = SavedStates.Select(x => x.Key).DefaultIfEmpty(0).Max();
                for (var i = transactionId + 1; i <= maxSavedTransaction; i++) SavedStates.Remove(i);
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
                    SavedStates[transactionId] = (_savedRightLinkContent, _savedBottomLinkContent);
                        
                    _savedRightLinkContent = savedState.Item1;
                    _savedBottomLinkContent = savedState.Item2;
                    
                    RejectChanges();
                }
            }
        }
        
        private readonly Layer _layer;
        private readonly CellNode[,] _cellNodes;
        private readonly LinkNode[,] _linkNodes;

        private const int InitialTransactionId = 1;
        private int _transactionId = InitialTransactionId, _lastKnownTransactionId = InitialTransactionId;

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

        public void CommitChanges(bool revertable)
        {
            if (!HasUncommitedChanges) return;

            if (revertable)
            {
                _transactionId++;
            }
            
            _lastKnownTransactionId = _transactionId;
            
            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
            {
                _cellNodes[i, j].CommitChanges(_transactionId, revertable);
                _linkNodes[i, j].CommitChanges(_transactionId, revertable);
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

        private static (Position, Side, LinkContent) NormalizeLinkPosition(Position position, Side side, LinkContent content)
        {
            switch (side)
            {
                case Side.Right:
                case Side.Bottom:
                    return (position, side, content);
                case Side.Left:
                    return (position.Offset(-1, 0), Side.Right, content.Invert());
                case Side.Top:
                    return (position.Offset(0, -1), Side.Bottom, content.Invert());
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

            if (_transactionId == InitialTransactionId) return;

            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
            {
                _cellNodes[i, j].Restore(_transactionId);
                _linkNodes[i, j].Restore(_transactionId);
            }

            _transactionId--;
        }
        
        public void Redo()
        {
            if (HasUncommitedChanges) return;

            if (_lastKnownTransactionId == _transactionId) return;
            _transactionId++;

            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
            {
                _cellNodes[i, j].Restore(_transactionId);
                _linkNodes[i, j].Restore(_transactionId);
            }
        }
        
        public bool HasUncommitedChanges { get; private set; }
        public bool CanUndo => HasUncommitedChanges || _transactionId > InitialTransactionId;
        public bool CanRedo => !HasUncommitedChanges && _transactionId < _lastKnownTransactionId;
    }
}