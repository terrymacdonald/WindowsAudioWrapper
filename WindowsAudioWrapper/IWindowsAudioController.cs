namespace WindowsAudioWrapper;

using WindowsAudioWrapper.Models;

/// <summary>
/// Public entry point for reading, validating, and applying Windows audio profiles and endpoints.
/// </summary>
public interface IWindowsAudioController : IDisposable
{
    /// <summary>
    /// Gets a list of available playback devices (render endpoints).
    /// </summary>
    /// <param name="states">The state flags to filter by (e.g., Active, Unplugged). Defaults to Active and Unplugged.</param>
    /// <returns>A read-only list of audio endpoint information.</returns>
    IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    /// <summary>
    /// Gets a list of available recording devices (capture endpoints).
    /// </summary>
    /// <param name="states">The state flags to filter by (e.g., Active, Unplugged). Defaults to Active and Unplugged.</param>
    /// <returns>A read-only list of audio endpoint information.</returns>
    IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    /// <summary>
    /// Gets the current default multimedia playback device.
    /// </summary>
    /// <returns>Audio endpoint information for the default playback device.</returns>
    AudioEndpointInfo GetDefaultPlaybackDevice();

    /// <summary>
    /// Gets the current default multimedia recording device.
    /// </summary>
    /// <returns>Audio endpoint information for the default recording device.</returns>
    AudioEndpointInfo GetDefaultRecordingDevice();

    /// <summary>
    /// Gets the current default communications playback device.
    /// </summary>
    /// <returns>Audio endpoint information for the default communications playback device.</returns>
    AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice();

    /// <summary>
    /// Gets the current default communications recording device.
    /// </summary>
    /// <returns>Audio endpoint information for the default communications recording device.</returns>
    AudioEndpointInfo GetDefaultCommunicationsRecordingDevice();

    /// <summary>
    /// Captures every setting WindowsAudioWrapper can currently read into a normal AudioProfile.
    /// The returned profile can be serialized to JSON, saved, edited, or applied later.
    /// </summary>
    /// <returns>A fully populated AudioProfile representing the current system state.</returns>
    AudioProfile GetCurrentProfile();

    /// <summary>
    /// Validates an AudioProfile to ensure the devices exist, are active, and support the requested settings.
    /// </summary>
    /// <param name="profile">The AudioProfile to validate.</param>
    /// <returns>A validation result detailing any errors or warnings.</returns>
    AudioProfileValidationResult ValidateProfile(AudioProfile profile);

    /// <summary>
    /// Safely applies the enabled settings within an AudioProfile to the Windows environment.
    /// </summary>
    /// <param name="profile">The AudioProfile to apply.</param>
    /// <returns>An apply result detailing success or failure and operation messages.</returns>
    AudioProfileApplyResult ApplyProfile(AudioProfile profile);
}