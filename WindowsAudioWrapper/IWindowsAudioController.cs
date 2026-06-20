using WindowsAudioWrapper.Models;

namespace WindowsAudioWrapper;

/// <summary>
/// Public entry point for reading, validating, and applying Windows audio profiles and endpoints.
/// Supports both full macro-profile assignment and direct, granular per-feature execution commands.
/// </summary>
public interface IWindowsAudioController : IDisposable
{
    /// <summary>Gets a list of available playback devices (render endpoints).</summary>
    IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    /// <summary>Gets a list of available recording devices (capture endpoints).</summary>
    IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    /// <summary>Gets the current default multimedia playback device.</summary>
    AudioEndpointInfo GetDefaultPlaybackDevice();

    /// <summary>Gets the current default multimedia recording device.</summary>
    AudioEndpointInfo GetDefaultRecordingDevice();

    /// <summary>Gets the current default communications playback device.</summary>
    AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice();

    /// <summary>Gets the current default communications recording device.</summary>
    AudioEndpointInfo GetDefaultCommunicationsRecordingDevice();

    /// <summary>Captures every setting WindowsAudioWrapper can currently read into a normal AudioProfile.</summary>
    AudioProfile GetCurrentProfile();

    /// <summary>Validates an AudioProfile to ensure the devices exist, are active, and support the requested settings.</summary>
    AudioProfileValidationResult ValidateProfile(AudioProfile profile);

    /// <summary>Safely applies the enabled settings within an AudioProfile to the Windows environment.</summary>
    AudioProfileApplyResult ApplyProfile(AudioProfile profile);

    // --- Direct Per-Feature Control Endpoints ---

    /// <summary>Directly alters the master volume percentage level of a targeted audio hardware endpoint.</summary>
    void SetVolumePercent(string deviceId, decimal volumePercent);

    /// <summary>Directly toggles the mute state of a targeted audio hardware endpoint.</summary>
    void SetMute(string deviceId, bool muted);

    /// <summary>Directly configures the stream sample rate and channel layout parameters of an endpoint.</summary>
    void SetFormat(string deviceId, AudioFormatProfile format);

    /// <summary>Directly toggles system enhancements (APOs) on a targeted audio hardware endpoint.</summary>
    void SetAudioEnhancements(string deviceId, AudioEnhancementProfile enhancements);

    /// <summary>Directly assigns the default multimedia playback device routing of the host machine.</summary>
    void SetDefaultPlaybackDevice(string deviceId);

    /// <summary>Directly assigns the default multimedia recording device routing of the host machine.</summary>
    void SetDefaultRecordingDevice(string deviceId);

    /// <summary>Directly configures the spatial audio provider format GUID token string for a targeted hardware endpoint.</summary>
    void SetSpatialAudioFormat(string deviceId, string spatialAudioFormat);

    /// <summary>Directly alters the OS visibility state of an endpoint without requiring administrator rights.</summary>
    void SetDeviceDisabled(string deviceId, bool disabled);

    /// <summary>Directly configures discrete Left and Right channel gain volume scalars on an endpoint.</summary>
    void SetChannelVolumes(string deviceId, decimal leftVolume, decimal rightVolume);
}