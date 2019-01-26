using System.Collections.Generic;

namespace Kohctpyktop
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

        public static Level CreateDummy()
        {
            var powerPin = new Pin { Col = 1, Row = 1 };
            var level = new Level
            {
                Cells = new Cell[25, 40],
                Pins = new HashSet<Pin> { powerPin },
                PowerPins = new HashSet<Pin> { powerPin },
                InitiallyHighPins = new HashSet<Pin> { powerPin },
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
            }
            return level;
        }
    }
}