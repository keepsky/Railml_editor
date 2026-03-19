using System;
using System.Globalization;
using System.Windows.Data;

namespace RailmlEditor.Utils
{
    /// <summary>
    /// 곡선 선로 조절점이나 기타 화면 요소에 달린 이름표(Tag)의 위치를 
    /// 지정된 X, Y 좌표값을 기준으로 적당히 움직여서 보여주기 위해 계산하는 변환기입니다.
    /// </summary>
    public class TagPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Expected values: [MX, X] or [MY, Y]
            if (values.Length >= 2)
            {
                var mVal = values[0] as double?;
                var baseVal = values[1] is double d ? d : 0.0;
                string? axis = parameter as string;

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
            var result = new object[targetTypes.Length];
            for (int i = 0; i < targetTypes.Length; i++)
                result[i] = Binding.DoNothing;
            return result;
        }
    }
}

