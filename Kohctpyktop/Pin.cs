namespace Kohctpyktop
{
    /// <summary>
    /// Describes a pin, that is, input or output
    /// of supposed 3x3 size spanning around the origin point
    /// 
    /// //ToDo: decide whether it is needed at all
    /// </summary>
    public class Pin
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string Name { get; set; }
    }
}