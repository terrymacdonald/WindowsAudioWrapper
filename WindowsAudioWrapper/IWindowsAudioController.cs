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
    IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged);

    AudioEndpointInfo GetDefaultPlaybackDevice();

    AudioEndpointInfo GetDefaultConsolePlaybackDevice();

    AudioEndpointInfo GetDefaultRecordingDevice();

    AudioEndpointInfo GetDefaultConsoleRecordingDevice();

    AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice();

    AudioEndpointInfo GetDefaultCommunicationsRecordingDevice();

    /// <summary>
    /// Captures every setting WindowsAudioWrapper can currently read into a normal AudioProfile.
    /// The returned profile can be saved, edited, or applied later.
    /// </summary>
    AudioProfile GetCurrentProfile();

    AudioProfileValidationResult ValidateProfile(AudioProfile profile);

    AudioProfileApplyResult ApplyProfile(AudioProfile profile);

    /// <summary>
    /// Synchronously blocks execution until a matching audio endpoint becomes visible and active to the OS,
    /// or the specified timeout duration is reached.
    /// </summary>
    /// <param name="device">The search criteria target containing target identifiers (DeviceId, ContainerId, etc.).</param>
    /// <param name="timeoutMilliseconds">The maximum block window length in milliseconds.</param>
    /// <returns>True if the matching hardware device arrives before the timeout expires; otherwise, false.</returns>
    bool WaitForAudioDeviceToAppear(AudioEndpointReference device, int timeoutMilliseconds);

    /// <summary>
    /// Asynchronously monitors system hardware notifications until a matching audio endpoint becomes visible and active.
    /// </summary>
    /// <param name="device">The search criteria target containing target identifiers (DeviceId, ContainerId, etc.).</param>
    /// <param name="timeoutMilliseconds">The maximum wait duration length in milliseconds.</param>
    /// <param name="cancellationToken">An optional token to cooperatively cancel the asynchronous monitoring task.</param>
    /// <returns>A task containing true if the hardware becomes active before the timeout; otherwise, false.</returns>
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
