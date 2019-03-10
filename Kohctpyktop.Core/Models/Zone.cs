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

        public bool Contains(int x, int y) =>
            Origin.X <= x && Origin.Y <= y && Origin.X + Width > x && Origin.Y + Height > y;
    }
}