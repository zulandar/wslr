using System.Globalization;
using System.Windows.Data;

namespace Wslr.App.Converters;

/// <summary>
/// Multi-value converter that returns true only if all input boolean values are false.
/// Useful for enabling buttons only when multiple conditions are not active.
/// </summary>
public class AllFalseConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is bool boolValue && boolValue)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
