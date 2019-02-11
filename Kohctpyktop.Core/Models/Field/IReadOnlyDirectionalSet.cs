using System.Collections.Generic;

namespace Kohctpyktop.Models.Field
{
    public interface IReadOnlyDirectionalSet<T> : IEnumerable<T>
    {
        T this[int side] { get; } // for @hacklex only
        T this[Side side] { get; }
    }
}