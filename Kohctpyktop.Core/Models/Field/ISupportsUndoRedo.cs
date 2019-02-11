namespace Kohctpyktop.Models.Field
{
    public interface ISupportsUndoRedo : ITransactional
    {
        int MaxUndoDepth { get; set; }
        int MaxRedoDepth { get; set; }
        
        bool CanUndo { get; }
        void Undo();
        
        bool CanRedo { get; }
        void Redo();
    }
}