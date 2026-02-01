using System.Globalization;
using System.Windows;
using Wslr.App.Converters;

namespace Wslr.App.Tests.Converters;

public class BoolToVisibilityConverterTests
{
    private readonly BoolToVisibilityConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(true, Visibility.Visible)]
    [InlineData(false, Visibility.Collapsed)]
    public void Convert_BoolValue_ReturnsExpectedVisibility(bool input, Visibility expected)
    {
        var result = _converter.Convert(input, typeof(Visibility), null!, _culture);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "Invert", Visibility.Collapsed)]
    [InlineData(false, "Invert", Visibility.Visible)]
    [InlineData(true, "Inverse", Visibility.Collapsed)]
    [InlineData(false, "Inverse", Visibility.Visible)]
    [InlineData(true, "invert", Visibility.Collapsed)]
    [InlineData(false, "inverse", Visibility.Visible)]
    public void Convert_WithInverseParameter_InvertsResult(bool input, string parameter, Visibility expected)
    {
        var result = _converter.Convert(input, typeof(Visibility), parameter, _culture);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("not a bool")]
    [InlineData(123)]
    [InlineData(null)]
    public void Convert_NonBoolValue_ReturnsCollapsed(object? input)
    {
        var result = _converter.Convert(input!, typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Theory]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Collapsed, false)]
    [InlineData(Visibility.Hidden, false)]
    public void ConvertBack_Visibility_ReturnsBool(Visibility input, bool expected)
    {
        var result = _converter.ConvertBack(input, typeof(bool), null!, _culture);

        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertBack_NonVisibility_ReturnsFalse()
    {
        var result = _converter.ConvertBack("not a visibility", typeof(bool), null!, _culture);

        result.Should().Be(false);
    }
}
