namespace Wslr.Core.Models;

/// <summary>
/// Represents the result of validating a .wslconfig file.
/// </summary>
public sealed record WslConfigValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<WslConfigValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Gets a successful validation result.
    /// </summary>
    public static WslConfigValidationResult Success { get; } = new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static WslConfigValidationResult Failure(params WslConfigValidationError[] errors)
        => new() { IsValid = false, Errors = errors };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static WslConfigValidationResult Failure(IEnumerable<WslConfigValidationError> errors)
        => new() { IsValid = false, Errors = errors.ToList() };
}

/// <summary>
/// Represents a single validation error in a .wslconfig file.
/// </summary>
public sealed record WslConfigValidationError
{
    /// <summary>
    /// Gets the section where the error occurred (e.g., "wsl2", "experimental").
    /// </summary>
    public string? Section { get; init; }

    /// <summary>
    /// Gets the key that caused the error.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error code for programmatic handling.
    /// </summary>
    public WslConfigErrorCode Code { get; init; } = WslConfigErrorCode.Unknown;
}

/// <summary>
/// Error codes for .wslconfig validation.
/// </summary>
public enum WslConfigErrorCode
{
    /// <summary>Unknown error.</summary>
    Unknown = 0,

    /// <summary>Invalid value for a setting.</summary>
    InvalidValue = 1,

    /// <summary>Invalid memory format (e.g., "8GB" should be "8GB").</summary>
    InvalidMemoryFormat = 2,

    /// <summary>Invalid numeric value.</summary>
    InvalidNumber = 3,

    /// <summary>Value out of range.</summary>
    ValueOutOfRange = 4,

    /// <summary>Invalid path format.</summary>
    InvalidPath = 5,

    /// <summary>File not found.</summary>
    FileNotFound = 6,

    /// <summary>Invalid boolean value.</summary>
    InvalidBoolean = 7
}
