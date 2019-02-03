using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point; 
namespace Kohctpyktop
{
    public class Game : INotifyPropertyChanged, IDisposable
    {
        private readonly Renderer _renderer;
        private BitmapSource _bitmapSource;

        public Level Level { get; }

        public Cell this[Position pos] => Level.Cells[pos.Y, pos.X];
        public Cell this[int row, int col] => Level.Cells[row, col];

        public Game(Level level)
        {
            Level = level;
            _renderer = new Renderer(level);
            
            RebuildModel();
        }
        
        public Game() : this(Level.CreateDummy()) {}

        public void Dispose() => _renderer.Dispose();

        public (List<SchemeNode> nodes, List<SchemeGate> gates) BuildTopology()
        {
            var nodes = new List<SchemeNode>();
            var gates = new List<SchemeGate>();

            for (int i = 0; i < Level.Height; i++)
            for (int j = 0; j < Level.Width; j++)
            {
                Level.Cells[i, j].LastAssignedMetalNode = null;
                Level.Cells[i, j].LastAssignedSiliconNode = null;
            }

            // Flood fill propagates the node to all cells accessible from its origin without passing thru gates
            void FloodFill(ElementaryPlaceLink origin, SchemeNode logicalNode)
            {
                if (!origin.Cell.HasMetal && origin.IsMetalLayer) return; //via dead end cases
                if (!origin.Cell.HasN && !origin.Cell.HasP && !origin.IsMetalLayer) return;
                var isCurrentlyOnMetal = origin.IsMetalLayer;
                var currentNode = logicalNode;
                if (logicalNode.AssociatedPlaces.Any(origin.IsSameAs)) return; //we've been there already, it seems
                logicalNode.AssociatedPlaces.Add(origin);
                origin.Cell.NeighborInfos.Where(q => 
                
                    (q.HasMetalLink && isCurrentlyOnMetal)

                    || ((q.SiliconLink == SiliconLink.BiDirectional) && !isCurrentlyOnMetal))
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
                                .Select(q => new { Node = new SchemeNode(), initCell = q.ToCell })
                                .ToArray(); //we generate nodes for each signal input 
                            Array.ForEach(signalNodes, node => FloodFill(new ElementaryPlaceLink(node.initCell, false), node.Node)); //and floodfill them
                            aggregateGate.GateInputs.Add(signalNodes.Select(q=>q.Node).ToArray()); //and add the array to the aggregate gate's list 
                        }
                        var lastGatePowerCell = this[pos]; //the cell next to the last gate cell will inevitably be the second power cell
                        var powerNode1 = new SchemeNode();
                        FloodFill(new ElementaryPlaceLink(firstGatePowerCell, false), powerNode1);
                        var powerNode2 = new SchemeNode();
                        FloodFill(new ElementaryPlaceLink(lastGatePowerCell, false), powerNode2);
                        gates.Add(aggregateGate);
                        nodes.Add(powerNode1);
                        nodes.Add(powerNode2);
                        aggregateGate.GatePowerNodes = new List<SchemeNode>{powerNode1, powerNode2};
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
                    if (gate.GatePowerNodes.Count > 1)
                        if (gate.GatePowerNodes[1] == a || gate.GatePowerNodes[1] == b) gate.GatePowerNodes[1] = mergedNode;
                    foreach (var singularGateSignalArray in gate.GateInputs)
                    {
                        if (singularGateSignalArray[0] == a || singularGateSignalArray[0] == b) singularGateSignalArray[0] = mergedNode;
                        if (singularGateSignalArray.Length > 1)
                            if(singularGateSignalArray[1] == a || singularGateSignalArray[1] == b) singularGateSignalArray[1] = mergedNode;
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
            foreach (var schemeNode in nodes)
            {
                schemeNode.AssociatedPlaces.ForEach(p =>
                {
                    if (p.IsMetalLayer) p.Cell.LastAssignedMetalNode = schemeNode;
                    else p.Cell.LastAssignedSiliconNode = schemeNode;
                });
            }
            return (nodes, gates); //hopefully this contains a complete non-intersecting set containing all gates and all logical nodes describing the entire level.
        }

        public void DrawMetal(DrawArgs args)
        {
            if (!this[args.From].HasMetal && this[args.From].IsLocked ||
                !this[args.To].HasMetal && this[args.To].IsLocked)
                return;
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
            if (this[args.To].IsLocked || this[args.From].IsLocked) return;
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
            if (cell.HasMetal && !cell.IsLocked)
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
                if (ni == null) continue;
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

        public void ClearTopologyMarkers()
        {
            if (Level == null) return;
            for (int i = 0; i < Level.Height; i++)
            for (int j = 0; j < Level.Width; j++)
            {
                Level.Cells[i, j].LastAssignedMetalNode = null;
                Level.Cells[i, j].LastAssignedSiliconNode = null;
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
