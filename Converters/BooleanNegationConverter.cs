using System.Globalization;
using System.Windows.Data;

namespace NMS_PotentialDetector.Converters;

/// <summary>
/// Inverts boolean values for WPF bindings.
/// </summary>
public class BooleanNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool boolean ? !boolean : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Convert(value, targetType, parameter, culture);
}