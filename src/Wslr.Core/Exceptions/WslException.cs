namespace Wslr.Core.Exceptions;

/// <summary>
/// Represents errors that occur during WSL operations.
/// </summary>
public class WslException : Exception
{
    /// <summary>
    /// Gets the exit code from the WSL command, if available.
    /// </summary>
    public int? ExitCode { get; }

    /// <summary>
    /// Gets the standard error output from the WSL command, if available.
    /// </summary>
    public string? StandardError { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WslException"/> class.
    /// </summary>
    public WslException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WslException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WslException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WslException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WslException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WslException"/> class with details from a WSL command failure.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="exitCode">The exit code from the WSL command.</param>
    /// <param name="standardError">The standard error output from the WSL command.</param>
    public WslException(string message, int exitCode, string? standardError)
        : base(message)
    {
        ExitCode = exitCode;
        StandardError = standardError;
    }
}
