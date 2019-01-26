using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DColor = System.Drawing.Color;
using DPen = System.Drawing.Pen;
using DBrush = System.Drawing.Brush;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;
using DPoint = System.Drawing.Point;

namespace Kohctpyktop
{
    public class Game : INotifyPropertyChanged, IDisposable
    {
        private BitmapSource _bitmapSource;
        private SelectedTool _selectedTool;
        private DrawMode _drawMode;
        private bool _isShiftPressed;
        public Level Level { get; }

        private const int CellSize = 12;
        private const int ViaSize = 6;

        public Game(Level level)
        {
            SelectedTool = SelectedTool.Silicon;
            
            Level = level;
            Bitmap = new Bitmap(
                (CellSize + 1) * level.Cells.GetLength(1) + 1, 
                (CellSize + 1) * level.Cells.GetLength(0) + 1);
            Graphics = Graphics.FromImage(Bitmap);
            Graphics.CompositingQuality = CompositingQuality.HighSpeed; // quality is not required actually, rendering is pixel-perfect
            Graphics.InterpolationMode = InterpolationMode.NearestNeighbor; // this too
            Graphics.SmoothingMode = SmoothingMode.None; // causes artifacts
            Graphics.PixelOffsetMode = PixelOffsetMode.None; // this too
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

        private static readonly DColor BorderColor = DColor.Black;
        private static readonly DColor BgColor = "959595".AsDColor();
        private static readonly DBrush PBrush = new SolidBrush("FFF6FF00".AsDColor());
        private static readonly DBrush NBrush = new SolidBrush("FFB60000".AsDColor());
        private static readonly DBrush PGateBrush = new SolidBrush("FF860000".AsDColor());
        private static readonly DBrush NGateBrush = new SolidBrush("FFEDC900".AsDColor());
        private static readonly DBrush MetalBrush = new SolidBrush("80FFFFFF".AsDColor());
        private static readonly DBrush BorderBrush = new SolidBrush(BorderColor);
        private static readonly DPen GridPen = new DPen(DColor.FromArgb(60, DColor.Black));
        private static readonly DPen BorderPen = new DPen(BorderBrush);
        private static readonly DPen PPen = new DPen(PBrush);
        private static readonly DPen NPen = new DPen(NBrush);
        private static readonly DPen MetalPen = new DPen(MetalBrush);
        
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
        
        private const int CellInsets = 2;

        private static Rectangle GetCellBounds(int x, int y)
        {
            return new Rectangle(1 + x * (CellSize + 1), 1 + y * (CellSize + 1), CellSize, CellSize);
        }
        
        public enum Side { Left, Top, Right, Bottom }

        [Flags]
        public enum Corner
        {
            Near = 0, 
            FarX = 1, 
            FarY = 2, 
            Far = FarX | FarY
        }
        
        private void FillMid(DBrush brush, Rectangle cellBounds)
        {
            cellBounds.Inflate(-CellInsets, -CellInsets);
            Graphics.FillRectangle(brush, cellBounds);
        }

        private static bool IsHorizontalSide(Side side) => side == Side.Left || side == Side.Right;
        private static bool IsVerticalSide(Side side) => side == Side.Bottom || side == Side.Top;

        private static bool IsSideNearToBoundsOrigin(Side side) => side == Side.Top || side == Side.Left;
        private static bool IsSideFarFromBoundsOrigin(Side side) => side == Side.Right || side == Side.Bottom;

        private static (Rectangle Rect, Rectangle NearToBounds, Rectangle NearToCenter) GetCellSideBounds(int originX, int originY, Side side)
        {
            var isFar = IsSideFarFromBoundsOrigin(side);

            var rectOfs = isFar ? CellSize - CellInsets : 0;
            var lineOfs = isFar ? CellSize - 1 : 0;
            var centerOfs = isFar ? CellSize - CellInsets : CellInsets - 1;

            return IsVerticalSide(side)
                ? (new Rectangle(originX + CellInsets, originY + rectOfs, CellSize - 2 * CellInsets, CellInsets),
                    new Rectangle(originX + CellInsets, originY + lineOfs, CellSize - 2 * CellInsets, 1),
                    new Rectangle(originX + CellInsets, originY + centerOfs, CellSize - 2 * CellInsets, 1))
                : (new Rectangle(originX + rectOfs, originY + CellInsets, CellInsets, CellSize - 2 * CellInsets),
                    new Rectangle(originX + lineOfs, originY + CellInsets, 1, CellSize - 2 * CellInsets),
                    new Rectangle(originX + centerOfs, originY + CellInsets, 1, CellSize - 2 * CellInsets));
        }
        
        private static (DPoint NearToCenter, DPoint NearToBounds, DPoint NearHorzLink, DPoint NearVertLink) 
            GetCellCornerBounds(int originX, int originY, Corner corner)
        {
            var isFarX = corner.HasFlag(Corner.FarX);
            var isFarY = corner.HasFlag(Corner.FarY);

            var nearHorz = originX + (isFarX ? CellSize - 1 : 0);
            var nearVert = originY + (isFarY ? CellSize - 1 : 0);
            var farHorz = originX + (isFarX ? CellSize - CellInsets : CellInsets - 1);
            var farVert = originY + (isFarY ? CellSize - CellInsets : CellInsets - 1);

            return (
                new DPoint(farHorz, farVert),
                new DPoint(nearHorz, nearVert),
                new DPoint(nearHorz, farVert),
                new DPoint(farHorz, nearVert));
        }

        private static (DPen NonGatePen, DBrush NonGateBrush, DBrush GateBrush) 
            SelectSiliconBrush(Cell cell)
        {
            return cell.HasP || cell.HasPGate
                ? cell.HasN || cell.HasNGate
                    ? throw new InvalidOperationException("both P and N silicon on single cell")
                    : (PPen, PBrush, PGateBrush)
                : cell.HasN || cell.HasNGate
                    ? (NPen, NBrush, NGateBrush)
                    : throw new InvalidOperationException("no silicon on cell");
        }

        private static Side GetOppositeSide(Side side)
        {
            switch (side)
            {
                case Side.Top: return Side.Bottom;
                case Side.Bottom: return Side.Top;
                case Side.Left: return Side.Right;
                case Side.Right: return Side.Left;
                default: throw new InvalidOperationException("invalid side " + side); 
            }
        }
        
        private void SiliconCellSide(Cell cell, Side side, Rectangle cellBounds)
        {
            var (_, brush, gateBrush) = SelectSiliconBrush(cell);

            var link = cell.NeighborInfos[(int) side]?.SiliconLink ?? SiliconLink.None;
            var oppositeLink = cell.NeighborInfos[(int) GetOppositeSide(side)]?.SiliconLink ?? SiliconLink.None;
            var hasSlaveLinkInDimension = link == SiliconLink.Slave || oppositeLink == SiliconLink.Slave;
            
            var actualBrush = hasSlaveLinkInDimension ? gateBrush : brush;
            
            var (rect, nearToBounds, nearToCenter) = GetCellSideBounds(cellBounds.X, cellBounds.Y, side);
            Graphics.FillRectangle(actualBrush, rect);
            
            if (link == SiliconLink.None || hasSlaveLinkInDimension) Graphics.FillRectangle(BorderBrush, nearToBounds);
            if (cell.HasGate && !hasSlaveLinkInDimension) Graphics.FillRectangle(BorderBrush, nearToCenter);
        }
        
        private void MetalCellSide(Cell cell, Side side, Rectangle cellBounds)
        {
            var (rect, nearToBounds, _) = GetCellSideBounds(cellBounds.X, cellBounds.Y, side);
            Graphics.FillRectangle(MetalBrush, rect);
            
            if (!(cell.NeighborInfos[(int) side]?.HasMetalLink ?? false))
                Graphics.FillRectangle(BorderBrush, nearToBounds);
        }

        private void GenericCellCorner(bool hasHorzLink, bool hasVertLink, DPen pen,
            DPoint nearToCenter, DPoint nearToBounds, DPoint nearHorzLink, DPoint nearVertLink)
        {
            if (hasHorzLink && hasVertLink)
            {
                // that won't work on insets larger than 2 (System.Drawing sucks)
                Graphics.DrawPolygon(pen, new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Bitmap.SetPixel(nearToBounds.X, nearToBounds.Y, BorderColor);
            }
            else if (hasHorzLink)
            {
                Graphics.DrawPolygon(pen, new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.DrawLine(BorderPen, nearToBounds, nearVertLink);
            }
            else if (hasVertLink)
            {
                Graphics.DrawPolygon(pen, new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.DrawLine(BorderPen, nearHorzLink, nearToBounds);
            }
            else
            {
                Graphics.DrawPolygon(pen, new[] { nearHorzLink, nearVertLink, nearToCenter });
                Graphics.DrawLine(BorderPen, nearHorzLink, nearVertLink);
            }
        }

        private void SiliconCellCorner(Cell cell, Corner corner, Rectangle cellBounds)
        {
            var (pen, _, _) = SelectSiliconBrush(cell);

            var (nearToCenter, nearToBounds, nearHorzLink, nearVertLink) = GetCellCornerBounds(cellBounds.X, cellBounds.Y, corner);
            
            var horzNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarX) ? 2 : 0];
            var vertNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarY) ? 3 : 1];

            var hasHorzLink = (horzNeigh?.SiliconLink ?? SiliconLink.None) != SiliconLink.None;
            var hasVertLink = (vertNeigh?.SiliconLink ?? SiliconLink.None) != SiliconLink.None;

            GenericCellCorner(hasHorzLink, hasVertLink, pen,
                nearToCenter, nearToBounds, nearHorzLink, nearVertLink);
            
            if (cell.HasGate) // overdraw!!!
            {
                var oppositeVertNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarY) ? 1 : 3];
                var isVerticalGate =
                    (vertNeigh?.SiliconLink ?? SiliconLink.None) == SiliconLink.Slave ||
                    (oppositeVertNeigh?.SiliconLink ?? SiliconLink.None) == SiliconLink.Slave;
                
                if (isVerticalGate) Graphics.DrawLine(BorderPen, nearVertLink, nearToCenter);
                else Graphics.DrawLine(BorderPen, nearHorzLink, nearToCenter);
            }
        }

        private void MetalCellCorner(Cell cell, Corner corner, Rectangle cellBounds)
        {
            var (nearToCenter, nearToBounds, nearHorzLink, nearVertLink) = GetCellCornerBounds(cellBounds.X, cellBounds.Y, corner);
            
            var horzNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarX) ? 2 : 0];
            var vertNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarY) ? 3 : 1];

            var hasHorzLink = horzNeigh?.HasMetalLink ?? false;
            var hasVertLink = vertNeigh?.HasMetalLink ?? false;

            GenericCellCorner(hasHorzLink, hasVertLink, MetalPen, 
                nearToCenter, nearToBounds, nearHorzLink, nearVertLink);
        }

        private void GenericIntercellular(Rectangle cellBounds, Pen pen, bool isVertical)
        {
            if (isVertical)
            {
                Bitmap.SetPixel(cellBounds.Left, cellBounds.Top - 1, BorderColor);
                Bitmap.SetPixel(cellBounds.Right - 1, cellBounds.Top - 1, BorderColor);
                Graphics.DrawLine(pen, 
                    cellBounds.Left + 1, 
                    cellBounds.Top - 1, 
                    cellBounds.Right - 2,
                    cellBounds.Top - 1);
            } 
            else 
            {
                Bitmap.SetPixel(cellBounds.Left - 1, cellBounds.Top, BorderColor);
                Bitmap.SetPixel(cellBounds.Left - 1, cellBounds.Bottom - 1, BorderColor);
                Graphics.DrawLine(pen, 
                    cellBounds.Left - 1, 
                    cellBounds.Top + 1, 
                    cellBounds.Left - 1,
                    cellBounds.Bottom - 2);
            }
        }
        
        private void SiliconIntercellular(Cell cell, bool isVertical, SiliconLink siliconLink, Rectangle cellBounds)
        {
            if (siliconLink == SiliconLink.None) return;

            var pen = // todo: simplify
                cell.HasN
                    ? NPen
                    : cell.HasP
                        ? PPen
                        : siliconLink != SiliconLink.Slave ^ cell.HasPGate
                            ? NPen
                            : PPen;

            GenericIntercellular(cellBounds, pen, isVertical);
        }

        private void MetalIntercellular(bool isVertical, Rectangle cellBounds)
        {
            GenericIntercellular(cellBounds, MetalPen, isVertical);
        }

        void DrawSiliconAndMetal()
        {
            for (int i = 0; i < Level.Height; i++)
            for (int j = 0; j < Level.Width; j++)
            {
                var cell = Level.Cells[i, j];
                var bounds = GetCellBounds(j, i);

                if (cell.HasN || cell.HasP || cell.HasNGate || cell.HasPGate)
                {
                    var (_, brush, gateBrush) = SelectSiliconBrush(cell);
                    FillMid(cell.HasGate ? gateBrush : brush, bounds);

                    SiliconCellSide(cell, Side.Top, bounds);
                    SiliconCellSide(cell, Side.Bottom, bounds);
                    SiliconCellSide(cell, Side.Left, bounds);
                    SiliconCellSide(cell, Side.Right, bounds);

                    SiliconCellCorner(cell, Corner.Near, bounds);
                    SiliconCellCorner(cell, Corner.FarX, bounds);
                    SiliconCellCorner(cell, Corner.FarY, bounds);
                    SiliconCellCorner(cell, Corner.Far, bounds);
                    
                    SiliconIntercellular(cell, false, cell.NeighborInfos[0]?.SiliconLink ?? SiliconLink.None, bounds);
                    SiliconIntercellular(cell, true, cell.NeighborInfos[1]?.SiliconLink ?? SiliconLink.None, bounds);
                    
                    if (cell.HasVia) // in original game vias displaying under metal layer
                    {
                        var viaX = bounds.X + (bounds.Width - ViaSize) / 2;
                        var viaY = bounds.Y + (bounds.Height - ViaSize) / 2;

                        Graphics.DrawLine(BorderPen, viaX + 1, viaY, viaX + ViaSize - 2, viaY);
                        Graphics.DrawLine(BorderPen, viaX + 1, viaY + ViaSize - 1, viaX + ViaSize - 2, viaY + ViaSize - 1);
                        Graphics.DrawLine(BorderPen,viaX, viaY + 1, viaX, viaY + ViaSize - 2);
                        Graphics.DrawLine(BorderPen, viaX + ViaSize - 1, viaY + 1, viaX + ViaSize - 1, viaY + ViaSize - 2);
                    }
                }

                if (cell.HasMetal)
                {
                    FillMid(MetalBrush, bounds);

                    MetalCellSide(cell, Side.Top, bounds);
                    MetalCellSide(cell, Side.Bottom, bounds);
                    MetalCellSide(cell, Side.Left, bounds);
                    MetalCellSide(cell, Side.Right, bounds);

                    MetalCellCorner(cell, Corner.Near, bounds);
                    MetalCellCorner(cell, Corner.FarX, bounds);
                    MetalCellCorner(cell, Corner.FarY, bounds);
                    MetalCellCorner(cell, Corner.Far, bounds);
                    
                    if (cell.NeighborInfos[0]?.HasMetalLink ?? false) MetalIntercellular(false, bounds);
                    if (cell.NeighborInfos[1]?.HasMetalLink ?? false) MetalIntercellular(true, bounds);
                }
            }
        }
        
        public void RebuildModel()
        {
            Graphics.Clear(BgColor);
            DrawGrid();
            DrawSiliconAndMetal();
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
