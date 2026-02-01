using System.Globalization;
using Wslr.App.Converters;

namespace Wslr.App.Tests.Converters;

public class AllFalseConverterTests
{
    private readonly AllFalseConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_AllFalse_ReturnsTrue()
    {
        var values = new object[] { false, false, false };

        var result = _converter.Convert(values, typeof(bool), null!, _culture);

        result.Should().Be(true);
    }

    [Fact]
    public void Convert_OneTrue_ReturnsFalse()
    {
        var values = new object[] { false, true, false };

        var result = _converter.Convert(values, typeof(bool), null!, _culture);

        result.Should().Be(false);
    }

    [Fact]
    public void Convert_AllTrue_ReturnsFalse()
    {
        var values = new object[] { true, true, true };

        var result = _converter.Convert(values, typeof(bool), null!, _culture);

        result.Should().Be(false);
    }

    [Fact]
    public void Convert_EmptyArray_ReturnsTrue()
    {
        var values = Array.Empty<object>();

        var result = _converter.Convert(values, typeof(bool), null!, _culture);

        result.Should().Be(true);
    }

    [Fact]
    public void Convert_SingleFalse_ReturnsTrue()
    {
        var values = new object[] { false };

        var result = _converter.Convert(values, typeof(bool), null!, _culture);

        result.Should().Be(true);
    }

    [Fact]
    public void Convert_SingleTrue_ReturnsFalse()
    {
        var values = new object[] { true };

        var result = _converter.Convert(values, typeof(bool), null!, _culture);

        result.Should().Be(false);
    }

    [Fact]
    public void Convert_NonBoolValues_TreatedAsFalse()
    {
        var values = new object[] { "string", 123, null! };

        var result = _converter.Convert(values, typeof(bool), null!, _culture);

        result.Should().Be(true);
    }

    [Fact]
    public void Convert_MixedBoolAndNonBool_ChecksBoolsOnly()
    {
        var values = new object[] { false, "string", true };

        var result = _converter.Convert(values, typeof(bool), null!, _culture);

        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        var action = () => _converter.ConvertBack(true, new[] { typeof(bool) }, null!, _culture);

        action.Should().Throw<NotSupportedException>();
    }
}
