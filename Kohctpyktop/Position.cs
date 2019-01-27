using System;
using System.Windows;

namespace Kohctpyktop
{
    // todo: readonly struct (c#7.2)
    public struct Position
    {
        public int X { get; }
        public int Y { get; }

        [Obsolete("Use Y")]
        public int Row => Y;

        [Obsolete("Use X")]
        public int Col => X;

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public static Position Invalid { get; } = new Position(-1, -1); 

        public static Position FromScreenPoint(int x, int y) =>
            new Position((x - 1) / (Renderer.CellSize + 1), (y - 1) / (Renderer.CellSize + 1));
        public static Position FromScreenPoint(Point pt) => FromScreenPoint((int) pt.X, (int) pt.Y);
        
        public int ManhattanDistance(Position target) => Math.Abs(X - target.X) + Math.Abs(Y - target.Y);
    }
}