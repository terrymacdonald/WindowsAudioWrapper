namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents global system audio settings that apply across all devices.
/// </summary>
public sealed class SystemAudioProfile
{
    /// <summary>Gets or sets whether system audio settings should be evaluated and applied.</summary>
    public bool IsSystemAudioEnabled { get; set; } = false;

    /// <summary>Gets or sets whether the Mono Audio setting should be changed.</summary>
    public bool IsMonoAudioEnabled { get; set; } = false;

    /// <summary>Gets or sets the desired Mono Audio state.</summary>
    public bool MonoAudio { get; set; } = false;

    /// <summary>Ensures all nested objects are instantiated properly after deserialization.</summary>
    public void EnsureDefaults()
    {
    }
}