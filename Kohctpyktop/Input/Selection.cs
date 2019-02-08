using System;
using Kohctpyktop.Models;

namespace Kohctpyktop.Input
{
    public enum SelectionState { None, Selecting, HasSelection, Dragging }
    
    public class Selection
    {
        public Position StartCell { get; }
        public Position EndCell { get; set; }
        public int DragOffsetX { get; set; }
        public int DragOffsetY { get; set; }
        
        public Selection(Position startCell)
        {
            StartCell = startCell;
            EndCell = startCell;
        }

        public Selection Resize(Position endCell)
        {
            return new Selection(StartCell) { EndCell = endCell };
        }

        public (Position, Position) ToFieldPositions()
        {
            var startX = StartCell.X;
            var startY = StartCell.Y;
            var endX = EndCell.X;
            var endY = EndCell.Y;

            var minX = Math.Min(startX, endX) ;
            var maxX = Math.Max(startX, endX) + 1;
            var minY = Math.Min(startY, endY);
            var maxY = Math.Max(startY, endY) + 1;

            return (new Position(minX, minY), new Position(maxX, maxY));
        }

        public bool Contains(Position position)
        {
            var (from, to) = ToFieldPositions();
            return position.X >= from.X && position.Y >= from.Y && position.X < to.X && position.Y < to.Y;
        }

        public Selection Drag(int offX, int offY)
        {
            return new Selection(StartCell) { EndCell = EndCell, DragOffsetX = offX, DragOffsetY = offY };
        }
    }
}