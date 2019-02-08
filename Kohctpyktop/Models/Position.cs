using System;
using System.Windows;
using Kohctpyktop.Rendering;

namespace Kohctpyktop.Models
{
    // todo: readonly struct (c#7.2)
    public struct Position
    {
        public int X { get; }
        public int Y { get; }
        
        public int Row => Y;
        public int Col => X;

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Position Shift(bool vertical, int steps) => vertical ? new Position(X, Y + steps) : new Position(X + steps, Y);

        public static Position Invalid { get; } = new Position(-1, -1); 

        public static Position FromScreenPoint(int x, int y) =>
            new Position((x - 1) / (Renderer.CellSize + 1), (y - 1) / (Renderer.CellSize + 1));
        public static Position FromScreenPoint(Point pt) => FromScreenPoint((int) pt.X, (int) pt.Y);
        
        public int ManhattanDistance(Position target) => Math.Abs(X - target.X) + Math.Abs(Y - target.Y);

        public override string ToString() => $"{X},{Y}";
    }
}