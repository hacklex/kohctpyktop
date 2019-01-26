using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using DColor = System.Drawing.Color;
using DPen = System.Drawing.Pen;
using DBrush = System.Drawing.Brush;
using Point = System.Windows.Point;

namespace Kohctpyktop
{
    public class Game : INotifyPropertyChanged, IDisposable
    {
        private BitmapSource _bitmapSource;
        private DrawMode _drawMode;
        public Level Level { get; }

        private const int CellSize = 16;

        public Game(Level level)
        {
            Level = level;
            Bitmap = new Bitmap(
                (CellSize + 1) * level.Cells.GetLength(1) + 1, 
                (CellSize + 1) * level.Cells.GetLength(0) + 1);
            Graphics = Graphics.FromImage(Bitmap);
            RebuildModel();
        }

        public Game() : this(Level.CreateDummy())
        {
        }

        public void Dispose()
        {
            Graphics?.Dispose();
            Bitmap?.Dispose();
        }

        public (int Row, int Col) OldMouseSpot { get; set; } = (-1, -1);

        public void ProcessMouse(Point pt)
        {
            if (pt.X < 1 || pt.Y < 1) return;
            var x = (Convert.ToInt32(pt.X) - 1) / (CellSize + 1);
            var y = (Convert.ToInt32(pt.Y) - 1) / (CellSize + 1);
            if (OldMouseSpot.Row < 0)
            {
                DrawSinglePoint((y, x));
                OldMouseSpot = (y, x);
            }
            else
            {
                DrawLine(OldMouseSpot, (y, x));
                OldMouseSpot = (y, x);
            }
        }

        public void ReleaseMouse(Point pt)
        {
            OldMouseSpot = (-1, -1);
        }
        void DrawLine((int Row, int Col) from, (int Row, int Col) to)
        {
            var args = new DrawArgs(from.Row, from.Col, to.Row, to.Col);
            switch (DrawMode)
            {
                case DrawMode.Metal: DrawMetal(args);
                    break;
                case DrawMode.PType: DrawSilicon(args, true);
                    break;
                case DrawMode.NType: DrawSilicon(args, false);
                    break;
                case DrawMode.Via: PutVia(to.Row, to.Col);
                    break;
                case DrawMode.DeleteMetal: DeleteMetal(to.Row, to.Col);
                    break;
                case DrawMode.DeleteSilicon: DeleteSilicon(to.Row, to.Col);
                    break;
                case DrawMode.DeleteVia: DeleteVia(to.Row, to.Col);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        void DrawSinglePoint((int Row, int Col) pt)
        {
            DrawLine(pt, pt);
        }

        public void DrawMetal(DrawArgs args)
        {
            if (args.IsOnSingleCell)
            {
                var cell = Level.Cells[args.FromRow, args.FromCol];
                if (cell.HasMetal) return;
                Level.Cells[args.FromRow, args.FromCol].HasMetal = true;
                RebuildModel(); 
            }
            if (!args.IsBetweenNeighbors) return; //don't ruin the level!
            var fromCell = Level.Cells[args.FromRow, args.FromCol];
            var toCell = Level.Cells[args.ToRow, args.ToCol];
            var neighborInfo = fromCell.GetNeighborInfo(toCell);
            if (fromCell.HasMetal && toCell.HasMetal && neighborInfo.HasMetalLink) return;
            fromCell.HasMetal = true;
            toCell.HasMetal = true;
            fromCell.GetNeighborInfo(toCell).HasMetalLink = true; 
            RebuildModel();
        }
        static bool CanDrawP(Cell from, Cell to)
        {
            if (from.HasGate) return false;
            if (from.HasN) return false;
            var linkInfo = from.GetNeighborInfo(to);
            if (from.HasP && to.HasP && linkInfo.SiliconLink != SiliconLink.BiDirectional) return true;
            if (from.HasP && to.HasNoSilicon) return true;
            var indexForTarget = to.GetNeighborIndex(from);
            var rotatedIndex1 = (indexForTarget + 1) % 4;
            var rotatedIndex2 = (indexForTarget + 3) % 4; // modular arithmetics, bitches
            //can only draw the gate into a line of at least 3 connected N cells
            if (from.HasP && to.HasN && to.NeighborInfos[rotatedIndex1]?.SiliconLink == SiliconLink.BiDirectional &&
                to.NeighborInfos[rotatedIndex2]?.SiliconLink == SiliconLink.BiDirectional) return true;
            return false;
        }
        static bool CanDrawN(Cell from, Cell to)
        {
            if (from.HasGate) return false;
            if (from.HasP) return false;
            var linkInfo = from.GetNeighborInfo(to);
            if (from.HasN && to.HasN && linkInfo.SiliconLink != SiliconLink.BiDirectional) return true;
            if (from.HasN && to.HasNoSilicon) return true;
            var indexForTarget = to.GetNeighborIndex(from);
            var rotatedIndex1 = (indexForTarget + 1) % 4;
            var rotatedIndex2 = (indexForTarget + 3) % 4; // modular arithmetics, bitches
            //can only draw the gate into a line of at least 3 connected N cells
            if (from.HasN && to.HasP && to.NeighborInfos[rotatedIndex1]?.SiliconLink == SiliconLink.BiDirectional &&
                to.NeighborInfos[rotatedIndex2]?.SiliconLink == SiliconLink.BiDirectional) return true;
            return false;
        }
        public void DrawSilicon(DrawArgs args, bool isPType)
        {
            if (args.IsOnSingleCell)
            {
                var cell = Level.Cells[args.FromRow, args.FromCol];
                if (cell.SiliconLayerContent != SiliconTypes.None) return;
                Level.Cells[args.FromRow, args.FromCol].SiliconLayerContent = 
                    isPType ? SiliconTypes.PType : SiliconTypes.NType;
                RebuildModel();
                return;
            }
            if (!args.IsBetweenNeighbors) return; //don't ruin the level!
            var fromCell = Level.Cells[args.FromRow, args.FromCol];
            var toCell = Level.Cells[args.ToRow, args.ToCol];
            var neighborInfo = fromCell.GetNeighborInfo(toCell);
            if (isPType && CanDrawP(fromCell, toCell))
            {
                if (toCell.HasNoSilicon)
                {
                    toCell.SiliconLayerContent = SiliconTypes.PType;
                    neighborInfo.SiliconLink = SiliconLink.BiDirectional;
                }
                else if (toCell.HasP)
                {
                    neighborInfo.SiliconLink = SiliconLink.BiDirectional;
                }
                else if (toCell.HasN)
                {
                    //the gate direction is perpendicular to the link direction
                    toCell.SiliconLayerContent = toCell.IsHorizontalNeighborOf(fromCell)
                        ? SiliconTypes.NTypeVGate : SiliconTypes.NTypeHGate;
                    neighborInfo.SiliconLink = SiliconLink.Master; //from cell is the master cell
                }
                else throw new InvalidOperationException("You missed a case here!");
                RebuildModel();
                return;
            }
            if (!isPType && CanDrawN(fromCell, toCell))
            {
                if (toCell.HasNoSilicon)
                {
                    toCell.SiliconLayerContent = SiliconTypes.NType;
                    neighborInfo.SiliconLink = SiliconLink.BiDirectional;
                }
                else if (toCell.HasN)
                {
                    neighborInfo.SiliconLink = SiliconLink.BiDirectional;
                }
                else if (toCell.HasP)
                {
                    //the gate direction is perpendicular to the link direction
                    toCell.SiliconLayerContent = toCell.IsHorizontalNeighborOf(fromCell)
                        ? SiliconTypes.PTypeVGate : SiliconTypes.PTypeHGate;
                    neighborInfo.SiliconLink = SiliconLink.Master; //from cell is the master cell
                }
                else throw new InvalidOperationException("You missed a case here!");
                RebuildModel();
                return;
            }


            RebuildModel();
            
        }
        public void PutVia(int row, int col)
        {
            var cell = Level.Cells[row, col];
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
        public void DeleteMetal(int row, int col)
        {
            var cell = Level.Cells[row, col];
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
        public void DeleteSilicon(int row, int col)
        {
            var cell = Level.Cells[row, col];
            if (cell.HasNoSilicon) return;
            foreach (var ni in cell.NeighborInfos)
            {
                if (ni.SiliconLink != SiliconLink.None) ni.SiliconLink = SiliconLink.None;
                if (ni.ToCell.HasGate) ni.ToCell.SiliconLayerContent = DeleteSiliconDic[ni.ToCell.SiliconLayerContent];
            }
            cell.SiliconLayerContent = SiliconTypes.None;
            RebuildModel();
        }
        public void DeleteVia(int row, int col)
        {
            var cell = Level.Cells[row, col];
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

        public Graphics Graphics { get; }
        public Bitmap Bitmap { get; }

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

        private static readonly DColor BgColor = "7F8AFF".AsDColor();
        private static readonly DBrush PBrush = new SolidBrush("A0B6BD00".AsDColor());
        private static readonly DBrush NBrush = new SolidBrush("60800000".AsDColor());
        private static readonly DBrush MetalBrush = new SolidBrush("80FFFFFF".AsDColor());
        private static readonly DPen GridPen = new DPen(DColor.FromArgb(60, DColor.Black));
        private static readonly DPen LightnessPen = new DPen(DColor.FromArgb(250, DColor.White));
        private static readonly DPen DarknessPen = new DPen(DColor.FromArgb(250, DColor.Black));
        void DrawGrid()
        {
            var w = Bitmap.Width;
            var h = Bitmap.Height;
            for (int i = 0; i <= Math.Max(Level.Width, Level.Height)*(CellSize+1); i+=(CellSize+1))
            {
                Graphics.DrawLine(GridPen, i, 0, i, h);
                Graphics.DrawLine(GridPen, 0, i, w, i);
            }
        }

        static Rectangle GetLinkRectangle(int cx, int cy, int neighborIndex)
        {
            switch (neighborIndex)
            {
                case 0: return new Rectangle(cx, cy + 2, 2, CellSize - 4);
                case 1: return new Rectangle(cx + 2, cy, CellSize - 4, 2);
                case 2: return new Rectangle(cx + CellSize - 2, cy + 2, 3, CellSize - 4); //to draw over grid
                case 3: return new Rectangle(cx + 2, cy + CellSize - 2, CellSize - 4, 3); //to draw over grid
                default: throw new ArgumentException("Expected index from 0 to 3");
            }
        }

        (DPen pen, PointF from, PointF to) GetBorderLineInfo(int cx, int cy, int neighborIndex)
        {
            switch (neighborIndex)
            {
                case 0: return (LightnessPen, 
                        new PointF(cx + 2, cy + 2), 
                        new PointF(cx + 2, cy + CellSize - 2) );
                case 1:
                    return (LightnessPen,
                        new PointF(cx + 2, cy + 2),
                        new PointF(cx + CellSize - 2, cy + 2) );
                case 2:
                    return (DarknessPen,
                        new PointF(cx + CellSize - 2, cy + 2),
                        new PointF(cx + CellSize - 2, cy + CellSize - 2));
                case 3:
                    return (DarknessPen,
                        new PointF(cx + 2, cy + CellSize - 2),
                        new PointF(cx + CellSize - 2, cy + CellSize - 2));
                default: throw new ArgumentException("Expected neighborIndex between 0 and 3");
            }
        }

        void DrawAuxLines(int cx, int cy, int sideIndex, NeighborInfo[] neighborInfos)
        {
            if (sideIndex == 0)
            {
                if (neighborInfos[1]?.SiliconLink != SiliconLink.None)
                    Graphics.DrawLineEx(LightnessPen, cx + 2, cy-1, cx + 2, cy + 3);
                if (neighborInfos[3]?.SiliconLink != SiliconLink.None)
                    Graphics.DrawLineEx(LightnessPen, cx + 2, cy + CellSize - 2, cx + 2, cy + CellSize);
            }
            if (sideIndex == 1)
            {
                if (neighborInfos[0]?.SiliconLink != SiliconLink.None)
                    Graphics.DrawLineEx(LightnessPen, cx, cy + 2, cx + 2, cy + 2);
                if (neighborInfos[2]?.SiliconLink != SiliconLink.None)
                    Graphics.DrawLineEx(LightnessPen, cx + CellSize - 2, cy + 2, cx + CellSize, cy + 2);
            }
            if (sideIndex == 2)
            {
                if (neighborInfos[1]?.SiliconLink != SiliconLink.None)
                    Graphics.DrawLineEx(DarknessPen, cx + CellSize - 2, cy, cx + CellSize - 2, cy + 2);
                if (neighborInfos[3]?.SiliconLink != SiliconLink.None)
                    Graphics.DrawLineEx(DarknessPen, cx + CellSize - 2, cy + CellSize - 2, cx + CellSize - 2,
                        cy + CellSize);
            }
            if (sideIndex == 3)
            {
                if (neighborInfos[0]?.SiliconLink != SiliconLink.None)
                    Graphics.DrawLineEx(DarknessPen, cx, cy + CellSize - 2, cx + 2, cy + CellSize - 2);
                if (neighborInfos[2]?.SiliconLink != SiliconLink.None)
                    Graphics.DrawLineEx(DarknessPen, cx + CellSize - 2, cy + CellSize - 2, cx + CellSize,
                        cy + CellSize - 2);
            }
        }

        void DrawSilicon()
        {
            for (int i = 0; i < Level.Height; i++)
                for (int j = 0; j < Level.Width; j++)
                {
                    var cell = Level.Cells[i, j];
                    var x0 = 1 + j * (CellSize + 1);
                    var y0 = 1 + i * (CellSize + 1);
                    if (cell.HasNoSilicon) continue;
                    if (cell.HasP) Graphics.FillRectangle(PBrush, x0 + 2, y0 + 2, CellSize - 4, CellSize - 4);
                    if (cell.HasN) Graphics.FillRectangle(NBrush, x0 + 2, y0 + 2, CellSize - 4, CellSize - 4);
                    if (cell.HasVia) Graphics.DrawEllipse(Pens.Black, x0 + 5, y0 + 5, CellSize - 10, CellSize - 10);
                    if (cell.HasNGate)
                    {
                        Graphics.FillRectangle(NBrush, x0 + 2, y0 + 2, CellSize - 4, CellSize - 4);
                        Graphics.FillRectangle(PBrush, x0 + 2, y0 + 2, CellSize - 4, CellSize - 4);
                    }
                    if (cell.HasPGate)
                    {
                        Graphics.FillRectangle(PBrush, x0 + 2, y0 + 2, CellSize - 4, CellSize - 4);
                        Graphics.FillRectangle(NBrush, x0 + 2, y0 + 2, CellSize - 4, CellSize - 4);
                    }
                    for (int k = 0; k < 4; k++)
                    {
                        var rect = GetLinkRectangle(x0, y0, k);
                        var ni = cell.NeighborInfos[k];
                        if (ni.SiliconLink == SiliconLink.BiDirectional || ni.SiliconLink == SiliconLink.Master)
                            Graphics.FillRectangle(cell.IsBaseN ? NBrush : PBrush, rect);
                        //gate slave
                        if (ni.SiliconLink == SiliconLink.Slave) Graphics.FillRectangle(cell.IsBaseN ? PBrush : NBrush, rect);
                         
                    }
                }
            for (int i = 0; i < Level.Height; i++)
            for (int j = 0; j < Level.Width; j++)
            {
                var cell = Level.Cells[i, j];
                if (cell.HasNoSilicon) continue;
                var x0 = 1 + j * (CellSize + 1);
                var y0 = 1 + i * (CellSize + 1);
                for (int k = 0; k < 4; k++)
                {
                    var ni = cell.NeighborInfos[k];
                    if (ni.SiliconLink == SiliconLink.None)
                    {
                        var nfo = GetBorderLineInfo(x0, y0, k);
                        Graphics.DrawLine(nfo.pen, nfo.from, nfo.to);
                    }
                    DrawAuxLines(x0, y0, k, cell.NeighborInfos);
                }
            }

        }
        void DrawMetal()
        {
            for (int i = 0; i < Level.Height; i++)
                for (int j = 0; j < Level.Width; j++)
                {
                    var cell = Level.Cells[i, j];
                    var x0 = 1 + j * (CellSize + 1);
                    var y0 = 1 + i * (CellSize + 1);
                    if (!cell.HasMetal) continue;
                    Graphics.FillRectangle(MetalBrush, x0 + 2, y0 + 2, CellSize - 4, CellSize - 4);
                    for (int k = 0; k < 4; k++)
                    {
                        var rect = GetLinkRectangle(x0, y0, k);
                        var ni = cell.NeighborInfos[k];
                        if (ni == null) continue;
                        if (ni.HasMetalLink)
                            Graphics.FillRectangle(MetalBrush, rect);
                    }
                    if (cell.HasVia) Graphics.DrawEllipse(Pens.Black, x0 + 3, y0 + 4, CellSize - 8, CellSize - 8);
                }
        }


        public void RebuildModel()
        {
            Graphics.Clear(BgColor);
            DrawGrid();
            DrawSilicon();
            DrawMetal();
            BitmapImage bmpImage = new BitmapImage();
            MemoryStream stream = new MemoryStream();
            Bitmap.Save(stream, ImageFormat.Bmp);
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

        #region PropertyChanged



        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
     
}
