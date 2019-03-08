using System;
using System.Collections.Generic;

namespace Kohctpyktop.Models.Field.ValuesFunctions
{
    public class RepeatingSequenceValuesFunction : ValuesFunction<RepeatingSequenceValuesFunction.State>
    {
        public class State
        {
            public State(int position)
            {
                Position = position;
            }

            public int Position { get; }
        }

        public RepeatingSequenceValuesFunction(IReadOnlyList<bool> sequence)
        {
            Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            if (sequence.Count == 0) throw new ArgumentException("Sequence must not be empty", nameof(sequence));
        }

        public IReadOnlyList<bool> Sequence { get; }

        public override ValuesFunctionType Type => ValuesFunctionType.RepeatingSequence;

        public override object Begin() => new State(0);

        public override (bool, State) Step(State state)
        {
            var cur = Sequence[state.Position];
            return (cur, new State(state.Position + 1 == Sequence.Count ? 0 : state.Position + 1));
        }
    }
}