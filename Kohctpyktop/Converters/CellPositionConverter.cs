using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Kohctpyktop.Input;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Converters
{
    public class CellPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Cell cell)
            {
                return $"[{cell.Row}, {cell.Col}]";
            }

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}