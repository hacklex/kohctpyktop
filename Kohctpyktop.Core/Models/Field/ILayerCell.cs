namespace Kohctpyktop.Models.Field
{
    public interface ILayerCell
    {
        bool IsValidCell { get; }

        int Row { get; }
        int Column { get; }

        SiliconTypes Silicon { get; } // todo: rename enum
        bool HasMetal { get; }

        IReadOnlyDirectionalSet<ICellLink> Links { get; }
        IReadOnlyDirectionalSet<ILayerCell> Neighbors { get; }
        Pin Pin { get; }
    }
}