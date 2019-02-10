using System.Collections.Generic;
using System.Linq;
using Kohctpyktop.Models.Topology;

namespace Kohctpyktop.Models.Field
{
    /// <summary>
    /// Contains static level data
    /// </summary>
    public class Level
    {
        public int Width => Cells.GetLength(1);
        public int Height => Cells.GetLength(0);
        public HashSet<Pin> Pins { get; set; }
        public Cell[,] Cells { get; set; }
        public HashSet<Pin> InitiallyHighPins { get; set; }
        public HashSet<Pin> PowerPins { get; set; }
        public Dictionary<Pin, bool[]> PinInputs { get; set; }
        public Dictionary<Pin, bool[]> DesiredPinOutputs { get; set; }
        public SchemeNode HoveredNode { get; set; }
        public static Level CreateDummy()
        {
            var powerPins = new[]
            {
                new Pin { Col = 2, Row = 3, Name = "+VCC"},
                new Pin { Col = 2, Row = 23, Name = "+VCC" },
                new Pin { Col = 41, Row = 3, Name = "+VCC" },
                new Pin { Col = 41, Row = 23, Name = "+VCC" },
            };
            var dataPins = new[]
            {
                new Pin { Col = 2, Row = 7, Name = "A0" },
                new Pin { Col = 2, Row = 11, Name = "A1" },
                new Pin { Col = 2, Row = 15, Name = "A2" },
                new Pin { Col = 2, Row = 19, Name = "A3" },
                new Pin { Col = 41, Row = 7, Name = "B0" },
                new Pin { Col = 41, Row = 11, Name = "B1" },
                new Pin { Col = 41, Row = 15, Name = "B2" },
                new Pin { Col = 41, Row = 19, Name = "B3" },
            };
            
            var level = new Level
            {
                Cells = new Cell[27, 44],
                Pins = new HashSet<Pin>(powerPins.Concat(dataPins)),
                PowerPins = new HashSet<Pin>(powerPins),
                InitiallyHighPins = new HashSet<Pin>(powerPins),
                DesiredPinOutputs = new Dictionary<Pin, bool[]>(),
                PinInputs = new Dictionary<Pin, bool[]>()
            };
            for (int i = 0; i < level.Height; i++)
            for (int j = 0; j < level.Width; j++)
            {
                level.Cells[i,j] = new Cell
                {
                    Col = j, Row = i,
                    SiliconLayerContent = SiliconTypes.None,
                };
            }

            for (int i = 0; i < level.Height; i++)
            for (int j = 0; j < level.Width; j++)
            {
                if (j > 0) NeighborInfo.ConnectCells(level.Cells[i, j], 0, level.Cells[i, j - 1], false, SiliconLink.None);
                if (j < level.Width - 1) NeighborInfo.ConnectCells(level.Cells[i, j], 2, level.Cells[i, j + 1], false, SiliconLink.None);
                if (i > 0) NeighborInfo.ConnectCells(level.Cells[i, j], 1, level.Cells[i - 1, j], false, SiliconLink.None);
                if (i < level.Height - 1) NeighborInfo.ConnectCells(level.Cells[i, j], 3, level.Cells[i + 1, j], false, SiliconLink.None);
                if (j < 4 || j >= level.Width - 4)
                    level.Cells[i, j].IsLocked = true;
            }
            
            void BuildPin(Cell center)
            {
                center.IsLocked = true;
                center.HasMetal = true;
                center.NorthNeighbor.IsLocked = true;
                center.NorthNeighbor.HasMetal = true;
                center.NorthNeighbor.WestNeighbor.IsLocked = true;
                center.NorthNeighbor.WestNeighbor.HasMetal = true;
                center.NorthNeighbor.EastNeighbor.IsLocked = true;
                center.NorthNeighbor.EastNeighbor.HasMetal = true;
                center.WestNeighbor.IsLocked = true;
                center.WestNeighbor.HasMetal = true;
                center.EastNeighbor.IsLocked = true;
                center.EastNeighbor.HasMetal = true;
                center.SouthNeighbor.IsLocked = true;
                center.SouthNeighbor.HasMetal = true;
                center.SouthNeighbor.WestNeighbor.IsLocked = true;
                center.SouthNeighbor.WestNeighbor.HasMetal = true;
                center.SouthNeighbor.EastNeighbor.IsLocked = true;
                center.SouthNeighbor.EastNeighbor.HasMetal = true;
                foreach (var x in center.NeighborInfos) x.HasMetalLink = true;
                var neighborLinks = center.WestNeighbor.NeighborInfos.Concat(center.EastNeighbor.NeighborInfos)
                    .Concat(center.NorthNeighbor.NeighborInfos).Concat(center.SouthNeighbor.NeighborInfos).ToArray();
                for (var i = 0; i < neighborLinks.Length; i++)
                {
                    var x = neighborLinks[i];
                    x.HasMetalLink |= ((i & 1) != 0) == (i < 8);
                }
            }

            foreach (var pin in level.Pins)
            {
                var pinCenterCell = level.Cells[pin.Row, pin.Col];
                BuildPin(pinCenterCell);
                pinCenterCell.LockedName = pin.Name;
            }

            return level;
        }
    }
}