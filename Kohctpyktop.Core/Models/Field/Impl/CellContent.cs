namespace Kohctpyktop.Models.Field
{
    public struct CellContent
    {
        public CellContent(ILayerCell cell)
        {
            Silicon = cell.Silicon;
            HasMetal = cell.HasMetal;
            Pin = cell.Pin;
        }

        public SiliconLayerContent Silicon { get; set; }
        public bool HasMetal { get; set; }
        public Pin Pin { get; set; }
    }
}