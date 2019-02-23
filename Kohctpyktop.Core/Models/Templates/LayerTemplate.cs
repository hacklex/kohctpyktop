using System.Collections.Generic;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Templates
{
    public class LayerTemplate
    {
        public LayerTemplate(int width, int height, IReadOnlyList<Pin> pins, IReadOnlyList<Zone> deadZones)
        {
            Width = width;
            Height = height;
            Pins = pins;
            DeadZones = deadZones;
        }

        public int Width { get; }
        public int Height { get; }
        
        public IReadOnlyList<Pin> Pins { get; }
        public IReadOnlyList<Zone> DeadZones { get; }
    }
}