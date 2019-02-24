using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Kohctpyktop.Converters
{
    public class PinValueBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is PinState state)) return null;

            var lb = state.Changed ? 1 : 0;
            
            return state.Value
                ? new Thickness(lb, 1, 0, 0)
                : new Thickness(lb, 0, 0, 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}