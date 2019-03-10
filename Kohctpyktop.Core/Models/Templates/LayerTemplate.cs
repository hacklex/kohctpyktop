using System;
using System.Collections.Generic;
using System.Linq;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Field.ValuesFunctions;
using Kohctpyktop.Models.Simulation;

namespace Kohctpyktop.Models.Templates
{
    public class LayerTemplate
    {
        public LayerTemplate(int width, int height, IReadOnlyList<Pin> pins, IReadOnlyList<Zone> deadZones,
            IReadOnlyDictionary<string, ValuesFunction> functions)
        {
            Width = width;
            Height = height;
            Pins = pins ?? throw new ArgumentNullException(nameof(pins));
            DeadZones = deadZones ?? throw new ArgumentNullException(nameof(deadZones));
            Functions = functions ?? throw new ArgumentNullException(nameof(functions));
            
            // todo: add function recursion checking
        }

        public int Width { get; }
        public int Height { get; }
        
        public IReadOnlyList<Pin> Pins { get; }
        public IReadOnlyList<Zone> DeadZones { get; }
        public IReadOnlyDictionary<string, ValuesFunction> Functions { get; }
    }
}