using System;
using System.Collections;
using System.Collections.Generic;
using Kohctpyktop.Models.Field.ValuesFunctions;

namespace Kohctpyktop.Models.Field
{
    /// <summary>
    /// Describes a pin, that is, input or output
    /// of 2x2 or bigger size (default 3x3) 
    /// spanning around the origin point 
    /// <!--ToDo: decide whether it is needed at all -->
    /// </summary>
    public class Pin
    {
        public Pin(int width, int height, int row, int col, string name, ValuesFunction valuesFunction, bool isOutputPin, bool isSignificant)
        {
            Width = width;
            Height = height;
            Row = row;
            Col = col;
            Name = name;
            ValuesFunction = valuesFunction;
            IsOutputPin = isOutputPin;
            IsSignificant = isSignificant;
        }

        public int Width { get; }
        public int Height { get; }
        public int Row { get; }
        public int Col { get;}
        public string Name { get; }
        public ValuesFunction ValuesFunction { get; }
        public bool IsOutputPin { get; }
        public bool IsSignificant { get; }
    }
}