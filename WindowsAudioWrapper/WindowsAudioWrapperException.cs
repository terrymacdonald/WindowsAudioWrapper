namespace WindowsAudioWrapper;

/// <summary>
/// Base exception type for unexpected WindowsAudioWrapper failures.
/// Normal validation and capability problems should be returned in result objects instead.
/// </summary>
public class WindowsAudioWrapperException : Exception
{
    /// <summary>Initializes a new exception without a message.</summary>
    public WindowsAudioWrapperException()
    {
    }

    /// <summary>Initializes a new exception with an error message.</summary>
    /// <param name="message">The error message.</param>
    public WindowsAudioWrapperException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new exception with an error message and an underlying exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public WindowsAudioWrapperException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
