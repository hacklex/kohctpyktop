using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Topology;
using Kohctpyktop.Rendering;
using Point = System.Windows.Point;

namespace Kohctpyktop.Input
{
    public class InputHandler : INotifyPropertyChanged
    {
        public ILayer Layer { get; }

        private bool _isShiftPressed;
        private SelectedTool _selectedTool;
        private DrawMode _drawMode;
        private ILayerCell _hoveredCell;
        [Obsolete]
        public CellAssignments[,] _assignments;
        
        public InputHandler(ILayer layer)
        {
            Layer = layer;
            
            SelectedTool = SelectedTool.Silicon;
        }
        
        private static DrawMode GetDrawMode(SelectedTool tool, bool isShiftHeld)
        {
            switch (tool)
            {
                case SelectedTool.AddOrDeleteVia: return isShiftHeld ? DrawMode.DeleteVia : DrawMode.Via;
                case SelectedTool.Metal: return DrawMode.Metal;
                case SelectedTool.Silicon: return isShiftHeld ? DrawMode.PType : DrawMode.NType;
                case SelectedTool.DeleteMetalOrSilicon: return isShiftHeld ? DrawMode.DeleteMetal : DrawMode.DeleteSilicon;
                case SelectedTool.TopologyDebug: return DrawMode.NoDraw;
                case SelectedTool.Selection: return DrawMode.Selection;
                default: throw new ArgumentException("Invalid tool type");
            }
        }
        
        public bool IsShiftPressed
        {
            get => _isShiftPressed;
            set
            {
                if (_isShiftPressed == value) return;
                _isShiftPressed = value;
                DrawMode = GetDrawMode(_selectedTool, IsShiftPressed);
                if (DrawMode == DrawMode.NoDraw)
                {
//                    GameModel.Level.HoveredNode = IsShiftPressed
//                        ? HoveredCell.LastAssignedSiliconNode
//                        : HoveredCell.LastAssignedMetalNode;
//                    GameModel.MarkModelAsChanged();
                }
                OnPropertyChanged();
            }
        }

        public DrawMode DrawMode
        {
            get => _drawMode;
            set
            {
                if (value == _drawMode) return;
                _drawMode = value;
                OnPropertyChanged();
            }
        }
        
        public SelectedTool SelectedTool
        {
            get => _selectedTool;
            set
            {
                if (value == _selectedTool) return;
                _selectedTool = value;
                DrawMode = GetDrawMode(_selectedTool, IsShiftPressed);
                if (_selectedTool == SelectedTool.TopologyDebug)
                    (_assignments, _, _) = TopologyBuilder.BuildTopology(Layer);
                else
                    _assignments = null;
                OnPropertyChanged();
                
                ResetSelection();
            }
        }
        
        public ILayerCell HoveredCell
        {
            get => _hoveredCell;
            private set
            {
                if (_hoveredCell == value) return;
                _hoveredCell = value;
                OnPropertyChanged();
            }
        }

        public SchemeNode HoveredNode { get; private set; }

        private Point _dragStartPos;
        private SelectionState _selectionState;
        private Selection _selection;
        public SelectionState SelectionState
        {
            get => _selectionState;
            private set
            {
                if (_selectionState == value) return;
                _selectionState = value;
                OnPropertyChanged();
            }
        }

        public Selection Selection
        {
            get => _selection;
            private set
            {
                if (_selection == value) return;
                _selection = value;
                OnPropertyChanged();
            }
        }

        private Position _oldMouseSpot = Position.Invalid;

        public void ProcessMouseMove(Point pt)
        {
            if (pt.X < 1 || pt.Y < 1) return;
            var pos = Position.FromScreenPoint(pt.X, pt.Y);
            if (pos.Row >= Layer.Height || pos.Col >= Layer.Width) return;
            var hoveredCell = Layer.Cells[pos];
            if (hoveredCell.Row != HoveredCell?.Row || hoveredCell.Column != HoveredCell?.Column)
            {
                HoveredCell = hoveredCell;
                if (_assignments != null)
                {
                    HoveredNode = IsShiftPressed
                        ? _assignments[HoveredCell.Row, HoveredCell.Column].LastAssignedSiliconNode
                        : _assignments[HoveredCell.Row, HoveredCell.Column].LastAssignedMetalNode;
                }

//                 GameModel.MarkModelAsChanged();
            }
        }

        private void ResetSelection()
        {
            SelectionState = SelectionState.None;
            Selection = null;
        }

        public void ProcessMouse(Point pt)
        {
            if (DrawMode == DrawMode.Selection)
                ProcessSelection(pt);
            else
                ProcessDrawing(pt);
        }

        private void ProcessSelection(Point pt)
        {
            var position = Position.FromScreenPoint(pt.X, pt.Y);
            
            switch (SelectionState)
            {
                case SelectionState.None:
                    Selection = new Selection(position);
                    SelectionState = SelectionState.Selecting;
                    break;
                case SelectionState.Selecting:
                    Selection = Selection.Resize(position);
                    break;
                case SelectionState.HasSelection:
                    if (Selection.Contains(position))
                    {
                        _dragStartPos = pt;
                        SelectionState = SelectionState.Dragging;
                        break;
                    }
                    else goto case SelectionState.None;
                case SelectionState.Dragging:
                    var diff = pt - _dragStartPos;
                    Selection = Selection.Drag((int) diff.X, (int) diff.Y);
                    break;
            }
        }

        // stack-overflow driven development incoming
        public bool DrawLongLine(Position from, Position to)
        {
            var x = from.X;
            var y = from.Y;
            
            var w = to.X - from.X;
            var h = to.Y - from.Y;
            
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1;
            else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1;
            else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1;
            else if (w > 0) dx2 = 1;
            var longest = Math.Abs(w);
            var shortest = Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1;
                else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            var numerator = longest >> 1;

            var prevX = -1;
            var prevY = -1;

            var drawResult = false;
            
            for (var i = 0; i <= longest; i++)
            {
                if ((x, y).ManhattanDistance((prevX, prevY)) == 1)
                    drawResult |= DrawAdjacentPoints(new Position(prevX, prevY), new Position(x, y));
                else
                    drawResult |= DrawSinglePoint(new Position(x, y));

                prevX = x;
                prevY = y;
                
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }

            return drawResult;
        }

        public bool DrawLine(Position from, Position to)
        {
            var args = new DrawArgs(from, to);
            
            if (args.IsOnSingleCell) return DrawSinglePoint(from);
            if (args.IsBetweenNeighbors) return DrawAdjacentPoints(from, to);

            return DrawLongLine(from, to);
        }

        private void ProcessDrawing(Point pt)
        {
            ResetSelection();
            
            if (pt.X < 1 || pt.Y < 1) return;
            var pos = Position.FromScreenPoint(pt.X, pt.Y);

            if (_oldMouseSpot.Row < 0)
            {
                DrawSinglePoint(pos);
                _oldMouseSpot = pos;
            }
            else
            {
                DrawLine(_oldMouseSpot, pos);
                _oldMouseSpot = pos;
            }
        }

        public bool DrawSinglePoint(Position pt)
        {
            switch (DrawMode)
            {
                case DrawMode.Metal: return Layer.AddCellMetal(pt);
                case DrawMode.PType: return Layer.AddCellSilicon(pt, SiliconType.PType);
                case DrawMode.NType: return Layer.AddCellSilicon(pt, SiliconType.NType);
                case DrawMode.Via: return Layer.AddVia(pt);
                case DrawMode.DeleteMetal: return Layer.RemoveCellMetal(pt);
                case DrawMode.DeleteSilicon: return Layer.RemoveCellSilicon(pt);
                case DrawMode.DeleteVia: return Layer.RemoveVia(pt);
                case DrawMode.NoDraw: return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool DrawAdjacentPoints(Position from, Position to)
        {
            switch (DrawMode)
            {
                case DrawMode.Metal: 
                    return Layer.AddCellMetal(from) | 
                           Layer.AddCellMetal(to) | 
                           Layer.AddLink(from, to, LinkType.MetalLink);
                case DrawMode.PType:
                    return Layer.AddCellSilicon(from, SiliconType.PType) | 
                           Layer.AddCellSilicon(to, SiliconType.PType) | 
                           Layer.AddLink(from, to, LinkType.SiliconLink);
                case DrawMode.NType: 
                    return Layer.AddCellSilicon(from, SiliconType.NType) | 
                           Layer.AddCellSilicon(to, SiliconType.NType) | 
                           Layer.AddLink(from, to, LinkType.SiliconLink);
                case DrawMode.DeleteMetal: 
                    return Layer.RemoveCellMetal(from) | 
                           Layer.RemoveCellMetal(to) | 
                           Layer.RemoveLink(from, to, LinkType.MetalLink);
                case DrawMode.DeleteSilicon: 
                    return Layer.RemoveCellSilicon(from) | 
                           Layer.RemoveCellSilicon(to) | 
                           Layer.RemoveLink(from, to, LinkType.SiliconLink);
                default:
                    return DrawSinglePoint(from) | DrawSinglePoint(to);
            }
        }

        public void ReleaseMouse(Point pt)
        {
            _oldMouseSpot = Position.Invalid;

            switch (SelectionState)
            {
                case SelectionState.Selecting:
                    SelectionState = SelectionState.HasSelection;
                    break;
                case SelectionState.Dragging:
                    TryDragSelection();
                    SelectionState = SelectionState.HasSelection;
                    Selection = Selection.Drag(0, 0);
                    break;
            }
        }

        private void TryDragSelection()
        {
            var (from, to) = Selection.ToFieldPositions();
            var offsetX = (int) Math.Round(Selection.DragOffsetX / (Renderer.CellSize + 1.0));
            var offsetY = (int) Math.Round(Selection.DragOffsetY / (Renderer.CellSize + 1.0));

            if (Layer.MoveCells(from, to, offsetX, offsetY))
            {
                Selection = Selection.Offset(offsetX, offsetY);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}