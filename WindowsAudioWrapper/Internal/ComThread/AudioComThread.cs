namespace WindowsAudioWrapper.Internal.ComThread;

/// <summary>
/// Placeholder for a future dedicated COM worker thread, if required.
/// The current public API is synchronous, but Core Audio COM calls can be serialised here later.
/// </summary>
internal sealed class AudioComThread : IDisposable
{
    public void Dispose()
    {
    }
}
