namespace Kohctpyktop.Models.Field
{
    public interface ICellLink
    {
        bool IsValidLink { get; }
        
        ILayerCell SourceCell { get; }
        ILayerCell TargetCell { get; }
        
        SiliconLink SiliconLink { get; }
        bool HasMetalLink { get; }

        ICellLink Inverted { get; }
    }
}