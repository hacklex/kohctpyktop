using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Kohctpyktop.Converters
{
    public class ActualValuesVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] arr, Type targetType, object parameter, CultureInfo culture)
        {
            if (arr.Length == 2 && arr[0] is bool isSimulatedOnce && arr[1] is bool isOutPin)
                return isOutPin && !isSimulatedOnce ? Visibility.Hidden : Visibility.Visible;

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}