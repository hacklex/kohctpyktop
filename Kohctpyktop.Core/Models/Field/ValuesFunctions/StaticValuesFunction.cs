namespace Kohctpyktop.Models.Field.ValuesFunctions
{
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
}