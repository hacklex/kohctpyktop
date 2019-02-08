namespace Kohctpyktop.Models.Field
{
    /// <summary>
    /// Describes the link state between two cells on all layers
    /// Incapsulates an object shared with the backwards link,
    /// so changing this will change the neighbor link appropriately.
    /// </summary>
    public class NeighborInfo
    {
        class LinkData
        {
            public Cell FromCell { get; set; }
            public Cell ToCell { get; set; }
            public bool HasMetalLink { get; set; }
            public SiliconLink SiliconLink { get; set; }
        }

        static SiliconLink InvertIfNeeded(SiliconLink x, bool invert)
        {
            if (!invert) return x;
            if (x == SiliconLink.Slave) return SiliconLink.Master;
            if (x == SiliconLink.Master) return SiliconLink.Slave;
            return x;
        }

        private LinkData _link;
        private bool _invertDirection;
        public Cell FromCell => _invertDirection ? _link.ToCell : _link.FromCell;
        public Cell ToCell => _invertDirection ? _link.FromCell : _link.ToCell;
        public bool HasMetalLink
        {
            get => _link.HasMetalLink;
            set => _link.HasMetalLink = value;
        }
        public SiliconLink SiliconLink
        {
            get => InvertIfNeeded(_link.SiliconLink, _invertDirection);
            set => _link.SiliconLink = InvertIfNeeded(value, _invertDirection);
        }

        static int ReverseLinkIndex(int x) => (x + 2) % 4;
        NeighborInfo()
        {
            //нехуй
        }
        
        public static void ConnectCells(Cell source, int sourceLinkIndex, Cell target, bool hasMetalLink, SiliconLink siliconLink)
        {
            if (source.NeighborInfos[sourceLinkIndex] != null) return;
            var sourceData = new LinkData
            {
                FromCell = source,
                ToCell = target,
                SiliconLink = siliconLink,
                HasMetalLink = hasMetalLink
            };
            source.NeighborInfos[sourceLinkIndex] = new NeighborInfo
            {
                _link = sourceData,
                _invertDirection = false
            };
            target.NeighborInfos[ReverseLinkIndex(sourceLinkIndex)] = new NeighborInfo
            {
                _link = sourceData,
                _invertDirection = true
            };
        }

        public void CopyFrom(NeighborInfo source)
        {
            if (source != null)
            {
                SiliconLink = source.SiliconLink;
                HasMetalLink = source.HasMetalLink;
            }
            else Clear();
        }

        public void Clear()
        {
            switch (SiliconLink)
            {
                case SiliconLink.Master:
                    RemoveGateIfNeeded(FromCell, ToCell);
                    break;
                case SiliconLink.Slave:
                    RemoveGateIfNeeded(ToCell, FromCell);
                    break;
            }
            
            SiliconLink = SiliconLink.None;
            HasMetalLink = false;
        }

        private static void RemoveGateIfNeeded(Cell slaveCell, Cell masterCell)
        {
            if (!HasOppositeGateSlave(slaveCell, masterCell))
                masterCell.SiliconLayerContent = masterCell.SiliconLayerContent.RemoveGate();
        }

        private static bool HasOppositeGateSlave(Cell slaveCell, Cell masterCell)
        {
            var slaveIx = masterCell.GetNeighborIndex(slaveCell);
            var oppositeSlaveIx = (slaveIx + 2) % 4;
            
            return masterCell.NeighborInfos[oppositeSlaveIx]?.SiliconLink == SiliconLink.Slave;
        }
    }
}