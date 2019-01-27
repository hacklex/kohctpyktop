using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Newtonsoft.Json;

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

        private class Map
        {
            public Map(int width, int height, string metal, string metalLinks, string silicon, string siliconLinks, string vias)
            {
                Width = width;
                Height = height;
                Metal = metal;
                MetalLinks = metalLinks;
                Silicon = silicon;
                SiliconLinks = siliconLinks;
                Vias = vias;
            }

            public int Width { get; }
            public int Height { get; }
            public string Metal { get; }
            public string MetalLinks { get; }
            public string Silicon { get; }
            public string SiliconLinks { get; }
            public string Vias { get; }
        }

        public void Clear()
        {
            for (int i = 0; i < Height; i++)
            for (int j = 0; j < Width; j++)
            {
                var cell = Cells[i, j];
                cell.SiliconLayerContent = SiliconTypes.None;
                cell.HasMetal = false;

                for (var k = 0; k < 4; k++)
                    if (cell.NeighborInfos[k] is NeighborInfo ni)
                    {
                        ni.HasMetalLink = false;
                        ni.SiliconLink = SiliconLink.None;
                    }
            }
        }

        public void LoadJson(string json)
        {
            Clear();
            
            var map = JsonConvert.DeserializeObject<Map>(json);

            var linearLength = map.Width * map.Height;
            for (int i = 0, charIx = 0; i < linearLength; i++)
            {
                var metal = map.Metal[charIx++];
                if (metal == '|')
                {
                    i--;
                    continue;
                }
                
                var x = i % map.Width;
                var y = i / map.Width;

                Cells[y, x].HasMetal = metal == 'X';
            }
            for (int i = 0, charIx = 0; i < linearLength; i++)
            {
                var linkChar = map.MetalLinks[charIx++];
                if (linkChar == '|')
                {
                    i--;
                    continue;
                }

                var link = Convert.ToInt32(linkChar.ToString(), 16); // todo - optimize
                
                var x = i % map.Width;
                var y = i / map.Width;

                void LinkIfMetalExist(NeighborInfo neighborInfo)
                {
                    if (neighborInfo.ToCell.HasMetal)
                        neighborInfo.HasMetalLink = true;
                }

                var cell = Cells[y, x];
                if (!cell.HasMetal) continue;
                if ((link & 4) > 0) LinkIfMetalExist(cell.NeighborInfos[0]);
                if ((link & 1) > 0) LinkIfMetalExist(cell.NeighborInfos[1]);
                if ((link & 8) > 0) LinkIfMetalExist(cell.NeighborInfos[2]);
                if ((link & 2) > 0) LinkIfMetalExist(cell.NeighborInfos[3]);
            }
            for (int i = 0, charIx = 0; i < linearLength; i++)
            {
                var silicon = map.Silicon[charIx++];
                if (silicon == '|')
                {
                    i--;
                    continue;
                }

                if (silicon == '_') continue;

                var x = i % map.Width;
                var y = i / map.Width;

                Cells[y, x].SiliconLayerContent = silicon == 'N' ? SiliconTypes.NType : SiliconTypes.PType;
            }
            for (int i = 0, charIx = 0; i < linearLength; i++)
            {
                var linkChar = map.SiliconLinks[charIx++];
                if (linkChar == '|')
                {
                    i--;
                    continue;
                }

                var link = Convert.ToInt32(linkChar.ToString(), 16); // todo - optimize

                var x = i % map.Width;
                var y = i / map.Width;
                
                void LinkSilicon(NeighborInfo neighborInfo)
                {
                    if (neighborInfo.ToCell.Base == neighborInfo.FromCell.Base)
                        neighborInfo.SiliconLink = SiliconLink.BiDirectional;
                    else if (!neighborInfo.ToCell.HasNoSilicon)
                    {
                        neighborInfo.SiliconLink = SiliconLink.Master;
                        // todo: replace
                        neighborInfo.ToCell.SiliconLayerContent = Game.ConvertSiliconGateType(
                            neighborInfo.ToCell.Base ?? throw new Exception("wtf"),
                            neighborInfo.ToCell.Row != neighborInfo.FromCell.Row);
                    }
                }

                var cell = Cells[y, x];
                if (cell.HasNoSilicon) continue;
                if ((link & 4) > 0) LinkSilicon(cell.NeighborInfos[0]);
                if ((link & 1) > 0) LinkSilicon(cell.NeighborInfos[1]);
                if ((link & 8) > 0) LinkSilicon(cell.NeighborInfos[2]);
                if ((link & 2) > 0) LinkSilicon(cell.NeighborInfos[3]);
            }
            for (int i = 0, charIx = 0; i < linearLength; i++)
            {
                var via = map.Vias[charIx++];
                if (via == '|')
                {
                    i--;
                    continue;
                }

                if (via == '_') continue;

                var x = i % map.Width;
                var y = i / map.Width;

                var cell = Cells[y, x];
                if (cell.SiliconLayerContent == SiliconTypes.NType)
                    cell.SiliconLayerContent = SiliconTypes.NTypeVia;
                if (cell.SiliconLayerContent == SiliconTypes.PType)
                    cell.SiliconLayerContent = SiliconTypes.PTypeVia;
            }
        }
    }
}