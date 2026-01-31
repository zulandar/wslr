using System.Globalization;
using System.Windows.Data;

namespace Wslr.App.Converters;

/// <summary>
/// Converts a boolean value to a specified text string.
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the text to display when the value is true.
    /// </summary>
    public string TrueText { get; set; } = "Yes";

    /// <summary>
    /// Gets or sets the text to display when the value is false.
    /// </summary>
    public string FalseText { get; set; } = "No";

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueText : FalseText;
        }

        return FalseText;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return text.Equals(TrueText, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
