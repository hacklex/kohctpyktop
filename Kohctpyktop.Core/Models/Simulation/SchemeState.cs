using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Topology;

namespace Kohctpyktop.Models.Simulation
{
    public struct PinState
    {
        public PinState(Pin pin, bool isHigh, object generatorState)
        {
            Pin = pin;
            IsHigh = isHigh;
            GeneratorState = generatorState;
        }

        public Pin Pin { get; }
        public bool IsHigh { get; }
        public object GeneratorState { get; }
    }
    
    public class SchemeState
    {
        public SchemeState(Topology.Topology topology)
        {
            Gates = topology.Gates.ToDictionary(x => x, x => new GateState(x, false));
            Nodes = topology.Nodes.ToDictionary(x => x, x => new NodeState(x, false));
            
            InputPins = topology.Pins.Where(x => !x.IsOutputPin).ToArray();
            OutputPins = topology.Pins.Where(x => x.IsOutputPin).ToArray();
            
            PinNodes = topology.Pins.ToDictionary(
                p => p, 
                p => (IReadOnlyList<SchemeNode>) topology.Nodes.Where(n => n.Pins.Contains(p)).ToArray());

            InputPinStates = InputPins.ToDictionary(x => x, x => new PinState(x, false, x.ValuesFunction.Begin(topology.ValuesFunctions)));
            OutputPinStates = OutputPins.ToDictionary(x => x, x => new PinState(x, false, x.ValuesFunction.Begin(topology.ValuesFunctions)));
            GeneratedOutputPinsStates = OutputPins.ToDictionary(x => x, _ => false);
        }

        public SchemeState(IReadOnlyList<Pin> inputPins, IReadOnlyList<Pin> outputPins,
            IReadOnlyDictionary<Pin, IReadOnlyList<SchemeNode>> pinNodes,
            IReadOnlyDictionary<SchemeGate, GateState> gates,
            IReadOnlyDictionary<SchemeNode, NodeState> nodes,
            IReadOnlyDictionary<Pin, PinState> inputPinStates, 
            IReadOnlyDictionary<Pin, PinState> outputPinStates, 
            IReadOnlyDictionary<Pin, bool> generatedOutputPinsStates)
        {
            InputPins = inputPins;
            OutputPins = outputPins;
            PinNodes = pinNodes;
            Gates = gates;
            Nodes = nodes;
            InputPinStates = inputPinStates;
            OutputPinStates = outputPinStates;
            GeneratedOutputPinsStates = generatedOutputPinsStates;
        }

        public IReadOnlyDictionary<SchemeGate, GateState> Gates { get; }
        public IReadOnlyDictionary<SchemeNode, NodeState> Nodes { get; }
        public IReadOnlyList<Pin> InputPins { get; }
        public IReadOnlyList<Pin> OutputPins { get; }
        
        public IReadOnlyDictionary<Pin, PinState> InputPinStates { get; }
        public IReadOnlyDictionary<Pin, PinState> OutputPinStates { get; }
        public IReadOnlyDictionary<Pin, IReadOnlyList<SchemeNode>> PinNodes { get; }
        public IReadOnlyDictionary<Pin, bool> GeneratedOutputPinsStates { get; }
    }
}