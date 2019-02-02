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
        private Cell _hoveredCell;

        public Level Level { get; }

        public Cell this[Position pos] => Level.Cells[pos.Y, pos.X];
        public Cell this[int row, int col] => Level.Cells[row, col];

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

        public void ProcessMouseMove(Point pt)
        {
            if (pt.X < 1 || pt.Y < 1) return;
            var pos = Position.FromScreenPoint(pt);
            if (pos.Row >= Level.Height || pos.Col >= Level.Width) return;
            var newCell = this[pos];
            newCell.UpdateNeighborInfoString();
            HoveredCell = newCell;
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

        public (List<SchemeNode> nodes, List<SchemeGate> gates) BuildTopology()
        {
            var nodes = new List<SchemeNode>();
            var gates = new List<SchemeGate>();

            // Flood fill propagates the node to all cells accessible from its origin without passing thru gates
            void FloodFill(ElementaryPlaceLink origin, SchemeNode logicalNode)
            {
                if (!origin.Cell.HasMetal && origin.IsMetalLayer) return; //via dead end cases
                if (!origin.Cell.HasN && !origin.Cell.HasP && !origin.IsMetalLayer) return;
                var isCurrentlyOnMetal = origin.IsMetalLayer;
                var currentNode = logicalNode;
                if (logicalNode.AssociatedPlaces.Any(origin.IsSameAs)) return; //we've been there already, it seems
                logicalNode.AssociatedPlaces.Add(origin);
                origin.Cell.NeighborInfos.Where(q => q.HasMetalLink && isCurrentlyOnMetal //implicit recursive call
                    || q.SiliconLink == SiliconLink.BiDirectional && !isCurrentlyOnMetal)
                    .ToList().ForEach(ni => FloodFill(new ElementaryPlaceLink(ni.ToCell, isCurrentlyOnMetal), currentNode));
                if (origin.Cell.HasVia)
                    FloodFill(new ElementaryPlaceLink(origin.Cell, !origin.IsMetalLayer), logicalNode);
            }
            //Stage I. Level-wide gate detection
            for (int i = 0; i < Level.Height; i++)
                for (int j = 0; j < Level.Width; j++)
                    if (Level.Cells[i, j].HasGate) 
                    {
                        // If the chain of gates looks like this (║ and │ stand for P and N)
                        //         b0  c0      e0
                        // p0 ──╥───╫───╫───╥───╫── p1
                        //     a0  b1  c1  d0  e1
                        // then the resulting list should be like 
                        // Inputs = { [a0], [b0, b1], [c0, c1], [d0], [e0, e1] }
                        // The formula for the gate state would be AND ( OR (a0), OR(b0, b1), OR(c0, c1), ...)
                        // for inversion gate, ORs are to be replaced with NORs of course
                        // p0 and p1 are the first and the last (also known as second) power nodes

                        var startingCell = Level.Cells[i, j]; //the first gate in a chain
                        if (gates.Any(g => g.GateCells.Contains(startingCell))) continue; //we've been there already
                        var aggregateGate = new SchemeGate //here we will store info on the whole chain
                        {
                            GateCells = { startingCell },
                            IsInversionGate = startingCell.IsBaseP
                        };
                        var firstGatePowerCell = startingCell.IsVerticalGate //TODO: verify this, maybe the condition should be reversed!
                            ? startingCell.NorthNeighbor //we're bound  to find topmost/leftmost gate first
                            : startingCell.WestNeighbor;
                        Position pos;
                        for (pos = new Position(j, i); //we travel forward (rightwards or downwards) if we find the same gates in adjacency
                            this[pos].SiliconLayerContent == startingCell.SiliconLayerContent;
                            pos = pos.Shift(startingCell.IsVerticalGate, 1))
                        {
                            var signalNodes = this[pos].NeighborInfos
                                //TODO: check if it really should be Slave, not Master, below:
                                .Where(q => q.SiliconLink == SiliconLink.Slave) //expect one or two connections perpendicular to travel direction
                                .Select(q => new SchemeNode { AssociatedPlaces = { new ElementaryPlaceLink(q.ToCell, false) } })
                                .ToArray(); //we generate nodes for each signal input 
                            Array.ForEach(signalNodes, node => FloodFill(node.AssociatedPlaces[0], node)); //and floodfill them
                            aggregateGate.GateInputs.Add(signalNodes); //and add the array to the aggregate gate's list 
                        }
                        var lastGatePowerCell = this[pos]; //the cell next to the last gate cell will inevitably be the second power cell
                        aggregateGate.GatePowerNodes = new[] { firstGatePowerCell, lastGatePowerCell }
                            .Select(q => new SchemeNode { AssociatedPlaces = { new ElementaryPlaceLink(q, false) } }).ToList();
                        aggregateGate.GatePowerNodes.ForEach(p => FloodFill(p.AssociatedPlaces[0], p)); //we floodfill these two nodes as well
                        gates.Add(aggregateGate);
                        nodes.AddRange(aggregateGate.GatePowerNodes);
                        aggregateGate.GateInputs.ForEach(nodes.AddRange);
                    }

            //Stage II. Pin Handling
            //TODO: add pin handling here

            //Stage III. Node Merging

            //Two nodes are to be merged if they share one or more Elementary Place
            void MergeNodes(SchemeNode a, SchemeNode b)
            {
                var mergedNode = new SchemeNode
                {
                    AssociatedPlaces = a.AssociatedPlaces.Concat(b.AssociatedPlaces.Where(place=>!a.AssociatedPlaces.Any(place.IsSameAs))).ToList()
                }; 
                foreach (var gate in gates)
                {
                    if (gate.GatePowerNodes[0] == a || gate.GatePowerNodes[0] == b) gate.GatePowerNodes[0] = mergedNode;
                    if (gate.GatePowerNodes[1] == a || gate.GatePowerNodes[1] == b) gate.GatePowerNodes[1] = mergedNode;
                    foreach (var singularGateSignalArray in gate.GateInputs)
                    {
                        if (singularGateSignalArray[0] == a || singularGateSignalArray[0] == b) singularGateSignalArray[0] = mergedNode;
                        if (singularGateSignalArray[1] == a || singularGateSignalArray[1] == b) singularGateSignalArray[1] = mergedNode;
                    }
                }
                var minIndex = Math.Min(nodes.IndexOf(a), nodes.IndexOf(b));
                nodes.Remove(a);
                nodes.Remove(b);
                nodes.Insert(minIndex, mergedNode);
            }
            //Non-optimized straightforward merger
            bool stopMerging;
            do
            {
                stopMerging = true; //we've merged nothing so far
                SchemeNode a = null, b = null; //the condition of the loop is such that we would merge the nodes one-by-one, until no collisions left
                for (var i = 0; i < nodes.Count && stopMerging; i++)
                {
                    var nodeA = nodes[i];
                    for (var j = 0; j < nodes.Count && stopMerging; j++)
                    {
                        var nodeB = nodes[j];
                        if (i == j) continue; //we don't want to merge a node with itself
                        if (nodeA.AssociatedPlaces.Any(p => nodeB.AssociatedPlaces.Any(p.IsSameAs)))
                        {
                            a = nodeA;
                            b = nodeB;
                            stopMerging = false; //break both loops and proceed to merge
                        }
                    }
                }
                // if we found anything to merge, we do just that
                if (!stopMerging)
                    MergeNodes(a, b);
                // otherwise we are done.
            } while (!stopMerging);
            
            return (nodes, gates); //hopefully this contains a complete non-intersecting set containing all gates and all logical nodes describing the entire level.
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
