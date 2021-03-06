using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.IO;
using System.Linq;
using Kohctpyktop.Models.Templates;

namespace Kohctpyktop.Models.Field
{
    public class Layer : ILayer
    {
        private readonly LayerCellMatrix _cellMatrix;
        
        public Layer(LayerTemplate template)
        {
            _isLockCheckEnabled = false;
            
            Template = template;
            Width = template.Width;
            Height = template.Height;
            
            void BuildPin(Position pos, int width, int height)
            {
                for (var i = 0; i < width; i++)
                for (var j = 0; j < height; j++)
                {
                    AddCellMetal(pos.Offset(i, j));
                }
                
                for (var i = 0; i < width - 1; i++)
                for (var j = 0; j < height - 1; j++)
                {
                    AddLink(pos.Offset(i, j), pos.Offset(i + 1, j), LinkType.MetalLink);
                    AddLink(pos.Offset(i, j), pos.Offset(i, j + 1), LinkType.MetalLink);
                }
                
                for (var i = 0; i < width; i++)
                    AddLink(pos.Offset(i, height - 1), pos.Offset(i + 1, height - 1), LinkType.MetalLink);
                for (var i = 0; i < height; i++)
                    AddLink(pos.Offset(width - 1, i), pos.Offset(width - 1, i + 1), LinkType.MetalLink);
            }
            
            _cellMatrix = new LayerCellMatrix(this);

            foreach (var pin in template.Pins)
            {
                var pos = new Position(pin.Col, pin.Row);
                BuildPin(pos, pin.Width, pin.Height);
                SetCellPin(pos, pin);
            }
                        
            CommitChanges(false);
            
            _isLockCheckEnabled = true;
        }

        public Layer(SavedLayer savedLayer) : this(savedLayer.Template)
        {
            var layerData = savedLayer.Data;
            
            // todo: verification
            var height = layerData.Cells.GetLength(0);
            var width = layerData.Cells.GetLength(1);

            if (width != Template.Width || height != Template.Height) 
                throw new ArgumentException();
            
            if (layerData.RightLinks.GetLength(0) != height || layerData.RightLinks.GetLength(1) != width) 
                throw new ArgumentException();
            if (layerData.BottomLinks.GetLength(0) != height || layerData.BottomLinks.GetLength(1) != width) 
                throw new ArgumentException();
            
            Width = width;
            Height = height;
            
            _cellMatrix = new LayerCellMatrix(this);
            for (var i = 0; i < height; i++)
            for (var j = 0; j < width; j++)
            {
                _cellMatrix.UpdateCellContent(new Position(j, i), layerData.Cells[i, j]);
                _cellMatrix.UpdateLinkContent(new Position(j, i), Side.Right, layerData.RightLinks[i, j]);
                _cellMatrix.UpdateLinkContent(new Position(j, i), Side.Bottom, layerData.BottomLinks[i, j]);
            }
            
            CommitChanges(false);
        }

        public SavedLayer Save()
        {
            var cells = new CellContent[Height, Width];
            var rightLinks = new LinkContent[Height, Width];
            var bottomLinks = new LinkContent[Height, Width];
            
            for (var i = 0; i < Height; i++)
            for (var j = 0; j < Width; j++)
            {
                var cell = Cells[i, j];
                cells[i, j] = new CellContent(cell);
                rightLinks[i, j] = new LinkContent(cell.Links[Side.Right].SiliconLink, cell.Links[Side.Right].HasMetalLink);
                bottomLinks[i, j] = new LinkContent(cell.Links[Side.Bottom].SiliconLink, cell.Links[Side.Bottom].HasMetalLink);
            }

            var data = new LayerData(cells, rightLinks, bottomLinks);
            return new SavedLayer(Template, data);
        }
        
        public int Width { get; }
        public int Height { get; }

        public IReadOnlyMatrix<ILayerCell> Cells => _cellMatrix;
        public LayerTemplate Template { get; }

        public bool HasUncommitedChanges => _cellMatrix.HasUncommitedChanges;
        
        public void CommitChanges(bool revertable = true) => _cellMatrix.CommitChanges(revertable);
        public void RejectChanges() => _cellMatrix.RejectChanges();

        public int MaxUndoDepth { get; set; }
        public int MaxRedoDepth { get; set; }

        public bool CanUndo => _cellMatrix.CanUndo;
        
        public void Undo() => _cellMatrix.Undo();

        public bool CanRedo => _cellMatrix.CanRedo;
        public void Redo() => _cellMatrix.Redo();

        private void RemoveCellLinks(Position position, LinkType type)
        {
            RemoveLink(position, Side.Left, type);
            RemoveLink(position, Side.Top, type);
            RemoveLink(position, Side.Right, type);
            RemoveLink(position, Side.Bottom, type);
        }

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private bool _isLockCheckEnabled;
        
        private bool IsCellLocked(ILayerCell cell)
        {
            return _isLockCheckEnabled && Template.DeadZones.Any(x => x.Contains(cell.Column, cell.Row));
        }
        
        public bool AddCellSilicon(Position position, SiliconType siliconType)
        {
            var cell = _cellMatrix[position];
            if (!cell.IsValidCell || IsCellLocked(cell) || cell.Silicon != SiliconLayerContent.None) return false;

            var slcType = siliconType == SiliconType.NType ? SiliconLayerContent.NType : SiliconLayerContent.PType;

            _cellMatrix.UpdateCellContent(position, new CellContent(cell) {Silicon = slcType});
            return true;
        }

        public bool RemoveCellSilicon(Position position)
        {
            var cell = _cellMatrix[position];
            if (!cell.IsValidCell || IsCellLocked(cell) || cell.Silicon == SiliconLayerContent.None) return false;

            _cellMatrix.UpdateCellContent(position, new CellContent(cell) {Silicon = SiliconLayerContent.None});
            RemoveCellLinks(position, LinkType.SiliconLink);
            
            return true;
        }

        public bool AddCellMetal(Position position)
        {
            var cell = _cellMatrix[position];
            if (!cell.IsValidCell || IsCellLocked(cell) || cell.HasMetal) return false;

            _cellMatrix.UpdateCellContent(position, new CellContent(cell) {HasMetal = true});
            return true;
        }

        public bool RemoveCellMetal(Position position)
        {
            var cell = _cellMatrix[position];
            if (!cell.IsValidCell || IsCellLocked(cell) || !cell.HasMetal) return false;

            _cellMatrix.UpdateCellContent(position, new CellContent(cell) {HasMetal = false});
            RemoveCellLinks(position, LinkType.MetalLink);
            
            return true;
        }

        public bool RemoveCellAny(Position position)
        {
	        var cell = _cellMatrix[position];

	        if (!cell.IsValidCell || IsCellLocked(cell)) return false;

	        _cellMatrix.UpdateCellContent(position, new CellContent(cell)
		        {
			        HasMetal = false,
			        Silicon = SiliconLayerContent.None
		        });

	        RemoveCellLinks(position, LinkType.SiliconLink);
	        RemoveCellLinks(position, LinkType.MetalLink);

			return true;
		}

        private (bool, SiliconLink, SiliconLayerContent) CheckSiliconLink(ILayerCell fromCell, ILayerCell toCell, Side side)
        {
            var fromBase = fromCell.IsBaseN() ? SiliconType.NType : SiliconType.PType;
            var toBase = toCell.IsBaseN() ? SiliconType.NType : SiliconType.PType;

            if (fromBase != toBase)
            {
                var (p1, p2) = side.GetPerpendicularSides();

                return toCell.Links[p1].SiliconLink == SiliconLink.BiDirectional &&
                       toCell.Links[p2].SiliconLink == SiliconLink.BiDirectional &&
                       toCell.Links[side].SiliconLink != SiliconLink.BiDirectional &&
                       !toCell.HasVia()
                    ? (true, SiliconLink.Master, toCell.HasGate() ? toCell.Silicon : toBase.ConvertToGate(!side.IsVertical()))
                    : (false, SiliconLink.None, SiliconLayerContent.None);
            }
            else
            {
                return fromCell.HasGate() || toCell.HasGate()
                    ? (false, SiliconLink.None, SiliconLayerContent.None)
                    : (true, SiliconLink.BiDirectional, toCell.Silicon);
            }
        }
        
        public bool AddLink(Position from, Position to, LinkType linkType)
        {
            if (!from.IsAdjacent(to))
                return false;
            
            var fromCell = _cellMatrix[from];
            var toCell = _cellMatrix[to];
            if (!fromCell.IsValidCell || !toCell.IsValidCell) return false;
            
            if (IsCellLocked(fromCell) && IsCellLocked(toCell)) return false;
            
            var side = from.GetAdjacentSide(to);
            
            var existingLink = fromCell.Links[side];
            
            switch (linkType)
            {
                case LinkType.SiliconLink:
                    if (existingLink.SiliconLink != SiliconLink.None ||
                        !fromCell.HasSilicon() ||
                        !toCell.HasSilicon()) return false;

                    var (canPlace, newLinkType, targetSlc) = CheckSiliconLink(fromCell, toCell, side);
                    if (canPlace)
                    {
                        _cellMatrix.UpdateCellContent(to, new CellContent(toCell) {Silicon = targetSlc});
                        _cellMatrix.UpdateLinkContent(from, side, new LinkContent(newLinkType, existingLink.HasMetalLink));
                        return true;
                    }

                    return false;

                case LinkType.MetalLink:
                    if (existingLink.HasMetalLink ||
                        !fromCell.HasMetal ||
                        !toCell.HasMetal) return false;
                    
                    _cellMatrix.UpdateLinkContent(from, side, new LinkContent(existingLink.SiliconLink, true));
                    return true;
                default:
                    throw new ArgumentException(nameof(linkType));
            }
        }

        private bool IsValidGate(ILayerCell layerCell)
        {
            var isVerticalGate = layerCell.IsVerticalGate();
            var baseS1 = isVerticalGate ? Side.Top : Side.Left;
            var baseS2 = baseS1.Invert();
            var (slS1, slS2) = baseS1.GetPerpendicularSides();

            return layerCell.Links[baseS1].SiliconLink == SiliconLink.BiDirectional &&
                   layerCell.Links[baseS2].SiliconLink == SiliconLink.BiDirectional &&
                   (layerCell.Links[slS1].SiliconLink == SiliconLink.Slave ||
                    layerCell.Links[slS2].SiliconLink == SiliconLink.Slave);
        }

        public bool RemoveLink(Position from, Position to, LinkType linkType)
        {
            if (!from.IsAdjacent(to))
                return false;
            
            var fromCell = _cellMatrix[from];
            var toCell = _cellMatrix[to];
            if (!fromCell.IsValidCell || !toCell.IsValidCell) return false;

            if (IsCellLocked(fromCell) && IsCellLocked(toCell)) return false;
            
            var side = from.GetAdjacentSide(to);
            
            var existingLink = fromCell.Links[side];
            
            switch (linkType)
            {
                case LinkType.SiliconLink:
                    if (existingLink.SiliconLink == SiliconLink.None) return false;
                    
                    _cellMatrix.UpdateLinkContent(from, side, new LinkContent(SiliconLink.None, existingLink.HasMetalLink));

                    void EnsureGateState(Position pos, ILayerCell cell)
                    {
                        if (!cell.HasGate() || IsValidGate(cell)) return;
                        
                        _cellMatrix.UpdateCellContent(pos,
                            new CellContent(cell) { Silicon = cell.Silicon.RemoveGate()});
                        for (var i = 0; i < 4; i++)
                        {
                            var gateLinkSide = (Side) i;
                            var oldLink = cell.Links[gateLinkSide];
                            if (oldLink.SiliconLink == SiliconLink.Slave)
                                _cellMatrix.UpdateLinkContent(pos, (Side) i, new LinkContent(SiliconLink.None, oldLink.HasMetalLink));
                        }
                    }
                    
                    EnsureGateState(from, fromCell);
                    EnsureGateState(to, toCell);
                    
                    return true;
                case LinkType.MetalLink:
                    if (!existingLink.HasMetalLink) return false;
                    
                    _cellMatrix.UpdateLinkContent(from, side, new LinkContent(existingLink.SiliconLink, false));
                    return true;
                default:
                    throw new ArgumentException(nameof(linkType));
            }
        }

        public bool RemoveLink(Position from, Side side, LinkType linkType)
        {
            return RemoveLink(from, from.Shift(side), linkType);
        }

        public bool AddVia(Position position)
        {
            var cell = _cellMatrix[position];
            if (!cell.IsValidCell || IsCellLocked(cell)) return false;
            
            if (cell.HasVia() || !cell.HasSilicon() || cell.HasGate()) return false;

            _cellMatrix.UpdateCellContent(position, new CellContent(cell) { Silicon = cell.Silicon.AddVia()});
            return true;
        }

        public bool RemoveVia(Position position)
        {
            var cell = _cellMatrix[position];
            if (!cell.IsValidCell || IsCellLocked(cell)) return false;
            
            if (!cell.HasVia()) return false;

            _cellMatrix.UpdateCellContent(position, new CellContent(cell) { Silicon = cell.Silicon.RemoveVia()});
            return true;
        }
        
        [Obsolete("Set pins via template")]
        public bool SetCellPin(Position position, Pin pin)
        {
            var cell = _cellMatrix[position];
            if (!cell.IsValidCell) return false;

            if (cell.Pin != pin)
            {
                _cellMatrix.UpdateCellContent(position, new CellContent(cell) { Pin = pin });
                return true;
            }

            return false;
        }

        public bool MoveCells(Position from, Position to, int offsetX, int offsetY)
        {
            if (offsetX == 0 && offsetY == 0) return true;

            var points = new[]
            {
                from,
                to.Offset(-1, -1),
                from.Offset(offsetX, offsetY),
                to.Offset(offsetX - 1, offsetY - 1)
            };
            
            foreach (var point in points)
                if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height) return false;

            var width = to.X - from.X;
            var height = to.Y - from.Y;

            var tempCells = new CellContent[height, width];
            var tempLinks = new (LinkContent right, LinkContent bottom)[height + 1, width + 1];
            
            // todo: replace with own classes
            var sourceRect = Rectangle.FromLTRB(from.X, from.Y, to.X, to.Y);
            var targetRect = sourceRect;
            targetRect.Offset(offsetX, offsetY);

            var intersection = sourceRect;
            intersection.Intersect(targetRect);
            
            if (intersection.Width == 0 || intersection.Height == 0) intersection = Rectangle.Empty;
            
            // copying map part to temporary array
            for (int i = from.Y, irel = 0; i < to.Y; i++, irel++)
            for (int j = from.X, jrel = 0; j < to.X; j++, jrel++)
            {
                if (!intersection.Contains(j + offsetX, i + offsetY))
                {
                    var layerCell = Cells[i + offsetY, j + offsetX];
                    tempCells[irel, jrel] = new CellContent(layerCell);
                    var rlink = new LinkContent(
                        layerCell.Links[Side.Right].SiliconLink,
                        layerCell.Links[Side.Right].HasMetalLink);
                    var blink = new LinkContent(
                        layerCell.Links[Side.Bottom].SiliconLink,
                        layerCell.Links[Side.Bottom].HasMetalLink);
                    tempLinks[irel + 1, jrel + 1] = (rlink, blink);
                }
            }
            
            // copying left-top links
            for (int i = from.Y, irel = 0; i < to.Y; i++, irel++)
                if (!intersection.Contains(from.X + offsetX, i + offsetY))
                {
                    var layerCell = Cells[i + offsetY, from.X + offsetX];
                    var rlink = new LinkContent(
                        layerCell.Links[Side.Left].SiliconLink,
                        layerCell.Links[Side.Left].HasMetalLink);
                    tempLinks[irel + 1, 0] = (rlink.Invert(), default(LinkContent));
                }
            
            for (int j = from.X, jrel = 0; j < to.X; j++, jrel++)
                if (!intersection.Contains(j + offsetX, from.Y + offsetY))
                {
                    var layerCell = Cells[from.Y + offsetY, j + offsetX];
                    var blink = new LinkContent(
                        layerCell.Links[Side.Top].SiliconLink,
                        layerCell.Links[Side.Top].HasMetalLink);
                    tempLinks[0, jrel + 1] = (default(LinkContent), blink.Invert());
                }

            // checking moving possibility (and moving cells)
            for (int i = from.Y, irel = 0; i < to.Y; i++, irel++)
            for (int j = from.X, jrel = 0; j < to.X; j++, jrel++)
            {
                var levelCell = Cells[i, j];
                ref var tmpCell = ref tempCells[irel, jrel];
                
                var isSourceOccupied = levelCell.HasSilicon() || levelCell.HasMetal;
                var isTargetOccupied = tmpCell.Silicon != SiliconLayerContent.None || tmpCell.HasMetal;

                if (isSourceOccupied)
                {
                    if (isTargetOccupied) return false;
                    
                    tmpCell.HasMetal = levelCell.HasMetal;
                    tmpCell.Silicon = levelCell.Silicon;
                }
            }
            
            // copying source links
            for (int i = from.Y, irel = 0; i < to.Y; i++, irel++)
            for (int j = from.X, jrel = 0; j < to.X; j++, jrel++)
            {
                var levelCell = Cells[i, j];
                var tmpCell = tempCells[irel, jrel];
                ref var tmpLinks = ref tempLinks[irel + 1, jrel + 1];
                
                if (tmpCell.Silicon != SiliconLayerContent.None)
                {
                    var rightSiliconLink =
                        tmpLinks.right.SiliconLink == SiliconLink.None
                            ? jrel + 1 < width &&
                              tempCells[irel, jrel + 1].Silicon != SiliconLayerContent.None
                                ? levelCell.Links[Side.Right].SiliconLink
                                : SiliconLink.None
                            : tmpLinks.right.SiliconLink;
                    var bottomSiliconLink =
                        tmpLinks.bottom.SiliconLink == SiliconLink.None
                            ? irel + 1 < height &&
                              tempCells[irel + 1, jrel].Silicon != SiliconLayerContent.None
                                ? levelCell.Links[Side.Bottom].SiliconLink
                                : SiliconLink.None
                            : tmpLinks.bottom.SiliconLink;
                    tmpLinks = (new LinkContent(rightSiliconLink, tmpLinks.right.HasMetalLink),
                        new LinkContent(bottomSiliconLink, tmpLinks.bottom.HasMetalLink));
                }
                
                if (tmpCell.HasMetal)
                {
                    var rightMetalLink =
                        tmpLinks.right.HasMetalLink ||
                        (jrel + 1 < width &&
                         tempCells[irel, jrel + 1].HasMetal &&
                         levelCell.Links[Side.Right].HasMetalLink);
                    var bottomMetalLink =
                        tmpLinks.bottom.HasMetalLink ||
                        (irel + 1 < height &&
                         tempCells[irel + 1, jrel].HasMetal &&
                         levelCell.Links[Side.Bottom].HasMetalLink);
                    tmpLinks = (new LinkContent(tmpLinks.right.SiliconLink, rightMetalLink),
                        new LinkContent(tmpLinks.bottom.SiliconLink, bottomMetalLink));
                }
            }
            
            // removing cells from source
            for (var i = from.Y; i < to.Y; i++)
            for (var j = from.X; j < to.X; j++)
            {
                RemoveCellSilicon(new Position(j, i));
                RemoveCellMetal(new Position(j, i));
            }
            
            // removing existing links from target zone
            // todo: maybe remove
            for (var i = from.Y; i < to.Y; i++)
            for (var j = from.X; j < to.X; j++)
            {
                RemoveCellLinks(new Position(j + offsetX, i + offsetY), LinkType.MetalLink);
                RemoveCellLinks(new Position(j + offsetX, i + offsetY), LinkType.SiliconLink);
            }

            // copying cells back
            for (int i = from.Y, irel = 0; i < to.Y; i++, irel++)
            for (int j = from.X, jrel = 0; j < to.X; j++, jrel++)
            {
                var levelCell = Cells[i + offsetY, j + offsetX];
                var tmpCell = tempCells[irel, jrel];

                _cellMatrix.UpdateCellContent(new Position(j + offsetX, i + offsetY), new CellContent(levelCell)
                {
                    HasMetal = tmpCell.HasMetal,
                    Silicon = tmpCell.Silicon
                });

                var (rlink, blink) = tempLinks[irel + 1, jrel + 1];
                _cellMatrix.UpdateLinkContent(new Position(j + offsetX, i + offsetY), Side.Right, rlink);
                _cellMatrix.UpdateLinkContent(new Position(j + offsetX, i + offsetY), Side.Bottom, blink);

                // restoring left-top links
                if (irel == 0 && i + offsetY - 1 >= 0)
                {
                    (_, blink) = tempLinks[0, jrel + 1];
                    _cellMatrix.UpdateLinkContent(new Position(j + offsetX, i + offsetY - 1), Side.Bottom, blink);
                }
                else if (jrel == 0 && j + offsetX - 1 >= 0)
                {
                    (rlink, _) = tempLinks[irel + 1, 0];
                    _cellMatrix.UpdateLinkContent(new Position(j + offsetX - 1, i + offsetY), Side.Right, rlink);
                }
            }
            
            DestroyBrokenGates();
            return true;
        }

        public bool RemoveContent(Position from, Position to)
        {
	        for (var i = from.Y; i < to.Y; i++)
	        for (var j = from.X; j < to.X; j++)
	        {
		        RemoveCellAny(new Position(j, i));
	        }

	        DestroyBrokenGates();
			return true;
		}


		private void DestroyBrokenGates()
        {
            // assuming links are valid 
            
            for (var i = 0; i < Height; i++)
            for (var j = 0; j < Width; j++)
            {
                var cell = Cells[i, j];

                if (cell.HasGate())
                {
                    var gateType = cell.HasPGate() ? SiliconType.PType : SiliconType.NType;
                    var isVertical = cell.IsVerticalGate();

                    if (!CheckGate(cell, gateType, isVertical))
                        RemoveGate(cell);
                }
            }
        }

        private bool CheckGate(ILayerCell cell, SiliconType gateType, bool isVertical)
        {
            // todo check gate type

            var ix1 = isVertical ? 1 : 0;
            var ix2 = ix1 + 2;

            var masterIx1 = ix1 + 1;
            var masterIx2 = (ix1 + 3) % 4;

            return cell.Links[ix1].SiliconLink == SiliconLink.BiDirectional &&
                   cell.Links[ix2].SiliconLink == SiliconLink.BiDirectional &&
                   (cell.Links[masterIx1].SiliconLink == SiliconLink.Slave ||
                    cell.Links[masterIx2].SiliconLink == SiliconLink.Slave);
        }

        private void RemoveGate(ILayerCell cell)
        {
            var pos = new Position(cell.Column, cell.Row);
            _cellMatrix.UpdateCellContent(pos, new CellContent(cell) {Silicon = cell.Silicon.RemoveGate()});
            for (var i = 0; i < 4; i++)
                if (cell.Links[i].SiliconLink == SiliconLink.Slave)
                    _cellMatrix.UpdateLinkContent(pos, (Side) i, new LinkContent(SiliconLink.None, cell.Links[i].HasMetalLink));
        }
    }
}