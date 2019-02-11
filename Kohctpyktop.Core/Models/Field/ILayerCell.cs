namespace Kohctpyktop.Models.Field
{
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
}