namespace WindowsAudioWrapper.Models;

public sealed class AudioEnhancementProfile
{
    public bool IsDeviceDefaultEffectsEnabled { get; set; } = false;

    public bool IsLoudnessEqualizationEnabled { get; set; } = false;

    public bool IsBassBoostEnabled { get; set; } = false;

    public bool IsVirtualSurroundEnabled { get; set; } = false;

    public List<AudioEffectSetting> Effects { get; set; } = new();

    public void EnsureDefaults()
    {
        Effects ??= new List<AudioEffectSetting>();
    }
}
