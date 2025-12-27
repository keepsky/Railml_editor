using System;
using System.Globalization;
using System.Windows.Data;

namespace RailmlEditor
{
    public class MidPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is double v1 && values[1] is double v2)
            {
                double mid = (v1 + v2) / 2.0;

                if (values.Length == 3 && values[2] is double origin)
                {
                    // Result relative to the origin
                    return mid - origin;
                }
                
                // Fallback for 2-value case (assume relative to v2 or v1 depending on original buggy usage)
                // Original was (v1 - v2) / 2.0. 
                // Let's keep it exactly same for 2-value case to avoid breaking existing templates I haven't touched yet.
                return (v1 - v2) / 2.0;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
