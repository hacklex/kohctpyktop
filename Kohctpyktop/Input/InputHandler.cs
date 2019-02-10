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
        public Game GameModel { get; }

        private bool _isShiftPressed;
        private SelectedTool _selectedTool;
        private DrawMode _drawMode;
        private Cell _hoveredCell;
        
        public InputHandler(Game gameModel)
        {
            GameModel = gameModel;
            
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
                    GameModel.Level.HoveredNode = IsShiftPressed
                        ? HoveredCell.LastAssignedSiliconNode
                        : HoveredCell.LastAssignedMetalNode;
                    GameModel.MarkModelAsChanged();
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
                    GameModel.BuildTopology();
                else
                    GameModel.ClearTopologyMarkers();
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
            if (pos.Row >= GameModel.Level.Height || pos.Col >= GameModel.Level.Width) return;
            var hoveredCell = GameModel[pos];
            if (hoveredCell.Row != HoveredCell?.Row || hoveredCell.Col != HoveredCell?.Col)
            {
                HoveredCell = hoveredCell;
                GameModel.Level.HoveredNode = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                    ? hoveredCell.LastAssignedSiliconNode
                    : hoveredCell.LastAssignedMetalNode;
                GameModel.MarkModelAsChanged();
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

        public void DrawLine(Position from, Position to)
        {
            var args = new DrawArgs(from, to);
            
            switch (DrawMode)
            {
                case DrawMode.Metal: GameModel.DrawMetal(args);
                    break;
                case DrawMode.PType: GameModel.DrawSilicon(args, true);
                    break;
                case DrawMode.NType: GameModel.DrawSilicon(args, false);
                    break;
                case DrawMode.Via: GameModel.PutVia(to);
                    break;
                case DrawMode.DeleteMetal: GameModel.DeleteMetal(to);
                    break;
                case DrawMode.DeleteSilicon: GameModel.DeleteSilicon(to);
                    break;
                case DrawMode.DeleteVia: GameModel.DeleteVia(to);
                    break;
                case DrawMode.NoDraw: break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void DrawSinglePoint(Position pt)
        {
            DrawLine(pt, pt);
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

            if (GameModel.TryMove(from, to, offsetX, offsetY))
            {
                Selection = Selection.Offset(offsetX, offsetY);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}