using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Kohctpyktop.ViewModels;

namespace Kohctpyktop.Converters
{
    public class FunctionsDerefConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is string reference &&
                values[1] is IEnumerable<NamedFunctionTemplate> fns)
            {
                return fns.FirstOrDefault(x => x.Name == reference);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is NamedFunctionTemplate fn)
            {
                return new[] {fn.Name, (object) null};
            }

            return null;
        }
    }
}