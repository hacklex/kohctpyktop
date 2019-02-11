namespace Kohctpyktop.Models.Field
{
    public interface ITransactional
    {
        bool HasUncommitedChanges { get; }
        
        void CommitChanges(bool revertable = true);
        void RejectChanges();
    }
}