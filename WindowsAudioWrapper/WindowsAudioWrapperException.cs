namespace WindowsAudioWrapper;

/// <summary>
/// Base exception type for unexpected WindowsAudioWrapper failures.
/// Normal validation and capability problems should be returned in result objects instead.
/// </summary>
public class WindowsAudioWrapperException : Exception
{
    public WindowsAudioWrapperException()
    {
    }

    public WindowsAudioWrapperException(string message)
        : base(message)
    {
    }

    public WindowsAudioWrapperException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
