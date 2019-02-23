namespace Kohctpyktop.Models
{
    // name 'Zone' is just for avoiding conflict with WPF's Rect and WinForms's Rectangle
    public struct Zone
    {
        public Zone(Position origin, int width, int height)
        {
            Origin = origin;
            Width = width;
            Height = height;
        }

        public Position Origin { get; }
        public int Width { get; }
        public int Height { get; }
    }
}