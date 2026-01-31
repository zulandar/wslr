using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wslr.App.Converters;

/// <summary>
/// Converts a boolean value to a Visibility value.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // Check for inverse parameter
            var inverse = parameter is string param &&
                          param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);

            if (inverse)
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }

        return false;
    }
}
