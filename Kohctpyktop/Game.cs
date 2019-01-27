using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point; 
namespace Kohctpyktop
{
    public class Game : INotifyPropertyChanged, IDisposable
    {
        private readonly Renderer _renderer;
        private BitmapSource _bitmapSource;
        private SelectedTool _selectedTool;
        private DrawMode _drawMode;
        private bool _isShiftPressed;
        
        public Level Level { get; }

        public Game(Level level)
        {
            SelectedTool = SelectedTool.Silicon;
            
            Level = level;
            _renderer = new Renderer(level);
            
            RebuildModel();
        }
        
        public Game() : this(Level.CreateDummy()) {}

        public void Dispose() => _renderer.Dispose();

        public bool IsShiftPressed
        {
            get => _isShiftPressed;
            set
            {
                if (_isShiftPressed == value) return;
                _isShiftPressed = value;
                DrawMode = GetDrawMode(_selectedTool, IsShiftPressed);
                OnPropertyChanged();
            }
        }

        public Position OldMouseSpot { get; set; } = Position.Invalid;

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

        public (List<SchemeNode> nodes, List<SchemeGate> gates) BuildTopology()
        {
            var nodes = new List<SchemeNode>();
            var gates = new List<SchemeGate>();

            void FloodFill(ElementaryPlaceLink initialPlace, SchemeNode baseNode)
            {
                if (!initialPlace.Cell.HasMetal && initialPlace.IsMetalLayer) return; //via dead end cases
                if (!initialPlace.Cell.HasN && !initialPlace.Cell.HasP && !initialPlace.IsMetalLayer) return;
                var isCurrentlyOnMetal = initialPlace.IsMetalLayer;
                var currentCell = initialPlace.Cell;
                var currentNode = baseNode;
                if (baseNode.AssociatedPlaces.Any(p => p.IsMetalLayer == isCurrentlyOnMetal && p.Cell == currentCell))
                    return; //we've been there already, it seems
                baseNode.AssociatedPlaces.Add(initialPlace);
                initialPlace.Cell.NeighborInfos.Where(q => (q.HasMetalLink && isCurrentlyOnMetal) 
                    || (q.SiliconLink == SiliconLink.BiDirectional && !isCurrentlyOnMetal))
                    .ToList().ForEach(ni => FloodFill(new ElementaryPlaceLink(ni.ToCell, isCurrentlyOnMetal), currentNode));
                if (initialPlace.Cell.HasVia)
                    FloodFill(new ElementaryPlaceLink(initialPlace.Cell, !initialPlace.IsMetalLayer), baseNode);
            }
            
            for (int i = 0; i < Level.Height; i++)
                for (int j = 0; j < Level.Width; j++)
                    if (Level.Cells[i, j].HasGate)
                    {
                        var signalNodes = Level.Cells[i, j].NeighborInfos //TODO: check if it really should be Slave, not Master, below:
                            .Where(q => q.SiliconLink == SiliconLink.Slave).Select(q =>
                                new SchemeNode { AssociatedPlaces = { new ElementaryPlaceLink(q.ToCell, false) } }).ToList();
                        var gatePowerNodes = Level.Cells[i, j].NeighborInfos
                            .Where(q => q.SiliconLink == SiliconLink.BiDirectional).Select(q =>
                                new SchemeNode { AssociatedPlaces = { new ElementaryPlaceLink(q.ToCell, false) } }).ToList();
                        foreach (var node in signalNodes.Concat(gatePowerNodes))
                        {
                            FloodFill(node.AssociatedPlaces[0], node);
                            nodes.Add(node);
                        }
                        gates.Add(new SchemeGate
                        {
                            GateInputs = signalNodes,
                            GatePowerNodes = gatePowerNodes,
                            IsInversionGate = Level.Cells[i,j].IsBaseP, //TODO: check if it really should be IsBaseP, not IsBaseN
                        });
                        
                    }
            return (nodes, gates);
        }

        public void ReleaseMouse(Point pt)
        {
            OldMouseSpot = Position.Invalid;
        }
        
        void DrawLine(Position from, Position to)
        {
            var args = new DrawArgs(from, to);
            
            switch (DrawMode)
            {
                case DrawMode.Metal: DrawMetal(args);
                    break;
                case DrawMode.PType: DrawSilicon(args, true);
                    break;
                case DrawMode.NType: DrawSilicon(args, false);
                    break;
                case DrawMode.Via: PutVia(to);
                    break;
                case DrawMode.DeleteMetal: DeleteMetal(to);
                    break;
                case DrawMode.DeleteSilicon: DeleteSilicon(to);
                    break;
                case DrawMode.DeleteVia: DeleteVia(to);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        void DrawSinglePoint(Position pt)
        {
            DrawLine(pt, pt);
        }

        public void DrawMetal(DrawArgs args)
        {
            if (args.IsOnSingleCell)
            {
                var cell = Level.Cells[args.From.Row, args.From.Col];
                if (cell.HasMetal) return;
                Level.Cells[args.From.Row, args.From.Col].HasMetal = true;
                RebuildModel(); 
            }
            if (!args.IsBetweenNeighbors) return; //don't ruin the level!
            var fromCell = Level.Cells[args.From.Row, args.From.Col];
            var toCell = Level.Cells[args.To.Row, args.To.Col];
            var neighborInfo = fromCell.GetNeighborInfo(toCell);
            if (fromCell.HasMetal && toCell.HasMetal && neighborInfo.HasMetalLink) return;
            fromCell.HasMetal = true;
            toCell.HasMetal = true;
            fromCell.GetNeighborInfo(toCell).HasMetalLink = true; 
            RebuildModel();
        }

        private static SiliconType InvertSilicon(SiliconType type) =>
            type == SiliconType.NType ? SiliconType.PType : SiliconType.NType;

        private static bool CanDrawSilicon(Cell from, Cell to, SiliconType siliconType)
        {
            var inverted = InvertSilicon(siliconType);
            
            if (from.HasGateOf(inverted)) return false;
            if (from.HasSilicon(inverted)) return false;
            var linkInfo = from.GetNeighborInfo(to);

            var hasSiliconOrGate = from.HasGateOf(siliconType) || from.HasSilicon(siliconType);
            
            if (hasSiliconOrGate && to.HasSilicon(siliconType) && linkInfo.SiliconLink != SiliconLink.BiDirectional) return true;
            if (hasSiliconOrGate && to.HasNoSilicon) return true;
            var indexForTarget = to.GetNeighborIndex(from);
            var rotatedIndex1 = (indexForTarget + 1) % 4;
            var rotatedIndex2 = (indexForTarget + 3) % 4; // modular arithmetics, bitches
            //can only draw the gate into a line of at least 3 connected silicon cells of other type
            if (hasSiliconOrGate  && (to.HasGateOf(inverted) || to.HasSilicon(inverted)) && 
                to.NeighborInfos[rotatedIndex1]?.SiliconLink == SiliconLink.BiDirectional &&
                to.NeighborInfos[rotatedIndex2]?.SiliconLink == SiliconLink.BiDirectional) return true;
            return false;
        }

        private static SiliconTypes ConvertSiliconType(SiliconType type) =>
            type == SiliconType.NType ? SiliconTypes.NType : SiliconTypes.PType;
        
        private static SiliconTypes ConvertSiliconGateType(SiliconType type, bool isVerticalGate) =>
            type == SiliconType.NType 
                ? isVerticalGate ? SiliconTypes.NTypeVGate : SiliconTypes.NTypeHGate 
                : isVerticalGate ? SiliconTypes.PTypeVGate : SiliconTypes.PTypeHGate;

        private static void DrawSilicon(Cell from, Cell to, NeighborInfo neighborInfo, SiliconType siliconType)
        {
            var inverted = InvertSilicon(siliconType);
            
            if (CanDrawSilicon(from, to, siliconType))
            {
                if (to.HasNoSilicon)
                {
                    to.SiliconLayerContent = ConvertSiliconType(siliconType);
                    neighborInfo.SiliconLink = SiliconLink.BiDirectional;
                }
                else if (to.HasSilicon(siliconType))
                {
                    neighborInfo.SiliconLink = SiliconLink.BiDirectional;
                }
                else if (to.HasSilicon(inverted) || to.HasGateOf(inverted))
                {
                    //the gate direction is perpendicular to the link direction
                    to.SiliconLayerContent = ConvertSiliconGateType(inverted, to.IsHorizontalNeighborOf(from));
                    neighborInfo.SiliconLink = SiliconLink.Master; //from cell is the master cell
                }
                else throw new InvalidOperationException("You missed a case here!");
            }
        }
        
        public void DrawSilicon(DrawArgs args, bool isPType)
        {
            if (args.IsOnSingleCell)
            {
                var cell = Level.Cells[args.From.Row, args.From.Col];
                if (cell.SiliconLayerContent != SiliconTypes.None) return;
                Level.Cells[args.From.Row, args.From.Col].SiliconLayerContent = 
                    isPType ? SiliconTypes.PType : SiliconTypes.NType;
                RebuildModel();
                return;
            }
            if (!args.IsBetweenNeighbors) return; //don't ruin the level!
            var fromCell = Level.Cells[args.From.Row, args.From.Col];
            var toCell = Level.Cells[args.To.Row, args.To.Col];
            var neighborInfo = fromCell.GetNeighborInfo(toCell);
            
            DrawSilicon(fromCell, toCell, neighborInfo, isPType ? SiliconType.PType : SiliconType.NType);

            RebuildModel();
        }
        
        public void PutVia(Position pos)
        {
            var cell = Level.Cells[pos.Row, pos.Col];
            if (cell.HasP)
            {
                cell.SiliconLayerContent = SiliconTypes.PTypeVia;
                RebuildModel();
            }
            else if (cell.HasN)
            {
                cell.SiliconLayerContent = SiliconTypes.NTypeVia;
                RebuildModel();
            }
        }
        public void DeleteMetal(Position pos)
        {
            var cell = Level.Cells[pos.Row, pos.Col];
            if (cell.HasMetal)
            {
                foreach (var ni in cell.NeighborInfos)
                {
                    if (ni != null) ni.HasMetalLink = false;
                }
                cell.HasMetal = false;
                RebuildModel();
            }
        }
        private static Dictionary<SiliconTypes, SiliconTypes> DeleteSiliconDic { get; } = new Dictionary<SiliconTypes, SiliconTypes>
        {
            { SiliconTypes.NTypeHGate, SiliconTypes.NType },
            { SiliconTypes.NTypeVGate, SiliconTypes.NType },
            { SiliconTypes.PTypeHGate, SiliconTypes.PType },
            { SiliconTypes.PTypeVGate, SiliconTypes.PType }
        };
        
        public void DeleteSilicon(Position pos)
        {
            var cell = Level.Cells[pos.Row, pos.Col];
            if (cell.HasNoSilicon) return;
            foreach (var ni in cell.NeighborInfos)
            {
                if (ni.SiliconLink != SiliconLink.None) ni.SiliconLink = SiliconLink.None;
                if (ni.ToCell.HasGate)
                {
                    ni.ToCell.SiliconLayerContent = DeleteSiliconDic[ni.ToCell.SiliconLayerContent];
                    
                    foreach (var innerNi in ni.ToCell.NeighborInfos)
                    {
                        if (innerNi.SiliconLink == SiliconLink.Slave) innerNi.SiliconLink = SiliconLink.None;
                    }
                }
            }
            cell.SiliconLayerContent = SiliconTypes.None;
            RebuildModel();
        }
        public void DeleteVia(Position pos)
        {
            var cell = Level.Cells[pos.Row, pos.Col];
            if (cell.SiliconLayerContent == SiliconTypes.PTypeVia)
            {
                cell.SiliconLayerContent = SiliconTypes.PType;
                RebuildModel();
            }
            if (cell.SiliconLayerContent == SiliconTypes.NTypeVia)
            {
                cell.SiliconLayerContent = SiliconTypes.NType;
                RebuildModel();
            }
        }

        public BitmapSource BitmapSource
        {
            get => _bitmapSource;
            set
            {
                if (Equals(value, _bitmapSource)) return;
                _bitmapSource = value;
                OnPropertyChanged();
            }
        }
        
        public void RebuildModel()
        {
            _renderer.Render();
            
            var bmpImage = new BitmapImage();
            var stream = new MemoryStream();
            _renderer.Bitmap.Save(stream, ImageFormat.Bmp);
            bmpImage.BeginInit();
            bmpImage.StreamSource = stream;
            bmpImage.EndInit();
            bmpImage.Freeze();
            BitmapSource = bmpImage;
            //if (LevelModel != null)
            //{
            //    var old = LevelModel;
            //    LevelModel = null; //force rebind
            //    LevelModel = old;
            //    return;
            //}
            //var result = new List<List<Cell>>();
            //for (var i = 0; i < Level.Cells.GetLength(0); i++)
            //{
            //    var row = new List<Cell>();
            //    for (var j = 0; j < Level.Cells.GetLength(1); j++)
            //    {
            //        row.Add(Level.Cells[i,j]);
            //    }
            //    result.Add(row);
            //}
            //LevelModel = result;
        }

        private static DrawMode GetDrawMode(SelectedTool tool, bool isShiftHeld)
        {
            switch (tool)
            {
                case SelectedTool.AddOrDeleteVia: return isShiftHeld ? DrawMode.DeleteVia : DrawMode.Via;
                case SelectedTool.Metal: return DrawMode.Metal;
                case SelectedTool.Silicon: return isShiftHeld ? DrawMode.PType : DrawMode.NType;
                case SelectedTool.DeleteMetalOrSilicon:
                    return isShiftHeld ? DrawMode.DeleteMetal : DrawMode.DeleteSilicon;
                default: throw new ArgumentException("Invalid tool type");
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
                OnPropertyChanged();
            }
        }

        #region PropertyChanged



        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
     
}
