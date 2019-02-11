namespace Kohctpyktop.Models.Field
{
    public struct CellContent
    {
        public CellContent(ILayerCell cell)
        {
            Silicon = cell.Silicon;
            HasMetal = cell.HasMetal;
            IsLocked = cell.IsLocked;
            Name = cell.Name;
        }

        public SiliconTypes Silicon { get; set; }
        public bool HasMetal { get; set; }
        public bool IsLocked { get; set; }
        public string Name { get; set; }
    }
}