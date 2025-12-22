using System;
using System.Globalization;
using System.Windows.Data;

namespace RailmlEditor
{
    public class AddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double v && double.TryParse(parameter?.ToString(), out double add))
            {
                return v + add;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
