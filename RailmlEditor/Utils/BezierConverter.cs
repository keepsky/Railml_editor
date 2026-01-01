using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace RailmlEditor.Utils
{
    public class BezierConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 6 && 
                values[0] is double x && values[1] is double y &&
                values[2] is double mx && values[3] is double my &&
                values[4] is double x2 && values[5] is double y2)
            {
                // Create PathGeometry
                // M StartX,StartY Q ControlX,ControlY EndX,EndY
                return Geometry.Parse($"M {x},{y} Q {mx},{my} {x2},{y2}");
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

