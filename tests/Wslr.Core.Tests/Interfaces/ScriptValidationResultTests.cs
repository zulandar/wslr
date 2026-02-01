using Wslr.Core.Interfaces;

namespace Wslr.Core.Tests.Interfaces;

public class ScriptValidationResultTests
{
    #region Static Success Property Tests

    [Fact]
    public void Success_ReturnsValidResult()
    {
        var result = ScriptValidationResult.Success;

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ErrorLine.Should().BeNull();
    }

    [Fact]
    public void Success_AlwaysReturnsNewInstance()
    {
        var result1 = ScriptValidationResult.Success;
        var result2 = ScriptValidationResult.Success;

        // Each call creates a new instance (computed property)
        result1.Should().Be(result2); // But they should be equal
    }

    #endregion

    #region Static Failure Factory Method Tests

    [Fact]
    public void Failure_WithMessageOnly_ReturnsInvalidResult()
    {
        var result = ScriptValidationResult.Failure("syntax error near unexpected token");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("syntax error near unexpected token");
        result.ErrorLine.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessageAndLine_ReturnsFullErrorInfo()
    {
        var result = ScriptValidationResult.Failure("unexpected end of file", 42);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("unexpected end of file");
        result.ErrorLine.Should().Be(42);
    }

    [Fact]
    public void Failure_WithEmptyMessage_ReturnsInvalidWithEmptyMessage()
    {
        var result = ScriptValidationResult.Failure("");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithLineNumberOne_ReturnsCorrectLine()
    {
        var result = ScriptValidationResult.Failure("error", 1);

        result.ErrorLine.Should().Be(1);
    }

    [Fact]
    public void Failure_WithLineNumberZero_ReturnsCorrectLine()
    {
        // Line 0 is technically valid (some parsers use 0-based indexing)
        var result = ScriptValidationResult.Failure("error", 0);

        result.ErrorLine.Should().Be(0);
    }

    [Fact]
    public void Failure_WithNegativeLineNumber_ReturnsNegativeLine()
    {
        // Negative line numbers might indicate special cases
        var result = ScriptValidationResult.Failure("error", -1);

        result.ErrorLine.Should().Be(-1);
    }

    #endregion

    #region Required Property Tests

    [Fact]
    public void IsValid_IsRequired_CanBeSetToTrue()
    {
        var result = new ScriptValidationResult { IsValid = true };

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_IsRequired_CanBeSetToFalse()
    {
        var result = new ScriptValidationResult { IsValid = false };

        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Optional Property Tests

    [Fact]
    public void ErrorMessage_DefaultValue_IsNull()
    {
        var result = new ScriptValidationResult { IsValid = true };

        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ErrorLine_DefaultValue_IsNull()
    {
        var result = new ScriptValidationResult { IsValid = true };

        result.ErrorLine.Should().BeNull();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        var result1 = ScriptValidationResult.Failure("error", 5);
        var result2 = ScriptValidationResult.Failure("error", 5);

        result1.Should().Be(result2);
    }

    [Fact]
    public void Equals_WithDifferentMessage_ReturnsFalse()
    {
        var result1 = ScriptValidationResult.Failure("error1", 5);
        var result2 = ScriptValidationResult.Failure("error2", 5);

        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Equals_WithDifferentLine_ReturnsFalse()
    {
        var result1 = ScriptValidationResult.Failure("error", 5);
        var result2 = ScriptValidationResult.Failure("error", 10);

        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Equals_SuccessAndFailure_ReturnsFalse()
    {
        var success = ScriptValidationResult.Success;
        var failure = ScriptValidationResult.Failure("error");

        success.Should().NotBe(failure);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void Failure_WithBashSyntaxError_ParsesCorrectly()
    {
        // Simulating a real bash syntax error
        var errorMessage = "line 5: syntax error near unexpected token `}'";
        var result = ScriptValidationResult.Failure(errorMessage, 5);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("syntax error");
        result.ErrorLine.Should().Be(5);
    }

    [Fact]
    public void Failure_WithMissingClosingQuote_ParsesCorrectly()
    {
        var errorMessage = "line 10: unexpected EOF while looking for matching `\"'";
        var result = ScriptValidationResult.Failure(errorMessage, 10);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EOF");
        result.ErrorLine.Should().Be(10);
    }

    [Fact]
    public void Success_ForValidScript_HasNoErrors()
    {
        var result = ScriptValidationResult.Success;

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ErrorLine.Should().BeNull();
    }

    #endregion
}
