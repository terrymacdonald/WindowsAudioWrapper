namespace WindowsAudioWrapper.Providers;

/// <summary>
/// Placeholder provider for system-wide audio accessibility settings such as mono audio.
/// </summary>
internal sealed class SystemAudioProvider : ISystemAudioProvider
{
    public bool IsMonoAudioReadSupported => false;

    public bool IsMonoAudioSetSupported => false;

    public bool GetMonoAudio()
    {
        throw new NotImplementedException("Mono audio read has not been implemented yet.");
    }

    public void SetMonoAudio(bool enabled)
    {
        throw new NotImplementedException("Mono audio set has not been implemented yet.");
    }
}
