using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Kohctpyktop.Input;

namespace Kohctpyktop.Converters
{
    public class SelectionConverter : IMultiValueConverter
    {
        public object Convert(IList<object> arr, Type targetType, object parameter, CultureInfo culture)
        {
            if (arr.Count == 2 && arr[0] is SelectionState state && arr[1] is Selection sel)
            {
                switch (state)
                {
                    case SelectionState.Dragging: return $"D {sel.StartCell} - {sel.EndCell}";
                    case SelectionState.HasSelection: return $"H {sel.StartCell} - {sel.EndCell}";
                    case SelectionState.Selecting: return $"S {sel.StartCell} - {sel.EndCell}";
                }
            }

            return "-";
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}