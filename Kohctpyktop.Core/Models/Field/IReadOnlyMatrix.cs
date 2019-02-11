namespace Kohctpyktop.Models.Field
{
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IReadOnlyMatrix<T>
    {
        int RowCount { get; }
        int ColumnCount { get; }
        
        T this[int Row, int Column] { get; }
        T this[Position position] { get; }
    }
}