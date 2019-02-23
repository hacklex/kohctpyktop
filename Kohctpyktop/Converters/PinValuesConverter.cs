using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Kohctpyktop.Converters
{
    public struct PinState
    {
        public bool Value { get; }
        public bool Changed { get; }

        public PinState(bool value, bool changed)
        {
            Value = value;
            Changed = changed;
        }
    }
    
    public class PinValuesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IReadOnlyList<bool> values)) return null;
            var result = new PinState[values.Count];

            var prev = false;
            for (var i = 0; i < result.Length; i++)
            {
                var val = values[i];
                result[i] = new PinState(val, val != prev && i > 0);

                prev = val;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}