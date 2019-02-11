namespace Kohctpyktop.Models.Field
{
    public class InvalidLink : ICellLink
    {
        private InvalidLink() {}
        
        public static InvalidLink Instance { get; } = new InvalidLink();
        
        public bool IsValidLink => false;
        public ILayerCell SourceCell => InvalidCell.Instance;
        public ILayerCell TargetCell => InvalidCell.Instance;
        public SiliconLink SiliconLink => SiliconLink.None;
        public bool HasMetalLink => false;
        public ICellLink Inverted => this;
    }
}