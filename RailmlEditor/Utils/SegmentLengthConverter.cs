using System;
using System.Globalization;
using System.Windows.Data;

namespace RailmlEditor.Utils
{
    public class SegmentLengthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 4 && 
                values[0] is double x1 && 
                values[1] is double y1 && 
                values[2] is double x2 && 
                values[3] is double y2)
            {
                double dx = x2 - x1;
                double dy = y2 - y1;
                double length = Math.Sqrt(dx * dx + dy * dy);

                if (parameter is string paramStr && double.TryParse(paramStr, out double padding))
                {
                    return length + padding;
                }
                return length;
            }
            return 10.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

