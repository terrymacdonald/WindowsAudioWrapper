namespace WindowsAudioWrapper.Models;

public sealed class SpatialSoundProfile
{
    public SpatialSoundMode Mode { get; set; } = SpatialSoundMode.Off;

    public string FormatId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}