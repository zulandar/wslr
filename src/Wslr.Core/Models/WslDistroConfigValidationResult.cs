namespace Wslr.Core.Models;

/// <summary>
/// Represents the result of validating a wsl.conf configuration.
/// </summary>
public sealed record WslDistroConfigValidationResult
{
    /// <summary>
    /// Gets a successful validation result.
    /// </summary>
    public static WslDistroConfigValidationResult Success { get; } = new() { IsValid = true };

    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyList<WslDistroConfigValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static WslDistroConfigValidationResult Failure(IEnumerable<WslDistroConfigValidationError> errors)
    {
        return new WslDistroConfigValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
}

/// <summary>
/// Represents a validation error for wsl.conf.
/// </summary>
public sealed record WslDistroConfigValidationError
{
    /// <summary>
    /// Gets the section name where the error occurred.
    /// </summary>
    public required string Section { get; init; }

    /// <summary>
    /// Gets the key name where the error occurred.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public WslDistroConfigErrorCode Code { get; init; }
}

/// <summary>
/// Error codes for wsl.conf validation.
/// </summary>
public enum WslDistroConfigErrorCode
{
    /// <summary>Unknown error.</summary>
    Unknown = 0,

    /// <summary>Invalid value for a setting.</summary>
    InvalidValue,

    /// <summary>Invalid path format.</summary>
    InvalidPath,

    /// <summary>Invalid mount options.</summary>
    InvalidMountOptions,

    /// <summary>Invalid hostname format.</summary>
    InvalidHostname,

    /// <summary>Invalid username format.</summary>
    InvalidUsername,
}
