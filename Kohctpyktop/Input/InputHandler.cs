using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Kohctpyktop.Models;
using Kohctpyktop.Models.Field;
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
        private Cell _hoveredCell;
        
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
//                if (_selectedTool == SelectedTool.TopologyDebug)
//                    GameModel.BuildTopology();
//                else
//                    GameModel.ClearTopologyMarkers();
                OnPropertyChanged();
                
                ResetSelection();
            }
        }
        
        public Cell HoveredCell
        {
            get => _hoveredCell;
            set
            {
                if (_hoveredCell == value) return;
                _hoveredCell = value;
                OnPropertyChanged();
            }
        }

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
            if (hoveredCell.Row != HoveredCell?.Row || hoveredCell.Column != HoveredCell?.Col)
            {
//                HoveredCell = hoveredCell;
//                GameModel.Level.HoveredNode = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
//                    ? hoveredCell.LastAssignedSiliconNode
//                    : hoveredCell.LastAssignedMetalNode;
                // GameModel.MarkModelAsChanged();
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

        public bool DrawLine(Position from, Position to)
        {
            var args = new DrawArgs(from, to);
            
            if (args.IsOnSingleCell) return DrawSinglePoint(from);
            if (args.IsBetweenNeighbors) return DrawAdjacentPoints(from, to);
            
            // todo: bresenham's line algorithm
            return false;
        }

        public bool DrawSinglePoint(Position pt)
        {
            switch (DrawMode)
            {
                case DrawMode.Metal: return Layer.AddCellMetal(pt);
                case DrawMode.PType: return Layer.AddCellSilicon(pt, SiliconType.PType);
                case DrawMode.NType: return Layer.AddCellSilicon(pt, SiliconType.NType);
//                case DrawMode.Via: GameModel.PutVia(to);
//                    break;
                case DrawMode.DeleteMetal: return Layer.AddCellMetal(pt);
                case DrawMode.DeleteSilicon: return Layer.RemoveCellSilicon(pt);
//                case DrawMode.DeleteVia: GameModel.DeleteVia(to);
//                    break;
//                case DrawMode.NoDraw: break;
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

//            if (GameModel.TryMove(from, to, offsetX, offsetY))
//            {
//                Selection = Selection.Offset(offsetX, offsetY);
//            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}