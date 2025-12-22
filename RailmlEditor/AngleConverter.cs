using System;
using System.Globalization;
using System.Windows.Data;

namespace RailmlEditor
{
    public class AngleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 4 && 
                values[0] is double x && 
                values[1] is double y && 
                values[2] is double x2 && 
                values[3] is double y2)
            {
                double deltaX = x2 - x;
                double deltaY = y2 - y;
                double angleRad = Math.Atan2(deltaY, deltaX);
                return angleRad * 180 / Math.PI;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
