namespace WindowsAudioWrapper;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsAudioWrapper.Models;

/// <summary>
/// Public entry point for reading and applying Windows audio profiles.
/// </summary>
public interface IWindowsAudioController : IDisposable
{
    /// <summary>Gets playback endpoints in the requested device states.</summary>
    /// <param name="states">The device states to include.</param>
    /// <returns>The matching playback endpoints.</returns>
    IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    /// <summary>Gets recording endpoints in the requested device states.</summary>
    /// <param name="states">The device states to include.</param>
    /// <returns>The matching recording endpoints.</returns>
    IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    /// <summary>Gets the default multimedia playback endpoint.</summary>
    AudioEndpointInfo GetDefaultPlaybackDevice();

    /// <summary>Gets the default console playback endpoint.</summary>
    AudioEndpointInfo GetDefaultConsolePlaybackDevice();

    /// <summary>Gets the default multimedia recording endpoint.</summary>
    AudioEndpointInfo GetDefaultRecordingDevice();

    /// <summary>Gets the default console recording endpoint.</summary>
    AudioEndpointInfo GetDefaultConsoleRecordingDevice();

    /// <summary>Gets the default communications playback endpoint.</summary>
    AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice();

    /// <summary>Gets the default communications recording endpoint.</summary>
    AudioEndpointInfo GetDefaultCommunicationsRecordingDevice();

    /// <summary>
    /// Captures every setting WindowsAudioWrapper can currently read into a normal AudioProfile.
    /// The returned profile can be saved, edited, or applied later.
    /// </summary>
    AudioProfile GetCurrentProfile();

    /// <summary>Checks whether the settings in a profile can be applied.</summary>
    /// <param name="profile">The profile to validate.</param>
    /// <returns>Validation results for the profile.</returns>
    AudioProfileValidationResult ValidateProfile(AudioProfile profile);

    /// <summary>Applies the enabled settings in an audio profile.</summary>
    /// <param name="profile">The profile to apply.</param>
    /// <returns>The result of applying the profile.</returns>
    AudioProfileApplyResult ApplyProfile(AudioProfile profile);

    /// <summary>
    /// Synchronously blocks execution until a matching audio endpoint becomes visible and active to the OS,
    /// or the specified timeout duration is reached.
    /// </summary>
    /// <param name="device">The search criteria target containing target identifiers (DeviceId, ContainerId, etc.).</param>
    /// <param name="timeoutMilliseconds">The maximum block window length in milliseconds.</param>
    /// <returns>True when a uniquely resolved endpoint in the requested direction is active. Nonpositive timeouts perform an immediate availability check.</returns>
    bool WaitForAudioDeviceToAppear(AudioEndpointReference device, int timeoutMilliseconds);

    /// <summary>
    /// Asynchronously monitors system hardware notifications until a matching audio endpoint becomes visible and active.
    /// </summary>
    /// <param name="device">The search criteria target containing target identifiers (DeviceId, ContainerId, etc.).</param>
    /// <param name="timeoutMilliseconds">The maximum wait duration length in milliseconds.</param>
    /// <param name="cancellationToken">An optional token to cooperatively cancel the asynchronous monitoring task.</param>
    /// <returns>A task containing true when a uniquely resolved endpoint is active before the timeout; false on timeout, cancellation or controller disposal. Nonpositive timeouts perform an immediate availability check.</returns>
    Task<bool> WaitForAudioDeviceToAppearAsync(AudioEndpointReference device, int timeoutMilliseconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a targeted device reference to explicitly monitor for custom connection arrivals.
    /// When matched, the <see cref="AudioDeviceConnected"/> event fires.
    /// </summary>
    /// <param name="device">The reference criteria mapping the target hardware to track.</param>
    void RegisterTargetDevice(AudioEndpointReference device);

    /// <summary>
    /// Unregisters a previously tracked device reference from arrival notification callback monitoring.
    /// </summary>
    /// <param name="device">The reference criteria mapping the target hardware to remove.</param>
    void UnregisterTargetDevice(AudioEndpointReference device);

    /// <summary>
    /// Fires when a specifically registered or targeted audio device connects and becomes active.
    /// </summary>
    event EventHandler<AudioDeviceEventArgs>? AudioDeviceConnected;

    /// <summary>
    /// Fires globally whenever any audio endpoint is plugged in, discovered, or becomes active in the system configuration.
    /// </summary>
    event EventHandler<AudioDeviceEventArgs>? AnyAudioDeviceConnected;
}
