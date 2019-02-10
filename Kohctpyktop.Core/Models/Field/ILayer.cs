using System.Collections.Generic;

namespace Kohctpyktop.Models.Field
{
    public enum Side { Left, Top, Right, Bottom }
    
    public interface IReadOnlyDirectionalSet<T> : IEnumerable<T>
    {
        T this[int side] { get; } // for @hacklex only
        T this[Side side] { get; }
    }
    
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IReadOnlyMatrix<T>
    {
        int RowCount { get; }
        int ColumnCount { get; }
        
        T this[int Row, int Column] { get; }
        T this[Position position] { get; }
    }

    public interface ICellLink
    {
        bool IsValidLink { get; }
        
        ILayerCell SourceCell { get; }
        ILayerCell TargetCell { get; }
        
        SiliconLink SiliconLink { get; }
        bool HasMetalLink { get; }

        ICellLink Inverted { get; }
    }

    public interface ILayerCell
    {
        bool IsValidCell { get; }

        int Row { get; }
        int Column { get; }

        SiliconTypes Silicon { get; } // todo: rename enum
        bool HasMetal { get; }
        bool IsLocked { get; }

        IReadOnlyDirectionalSet<ICellLink> Links { get; }
        IReadOnlyDirectionalSet<ILayerCell> Neighbors { get; }
        string Name { get; }
    }

    public interface ITransactional
    {
        bool HasUncommitedChanges { get; }
        
        void CommitChanges(bool revertable = true);
        void RejectChanges();
    }
    
    public interface ISupportsUndoRedo : ITransactional
    {
        int MaxUndoDepth { get; set; }
        int MaxRedoDepth { get; set; }
        
        bool CanUndo { get; }
        void Undo();
        
        bool CanRedo { get; }
        void Redo();
    }
    
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
        bool SetLockState(Position position, bool isLocked);
        bool SetCellName(Position position, string name);
        
        bool MoveCells(Position from, Position to, int offsetX, int offsetY);
    }
}