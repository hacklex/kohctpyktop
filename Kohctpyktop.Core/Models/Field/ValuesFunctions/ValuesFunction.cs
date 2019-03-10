using System;
using System.Collections.Generic;

namespace Kohctpyktop.Models.Field.ValuesFunctions
{
    public enum ValuesFunctionType { Static, Periodic, Aggregate, RepeatingSequence, Reference }
    
    public abstract class ValuesFunction
    {
        public abstract ValuesFunctionType Type { get; }
        public abstract object Begin(IReadOnlyDictionary<string, ValuesFunction> declaredFunctions);
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
}