using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wslr.App.Converters;

/// <summary>
/// Converts a count value to a Visibility value.
/// Returns Visible if count > 0, Collapsed otherwise.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var count = value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            _ => 0
        };

        // Check for inverse parameter
        var inverse = parameter is string param &&
                      param.Equals("Inverse", StringComparison.OrdinalIgnoreCase);

        var visible = count > 0;

        if (inverse)
        {
            visible = !visible;
        }

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
