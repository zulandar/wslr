using System.Globalization;
using System.Windows;
using Wslr.App.Converters;

namespace Wslr.App.Tests.Converters;

public class CountToVisibilityConverterTests
{
    private readonly CountToVisibilityConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(1, Visibility.Visible)]
    [InlineData(5, Visibility.Visible)]
    [InlineData(100, Visibility.Visible)]
    [InlineData(0, Visibility.Collapsed)]
    [InlineData(-1, Visibility.Collapsed)]
    public void Convert_IntValue_ReturnsExpectedVisibility(int input, Visibility expected)
    {
        var result = _converter.Convert(input, typeof(Visibility), null!, _culture);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1L, Visibility.Visible)]
    [InlineData(0L, Visibility.Collapsed)]
    public void Convert_LongValue_ReturnsExpectedVisibility(long input, Visibility expected)
    {
        var result = _converter.Convert(input, typeof(Visibility), null!, _culture);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1.0, Visibility.Visible)]
    [InlineData(0.0, Visibility.Collapsed)]
    [InlineData(0.9, Visibility.Collapsed)] // Truncates to 0
    public void Convert_DoubleValue_ReturnsExpectedVisibility(double input, Visibility expected)
    {
        var result = _converter.Convert(input, typeof(Visibility), null!, _culture);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("not a number")]
    [InlineData(null)]
    public void Convert_NonNumericValue_ReturnsCollapsed(object? input)
    {
        var result = _converter.Convert(input!, typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Theory]
    [InlineData(1, "Inverse", Visibility.Collapsed)]
    [InlineData(0, "Inverse", Visibility.Visible)]
    [InlineData(5, "Inverse", Visibility.Collapsed)]
    public void Convert_WithInverseParameter_InvertsResult(int input, string parameter, Visibility expected)
    {
        var result = _converter.Convert(input, typeof(Visibility), parameter, _culture);

        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        var action = () => _converter.ConvertBack(Visibility.Visible, typeof(int), null!, _culture);

        action.Should().Throw<NotSupportedException>();
    }
}
