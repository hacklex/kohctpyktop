using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kohctpyktop.Models.Field;
using Kohctpyktop.Models.Topology;

namespace Kohctpyktop.Models.Simulation
{
    public static class Simulator
    {
        public static SimulationResult Simulate(Topology.Topology topology, int maxSimulationSteps)
        {
            foreach (var gate in topology.Gates)
            {
                gate.IsOpen = gate.IsInversionGate;
            }
            
            var inputPins = topology.Pins.Where(x => !x.IsOutputPin).ToList();
            var outputPins = topology.Pins.Where(x => x.IsOutputPin).ToList();
            var pinNodes = topology.Pins.ToDictionary(p => p, p => topology.Nodes.Where(n => n.Pins.Contains(p)).ToList());
            var inputs = inputPins.ToDictionary(p => p, p => p.ValuesFunction.Generate().GetEnumerator());
            var outputs = outputPins.ToDictionary(p => p, p => p.ValuesFunction.Generate().GetEnumerator());
            var correctOutputValues = outputPins.ToDictionary(p => p, p => new List<bool>());
            var simulatedOutputValues = outputPins.ToDictionary(p => p, p => new List<bool>());
            var inputPinValues = inputPins.ToDictionary(p => p, p => new List<bool>());

            var currentPinValues = inputPins.ToDictionary(p => p, p => false);

            bool Nor(SchemeNode[] nodes) => nodes.Aggregate(true, (b, node) => b && !node.IsHigh);
            bool Or(SchemeNode[] nodes) => nodes.Aggregate(false, (b, node) => b || node.IsHigh);
            void UpdateGateState(SchemeGate gate)
            {
                gate.IsOpen = gate.IsInversionGate 
                    ? gate.GateInputs.Aggregate(true, (old, pair) => old && Nor(pair)) 
                    : gate.GateInputs.Aggregate(true, (old, pair) => old && Or(pair));
            }

            void UpdatePinValues()
            { 
                foreach (var pin in topology.Pins)
                {
                    if (inputs.TryGetValue(pin, out var input))
                    {
                        var currentPinValue = input.Current;
                        currentPinValues[pin] = currentPinValue;
                        inputPinValues[pin].Add(currentPinValue);
                        input.MoveNext();
                    } 
                    else if (outputs.TryGetValue(pin, out var output))
                    {
                        var currentPinValue = output.Current;
                        correctOutputValues[pin].Add(currentPinValue);
                        output.MoveNext();
                    }
                }
            }

            void ZeroAllNodes() => topology.Nodes.ForEach(n => n.IsHigh = false);
            void LoadPinNodes()
            {
                foreach (var n in topology.Nodes)
                {
                    foreach (var p in n.Pins)
                    {
                        if (currentPinValues.TryGetValue(p, out var input) && input) n.IsHigh = true;
                    }
                }
            }
            void PropagateHigh()
            {
                for (bool hadChanges = true; hadChanges; hadChanges = false)
                {
                    foreach (var gate in topology.Gates)
                    {
                        if (gate.IsOpen)
                        {
                            var left = gate.GatePowerNodes[0];
                            var right = gate.GatePowerNodes[1];
                            var isHigh = left.IsHigh || right.IsHigh;
                            hadChanges |= left.IsHigh != right.IsHigh;
                            gate.GatePowerNodes[0].IsHigh = gate.GatePowerNodes[1].IsHigh = isHigh;
                        }
                    }
                }
            }
            void CalculateGates()
            {
                topology.Gates.ForEach(UpdateGateState);
            }
            void WriteSimulatedValues()
            {
                foreach (var pin in outputPins)
                    simulatedOutputValues[pin].Add(pinNodes[pin].Any(node => node.IsHigh));
            }

            void SimulationStep()
            {
                UpdatePinValues();
                ZeroAllNodes();
                LoadPinNodes();
                PropagateHigh();
                CalculateGates();
                WriteSimulatedValues(); 
            }
            var score = 1.0;

            for (var i = 0; i < maxSimulationSteps + 1; i++)
            {
                SimulationStep();
                
                double scorePart = 0;
                foreach (var pin in outputPins)
                {
                    if (simulatedOutputValues[pin].Last() == correctOutputValues[pin].Last())
                        scorePart += 1.0 / outputPins.Count;
                }
                score = (score * i + scorePart) / (i + 1);
            }

            return new SimulationResult(
                inputPinValues
                    .Concat(simulatedOutputValues)
                    .Where(x => x.Key.IsSignificant)
                    .OrderBy(x => x.Key.IsOutputPin)
                    .ThenBy(x => x.Key.Name)
                    .Select(x =>
                    {
                        x.Value.RemoveAt(0);
                        List<bool> correctValues;
                        if (x.Key.IsOutputPin)
                        {
                            correctValues = correctOutputValues[x.Key];
                            correctValues.RemoveAt(0);
                        }
                        else correctValues = x.Value;
                        
                        return new SimulatedPin(x.Key.Name, x.Value, correctValues);
                    })
                    .ToArray(), score);
        }
    }
}
