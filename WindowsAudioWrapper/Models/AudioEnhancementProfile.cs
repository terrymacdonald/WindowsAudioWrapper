namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the requested state for audio enhancements and effects.
/// </summary>
public sealed class AudioEnhancementProfile
{
    /// <summary>Gets or sets whether default device effects (SysFx) are enabled.</summary>
    public bool IsDeviceDefaultEffectsEnabled { get; set; } = false;

    /// <summary>Gets or sets whether loudness equalization is enabled.</summary>
    public bool IsLoudnessEqualizationEnabled { get; set; } = false;

    /// <summary>Gets or sets whether bass boost is enabled.</summary>
    public bool IsBassBoostEnabled { get; set; } = false;

    /// <summary>Gets or sets whether virtual surround is enabled.</summary>
    public bool IsVirtualSurroundEnabled { get; set; } = false;

    /// <summary>Gets or sets a list of specific effect settings.</summary>
    public List<AudioEffectSetting> Effects { get; set; } = new();

    /// <summary>Ensures all nested objects are instantiated properly after deserialization.</summary>
    public void EnsureDefaults()
    {
        Effects ??= new List<AudioEffectSetting>();
    }
}