namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the spatial sound configuration for an endpoint.
/// </summary>
public sealed class SpatialSoundProfile
{
    /// <summary>Gets or sets the spatial sound mode (e.g., Off, Windows Sonic).</summary>
    public SpatialSoundMode Mode { get; set; } = SpatialSoundMode.Off;

    /// <summary>Gets or sets the underlying format identifier used by Windows.</summary>
    public string FormatId { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable display name of the spatial sound format.</summary>
    public string DisplayName { get; set; } = string.Empty;
}