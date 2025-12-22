using System;
using System.Globalization;
using System.Windows.Data;

namespace RailmlEditor
{
    public class DiffConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double v1 && values[1] is double v2)
            {
                return v1 - v2;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
