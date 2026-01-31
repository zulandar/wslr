using System.Globalization;
using System.Windows.Data;

namespace Wslr.App.Converters;

/// <summary>
/// Converts an integer index to a boolean based on whether it matches the parameter.
/// Used for RadioButton IsChecked bindings with a shared SelectedIndex property.
/// </summary>
public class IndexToBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentIndex && parameter is string paramStr && int.TryParse(paramStr, out var targetIndex))
        {
            return currentIndex == targetIndex;
        }

        return false;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string paramStr && int.TryParse(paramStr, out var targetIndex))
        {
            return targetIndex;
        }

        return Binding.DoNothing;
    }
}
