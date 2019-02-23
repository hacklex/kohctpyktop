using System;
using System.Collections.Generic;
using System.Linq;

namespace Kohctpyktop.Models.Field
{
    public enum ValuesFunctionType { Static, Periodic, Aggregate }
    
    public abstract class ValuesFunction
    {
        public abstract ValuesFunctionType Type { get; }
        public abstract object Begin();
        public abstract (bool, object) Step(object state);

        public static implicit operator ValuesFunction(bool v) =>
            v ? StaticValuesFunction.AlwaysOn : StaticValuesFunction.AlwaysOff;

        public static ValuesFunction operator !(ValuesFunction function)
            => new AggregateValuesFunction(AggregateOperation.Xor, function, true);
        
        public static ValuesFunction operator &(ValuesFunction a, ValuesFunction b)
            => new AggregateValuesFunction(AggregateOperation.And, a, b);
        
        public static ValuesFunction operator |(ValuesFunction a, ValuesFunction b)
            => new AggregateValuesFunction(AggregateOperation.Or, a, b);
        
        public static ValuesFunction operator ^(ValuesFunction a, ValuesFunction b)
            => new AggregateValuesFunction(AggregateOperation.Xor, a, b);
    }

    public abstract class ValuesFunction<TState> : ValuesFunction
    {
        public sealed override (bool, object) Step(object state)
        {
            if (state is TState s) return Step(s);
            throw new ArgumentException("Invalid state type", nameof(state));
        }

        public abstract (bool, TState) Step(TState state);
    }

    public class StaticValuesFunction : ValuesFunction
    {
        public override ValuesFunctionType Type => ValuesFunctionType.Static;
        
        public bool Value { get; }

        public StaticValuesFunction(bool value)
        {
            Value = value;
        }

        public override (bool, object) Step(object state) => (Value, null);
        public override object Begin() => null;

        public static StaticValuesFunction AlwaysOn { get; } = new StaticValuesFunction(true);
        public static StaticValuesFunction AlwaysOff { get; } = new StaticValuesFunction(false);
    }

    public class PeriodicValuesFunction : ValuesFunction<PeriodicValuesFunction.State>
    {
        public class State
        {
            public State(int skipped, int position) { Skipped = skipped; Position = position; }

            public int Skipped { get; }
            public int Position { get; }
        }
        
        public override ValuesFunctionType Type => ValuesFunctionType.Periodic;
        
        public int Skip { get; }
        public int On { get; }
        public int Off { get; }

        public PeriodicValuesFunction(int on, int off, int skip = 0)
        {
            Skip = skip;
            On = on;
            Off = off;
        }
        
        public override (bool, State) Step(State state)
        {
            if (state.Skipped < Skip) return (false, new State(state.Skipped + 1, 0));
            if (state.Position < On) return (true, new State(state.Skipped, state.Position + 1));
            return state.Position < On + Off - 1
                ? (false, new State(state.Skipped, state.Position + 1))
                : (false, new State(state.Skipped, 0));
        }

        public override object Begin() => new State(0, 0);
    }

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