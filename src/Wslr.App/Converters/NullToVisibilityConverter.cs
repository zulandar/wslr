using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wslr.App.Converters;

/// <summary>
/// Converts a null value to a Visibility value.
/// Returns Visible when value is not null, Collapsed when null.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isNotNull = value is not null;

        // Check for inverse parameter (accept both "Invert" and "Inverse")
        var inverse = parameter is string param &&
                      (param.Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                       param.Equals("Inverse", StringComparison.OrdinalIgnoreCase));

        if (inverse)
        {
            isNotNull = !isNotNull;
        }

        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
