using System;
using System.Globalization;
using System.Windows.Data;

using System.Windows.Markup;

namespace RailmlEditor
{
    public class BoolToOpacityConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
                return 1.0; // Selected: Fully Opaque
            return 0.7; // Not Selected: Slightly Transparent (or 1.0 if we just want a border change, but user asked for indicators so this is a placeholder)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
