using System;
using System.Windows;
using System.Windows.Data;

namespace Kohctpyktop
{
    public class BooleanChoiceConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b) return b ? OnTrue : OnFalse;

            return OnFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion

        public object OnFalse { get; set; }

        public object OnTrue { get; set; }
    }
}