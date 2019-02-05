using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Point = System.Windows.Point;

namespace Kohctpyktop
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
                    GameModel.RebuildModel();
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
        

        public Position OldMouseSpot { get; set; } = Position.Invalid;

        public void ProcessMouseMove(Point pt)
        {
            if (pt.X < 1 || pt.Y < 1) return;
            var pos = Position.FromScreenPoint(pt);
            if (pos.Row >= GameModel.Level.Height || pos.Col >= GameModel.Level.Width) return;
            var hoveredCell = GameModel[pos];
            HoveredCell = hoveredCell;
            GameModel.Level.HoveredNode = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                ? hoveredCell.LastAssignedSiliconNode
                : hoveredCell.LastAssignedMetalNode;
            if (DrawMode == DrawMode.NoDraw)
                GameModel.RebuildModel();
        }

        public void ProcessMouse(Point pt)
        {
            if (pt.X < 1 || pt.Y < 1) return;
            var pos = Position.FromScreenPoint(pt);
            
            if (OldMouseSpot.Row < 0)
            {
                DrawSinglePoint(pos);
                OldMouseSpot = pos;
            }
            else
            {
                DrawLine(OldMouseSpot, pos);
                OldMouseSpot = pos;
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
            OldMouseSpot = Position.Invalid;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}