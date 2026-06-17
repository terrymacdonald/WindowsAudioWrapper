namespace WindowsAudioWrapper.Providers;

internal sealed class SystemAudioProvider : ISystemAudioProvider
{
    public bool IsMonoAudioReadSupported => false;

    public bool IsMonoAudioSetSupported => false;

    public bool GetMonoAudio()
    {
        return false;
    }

    public void SetMonoAudio(bool enabled)
    {
        throw new NotSupportedException("Mono audio setting is not supported by this version of WindowsAudioWrapper.");
    }
}
