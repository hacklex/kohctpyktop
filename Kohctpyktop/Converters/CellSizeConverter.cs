using System;
using System.Globalization;
using System.Windows.Data;

namespace Kohctpyktop.Converters
{
    public class CellSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const int cellSize = 13;
            
            switch (value)
            {
                case int i: return i * cellSize + 1;
                case double d: return d * cellSize + 1;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class CellOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const int cellSize = 13;
            
            switch (value)
            {
                case int i: return i * cellSize;
                case double d: return d * cellSize;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}