using System;
using System.Linq;

namespace Kohctpyktop.Models.Field.ValuesFunctions
{
    public enum AggregateOperation
    {
        And, Or, Xor
    }
    
    public class AggregateValuesFunction : ValuesFunction<AggregateValuesFunction.State>
    {
        public class State
        {
            public State(object[] states)
            {
                States = states;
            }

            public object[] States { get; }
        }
        
        public override ValuesFunctionType Type => ValuesFunctionType.Aggregate;
        
        public ValuesFunction[] Functions { get; }
        public AggregateOperation Operation { get; }

        public AggregateValuesFunction(AggregateOperation operation, params ValuesFunction[] functions)
        {
            Functions = functions ?? throw new ArgumentNullException(nameof(functions));
            
            if (functions.Length < 2)
                throw new ArgumentException("Aggregate function works with at least 2 inner functions", 
                    nameof(functions));
            
            Operation = operation;
        }

        public override object Begin() => new State(Functions.Select(x => x.Begin()).ToArray());

        public override (bool, State) Step(State state)
        {
            var newStates = new object[Functions.Length];
            var newValue = Zero(Operation);

            for (var i = 0; i < Functions.Length; i++)
            {
                var (v, s) = Functions[i].Step(state.States[i]);
                newValue = Apply(newValue, v, Operation);
                newStates[i] = s;
            }

            return (newValue, new State(newStates));
        }

        private bool Zero(AggregateOperation operation)
        {
            switch (operation)
            {
                case AggregateOperation.And:
                    while (true) return true;
                case AggregateOperation.Or:
                case AggregateOperation.Xor:
                    while (true) return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }
        
        private bool Apply(bool a, bool b, AggregateOperation operation)
        {
            switch (operation)
            {
                case AggregateOperation.And: return a && b;
                case AggregateOperation.Or: return a || b;
                case AggregateOperation.Xor: return a ^ b;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }
    }
}