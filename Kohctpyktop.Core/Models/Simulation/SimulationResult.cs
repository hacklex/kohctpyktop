using System;
using System.Collections.Generic;

namespace Kohctpyktop.Models.Simulation
{
    // todo: make SimulationResult can store/produce infinite steps 
    // todo: invert model (steps-first instead of pins-first)

    public class SimulatedPin
    {
        public SimulatedPin(string name, IReadOnlyList<bool> actualValues, IReadOnlyList<bool> correctValues)
        {
            Name = name;
            ActualValues = actualValues;
            CorrectValues = correctValues;
        }

        public string Name { get; }
        public IReadOnlyList<bool> ActualValues { get; }
        public IReadOnlyList<bool> CorrectValues { get; }
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