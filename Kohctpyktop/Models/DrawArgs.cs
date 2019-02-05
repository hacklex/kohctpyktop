namespace Kohctpyktop.Models
{
    // todo: readonly struct
    public struct DrawArgs
    {
        public Position From { get; }
        public Position To { get; }
        public bool IsOnSingleCell => From.ManhattanDistance(To) == 0;
        public bool IsBetweenNeighbors => From.ManhattanDistance(To) == 1;
        public DrawArgs(Position from, Position to)
        {
            From = from;
            To = to;
        }
    }
}