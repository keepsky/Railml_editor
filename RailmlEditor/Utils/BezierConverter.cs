using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace RailmlEditor.Utils
{
    /// <summary>
    /// X, Y 좌표 여러 개를 받아서 한 번에 부드럽게 휘어지는 '베지어 곡선(Bezier Curve)' 그림 정보로 바꿔주는 변환기(Converter)입니다.
    /// 화면에 곡선 선로를 그릴 때 주로 쓰입니다.
    /// </summary>
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
            return System.Windows.DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var result = new object[targetTypes.Length];
            for (int i = 0; i < targetTypes.Length; i++)
                result[i] = Binding.DoNothing;
            return result;
        }
    }
}

