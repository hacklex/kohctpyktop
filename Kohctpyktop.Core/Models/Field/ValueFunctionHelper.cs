using System;
using System.Collections.Generic;
using System.Linq;
using BoolSeq = System.Collections.Generic.IEnumerable<bool>;
using BoolSeqFunc = System.Func<System.Collections.Generic.IEnumerable<bool>>;

namespace Kohctpyktop.Models.Field
{
    /// <summary>
    /// Provides useful prefabs for describing input and output value functions
    /// </summary>
    [Obsolete("non-serializable, use ValuesFunction class")]
    public static class ValueFunctionHelper
    {
        private static BoolSeq PowerSupplyFunc()
        {
            for (;;) yield return true;
        }
        /// <summary>
        /// This describes a simple always-powered pin
        /// </summary>
        public static readonly BoolSeqFunc PowerSupply = PowerSupplyFunc;
        /// <summary>
        /// This returns a function that either repeats the input sequence,
        /// or prints it out once, sending endless zeros afterwards
        /// </summary>
        /// <param name="sequence">Input sequence</param>
        /// <param name="loop">true to loop the sequence, false to output it once</param>
        /// <returns></returns>
        public static BoolSeqFunc FromSequence(BoolSeq sequence, bool loop = false)
        {
            IEnumerable<bool> Func()
            {
                var arr = sequence.ToArray();
                do
                    foreach (var b in arr)
                    {
                        yield return b;
                    }
                while (loop);
                for (;;) yield return false;
            }
            return Func;
        }
        /// <summary>
        /// This returns a function that returns given sequence endlessly
        /// </summary>
        /// <param name="sequence">Input sequence</param>
        /// <returns></returns>
        public static BoolSeqFunc Repeat(BoolSeq sequence) => FromSequence(sequence, true);
        /// <summary>
        /// This allows to describe 00001111000011110000 with just (01010, 4)
        /// </summary>
        /// <param name="sequence">Input sequence</param>
        /// <param name="multiplier">Number of times to repeat each input state</param>
        /// <param name="repeat">true to loop the sequence after reaching its end</param>
        /// <returns></returns>
        public static BoolSeqFunc Stretch(BoolSeq sequence, int multiplier, bool repeat) =>
            FromSequence(sequence.SelectMany(b => Enumerable.Repeat(b, multiplier)), repeat);

    }
}