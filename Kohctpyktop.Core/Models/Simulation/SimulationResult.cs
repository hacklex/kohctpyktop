using System;
using System.Collections.Generic;
using System.Linq;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Simulation
{
    // todo: make SimulationResult can store/produce infinite steps 
    // todo: invert model (steps-first instead of pins-first)

    public class SimulatedPin
    {
        public SimulatedPin(string name, bool isOutputPin, IReadOnlyList<bool> actualValues, IReadOnlyList<bool> correctValues)
        {
            Name = name;
            IsOutputPin = isOutputPin;
            ActualValues = actualValues;
            CorrectValues = correctValues;
        }

        public string Name { get; }
        public bool IsOutputPin { get; }
        public IReadOnlyList<bool> ActualValues { get; }
        public IReadOnlyList<bool> CorrectValues { get; }
    }
    
    public class SimulationResult
    {
        public SimulationResult(IReadOnlyList<SchemeState> states)
        {
            Pins = states[0]
                .InputPins
                .Concat(states[0].OutputPins)
                .Where(x => x.IsSignificant)
                .Select(x => ExtractPinValuesFromStateList(x, states))
                .ToArray();
            
            foreach (var pin in Pins.Where(x => x.IsOutputPin))
            {
                Score += pin.ActualValues.Zip(pin.CorrectValues, (a, c) => a == c ? 1.0 : 0).Sum();
            }

            Score /= states.Count;
            Score /= Pins.Count(x => x.IsOutputPin);
        }

        private SimulatedPin ExtractPinValuesFromStateList(Pin pin, IReadOnlyList<SchemeState> states)
        {
            var correct = states.Select(x => (pin.IsOutputPin ? x.OutputPinStates : x.InputPinStates)[pin].IsHigh).ToArray();
            return new SimulatedPin(pin.Name, pin.IsOutputPin,
                pin.IsOutputPin ? states.Select(x => x.GeneratedOutputPinsStates[pin]).ToArray() : correct,
                correct);
        }

        public IReadOnlyList<SimulatedPin> Pins { get; }
        public double Score { get; }
    }
}