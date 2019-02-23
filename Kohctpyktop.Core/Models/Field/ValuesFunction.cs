using System;
using System.Collections.Generic;
using System.Linq;

namespace Kohctpyktop.Models.Field
{
    public enum ValuesFunctionType { Static, Periodic, Aggregate }
    
    public abstract class ValuesFunction
    {
        public abstract ValuesFunctionType Type { get; }
        public abstract IEnumerable<bool> Generate();

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

    public class StaticValuesFunction : ValuesFunction
    {
        public override ValuesFunctionType Type => ValuesFunctionType.Static;
        
        public bool Value { get; }

        public StaticValuesFunction(bool value)
        {
            Value = value;
        }
        
        public override IEnumerable<bool> Generate()
        {
            while (true) yield return Value;
        }

        public static StaticValuesFunction AlwaysOn { get; } = new StaticValuesFunction(true);
        public static StaticValuesFunction AlwaysOff { get; } = new StaticValuesFunction(false);
    }

    public class PeriodicValuesFunction : ValuesFunction
    {
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
        
        public override IEnumerable<bool> Generate()
        {
            for (var i = 0; i < Skip; i++) yield return false;

            while (true)
            {
                for (var i = 0; i < On; i++) yield return true;
                for (var i = 0; i < Off; i++) yield return false;
            }
        }
    }

    public enum AggregateOperation
    {
        And, Or, Xor
    }

    public class AggregateValuesFunction : ValuesFunction
    {
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

        public override IEnumerable<bool> Generate() =>
            Functions.Aggregate(GenerateZero(Operation),
                (a, b) => a.Zip(b.Generate(), (av, bv) => Apply(av, bv, Operation)));

        private IEnumerable<bool> GenerateZero(AggregateOperation operation)
        {
            switch (operation)
            {
                case AggregateOperation.And:
                    while (true) yield return true;
                case AggregateOperation.Or:
                case AggregateOperation.Xor:
                    while (true) yield return false;
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