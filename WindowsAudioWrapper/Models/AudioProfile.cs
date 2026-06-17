namespace WindowsAudioWrapper.Models;

/// <summary>
/// The only profile/state object used by WindowsAudioWrapper.
/// A profile may be created by a user, saved in JSON, or captured from the current Windows audio state.
/// </summary>
public sealed class AudioProfile
{
    public int SchemaVersion { get; set; } = 1;

    public PlaybackAudioProfile Playback { get; set; } = new();

    public RecordingAudioProfile Recording { get; set; } = new();

    public SystemAudioProfile System { get; set; } = new();

    public bool IsAnyAudioSettingEnabled =>
        Playback.IsPlaybackEnabled ||
        Recording.IsRecordingEnabled ||
        System.IsSystemAudioEnabled;

    public void EnsureDefaults()
    {
        Playback ??= new PlaybackAudioProfile();
        Recording ??= new RecordingAudioProfile();
        System ??= new SystemAudioProfile();

        Playback.EnsureDefaults();
        Recording.EnsureDefaults();
        System.EnsureDefaults();
    }
}
