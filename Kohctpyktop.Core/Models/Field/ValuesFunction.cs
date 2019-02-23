using System.Collections.Generic;

namespace Kohctpyktop.Models.Field
{
    public enum ValuesFunctionType { Static, Periodic }
    
    public abstract class ValuesFunction
    {
        public abstract ValuesFunctionType Type { get; }
        public abstract IEnumerable<bool> Generate();
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
}