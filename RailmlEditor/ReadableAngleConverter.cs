using System;
using System.Globalization;
using System.Windows.Data;

namespace RailmlEditor
{
    public class ReadableAngleConverter : IMultiValueConverter
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
                double angleDeg = angleRad * 180 / Math.PI;

                // Normalize to [-90, 90] range to keep text readable (not upside down)
                while (angleDeg <= -90) angleDeg += 180;
                while (angleDeg > 90) angleDeg -= 180;

                return angleDeg;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
