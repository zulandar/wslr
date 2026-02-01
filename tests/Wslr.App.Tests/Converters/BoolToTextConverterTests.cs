using System.Globalization;
using Wslr.App.Converters;

namespace Wslr.App.Tests.Converters;

public class BoolToTextConverterTests
{
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_True_ReturnsDefaultTrueText()
    {
        var converter = new BoolToTextConverter();

        var result = converter.Convert(true, typeof(string), null!, _culture);

        result.Should().Be("Yes");
    }

    [Fact]
    public void Convert_False_ReturnsDefaultFalseText()
    {
        var converter = new BoolToTextConverter();

        var result = converter.Convert(false, typeof(string), null!, _culture);

        result.Should().Be("No");
    }

    [Fact]
    public void Convert_True_ReturnsCustomTrueText()
    {
        var converter = new BoolToTextConverter { TrueText = "Enabled" };

        var result = converter.Convert(true, typeof(string), null!, _culture);

        result.Should().Be("Enabled");
    }

    [Fact]
    public void Convert_False_ReturnsCustomFalseText()
    {
        var converter = new BoolToTextConverter { FalseText = "Disabled" };

        var result = converter.Convert(false, typeof(string), null!, _culture);

        result.Should().Be("Disabled");
    }

    [Theory]
    [InlineData("not a bool")]
    [InlineData(123)]
    [InlineData(null)]
    public void Convert_NonBoolValue_ReturnsFalseText(object? input)
    {
        var converter = new BoolToTextConverter();

        var result = converter.Convert(input!, typeof(string), null!, _culture);

        result.Should().Be("No");
    }

    [Fact]
    public void ConvertBack_TrueText_ReturnsTrue()
    {
        var converter = new BoolToTextConverter();

        var result = converter.ConvertBack("Yes", typeof(bool), null!, _culture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_TrueTextCaseInsensitive_ReturnsTrue()
    {
        var converter = new BoolToTextConverter();

        var result = converter.ConvertBack("yes", typeof(bool), null!, _culture);

        result.Should().Be(true);
    }

    [Fact]
    public void ConvertBack_FalseText_ReturnsFalse()
    {
        var converter = new BoolToTextConverter();

        var result = converter.ConvertBack("No", typeof(bool), null!, _culture);

        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_CustomTrueText_ReturnsTrue()
    {
        var converter = new BoolToTextConverter { TrueText = "Active" };

        var result = converter.ConvertBack("Active", typeof(bool), null!, _culture);

        result.Should().Be(true);
    }

    [Theory]
    [InlineData(123)]
    [InlineData(null)]
    public void ConvertBack_NonStringValue_ReturnsFalse(object? input)
    {
        var converter = new BoolToTextConverter();

        var result = converter.ConvertBack(input!, typeof(bool), null!, _culture);

        result.Should().Be(false);
    }
}
