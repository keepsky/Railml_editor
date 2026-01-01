using System;
using System.Globalization;
using System.Windows.Data;

namespace RailmlEditor.Utils
{
    public class TagPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Expected values: [MX, X] or [MY, Y]
            if (values.Length >= 2)
            {
                var mVal = values[0] as double?;
                var baseVal = values[1] is double d ? d : 0.0;
                string axis = parameter as string;

                if (axis == "X")
                {
                    if (mVal.HasValue)
                        return mVal.Value - baseVal;
                    return -15.0; // Default X offset
                }
                else if (axis == "Y")
                {
                    if (mVal.HasValue) // Note using same first arg for convenience, binding should match
                        return mVal.Value - baseVal;
                    return 7.0; // Default Y offset
                }
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

