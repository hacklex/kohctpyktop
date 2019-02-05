using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Kohctpyktop
{
    public class NeighborInfoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Cell cell)) return "****|****";
            
            var sBuilder = new StringBuilder();
            var mBuilder = new StringBuilder();
            
            foreach (var neighborInfo in cell.NeighborInfos)
            {
                if (neighborInfo == null)
                {
                    mBuilder.Append("*");
                    sBuilder.Append("*");
                    continue;
                }
                switch (neighborInfo.SiliconLink)
                {
                    case SiliconLink.BiDirectional: sBuilder.Append("B"); break;
                    case SiliconLink.Master: sBuilder.Append("M"); break;
                    case SiliconLink.Slave: sBuilder.Append("S"); break;
                    case SiliconLink.None: sBuilder.Append("x"); break;
                }
                mBuilder.Append(neighborInfo.HasMetalLink ? "1" : "0");
            }

            return sBuilder + "|" + mBuilder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}