﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Drawing.Brushes;
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
        private DrawMode _drawMode;
        public Level Level { get; }

        private const int CellSize = 12;
        private const int ViaSize = 6;

        public Game(Level level)
        {
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

        private static readonly DColor BgColor = "959595".AsDColor();
        private static readonly DBrush PBrush = new SolidBrush("FFF6FF00".AsDColor());
        private static readonly DBrush NBrush = new SolidBrush("FFB60000".AsDColor());
        private static readonly DBrush PGateBrush = new SolidBrush("FF860000".AsDColor());
        private static readonly DBrush NGateBrush = new SolidBrush("FFEDC900".AsDColor());
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
            const int linkPenetration = 5;
            
            switch (neighborIndex)
            {
                case 0: return new Rectangle(cx - (linkPenetration / 2 + 1), cy, linkPenetration, CellSize - 1);
                case 1: return new Rectangle(cx, cy - (linkPenetration / 2 + 1), CellSize - 1, linkPenetration);
//                case 2: return new Rectangle(cx + CellSize, cy, 1, CellSize); //to draw over grid
//                case 3: return new Rectangle(cx, cy + CellSize, CellSize, 1); //to draw over grid
                default: throw new ArgumentException("Expected index from 0 to 1");
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

        private static (DBrush NonGate, DBrush Gate) SelectSiliconBrush(Cell cell)
        {
            return cell.HasP || cell.HasPGate
                ? cell.HasN || cell.HasNGate
                    ? throw new InvalidOperationException("both P and N silicon on single cell")
                    : (PBrush, PGateBrush)
                : cell.HasN || cell.HasNGate
                    ? (NBrush, NGateBrush)
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
            var (brush, gateBrush) = SelectSiliconBrush(cell);

            var link = cell.NeighborInfos[(int) side]?.SiliconLink ?? SiliconLink.None;
            var oppositeLink = cell.NeighborInfos[(int) GetOppositeSide(side)]?.SiliconLink ?? SiliconLink.None;
            var hasSlaveLinkInDimension = link == SiliconLink.Slave || oppositeLink == SiliconLink.Slave;
            
            var actualBrush = hasSlaveLinkInDimension ? gateBrush : brush;
            
            var (rect, nearToBounds, nearToCenter) = GetCellSideBounds(cellBounds.X, cellBounds.Y, side);
            Graphics.FillRectangle(actualBrush, rect);
            
            // todo);
            if (link == SiliconLink.None || hasSlaveLinkInDimension) Graphics.FillRectangle(Brushes.Black, nearToBounds);
            if (cell.HasGate && !hasSlaveLinkInDimension) Graphics.FillRectangle(Brushes.Black, nearToCenter);
        }
        
        private void MetalCellSide(Cell cell, Side side, Rectangle cellBounds)
        {
            var (rect, nearToBounds, _) = GetCellSideBounds(cellBounds.X, cellBounds.Y, side);
            Graphics.FillRectangle(MetalBrush, rect);
            
            // todo);
            if (!(cell.NeighborInfos[(int) side]?.HasMetalLink ?? false)) Graphics.FillRectangle(Brushes.Black, nearToBounds);
        }

        private void SiliconCellCorner(Cell cell, Corner corner, bool linked, Rectangle cellBounds)
        {
            var (brush, _) = SelectSiliconBrush(cell);

            var (nearToCenter, nearToBounds, nearHorzLink, nearVertLink) = GetCellCornerBounds(cellBounds.X, cellBounds.Y, corner);
            
            var horzNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarX) ? 2 : 0];
            var vertNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarY) ? 3 : 1];

            var hasHorzLink = (horzNeigh?.SiliconLink ?? SiliconLink.None) != SiliconLink.None;
            var hasVertLink = (vertNeigh?.SiliconLink ?? SiliconLink.None) != SiliconLink.None;

            // todo remove copypaste
            if (hasHorzLink && hasVertLink)
            {
                Graphics.DrawPolygon(new DPen(brush), new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.FillPolygon(brush, new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Bitmap.SetPixel(nearToBounds.X, nearToBounds.Y, DColor.Black);
            }
            else if (hasHorzLink)
            {
                Graphics.DrawPolygon(new DPen(brush), new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.FillPolygon(brush, new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.DrawLine(Pens.Black, nearToBounds, nearVertLink);
            }
            else if (hasVertLink)
            {
                Graphics.DrawPolygon(new DPen(brush), new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.FillPolygon(brush, new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.DrawLine(Pens.Black, nearHorzLink, nearToBounds);
            }
            else
            {
                Graphics.DrawPolygon(new DPen(brush), new[] { nearHorzLink, nearVertLink, nearToCenter });
                Graphics.FillPolygon(brush, new[] { nearHorzLink, nearVertLink, nearToCenter });
                Graphics.DrawLine(Pens.Black, nearHorzLink, nearVertLink);
            }
            
            if (cell.HasGate) // overdraw!!!
            {
                var oppositeVertNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarY) ? 1 : 3];
                var isVerticalGate =
                    (vertNeigh?.SiliconLink ?? SiliconLink.None) == SiliconLink.Slave ||
                    (oppositeVertNeigh?.SiliconLink ?? SiliconLink.None) == SiliconLink.Slave;
                
                if (isVerticalGate) Graphics.DrawLine(Pens.Black, nearVertLink, nearToCenter);
                else Graphics.DrawLine(Pens.Black, nearHorzLink, nearToCenter);
            }
        }

        private void MetalCellCorner(Cell cell, Corner corner, bool linked, Rectangle cellBounds)
        {
            var brush = MetalBrush;

            var (nearToCenter, nearToBounds, nearHorzLink, nearVertLink) = GetCellCornerBounds(cellBounds.X, cellBounds.Y, corner);
            
            var horzNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarX) ? 2 : 0];
            var vertNeigh = cell.NeighborInfos[corner.HasFlag(Corner.FarY) ? 3 : 1];

            var hasHorzLink = horzNeigh?.HasMetalLink ?? false;
            var hasVertLink = vertNeigh?.HasMetalLink ?? false;

            // todo remove copypaste
            if (hasHorzLink && hasVertLink)
            {
                // that won't work on insets larger than 2 (System.Drawing sucks)
                Graphics.DrawPolygon(new DPen(brush), new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Bitmap.SetPixel(nearToBounds.X, nearToBounds.Y, DColor.Black);
            }
            else if (hasHorzLink)
            {
                Graphics.DrawPolygon(new DPen(brush), new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.DrawLine(Pens.Black, nearToBounds, nearVertLink);
            }
            else if (hasVertLink)
            {
                Graphics.DrawPolygon(new DPen(brush), new[] { nearToCenter, nearHorzLink, nearToBounds, nearVertLink });
                Graphics.DrawLine(Pens.Black, nearHorzLink, nearToBounds);
            }
            else
            {
                Graphics.DrawPolygon(new DPen(brush), new[] { nearHorzLink, nearVertLink, nearToCenter });
                Graphics.DrawLine(Pens.Black, nearHorzLink, nearVertLink);
            }
        }

        private void SiliconIntercellular(Cell cell, bool isVertical, SiliconLink siliconLink, Rectangle cellBounds)
        {
            if (siliconLink == SiliconLink.None) return;

            
            var brush =
                cell.HasN ? NBrush : cell.HasP ? PBrush : siliconLink != SiliconLink.Slave ? cell.HasPGate ? PBrush : NBrush : cell.HasPGate ? NBrush : PBrush;

            if (isVertical)
            {
                Bitmap.SetPixel(cellBounds.Left, cellBounds.Top - 1, DColor.Black);
                Bitmap.SetPixel(cellBounds.Right - 1, cellBounds.Top - 1, DColor.Black);
                Graphics.DrawLine(new DPen(brush), 
                    cellBounds.Left + 1, 
                    cellBounds.Top - 1, 
                    cellBounds.Right - 2,
                    cellBounds.Top - 1);
            } 
            else 
            {
                Bitmap.SetPixel(cellBounds.Left - 1, cellBounds.Top, DColor.Black);
                Bitmap.SetPixel(cellBounds.Left - 1, cellBounds.Bottom - 1, DColor.Black);
                Graphics.DrawLine(new DPen(brush), 
                    cellBounds.Left - 1, 
                    cellBounds.Top + 1, 
                    cellBounds.Left - 1,
                    cellBounds.Bottom - 2);
            }
        }

        private void MetalIntercellular(Cell cell, bool isVertical, Rectangle cellBounds)
        {
            var brush = MetalBrush;

            if (isVertical)
            {
                Bitmap.SetPixel(cellBounds.Left, cellBounds.Top - 1, DColor.Black);
                Bitmap.SetPixel(cellBounds.Right - 1, cellBounds.Top - 1, DColor.Black);
                Graphics.DrawLine(new DPen(brush), 
                    cellBounds.Left + 1, 
                    cellBounds.Top - 1, 
                    cellBounds.Right - 2,
                    cellBounds.Top - 1);
            } 
            else 
            {
                Bitmap.SetPixel(cellBounds.Left - 1, cellBounds.Top, DColor.Black);
                Bitmap.SetPixel(cellBounds.Left - 1, cellBounds.Bottom - 1, DColor.Black);
                Graphics.DrawLine(new DPen(brush), 
                    cellBounds.Left - 1, 
                    cellBounds.Top + 1, 
                    cellBounds.Left - 1,
                    cellBounds.Bottom - 2);
            }
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
                    var (brush, gateBrush) = SelectSiliconBrush(cell);
                    FillMid(cell.HasGate ? gateBrush : brush, bounds);

                    // todo
                    SiliconCellSide(cell, Side.Top, bounds);
                    SiliconCellSide(cell, Side.Bottom, bounds);
                    SiliconCellSide(cell, Side.Left, bounds);
                    SiliconCellSide(cell, Side.Right, bounds);

                    SiliconCellCorner(cell, Corner.Near, false, bounds);
                    SiliconCellCorner(cell, Corner.FarX, false, bounds);
                    SiliconCellCorner(cell, Corner.FarY, false, bounds);
                    SiliconCellCorner(cell, Corner.Far, false, bounds);
                    
                    SiliconIntercellular(cell, false, cell.NeighborInfos[0]?.SiliconLink ?? SiliconLink.None, bounds);
                    SiliconIntercellular(cell, true, cell.NeighborInfos[1]?.SiliconLink ?? SiliconLink.None, bounds);
                    
                    if (cell.HasVia) // in original game vias displaying under metal layer
                    {
                        var viaX = bounds.X + (bounds.Width - ViaSize) / 2;
                        var viaY = bounds.Y + (bounds.Height - ViaSize) / 2;

                        Graphics.DrawLine(Pens.Black, viaX + 1, viaY, viaX + ViaSize - 2, viaY);
                        Graphics.DrawLine(Pens.Black, viaX + 1, viaY + ViaSize - 1, viaX + ViaSize - 2, viaY + ViaSize - 1);
                        Graphics.DrawLine(Pens.Black,viaX, viaY + 1, viaX, viaY + ViaSize - 2);
                        Graphics.DrawLine(Pens.Black, viaX + ViaSize - 1, viaY + 1, viaX + ViaSize - 1, viaY + ViaSize - 2);
                    }
                }

                if (cell.HasMetal)
                {
                    FillMid(MetalBrush, bounds);

                    // todo
                    MetalCellSide(cell, Side.Top, bounds);
                    MetalCellSide(cell, Side.Bottom, bounds);
                    MetalCellSide(cell, Side.Left, bounds);
                    MetalCellSide(cell, Side.Right, bounds);

                    MetalCellCorner(cell, Corner.Near, false, bounds);
                    MetalCellCorner(cell, Corner.FarX, false, bounds);
                    MetalCellCorner(cell, Corner.FarY, false, bounds);
                    MetalCellCorner(cell, Corner.Far, false, bounds);
                    
                    if (cell.NeighborInfos[0]?.HasMetalLink ?? false) MetalIntercellular(cell, false, bounds);
                    if (cell.NeighborInfos[1]?.HasMetalLink ?? false) MetalIntercellular(cell, true, bounds);
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
                    Graphics.FillRectangle(MetalBrush, x0, y0, CellSize, CellSize);
                    for (int k = 0; k < 2; k++)
                    {
                        var rect = GetLinkRectangle(x0, y0, k);
                        var ni = cell.NeighborInfos[k];
                        if (ni == null) continue;
                        if (ni.HasMetalLink)
                            Graphics.FillRectangle(MetalBrush, rect);
                    }
                }
        }


        public void RebuildModel()
        {
            Graphics.Clear(BgColor);
            DrawGrid();
            DrawSiliconAndMetal();
            // DrawMetal();
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
