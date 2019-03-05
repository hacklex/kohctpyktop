using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Topology;

namespace Kohctpyktop.Models.Simulation
{
    public static class Simulator
    {
        public static SchemeState InitState(Topology.Topology topology)
        {
            var state = new SchemeState(topology);
            return Step(state, true); // ensuring gates in the right states
        }
        
        public static SimulationResult Simulate(Topology.Topology topology, int maxSimulationSteps)
        {
            var state = InitState(topology);
            var stateList = new List<SchemeState>();
            
            for (var i = 0; i < maxSimulationSteps; i++)
            {
                state = Step(state, false);
                stateList.Add(state);
            }
            
            return new SimulationResult(stateList);
        }

        public static SchemeState StepBack(SchemeState state)
        {
            // todo:
            // - declare StepBack in value generators
            // - ???
            throw new NotImplementedException();
        }

        public static SchemeState Step(SchemeState state) => Step(state, false);
        
        private static SchemeState Step(SchemeState state, bool dry)
        {
            var gateStates = state.Gates.ToDictionary(x => x.Key, x => x.Value); // duplicating gate states
            var nodeStates = state.Nodes.ToDictionary(x => x.Key, x => new NodeState(x.Key, false));
            var inputPinStates = new Dictionary<Pin, PinState>();
            var outputPinStates = new Dictionary<Pin, PinState>();

            bool Nor(IEnumerable<SchemeNode> nodes) => nodes.Aggregate(true, (b, node) => b && !nodeStates[node].IsHigh);
            bool Or(IEnumerable<SchemeNode> nodes) => nodes.Aggregate(false, (b, node) => b || nodeStates[node].IsHigh);
            
            void UpdateGateState(KeyValuePair<SchemeGate, GateState> gateInfo)
            {
                var gate = gateInfo.Key;
                gateStates[gate] = new GateState(gate, gate.IsInversionGate 
                    ? gate.GateInputs.Aggregate(true, (old, pair) => old && Nor(pair)) 
                    : gate.GateInputs.Aggregate(true, (old, pair) => old && Or(pair)));
            }

            void UpdatePinValues()
            { 
                foreach (var (pin, pinState) in state.InputPinStates.Concat(state.OutputPinStates))
                {
                    var (isHigh, newState) = dry ? (false, pinState.GeneratorState) : pin.ValuesFunction.Step(pinState.GeneratorState);
                    
                    if (pin.IsOutputPin)
                    {
                        outputPinStates[pin] = new PinState(pin, isHigh, newState);
                    }
                    else
                    {
                        inputPinStates[pin] = new PinState(pin, isHigh, newState);
                    }
                }
            }

            void LoadPinNodes()
            {
                foreach (var (n, _) in state.Nodes)
                {
                    foreach (var p in n.Pins)
                    {
                        if (inputPinStates.TryGetValue(p, out var pinState) && pinState.IsHigh) 
                            nodeStates[n] = new NodeState(n, true);
                    }
                }
            }
            void PropagateHigh()
            {
                for (bool hadChanges = true; hadChanges; hadChanges = false)
                {
                    foreach (var (gate, gateState) in gateStates)
                    {
                        if (gateState.IsOpen)
                        {
                            var left = nodeStates[gate.GatePowerNodes[0]];
                            var right = nodeStates[gate.GatePowerNodes[1]];
                            var isHigh = left.IsHigh || right.IsHigh;
                            hadChanges |= left.IsHigh != right.IsHigh;
                            nodeStates[gate.GatePowerNodes[0]] = new NodeState(gate.GatePowerNodes[0], isHigh);
                            nodeStates[gate.GatePowerNodes[1]] = new NodeState(gate.GatePowerNodes[1], isHigh);
                        }
                    }
                }
            }
            void CalculateGates()
            {
                state.Gates.ForEach(UpdateGateState);
            }
            
            UpdatePinValues();
            LoadPinNodes();
            PropagateHigh();
            CalculateGates();
            
            return new SchemeState(state.InputPins, state.OutputPins, state.PinNodes,
                gateStates, nodeStates, inputPinStates, outputPinStates,
                state.OutputPins.ToDictionary(pin => pin, pin => state.PinNodes[pin].Any(node => nodeStates[node].IsHigh)));
        }

        private static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key,
            out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }

        private static void ForEach<TValue>(this IEnumerable<TValue> list, Action<TValue> actor)
        {
            foreach (var item in list) actor(item);
        }
    }
}
