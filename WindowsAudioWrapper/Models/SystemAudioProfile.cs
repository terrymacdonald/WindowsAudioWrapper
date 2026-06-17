namespace WindowsAudioWrapper.Models;

public sealed class SystemAudioProfile
{
    public bool IsSystemAudioEnabled { get; set; } = false;

    public bool IsMonoAudioEnabled { get; set; } = false;

    public bool MonoAudio { get; set; } = false;

    public void EnsureDefaults()
    {
    }
}
