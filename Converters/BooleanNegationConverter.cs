using System.Globalization;
using System.Windows.Data; // Для IValueConverter

namespace NMS_PotentialDetector.Converters
{
    public class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue; // Инверсия
            }
            return value; // Fallback: Вернуть как есть, если не bool
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue; // Двусторонний, для two-way binding (если нужно)
            }
            return value;
        }
    }
}