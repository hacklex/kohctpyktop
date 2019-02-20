using System;
using System.Collections;
using System.Collections.Generic;

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
        public int Size { get; set; } = 3;
        public int Row { get; set; }
        public int Col { get; set; }
        public string Name { get; set; }
        public Func<IEnumerable<bool>> ValuesFunction { get; set; } = ValueFunctionHelper.PowerSupply;
        public bool IsOutputPin { get; set; }
    }
}