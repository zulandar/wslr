using Wslr.Core.Models;

namespace Wslr.Core.Tests.Models;

public class WslDistroConfigValidationResultTests
{
    #region Static Success Property Tests

    [Fact]
    public void Success_ReturnsValidResult()
    {
        var result = WslDistroConfigValidationResult.Success;

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Success_IsSingleton()
    {
        var result1 = WslDistroConfigValidationResult.Success;
        var result2 = WslDistroConfigValidationResult.Success;

        result1.Should().BeSameAs(result2);
    }

    #endregion

    #region Static Failure Factory Method Tests

    [Fact]
    public void Failure_WithSingleError_ReturnsInvalidResult()
    {
        var error = new WslDistroConfigValidationError
        {
            Section = "automount",
            Key = "root",
            Message = "Path must start with /",
            Code = WslDistroConfigErrorCode.InvalidPath
        };

        var result = WslDistroConfigValidationResult.Failure([error]);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void Failure_WithMultipleErrors_ReturnsAllErrors()
    {
        var errors = new[]
        {
            new WslDistroConfigValidationError
            {
                Section = "automount",
                Key = "root",
                Message = "Invalid path",
                Code = WslDistroConfigErrorCode.InvalidPath
            },
            new WslDistroConfigValidationError
            {
                Section = "network",
                Key = "hostname",
                Message = "Invalid hostname",
                Code = WslDistroConfigErrorCode.InvalidHostname
            }
        };

        var result = WslDistroConfigValidationResult.Failure(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Section == "automount");
        result.Errors.Should().Contain(e => e.Section == "network");
    }

    [Fact]
    public void Failure_WithEmptyErrorList_ReturnsInvalidWithNoErrors()
    {
        var result = WslDistroConfigValidationResult.Failure([]);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void DefaultConstructor_IsValidIsFalse()
    {
        var result = new WslDistroConfigValidationResult();

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DefaultConstructor_ErrorsIsEmptyList()
    {
        var result = new WslDistroConfigValidationResult();

        result.Errors.Should().BeEmpty();
    }

    #endregion
}

public class WslDistroConfigValidationErrorTests
{
    #region Required Property Tests

    [Fact]
    public void Section_IsRequired_CanBeSet()
    {
        var error = new WslDistroConfigValidationError
        {
            Section = "automount",
            Key = "root",
            Message = "Error"
        };

        error.Section.Should().Be("automount");
    }

    [Fact]
    public void Key_IsRequired_CanBeSet()
    {
        var error = new WslDistroConfigValidationError
        {
            Section = "network",
            Key = "hostname",
            Message = "Error"
        };

        error.Key.Should().Be("hostname");
    }

    [Fact]
    public void Message_IsRequired_CanBeSet()
    {
        var error = new WslDistroConfigValidationError
        {
            Section = "user",
            Key = "default",
            Message = "Username must be lowercase"
        };

        error.Message.Should().Be("Username must be lowercase");
    }

    #endregion

    #region Code Property Tests

    [Fact]
    public void Code_DefaultValue_IsUnknown()
    {
        var error = new WslDistroConfigValidationError
        {
            Section = "test",
            Key = "test",
            Message = "Error"
        };

        error.Code.Should().Be(WslDistroConfigErrorCode.Unknown);
    }

    [Theory]
    [InlineData(WslDistroConfigErrorCode.Unknown)]
    [InlineData(WslDistroConfigErrorCode.InvalidValue)]
    [InlineData(WslDistroConfigErrorCode.InvalidPath)]
    [InlineData(WslDistroConfigErrorCode.InvalidMountOptions)]
    [InlineData(WslDistroConfigErrorCode.InvalidHostname)]
    [InlineData(WslDistroConfigErrorCode.InvalidUsername)]
    public void Code_CanBeSetToAnyValue(WslDistroConfigErrorCode code)
    {
        var error = new WslDistroConfigValidationError
        {
            Section = "test",
            Key = "test",
            Message = "Error",
            Code = code
        };

        error.Code.Should().Be(code);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var error1 = new WslDistroConfigValidationError
        {
            Section = "automount",
            Key = "root",
            Message = "Invalid path",
            Code = WslDistroConfigErrorCode.InvalidPath
        };
        var error2 = new WslDistroConfigValidationError
        {
            Section = "automount",
            Key = "root",
            Message = "Invalid path",
            Code = WslDistroConfigErrorCode.InvalidPath
        };

        error1.Should().Be(error2);
    }

    [Fact]
    public void Equals_WithDifferentSection_ReturnsFalse()
    {
        var error1 = new WslDistroConfigValidationError
        {
            Section = "automount",
            Key = "root",
            Message = "Error"
        };
        var error2 = new WslDistroConfigValidationError
        {
            Section = "network",
            Key = "root",
            Message = "Error"
        };

        error1.Should().NotBe(error2);
    }

    #endregion
}

public class WslDistroConfigErrorCodeTests
{
    [Fact]
    public void Unknown_HasValueZero()
    {
        ((int)WslDistroConfigErrorCode.Unknown).Should().Be(0);
    }

    [Theory]
    [InlineData(WslDistroConfigErrorCode.Unknown, "Unknown")]
    [InlineData(WslDistroConfigErrorCode.InvalidValue, "InvalidValue")]
    [InlineData(WslDistroConfigErrorCode.InvalidPath, "InvalidPath")]
    [InlineData(WslDistroConfigErrorCode.InvalidMountOptions, "InvalidMountOptions")]
    [InlineData(WslDistroConfigErrorCode.InvalidHostname, "InvalidHostname")]
    [InlineData(WslDistroConfigErrorCode.InvalidUsername, "InvalidUsername")]
    public void ToString_ReturnsExpectedName(WslDistroConfigErrorCode code, string expected)
    {
        code.ToString().Should().Be(expected);
    }

    [Fact]
    public void AllErrorCodes_HaveUniqueValues()
    {
        var values = Enum.GetValues<WslDistroConfigErrorCode>().Cast<int>().ToList();

        values.Should().OnlyHaveUniqueItems();
    }
}
