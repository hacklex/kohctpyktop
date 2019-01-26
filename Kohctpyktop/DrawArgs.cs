namespace Kohctpyktop
{
    public struct DrawArgs
    {
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public bool IsOnSingleCell => (FromRow, FromCol).ManhattanDistance((ToRow, ToCol)) == 0;
        public bool IsBetweenNeighbors => (FromRow, FromCol).ManhattanDistance((ToRow, ToCol)) == 1;
        public DrawArgs(int fromRow, int fromCol, int toRow, int toCol)
        {
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
        }
    }
}