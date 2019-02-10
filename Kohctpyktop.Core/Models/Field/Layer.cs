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
        
        private class MatrixNode
        {
            private CellContent _savedCellContent;

            public MatrixNode(Layer layer, int row, int column)
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
        
        private readonly Layer _layer;
        private readonly MatrixNode[,] _nodes;

        private int _transactionId;

        public LayerCellMatrix(Layer layer)
        {
            _layer = layer;
            _nodes = new MatrixNode[RowCount, ColumnCount];
            
            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
                _nodes[i, j] = new MatrixNode(_layer, i, j);
        }

        public int RowCount => _layer.Height;
        public int ColumnCount => _layer.Width;

        public ILayerCell this[int row, int column] => _nodes[row, column].HostedCell;
        public ILayerCell this[Position position] => _nodes[position.Row, position.Col].HostedCell;

        public void CommitChanges()
        {
            if (!HasUncommitedChanges) return;
            
            _transactionId++;
            
            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
                _nodes[i, j].CommitChanges(_transactionId);

            HasUncommitedChanges = false;
        }

        public void RejectChanges()
        {
            if (!HasUncommitedChanges) return;
            
            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
                _nodes[i, j].RejectChanges();

            HasUncommitedChanges = false;
        }
        
        public void Update(Position position, CellContent cellContent)
        {
            _nodes[position.Row, position.Col].Update(cellContent);
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
                _nodes[i, j].Restore(_transactionId);
            
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
        
        public bool AddCellSilicon(Position position, SiliconType siliconType)
        {
            var cell = _cellMatrix[position];
            if (cell.Silicon != SiliconTypes.None) return false;

            _cellMatrix.Update(position, new LayerCellMatrix.CellContent(SiliconTypes.NType, cell.HasMetal));
            return true;
        }

        public bool RemoveCellSilicon(Position position)
        {
            var cell = _cellMatrix[position];
            if (cell.Silicon == SiliconTypes.None) return true;

            _cellMatrix.Update(position, new LayerCellMatrix.CellContent(SiliconTypes.None, cell.HasMetal));
            return true;
        }

        public bool AddCellMetal(Position position)
        {
            throw new System.NotImplementedException();
        }

        public bool RemoveCellMetal(Position position)
        {
            throw new System.NotImplementedException();
        }

        public bool AddLink(Position @from, Position to, LinkType linkType)
        {
            throw new System.NotImplementedException();
        }

        public bool RemoveLink(Position @from, Position to, LinkType linkType)
        {
            throw new System.NotImplementedException();
        }
    }
}