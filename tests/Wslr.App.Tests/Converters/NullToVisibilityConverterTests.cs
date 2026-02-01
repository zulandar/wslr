using System.Globalization;
using System.Windows;
using Wslr.App.Converters;

namespace Wslr.App.Tests.Converters;

public class NullToVisibilityConverterTests
{
    private readonly NullToVisibilityConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_NotNull_ReturnsVisible()
    {
        var result = _converter.Convert("some value", typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_Null_ReturnsCollapsed()
    {
        var result = _converter.Convert(null!, typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Theory]
    [InlineData("Invert")]
    [InlineData("Inverse")]
    [InlineData("invert")]
    [InlineData("inverse")]
    public void Convert_NotNullWithInverse_ReturnsCollapsed(string parameter)
    {
        var result = _converter.Convert("some value", typeof(Visibility), parameter, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Theory]
    [InlineData("Invert")]
    [InlineData("Inverse")]
    public void Convert_NullWithInverse_ReturnsVisible(string parameter)
    {
        var result = _converter.Convert(null!, typeof(Visibility), parameter, _culture);

        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        var action = () => _converter.ConvertBack(Visibility.Visible, typeof(object), null!, _culture);

        action.Should().Throw<NotImplementedException>();
    }
}
