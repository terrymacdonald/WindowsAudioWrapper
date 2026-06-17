namespace WindowsAudioWrapper.Models;

public sealed class AudioEffectSetting
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = false;

    public string Value { get; set; } = string.Empty;
}
