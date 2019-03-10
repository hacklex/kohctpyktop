using System.Collections.Generic;

namespace Kohctpyktop.Models.Field.ValuesFunctions
{
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

        public override object Begin(IReadOnlyDictionary<string, ValuesFunction> _) => new State(0, 0);
    }
}