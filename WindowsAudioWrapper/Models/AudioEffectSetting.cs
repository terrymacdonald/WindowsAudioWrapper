namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents an individual audio effect or enhancement setting.
/// </summary>
public sealed class AudioEffectSetting
{
    /// <summary>Gets or sets the unique identifier for the effect.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable name of the effect.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the effect is currently enabled.</summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>Gets or sets an optional value associated with the effect.</summary>
    public string Value { get; set; } = string.Empty;
}