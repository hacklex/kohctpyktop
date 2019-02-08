using System;
using Avalonia;
using Avalonia.Data.Converters;

namespace Kohctpyktop.Converters
{
    public class EnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return AvaloniaProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return AvaloniaProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return AvaloniaProperty.UnsetValue;

            object parameterValue = Enum.Parse(targetType, parameterString);

            bool? val = value as bool?;
            if (val != true) return AvaloniaProperty.UnsetValue;

            return parameterValue;
        }
        #endregion
    }
}