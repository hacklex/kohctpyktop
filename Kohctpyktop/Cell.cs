using System;
using System.Linq;

namespace Kohctpyktop
{
    /// <summary>
    /// Describes the cell with all layers' contents
    /// </summary>
    public class Cell
    {
        public bool HasNoSilicon => SiliconLayerContent == SiliconTypes.None;
        public bool HasP => SiliconLayerContent == SiliconTypes.PType || SiliconLayerContent == SiliconTypes.PTypeVia;
        public bool HasN => SiliconLayerContent == SiliconTypes.NType || SiliconLayerContent == SiliconTypes.NTypeVia;
        public bool HasVia => SiliconLayerContent == SiliconTypes.PTypeVia || SiliconLayerContent == SiliconTypes.NTypeVia;
        public bool IsBaseP => HasP || HasPGate;
        public bool IsBaseN => HasN || HasNGate;

        public bool IsHorizontalGate => SiliconLayerContent == SiliconTypes.NTypeHGate ||
                                        SiliconLayerContent == SiliconTypes.PTypeHGate;
        public bool IsVerticalGate => SiliconLayerContent == SiliconTypes.NTypeVGate ||
                                        SiliconLayerContent == SiliconTypes.PTypeVGate;
        public bool HasPGate => SiliconLayerContent == SiliconTypes.PTypeHGate ||
                                SiliconLayerContent == SiliconTypes.PTypeVGate;
        public bool HasNGate => SiliconLayerContent == SiliconTypes.NTypeVGate ||
                                SiliconLayerContent == SiliconTypes.NTypeHGate;

        public bool HasSilicon(SiliconType type) => type == SiliconType.NType ? HasN : HasP;
        public bool HasGateOf(SiliconType type) => type == SiliconType.NType ? HasNGate : HasPGate;
        
        /// <summary>
        /// true, if the silicon layer of the cell is the gate slave cell
        /// </summary>
        public bool HasGate => SiliconLayerContent.IsAmong(SiliconTypes.NTypeHGate, SiliconTypes.NTypeVGate, SiliconTypes.PTypeHGate, SiliconTypes.PTypeVGate);
        /// <summary>
        /// The logical node this cell's metal layer belongs to in a simulation
        /// </summary>
        public SchemeNode MetalLayerNode { get; set; }
        /// <summary>
        /// The logical node this cell's silicon layer belongs to in a simulation
        /// </summary>
        public SchemeNode SiliconLayerNode { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        /// <summary>
        /// true, if this cell is part of a pin and thus cannot be edited
        /// </summary>
        public bool IsLocked { get; set; }
        /// <summary>
        /// The string that should be rendered as the pin name
        /// </summary>
        public string LockedName { get; set; }
        public SiliconTypes SiliconLayerContent { get; set; } 
        public bool HasMetal { get; set; }

        /// <summary>
        /// Contains the four neighbor links for this cell, ordered LTRB.
        /// A cell with no link to the current is still its neighbor
        /// if its manhattan distance equals 1
        /// </summary>
        public NeighborInfo GetNeighborInfo(Cell x)
        {
            if ((x.Row, x.Col).ManhattanDistance((Row, Col)) != 1) 
                throw new ArgumentException("WTF man I'm expecting a neighboring cell here!");
            return NeighborInfos.First(n => n?.ToCell?.Row == x.Row && n.ToCell?.Col == x.Col);
        }
        public NeighborInfo[] NeighborInfos { get; } = new NeighborInfo[4]; // LTRB
        /// <summary>
        /// Returns the actual neighbors of the cell, considering
        /// its position relative to the edge of the chip
        /// </summary>
        public Cell[] Neighbors => NeighborInfos.Where(l => l != null).Select(l=>l.ToCell).ToArray();

        public int GetNeighborIndex(Cell x)
        {
            if ((x.Row, x.Col).ManhattanDistance((Row, Col)) != 1) 
                throw new ArgumentException("WTF man I'm expecting a neighboring cell here!");
            return NeighborInfos.ToList().FindIndex(n => n?.ToCell?.Row == x.Row && n.ToCell?.Col == x.Col);
        }

        public bool IsVerticalNeighborOf(Cell x) => x == NorthNeighbor || x == SouthNeighbor;
        public bool IsHorizontalNeighborOf(Cell x) => x == WestNeighbor || x == EastNeighbor;
        public Cell WestNeighbor => NeighborInfos[0]?.ToCell;
        public Cell EastNeighbor => NeighborInfos[2]?.ToCell;
        public Cell NorthNeighbor => NeighborInfos[1]?.ToCell;
        public Cell SouthNeighbor => NeighborInfos[3]?.ToCell;
    }
}