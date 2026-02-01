using System.Globalization;
using System.Windows.Data;
using Wslr.App.Converters;

namespace Wslr.App.Tests.Converters;

public class IndexToBoolConverterTests
{
    private readonly IndexToBoolConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(0, "0", true)]
    [InlineData(1, "1", true)]
    [InlineData(5, "5", true)]
    [InlineData(0, "1", false)]
    [InlineData(1, "0", false)]
    [InlineData(5, "3", false)]
    public void Convert_IntWithMatchingParameter_ReturnsExpected(int index, string parameter, bool expected)
    {
        var result = _converter.Convert(index, typeof(bool), parameter, _culture);

        result.Should().Be(expected);
    }

    [Fact]
    public void Convert_NonIntValue_ReturnsFalse()
    {
        var result = _converter.Convert("not an int", typeof(bool), "0", _culture);

        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NonStringParameter_ReturnsFalse()
    {
        var result = _converter.Convert(0, typeof(bool), 0, _culture);

        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NonNumericParameter_ReturnsFalse()
    {
        var result = _converter.Convert(0, typeof(bool), "not a number", _culture);

        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NullParameter_ReturnsFalse()
    {
        var result = _converter.Convert(0, typeof(bool), null!, _culture);

        result.Should().Be(false);
    }

    [Theory]
    [InlineData(true, "0", 0)]
    [InlineData(true, "1", 1)]
    [InlineData(true, "5", 5)]
    public void ConvertBack_TrueWithParameter_ReturnsTargetIndex(bool isChecked, string parameter, int expected)
    {
        var result = _converter.ConvertBack(isChecked, typeof(int), parameter, _culture);

        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertBack_False_ReturnsDoNothing()
    {
        var result = _converter.ConvertBack(false, typeof(int), "0", _culture);

        result.Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_NonBoolValue_ReturnsDoNothing()
    {
        var result = _converter.ConvertBack("not a bool", typeof(int), "0", _culture);

        result.Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_NonStringParameter_ReturnsDoNothing()
    {
        var result = _converter.ConvertBack(true, typeof(int), 0, _culture);

        result.Should().Be(Binding.DoNothing);
    }

    [Fact]
    public void ConvertBack_NonNumericParameter_ReturnsDoNothing()
    {
        var result = _converter.ConvertBack(true, typeof(int), "not a number", _culture);

        result.Should().Be(Binding.DoNothing);
    }
}
