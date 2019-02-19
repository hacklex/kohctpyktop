using System;
using System.Collections.Generic;
using System.Linq;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Topology
{
    public struct CellAssignments
    {
        public SchemeNode LastAssignedMetalNode { get; set; }
        public SchemeNode LastAssignedSiliconNode { get; set; }
    }

    public class Topology
    {
        public CellAssignments [ ,] CellMappings { get; set; }
        public List<SchemeNode> Nodes { get; set; }
        public List<SchemeGate> Gates { get; set; }
    }

    public static class TopologyBuilder
    {
        public static Topology BuildTopology(ILayer layer)
        {
            var nodes = new List<SchemeNode>();
            var gates = new List<SchemeGate>();

            var assignments = new CellAssignments[layer.Height, layer.Width];

            // this will contain only the pins not connected to any of the gates.
            var pins = new HashSet<Pin>();

            // Flood fill propagates the node to all cells accessible from its origin without passing thru gates
            void FloodFill(ElementaryPlaceLink origin, SchemeNode logicalNode)
            {
                if (!origin.Cell.HasMetal && origin.IsMetalLayer) return; //via dead end cases
                if (!origin.Cell.HasN() && !origin.Cell.HasP() && !origin.IsMetalLayer) return;
                var isCurrentlyOnMetal = origin.IsMetalLayer;
                var currentNode = logicalNode;
                // Pin Handling
                if (origin.IsMetalLayer && origin.Cell.Pin != null)
                {
                    logicalNode.Pins.Add(origin.Cell.Pin);
                    pins.Remove(origin.Cell.Pin);
                }
                if (logicalNode.AssociatedPlaces.Any(origin.IsSameAs)) return; //we've been there already, it seems
                logicalNode.AssociatedPlaces.Add(origin);
                origin.Cell.Links.Where(q => 
                
                    (q.HasMetalLink && isCurrentlyOnMetal)

                    || ((q.SiliconLink == SiliconLink.BiDirectional) && !isCurrentlyOnMetal))
                    .ToList().ForEach(ni => FloodFill(new ElementaryPlaceLink(ni.TargetCell, isCurrentlyOnMetal), currentNode));
                if (origin.Cell.HasVia())
                    FloodFill(new ElementaryPlaceLink(origin.Cell, !origin.IsMetalLayer), logicalNode);
            }
            //Stage I. Level-wide gate and pin detection
            for (int i = 0; i < layer.Height; i++)
                for (int j = 0; j < layer.Width; j++)
                {
                    if (layer.Cells[i, j].Pin != null)
                    {
                        pins.Add(layer.Cells[i, j].Pin);
                    }
                    if (layer.Cells[i, j].HasGate()) 
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

                        var startingCell = layer.Cells[i, j]; //the first gate in a chain
                        if (gates.Any(g => g.GateCells.Contains(startingCell))) continue; //we've been there already
                        var aggregateGate = new SchemeGate //here we will store info on the whole chain
                        {
                            GateCells = { startingCell },
                            IsInversionGate = startingCell.IsBaseP()
                        };
                        var firstGatePowerCell = startingCell.IsVerticalGate() //TODO: verify this, maybe the condition should be reversed!
                            ? startingCell.Neighbors[Side.Top] //we're bound  to find topmost/leftmost gate first
                            : startingCell.Neighbors[Side.Left];
                        Position pos;
                        for (pos = new Position(j, i); //we travel forward (rightwards or downwards) if we find the same gates in adjacency
                            layer.Cells[pos].Silicon == startingCell.Silicon;
                            pos = pos.Shift(startingCell.IsVerticalGate(), 1))
                        {
                            var signalNodes = layer.Cells[pos].Links
                                //TODO: check if it really should be Slave, not Master, below:
                                .Where(q => q.SiliconLink == SiliconLink.Slave) //expect one or two connections perpendicular to travel direction
                                .Select(q => new { Node = new SchemeNode(), initCell = q.TargetCell })
                                .ToList(); //we generate nodes for each signal input 
                            signalNodes.ForEach(node => FloodFill(new ElementaryPlaceLink(node.initCell, false), node.Node)); //and floodfill them
                            aggregateGate.GateInputs.Add(signalNodes.Select(q=>q.Node).ToArray()); //and add the array to the aggregate gate's list 
                        }
                        var lastGatePowerCell = layer.Cells[pos]; //the cell next to the last gate cell will inevitably be the second power cell
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
                }

            //Stage II. Pin Handling
            foreach (var pin in pins.ToArray()) //ToArray() call avoids exceptions thrown by element removals in FloodFill()
            {
                var pinNode = new SchemeNode { Pins = { pin } };
                FloodFill(new ElementaryPlaceLink(layer.Cells[pin.Row, pin.Col], true), pinNode);
                nodes.Add(pinNode);
            }

            //Stage III. Node Merging

            //Two nodes are to be merged if they share one or more Elementary Place
            void MergeNodes(SchemeNode a, SchemeNode b)
            {
                var mergedNode = new SchemeNode
                {
                    AssociatedPlaces = a.AssociatedPlaces.Concat(b.AssociatedPlaces.Where(place=>!a.AssociatedPlaces.Any(place.IsSameAs))).ToList()
                };
                a.Pins.Concat(b.Pins).ToList().ForEach(p => mergedNode.Pins.Add(p));
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
                    if (p.IsMetalLayer) assignments[p.Cell.Row, p.Cell.Column].LastAssignedMetalNode = schemeNode;
                    else assignments[p.Cell.Row, p.Cell.Column].LastAssignedSiliconNode = schemeNode;
                });
            }
            return new Topology
            {
                CellMappings = assignments,
                Gates = gates,
                Nodes = nodes
            }; //hopefully this contains a complete non-intersecting set containing all gates and all logical nodes describing the entire level.
        }
    }
}