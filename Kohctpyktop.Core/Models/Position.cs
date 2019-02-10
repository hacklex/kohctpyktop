using System;
using System.Drawing;
using System.Windows;
using Kohctpyktop.Models.Field;

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

        public Position Shift(Side side)
        {
            switch (side)
            {
                case Side.Left: return Offset(-1, 0);
                case Side.Top: return Offset(0, -1);
                case Side.Right: return Offset(1, 0);
                case Side.Bottom: return Offset(0, 1);
                default:
                    throw new ArgumentException(nameof(side));
            }
        }

        public Position Offset(int offsetX, int offsetY) => new Position(X + offsetX, Y + offsetY);

        public static Position Invalid { get; } = new Position(-1, -1); 

        public static Position FromScreenPoint(int x, int y) =>
            new Position((x - 1) / (12 + 1), (y - 1) / (12 + 1));
        
        public static Position FromScreenPoint(double x, double y) => FromScreenPoint((int) x, (int) y);
        
        public int ManhattanDistance(Position target) => Math.Abs(X - target.X) + Math.Abs(Y - target.Y);

        public override string ToString() => $"{X},{Y}";

        public bool IsAdjacent(Position to) => ManhattanDistance(to) == 1;

        public Side GetAdjacentSide(Position to) => 
            to.X > X ? Side.Right : to.X < X ? Side.Left : to.Y > Y ? Side.Bottom : Side.Top;
    }
}