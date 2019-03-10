using System;
using System.Collections.Generic;
using System.Linq;

namespace Kohctpyktop.Models.Field.ValuesFunctions
{
    public class SequencePart
    {
        public SequencePart(bool value, int length)
        {
            Value = value;
            Length = length;
        }

        public bool Value { get; }
        public int Length { get; }
    }
    
    public class RepeatingSequenceValuesFunction : ValuesFunction<RepeatingSequenceValuesFunction.State>
    {
        private readonly int _totalLength;
        private readonly bool[] _rawSequence;
        
        public class State
        {
            public State(int position)
            {
                Position = position;
            }

            public int Position { get; }
        }

        public RepeatingSequenceValuesFunction(IReadOnlyList<SequencePart> sequence)
        {
            Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _totalLength = sequence.Sum(x => x.Length);
            _rawSequence = new bool[_totalLength];
            var ix = 0;
            foreach (var part in sequence)
            {
                for (var i = 0; i < part.Length; i++, ix++)
                    _rawSequence[ix] = part.Value;
            }
            if (sequence.Count == 0) throw new ArgumentException("Sequence must not be empty", nameof(sequence));
        }

        public IReadOnlyList<SequencePart> Sequence { get; }

        public override ValuesFunctionType Type => ValuesFunctionType.RepeatingSequence;

        public override object Begin(IReadOnlyDictionary<string, ValuesFunction> _) => new State(0);

        public override (bool, State) Step(State state)
        {
            var cur = _rawSequence[state.Position];
            return (cur, new State(state.Position + 1 == _totalLength ? 0 : state.Position + 1));
        }
    }
}