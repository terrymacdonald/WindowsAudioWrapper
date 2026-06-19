namespace WindowsAudioWrapper.Models;

public sealed class AudioEnhancementProfile
{
    /// <summary>Gets or sets whether default device effects (SysFx) are enabled.</summary>
    public bool IsDeviceDefaultEffectsEnabled { get; set; } = false;

    public void EnsureDefaults()
    {
    }
}