using System.Collections.Generic;

namespace Kohctpyktop.Models.Field.ValuesFunctions
{
    public class ReferenceValuesFunction : ValuesFunction<ReferenceValuesFunction.State>
    {
        public class State
        {
            public State(ValuesFunction referencedFunction, object innerState)
            {
                ReferencedFunction = referencedFunction;
                InnerState = innerState;
            }

            public ValuesFunction ReferencedFunction { get; }
            public object InnerState { get; }
        }
        
        public override ValuesFunctionType Type => ValuesFunctionType.Reference;
        
        public ReferenceValuesFunction(string reference)
        {
            Reference = reference;
        }
        
        public string Reference { get; }
        
        public override object Begin(IReadOnlyDictionary<string, ValuesFunction> funcs)
        {
            var fn = funcs[Reference];
            var innerState = fn.Begin(funcs);

            return new State(fn, innerState);
        }

        public override (bool, State) Step(State state)
        {
            var (result, innerState) = state.ReferencedFunction.Step(state.InnerState);
            return (result, new State(state.ReferencedFunction, innerState));
        }
    }
}