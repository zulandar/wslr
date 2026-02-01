using System.Globalization;
using Wslr.App.Converters;

namespace Wslr.App.Tests.Converters;

public class InverseBoolConverterTests
{
    private readonly InverseBoolConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_BoolValue_ReturnsInverse(bool input, bool expected)
    {
        var result = _converter.Convert(input, typeof(bool), null!, _culture);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("not a bool")]
    [InlineData(123)]
    [InlineData(null)]
    public void Convert_NonBoolValue_ReturnsTrue(object? input)
    {
        var result = _converter.Convert(input!, typeof(bool), null!, _culture);

        result.Should().Be(true);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConvertBack_BoolValue_ReturnsInverse(bool input, bool expected)
    {
        var result = _converter.ConvertBack(input, typeof(bool), null!, _culture);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("not a bool")]
    [InlineData(123)]
    [InlineData(null)]
    public void ConvertBack_NonBoolValue_ReturnsFalse(object? input)
    {
        var result = _converter.ConvertBack(input!, typeof(bool), null!, _culture);

        result.Should().Be(false);
    }
}
