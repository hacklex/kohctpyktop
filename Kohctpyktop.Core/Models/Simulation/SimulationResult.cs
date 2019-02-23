using System;
using System.Collections.Generic;

namespace Kohctpyktop.Models.Simulation
{
    // todo: make SimulationResult can store/produce infinite steps 
    // todo: invert model (steps-first instead of pins-first)

    public class SimulatedPin
    {
        public SimulatedPin(string name, IReadOnlyList<bool> values)
        {
            Name = name;
            Values = values;
        }

        public string Name { get; }
        public IReadOnlyList<bool> Values { get; }
    }
    
    public class SimulationResult
    {
        public SimulationResult(IReadOnlyList<SimulatedPin> pins, double score)
        {
            Pins = pins;
            Score = score;
        }

        public IReadOnlyList<SimulatedPin> Pins { get; }
        public double Score { get; }
    }
}