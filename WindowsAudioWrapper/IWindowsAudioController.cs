namespace WindowsAudioWrapper;

using WindowsAudioWrapper.Models;

/// <summary>
/// Public entry point for reading and applying Windows audio profiles.
/// </summary>
public interface IWindowsAudioController : IDisposable
{
    IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    AudioEndpointInfo GetDefaultPlaybackDevice();

    AudioEndpointInfo GetDefaultRecordingDevice();

    AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice();

    AudioEndpointInfo GetDefaultCommunicationsRecordingDevice();

    /// <summary>
    /// Captures every setting WindowsAudioWrapper can currently read into a normal AudioProfile.
    /// The returned profile can be saved, edited, or applied later.
    /// </summary>
    AudioProfile GetCurrentProfile();

    AudioProfileValidationResult ValidateProfile(AudioProfile profile);

    AudioProfileApplyResult ApplyProfile(AudioProfile profile);
}
