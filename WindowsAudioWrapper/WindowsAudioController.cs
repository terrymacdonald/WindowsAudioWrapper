namespace WindowsAudioWrapper;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsAudioWrapper.Models;
using WindowsAudioWrapper.Providers;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Internal.PolicyConfig;

/// <summary>
/// Provides the public facade managing and coordinating core Windows audio configurations.
/// </summary>
public sealed class WindowsAudioController : IWindowsAudioController
{
    private readonly IAudioDeviceProvider _deviceProvider;
    private readonly IDefaultAudioDeviceProvider _defaultDeviceProvider;
    private readonly IAudioVolumeProvider _volumeProvider;
    private readonly IAudioFormatProvider _formatProvider;
    private readonly IAudioEnhancementProvider _audioEnhancementProvider;
    private readonly ISystemAudioProvider _systemAudioProvider;
    private bool _disposed;

    private readonly AudioNotificationRouter _notificationRouter;
    private IMMDeviceEnumerator? _comEnumerator;
    private readonly List<AudioEndpointReference> _targetedDevices;
    private readonly object _targetedDevicesLock;
    private readonly object _notificationRegistrationLock;
    private IntPtr _comCallbackPointer;
    private EventHandler<AudioDeviceEventArgs>? _audioDeviceConnected;
    private EventHandler<AudioDeviceEventArgs>? _anyAudioDeviceConnected;

    private static readonly System.Runtime.InteropServices.Marshalling.StrategyBasedComWrappers ComWrappers = new();

    /// <summary>
    /// Fires when a specifically registered or targeted audio device connects and becomes active.
    /// </summary>
    public event EventHandler<AudioDeviceEventArgs>? AudioDeviceConnected
    {
        add
        {
            EnsureNotificationRegistration();
            _audioDeviceConnected += value;
        }
        remove
        {
            _audioDeviceConnected -= value;
        }
    }

    /// <summary>
    /// Fires globally whenever any audio endpoint is plugged in, discovered, or becomes active in the system.
    /// </summary>
    public event EventHandler<AudioDeviceEventArgs>? AnyAudioDeviceConnected
    {
        add
        {
            EnsureNotificationRegistration();
            _anyAudioDeviceConnected += value;
        }
        remove
        {
            _anyAudioDeviceConnected -= value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsAudioController"/> class using target concrete engines.
    /// </summary>
    public WindowsAudioController()
        : this(
            new AudioDeviceProvider(),
            new DefaultAudioDeviceProvider(),
            new AudioVolumeProvider(),
            new AudioFormatProvider(),
            new AudioEnhancementProvider(),
            new SystemAudioProvider())
    {
    }

    internal WindowsAudioController(
        IAudioDeviceProvider deviceProvider,
        IDefaultAudioDeviceProvider defaultDeviceProvider,
        IAudioVolumeProvider volumeProvider,
        IAudioFormatProvider formatProvider,
        IAudioEnhancementProvider audioEnhancementProvider,
        ISystemAudioProvider systemAudioProvider)
    {
        _deviceProvider = deviceProvider ?? throw new ArgumentNullException(nameof(deviceProvider));
        _defaultDeviceProvider = defaultDeviceProvider ?? throw new ArgumentNullException(nameof(defaultDeviceProvider));
        _volumeProvider = volumeProvider ?? throw new ArgumentNullException(nameof(volumeProvider));
        _formatProvider = formatProvider ?? throw new ArgumentNullException(nameof(formatProvider));
        _audioEnhancementProvider = audioEnhancementProvider ?? throw new ArgumentNullException(nameof(audioEnhancementProvider));
        _systemAudioProvider = systemAudioProvider ?? throw new ArgumentNullException(nameof(systemAudioProvider));

        _targetedDevices = new List<AudioEndpointReference>();
        _targetedDevicesLock = new object();
        _notificationRegistrationLock = new object();
        _notificationRouter = new AudioNotificationRouter();
        _notificationRouter.DeviceNotificationReceived += OnDeviceNotificationReceived;
    }

    /// <inheritdoc/>
    public IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged)
    {
        ThrowIfDisposed();
        return _deviceProvider.GetPlaybackDevices(states);
    }

    /// <inheritdoc/>
    public IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged)
    {
        ThrowIfDisposed();
        return _deviceProvider.GetRecordingDevices(states);
    }

    /// <inheritdoc/>
    public AudioEndpointInfo GetDefaultPlaybackDevice() { ThrowIfDisposed(); return _defaultDeviceProvider.GetDefaultPlaybackDevice(); }
    /// <inheritdoc/>
    public AudioEndpointInfo GetDefaultConsolePlaybackDevice() { ThrowIfDisposed(); return _defaultDeviceProvider.GetDefaultConsolePlaybackDevice(); }
    /// <inheritdoc/>
    public AudioEndpointInfo GetDefaultRecordingDevice() { ThrowIfDisposed(); return _defaultDeviceProvider.GetDefaultRecordingDevice(); }
    /// <inheritdoc/>
    public AudioEndpointInfo GetDefaultConsoleRecordingDevice() { ThrowIfDisposed(); return _defaultDeviceProvider.GetDefaultConsoleRecordingDevice(); }
    /// <inheritdoc/>
    public AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice() { ThrowIfDisposed(); return _defaultDeviceProvider.GetDefaultCommunicationsPlaybackDevice(); }
    /// <inheritdoc/>
    public AudioEndpointInfo GetDefaultCommunicationsRecordingDevice() { ThrowIfDisposed(); return _defaultDeviceProvider.GetDefaultCommunicationsRecordingDevice(); }

    // --- Direct Per-Feature Methods Implementation Block ---

    /// <inheritdoc/>
    public void SetVolumePercent(string deviceId, decimal volumePercent)
    {
        ThrowIfDisposed();
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _volumeProvider.SetVolumePercent(reference, volumePercent);
    }

    /// <inheritdoc/>
    public void SetMute(string deviceId, bool muted)
    {
        ThrowIfDisposed();
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _volumeProvider.SetMute(reference, muted);
    }

    /// <inheritdoc/>
    public void SetFormat(string deviceId, AudioFormatProfile format)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(format);
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _formatProvider.SetFormat(reference, format);
    }

    /// <inheritdoc/>
    public void SetAudioEnhancements(string deviceId, AudioEnhancementProfile enhancements)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(enhancements);
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _audioEnhancementProvider.SetAudioEnhancements(reference, enhancements);
    }

    /// <inheritdoc/>
    public void SetDefaultPlaybackDevice(string deviceId)
    {
        ThrowIfDisposed();
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _defaultDeviceProvider.SetDefaultPlaybackDevice(reference);
    }

    /// <summary>
    /// Sets the default console playback device.
    /// </summary>
    public void SetDefaultConsolePlaybackDevice(string deviceId)
    {
        ThrowIfDisposed();
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _defaultDeviceProvider.SetDefaultConsolePlaybackDevice(reference);
    }

    /// <inheritdoc/>
    public void SetDefaultRecordingDevice(string deviceId)
    {
        ThrowIfDisposed();
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _defaultDeviceProvider.SetDefaultRecordingDevice(reference);
    }

    /// <summary>
    /// Sets the default console recording device.
    /// </summary>
    public void SetDefaultConsoleRecordingDevice(string deviceId)
    {
        ThrowIfDisposed();
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _defaultDeviceProvider.SetDefaultConsoleRecordingDevice(reference);
    }

    /// <inheritdoc/>
    public void SetDeviceDisabled(string deviceId, bool disabled)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(deviceId)) return;
        try
        {
            var policyType = Type.GetTypeFromCLSID(PolicyConfigInterop.CLSID_PolicyConfigClient);
            if (policyType != null)
            {
                var policyConfig = (PolicyConfigInterop.IPolicyConfig)Activator.CreateInstance(policyType)!;
                policyConfig.SetEndpointVisibility(deviceId, !disabled); // visible = true when disabled is false
            }
        }
        catch {}
    }

    /// <inheritdoc/>
    public void SetChannelVolumes(string deviceId, decimal leftVolume, decimal rightVolume)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(deviceId)) return;
        try
        {
            var vol = CoreAudioUtilities.ActivateEndpointVolume(deviceId);
            Guid ctx = Guid.Empty;
            if (vol.GetChannelCount(out uint counts) < 0)
            {
                return;
            }

            if (counts >= 2)
            {
                vol.SetChannelVolumeLevelScalar(0, (float)(leftVolume / 100m), in ctx);
                vol.SetChannelVolumeLevelScalar(1, (float)(rightVolume / 100m), in ctx);
            }
            else if (counts == 1)
            {
                decimal averageVolume = (leftVolume + rightVolume) / 2m;
                vol.SetMasterVolumeLevelScalar((float)(averageVolume / 100m), in ctx);
            }
        }
        catch {}
    }

    /// <inheritdoc/>
    public bool WaitForAudioDeviceToAppear(AudioEndpointReference device, int timeoutMilliseconds)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(device);

        if (CheckIfDeviceAlreadyActive(device))
        {
            return true;
        }

        if (timeoutMilliseconds <= 0)
        {
            return false;
        }

        using var resetEvent = new ManualResetEventSlim(false);
        bool matched = false;

        EventHandler<AudioDeviceEventArgs> handler = (sender, args) =>
        {
            if (IsDeviceMatch(device, args))
            {
                matched = true;
                resetEvent.Set();
            }
        };

        AnyAudioDeviceConnected += handler;

        try
        {
            if (CheckIfDeviceAlreadyActive(device))
            {
                return true;
            }

            resetEvent.Wait(timeoutMilliseconds);
            return matched;
        }
        finally
        {
            AnyAudioDeviceConnected -= handler;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> WaitForAudioDeviceToAppearAsync(AudioEndpointReference device, int timeoutMilliseconds, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(device);

        if (CheckIfDeviceAlreadyActive(device))
        {
            return true;
        }

        if (timeoutMilliseconds <= 0 || cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<AudioDeviceEventArgs> handler = (sender, args) =>
        {
            if (IsDeviceMatch(device, args))
            {
                tcs.TrySetResult(true);
            }
        };

        AnyAudioDeviceConnected += handler;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutMilliseconds > 0 && timeoutMilliseconds != Timeout.Infinite)
        {
            cts.CancelAfter(timeoutMilliseconds);
        }

        using var registration = cts.Token.Register(() => tcs.TrySetResult(false));

        try
        {
            if (CheckIfDeviceAlreadyActive(device))
            {
                return true;
            }

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            AnyAudioDeviceConnected -= handler;
        }
    }

    /// <inheritdoc/>
    public void RegisterTargetDevice(AudioEndpointReference device)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(device);
        EnsureNotificationRegistration();

        lock (_targetedDevicesLock)
        {
            if (!_targetedDevices.Any(d => d.DeviceId.Equals(device.DeviceId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(device.DeviceId)))
            {
                _targetedDevices.Add(device);
            }
        }
    }

    /// <inheritdoc/>
    public void UnregisterTargetDevice(AudioEndpointReference device)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(device);

        lock (_targetedDevicesLock)
        {
            _targetedDevices.RemoveAll(d => 
                (string.IsNullOrEmpty(device.DeviceId) || d.DeviceId.Equals(device.DeviceId, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(device.ContainerId) || d.ContainerId.Equals(device.ContainerId, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(device.FriendlyName) || d.FriendlyName.Equals(device.FriendlyName, StringComparison.OrdinalIgnoreCase)));
        }
    }

    // --- Macro Profile Processing Block ---

    /// <inheritdoc/>
    public AudioProfile GetCurrentProfile()
    {
        ThrowIfDisposed();
        AudioProfile profile = new();
        CaptureEndpointVisibilities(profile);
        CapturePlaybackProfile(profile.Playback);
        CaptureRecordingProfile(profile.Recording);
        CaptureSystemAudioProfile(profile.System);
        return profile;
    }

    /// <inheritdoc/>
    public AudioProfileValidationResult ValidateProfile(AudioProfile profile)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(profile);
        profile.EnsureDefaults();

        AudioProfileValidationResult result = new();
        ValidatePlaybackProfile(profile.Playback, result);
        ValidateRecordingProfile(profile.Recording, result);
        ValidateSystemAudioProfile(profile.System, result);

        result.RecalculateSeverity();
        return result;
    }

    /// <inheritdoc/>
    public AudioProfileApplyResult ApplyProfile(AudioProfile profile)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(profile);
        profile.EnsureDefaults();

        AudioProfileApplyResult result = new();
        AudioProfileValidationResult validationResult = ValidateProfile(profile);
        foreach (var message in validationResult.Messages) result.Messages.Add(message);

        if (!validationResult.Successful)
        {
            result.Successful = false;
            return result;
        }

        try
        {
            if (profile.IsEndpointVisibilityTrackingEnabled) ApplyEndpointVisibilities(profile.EndpointVisibilities);
            if (profile.Playback.IsPlaybackEnabled) ApplyPlaybackProfile(profile.Playback, result);
            if (profile.Recording.IsRecordingEnabled) ApplyRecordingProfile(profile.Recording, result);
            if (profile.System.IsSystemAudioEnabled) ApplySystemAudioProfile(profile.System, result);
        }
        catch (Exception ex)
        {
            result.Successful = false;
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.UnexpectedError, $"Unexpected error applying audio profile: {ex.Message}"));
        }

        result.RecalculateSuccess();
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        if (_comEnumerator is not null && _comCallbackPointer != IntPtr.Zero)
        {
            try
            {
                _comEnumerator.UnregisterEndpointNotificationCallback(_comCallbackPointer);
            }
            catch
            {
                // Robustness safety guard
            }
        }

        if (_notificationRouter is not null)
        {
            _notificationRouter.DeviceNotificationReceived -= OnDeviceNotificationReceived;
        }

        (_deviceProvider as IDisposable)?.Dispose();
        (_defaultDeviceProvider as IDisposable)?.Dispose();
        (_volumeProvider as IDisposable)?.Dispose();
        (_formatProvider as IDisposable)?.Dispose();
        (_audioEnhancementProvider as IDisposable)?.Dispose();
        (_systemAudioProvider as IDisposable)?.Dispose();
        _disposed = true;
    }

    private void OnDeviceNotificationReceived(string deviceId)
    {
        // Safe asynchronous offload to prevent MMDevice API deadlock/re-entrancy process crashes
        Task.Run(() =>
        {
            try
            {
                AudioEndpointInfo? found = GetPlaybackDevices(AudioDeviceState.All)
                    .FirstOrDefault(d => d.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));

                if (found is null)
                {
                    found = GetRecordingDevices(AudioDeviceState.All)
                        .FirstOrDefault(d => d.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
                }

                if (found is not null && found.IsAvailable)
                {
                    var args = new AudioDeviceEventArgs
                    {
                        DeviceId = found.DeviceId,
                        ContainerId = found.ContainerId,
                        FriendlyName = found.FriendlyName,
                        FullName = found.FullName,
                        Flow = found.Flow
                    };

                    _anyAudioDeviceConnected?.Invoke(this, args);

                    lock (_targetedDevicesLock)
                    {
                        if (_targetedDevices.Any(target => IsDeviceMatch(target, args)))
                        {
                            _audioDeviceConnected?.Invoke(this, args);
                        }
                    }
                }
            }
            catch
            {
                // Deaden background thread errors to honor stability guidelines
            }
        });
    }

    private void EnsureNotificationRegistration()
    {
        ThrowIfDisposed();

        if (_comCallbackPointer != IntPtr.Zero)
        {
            return;
        }

        lock (_notificationRegistrationLock)
        {
            if (_comCallbackPointer != IntPtr.Zero)
            {
                return;
            }

            try
            {
                _comEnumerator = CoreAudioUtilities.CreateEnumerator();
                _comCallbackPointer = ComWrappers.GetOrCreateComInterfaceForObject(_notificationRouter, CreateComInterfaceFlags.None);
                _comEnumerator.RegisterEndpointNotificationCallback(_comCallbackPointer);
            }
            catch
            {
                _comEnumerator = null;
                _comCallbackPointer = IntPtr.Zero;
            }
        }
    }

    private bool CheckIfDeviceAlreadyActive(AudioEndpointReference target)
    {
        IEnumerable<AudioEndpointInfo> currentDevices;
        if (target.Flow == AudioFlow.Capture)
        {
            currentDevices = GetRecordingDevices(AudioDeviceState.Active);
        }
        else if (target.Flow == AudioFlow.Render)
        {
            currentDevices = GetPlaybackDevices(AudioDeviceState.Active);
        }
        else
        {
            currentDevices = GetPlaybackDevices(AudioDeviceState.Active).Concat(GetRecordingDevices(AudioDeviceState.Active));
        }

        foreach (var existing in currentDevices)
        {
            var incomingDummy = new AudioDeviceEventArgs
            {
                DeviceId = existing.DeviceId,
                ContainerId = existing.ContainerId,
                FriendlyName = existing.FriendlyName,
                FullName = existing.FullName,
                Flow = existing.Flow
            };

            if (IsDeviceMatch(target, incomingDummy))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDeviceMatch(AudioEndpointReference target, AudioDeviceEventArgs incoming)
    {
        if (!string.IsNullOrWhiteSpace(target.DeviceId) &&
            target.DeviceId.Equals(incoming.DeviceId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(target.ContainerId) &&
            target.ContainerId.Equals(incoming.ContainerId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(target.FullName) &&
            target.FullName.Equals(incoming.FullName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(target.FriendlyName) &&
            target.FriendlyName.Equals(incoming.FriendlyName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private void CaptureEndpointVisibilities(AudioProfile profile)
    {
        profile.EndpointVisibilities.Clear();

        IReadOnlyList<AudioEndpointInfo> playbackDevices = _deviceProvider.GetPlaybackDevices(AudioDeviceState.All);
        IReadOnlyList<AudioEndpointInfo> recordingDevices = _deviceProvider.GetRecordingDevices(AudioDeviceState.All);

        foreach (AudioEndpointInfo endpoint in playbackDevices.Concat(recordingDevices))
        {
            if (string.IsNullOrWhiteSpace(endpoint.DeviceId))
            {
                continue;
            }

            profile.EndpointVisibilities.Add(AudioEndpointVisibility.FromEndpointInfo(endpoint));
        }

        profile.IsEndpointVisibilityTrackingEnabled = profile.EndpointVisibilities.Count > 0;
    }

    private void ApplyEndpointVisibilities(IReadOnlyCollection<AudioEndpointVisibility> endpointVisibilities)
    {
        if (endpointVisibilities.Count == 0)
        {
            return;
        }

        foreach (AudioEndpointVisibility endpointVisibility in endpointVisibilities)
        {
            if (string.IsNullOrWhiteSpace(endpointVisibility.DeviceId))
            {
                continue;
            }

            SetDeviceDisabled(endpointVisibility.DeviceId, endpointVisibility.IsDisabled);
        }
    }

    private void CapturePlaybackProfile(PlaybackAudioProfile playback)
    {
        AudioEndpointInfo defaultDevice = GetDefaultPlaybackDevice();
        AudioEndpointInfo consoleDevice = GetDefaultConsolePlaybackDevice();
        AudioEndpointInfo communicationsDevice = GetDefaultCommunicationsPlaybackDevice();

        playback.IsPlaybackEnabled = true;

        if (defaultDevice.IsAvailable)
        {
            playback.IsDefaultPlaybackDeviceEnabled = true;
            playback.MultimediaDevice = AudioEndpointReference.FromEndpointInfo(defaultDevice);

            playback.IsVolumeEnabled = defaultDevice.Capabilities.IsVolumeSupported;
            if (playback.IsVolumeEnabled) playback.VolumePercent = defaultDevice.VolumePercent;

            playback.IsMuteEnabled = defaultDevice.Capabilities.IsMuteSupported;
            if (playback.IsMuteEnabled) playback.IsMuted = defaultDevice.IsMuted;

            playback.IsFormatEnabled = true;
            playback.StreamFormat = _formatProvider.GetFormat(defaultDevice.DeviceId);

            // Capture Speaker Layout Configuration Mask via registry extraction
            playback.StreamFormat.ChannelMask = ReadRegistryPropertyUint(defaultDevice.DeviceId, true, "{14242002-0320-4de4-9555-a7d82b73c286},3");

            playback.IsAudioEnhancementsEnabled = true;
            playback.AudioEnhancements = _audioEnhancementProvider.GetAudioEnhancements(defaultDevice.DeviceId);
            
            playback.IsDeviceDisabled = defaultDevice.State.HasFlag(AudioDeviceState.Disabled);
            playback.IsDeviceDisabledTrackingEnabled = false;
            
            CaptureChannelVolumes(defaultDevice.DeviceId, out float l, out float r);
            playback.VolumeLeft = (decimal)(l * 100f);
            playback.VolumeRight = (decimal)(r * 100f);
            playback.IsChannelVolumeEnabled = true;

            // Capture Fine-Grained Slider Percentages from FxProperties
            CaptureRegistrySliders(defaultDevice.DeviceId, true, playback.ApoSliders);
            playback.IsApoSlidersEnabled = playback.ApoSliders.Count > 0;
        }

        if (consoleDevice.IsAvailable)
        {
            playback.IsDefaultConsolePlaybackDeviceEnabled = true;
            playback.ConsoleDevice = AudioEndpointReference.FromEndpointInfo(consoleDevice);
        }

        if (communicationsDevice.IsAvailable)
        {
            playback.IsDefaultCommunicationsPlaybackDeviceEnabled = true;
            playback.CommunicationsDevice = AudioEndpointReference.FromEndpointInfo(communicationsDevice);
        }
    }

    private void CaptureRecordingProfile(RecordingAudioProfile recording)
    {
        AudioEndpointInfo defaultDevice = GetDefaultRecordingDevice();
        AudioEndpointInfo consoleDevice = GetDefaultConsoleRecordingDevice();
        AudioEndpointInfo communicationsDevice = GetDefaultCommunicationsRecordingDevice();

        recording.IsRecordingEnabled = true;

        if (defaultDevice.IsAvailable)
        {
            recording.IsDefaultRecordingDeviceEnabled = true;
            recording.MultimediaDevice = AudioEndpointReference.FromEndpointInfo(defaultDevice);

            recording.IsVolumeEnabled = defaultDevice.Capabilities.IsVolumeSupported;
            if (recording.IsVolumeEnabled) recording.VolumePercent = defaultDevice.VolumePercent;

            recording.IsMuteEnabled = defaultDevice.Capabilities.IsMuteSupported;
            if (recording.IsMuteEnabled) recording.IsMuted = defaultDevice.IsMuted;

            recording.IsFormatEnabled = true;
            recording.StreamFormat = _formatProvider.GetFormat(defaultDevice.DeviceId);

            // Capture Recording Layout Configuration Mask via registry extraction
            recording.StreamFormat.ChannelMask = ReadRegistryPropertyUint(defaultDevice.DeviceId, false, "{14242002-0320-4de4-9555-a7d82b73c286},3");

            recording.IsAudioEnhancementsEnabled = true;
            recording.AudioEnhancements = _audioEnhancementProvider.GetAudioEnhancements(defaultDevice.DeviceId);

            recording.IsDeviceDisabled = defaultDevice.State.HasFlag(AudioDeviceState.Disabled);
            recording.IsDeviceDisabledTrackingEnabled = false;
            
            CaptureChannelVolumes(defaultDevice.DeviceId, out float l, out float r);
            recording.VolumeLeft = (decimal)(l * 100f);
            recording.VolumeRight = (decimal)(r * 100f);
            recording.IsChannelVolumeEnabled = true;

            // Capture Fine-Grained Slider Percentages from FxProperties
            CaptureRegistrySliders(defaultDevice.DeviceId, false, recording.ApoSliders);
            recording.IsApoSlidersEnabled = recording.ApoSliders.Count > 0;
        }

        if (consoleDevice.IsAvailable)
        {
            recording.IsDefaultConsoleRecordingDeviceEnabled = true;
            recording.ConsoleDevice = AudioEndpointReference.FromEndpointInfo(consoleDevice);
        }

        if (communicationsDevice.IsAvailable)
        {
            recording.IsDefaultCommunicationsRecordingDeviceEnabled = true;
            recording.CommunicationsDevice = AudioEndpointReference.FromEndpointInfo(communicationsDevice);
        }
    }

    private void CaptureChannelVolumes(string id, out float left, out float right)
    {
        left = 1.0f; right = 1.0f;
        try
        {
            var vol = CoreAudioUtilities.ActivateEndpointVolume(id);
            if (vol.GetChannelCount(out uint counts) < 0)
            {
                return;
            }

            if (counts >= 2)
            {
                int leftHr = vol.GetChannelVolumeLevelScalar(0, out float capturedLeft);
                int rightHr = vol.GetChannelVolumeLevelScalar(1, out float capturedRight);
                if (leftHr >= 0 && rightHr >= 0)
                {
                    left = capturedLeft;
                    right = capturedRight;
                }
            }
            else if (counts == 1 && vol.GetMasterVolumeLevelScalar(out float masterVolume) >= 0)
            {
                left = masterVolume;
                right = masterVolume;
            }
        }
        catch {}
    }

    private void CaptureSystemAudioProfile(SystemAudioProfile system)
    {
        system.IsSystemAudioEnabled = true;
        system.IsMonoAudioEnabled = true;
        system.MonoAudio = _systemAudioProvider.GetMonoAudio();
    }

    private void ApplyPlaybackProfile(PlaybackAudioProfile playback, AudioProfileApplyResult result)
    {
        if (playback.IsDeviceDisabledTrackingEnabled)
        {
            SetDeviceDisabled(playback.MultimediaDevice.DeviceId, playback.IsDeviceDisabled);
        }
        if (playback.IsDeviceDisabledTrackingEnabled && playback.IsDeviceDisabled) return;

        if (playback.IsDefaultConsolePlaybackDeviceEnabled && !IsCurrentDefaultConsolePlaybackDevice(playback.ConsoleDevice)) _defaultDeviceProvider.SetDefaultConsolePlaybackDevice(playback.ConsoleDevice);
        if (playback.IsDefaultPlaybackDeviceEnabled && !IsCurrentDefaultPlaybackDevice(playback.MultimediaDevice)) _defaultDeviceProvider.SetDefaultPlaybackDevice(playback.MultimediaDevice);
        if (playback.IsDefaultCommunicationsPlaybackDeviceEnabled && !IsCurrentDefaultCommunicationsPlaybackDevice(playback.CommunicationsDevice)) _defaultDeviceProvider.SetDefaultCommunicationsPlaybackDevice(playback.CommunicationsDevice);
        if (playback.IsVolumeEnabled) _volumeProvider.SetVolumePercent(playback.MultimediaDevice, playback.VolumePercent);
        if (playback.IsMuteEnabled) _volumeProvider.SetMute(playback.MultimediaDevice, playback.IsMuted);
        if (playback.IsFormatEnabled) _formatProvider.SetFormat(playback.MultimediaDevice, playback.StreamFormat);
        if (playback.IsAudioEnhancementsEnabled) _audioEnhancementProvider.SetAudioEnhancements(playback.MultimediaDevice, playback.AudioEnhancements);
        if (playback.IsChannelVolumeEnabled) SetChannelVolumes(playback.MultimediaDevice.DeviceId, playback.VolumeLeft, playback.VolumeRight);
        
        // Restore Channel Layout Configuration Masks and Driver Slider Percentages securely via Property Store
        if (playback.IsFormatEnabled && playback.StreamFormat.ChannelMask > 0)
        {
            var maskDict = new Dictionary<string, string> { { "{14242002-0320-4de4-9555-a7d82b73c286},3", $"int:{playback.StreamFormat.ChannelMask}" } };
            ApplyPropertiesViaPropertyStore(playback.MultimediaDevice.DeviceId, maskDict);
        }
        if (playback.IsApoSlidersEnabled)
        {
            ApplyPropertiesViaPropertyStore(playback.MultimediaDevice.DeviceId, playback.ApoSliders);
        }

        result.Messages.Add(AudioOperationMessage.Info(AudioMessageCode.ProfileApplied, "Playback audio profile applied."));
    }

    private void ApplyRecordingProfile(RecordingAudioProfile recording, AudioProfileApplyResult result)
    {
        if (recording.IsDeviceDisabledTrackingEnabled)
        {
            SetDeviceDisabled(recording.MultimediaDevice.DeviceId, recording.IsDeviceDisabled);
        }
        if (recording.IsDeviceDisabledTrackingEnabled && recording.IsDeviceDisabled) return;

        if (recording.IsDefaultConsoleRecordingDeviceEnabled && !IsCurrentDefaultConsoleRecordingDevice(recording.ConsoleDevice)) _defaultDeviceProvider.SetDefaultConsoleRecordingDevice(recording.ConsoleDevice);
        if (recording.IsDefaultRecordingDeviceEnabled && !IsCurrentDefaultRecordingDevice(recording.MultimediaDevice)) _defaultDeviceProvider.SetDefaultRecordingDevice(recording.MultimediaDevice);
        if (recording.IsDefaultCommunicationsRecordingDeviceEnabled && !IsCurrentDefaultCommunicationsRecordingDevice(recording.CommunicationsDevice)) _defaultDeviceProvider.SetDefaultCommunicationsRecordingDevice(recording.CommunicationsDevice);
        if (recording.IsVolumeEnabled) _volumeProvider.SetVolumePercent(recording.MultimediaDevice, recording.VolumePercent);
        if (recording.IsMuteEnabled) _volumeProvider.SetMute(recording.MultimediaDevice, recording.IsMuted);
        if (recording.IsFormatEnabled) _formatProvider.SetFormat(recording.MultimediaDevice, recording.StreamFormat);
        if (recording.IsAudioEnhancementsEnabled) _audioEnhancementProvider.SetAudioEnhancements(recording.MultimediaDevice, recording.AudioEnhancements);
        if (recording.IsChannelVolumeEnabled) SetChannelVolumes(recording.MultimediaDevice.DeviceId, recording.VolumeLeft, recording.VolumeRight);

        // Restore Channel Layout Configuration Masks and Driver Slider Percentages securely via Property Store
        if (recording.IsFormatEnabled && recording.StreamFormat.ChannelMask > 0)
        {
            var maskDict = new Dictionary<string, string> { { "{14242002-0320-4de4-9555-a7d82b73c286},3", $"int:{recording.StreamFormat.ChannelMask}" } };
            ApplyPropertiesViaPropertyStore(recording.MultimediaDevice.DeviceId, maskDict);
        }
        if (recording.IsApoSlidersEnabled)
        {
            ApplyPropertiesViaPropertyStore(recording.MultimediaDevice.DeviceId, recording.ApoSliders);
        }

        result.Messages.Add(AudioOperationMessage.Info(AudioMessageCode.ProfileApplied, "Recording audio profile applied."));
    }

    private void ApplySystemAudioProfile(SystemAudioProfile system, AudioProfileApplyResult result)
    {
        if (system.IsMonoAudioEnabled) _systemAudioProvider.SetMonoAudio(system.MonoAudio);
        result.Messages.Add(AudioOperationMessage.Info(AudioMessageCode.ProfileApplied, "System audio profile applied."));
    }

    private bool IsCurrentDefaultPlaybackDevice(AudioEndpointReference endpoint)
    {
        return IsSameDevice(endpoint, _defaultDeviceProvider.GetDefaultPlaybackDevice());
    }

    private bool IsCurrentDefaultConsolePlaybackDevice(AudioEndpointReference endpoint)
    {
        return IsSameDevice(endpoint, _defaultDeviceProvider.GetDefaultConsolePlaybackDevice());
    }

    private bool IsCurrentDefaultRecordingDevice(AudioEndpointReference endpoint)
    {
        return IsSameDevice(endpoint, _defaultDeviceProvider.GetDefaultRecordingDevice());
    }

    private bool IsCurrentDefaultConsoleRecordingDevice(AudioEndpointReference endpoint)
    {
        return IsSameDevice(endpoint, _defaultDeviceProvider.GetDefaultConsoleRecordingDevice());
    }

    private bool IsCurrentDefaultCommunicationsPlaybackDevice(AudioEndpointReference endpoint)
    {
        return IsSameDevice(endpoint, _defaultDeviceProvider.GetDefaultCommunicationsPlaybackDevice());
    }

    private bool IsCurrentDefaultCommunicationsRecordingDevice(AudioEndpointReference endpoint)
    {
        return IsSameDevice(endpoint, _defaultDeviceProvider.GetDefaultCommunicationsRecordingDevice());
    }

    private static bool IsSameDevice(AudioEndpointReference endpoint, AudioEndpointInfo current)
    {
        return !string.IsNullOrWhiteSpace(endpoint.DeviceId) &&
            endpoint.DeviceId.Equals(current.DeviceId, StringComparison.OrdinalIgnoreCase);
    }

    private void ValidatePlaybackProfile(PlaybackAudioProfile playback, AudioProfileValidationResult result)
    {
        if (!playback.IsPlaybackEnabled) return;

        WindowsAudioWrapper.Models.AudioEndpointInfo? device = null;
        if (playback.IsDefaultPlaybackDeviceEnabled || playback.IsVolumeEnabled || playback.IsMuteEnabled || playback.IsFormatEnabled || playback.IsAudioEnhancementsEnabled || playback.IsChannelVolumeEnabled || playback.IsDeviceDisabledTrackingEnabled)
        {
            device = ValidateEndpoint(playback.MultimediaDevice, AudioFlow.Render, nameof(playback.MultimediaDevice), result, playback.IsDeviceDisabledTrackingEnabled);
        }

        if (playback.IsDefaultConsolePlaybackDeviceEnabled) ValidateEndpoint(playback.ConsoleDevice, AudioFlow.Render, nameof(playback.ConsoleDevice), result, playback.IsDeviceDisabledTrackingEnabled);
        if (playback.IsDefaultCommunicationsPlaybackDeviceEnabled) ValidateEndpoint(playback.CommunicationsDevice, AudioFlow.Render, nameof(playback.CommunicationsDevice), result);
        ValidateVolume(playback.IsVolumeEnabled, playback.VolumePercent, result);
        
        if (playback.IsChannelVolumeEnabled)
        {
            ValidateVolume(true, playback.VolumeLeft, result);
            ValidateVolume(true, playback.VolumeRight, result);
        }

        if (device is not null)
        {
            if (playback.IsVolumeEnabled && !device.Capabilities.IsVolumeSupported) AddUnsupported(result, "Playback volume is not supported.");
            if (playback.IsMuteEnabled && !device.Capabilities.IsMuteSupported) AddUnsupported(result, "Playback mute is not supported.");
        }
    }

    private void ValidateRecordingProfile(RecordingAudioProfile recording, AudioProfileValidationResult result)
    {
        if (!recording.IsRecordingEnabled) return;

        WindowsAudioWrapper.Models.AudioEndpointInfo? device = null;
        if (recording.IsDefaultRecordingDeviceEnabled || recording.IsVolumeEnabled || recording.IsMuteEnabled || recording.IsFormatEnabled || recording.IsAudioEnhancementsEnabled || recording.IsChannelVolumeEnabled || recording.IsDeviceDisabledTrackingEnabled)
        {
            device = ValidateEndpoint(recording.MultimediaDevice, AudioFlow.Capture, nameof(recording.MultimediaDevice), result, recording.IsDeviceDisabledTrackingEnabled);
        }

        if (recording.IsDefaultConsoleRecordingDeviceEnabled) ValidateEndpoint(recording.ConsoleDevice, AudioFlow.Capture, nameof(recording.ConsoleDevice), result, recording.IsDeviceDisabledTrackingEnabled);
        if (recording.IsDefaultCommunicationsRecordingDeviceEnabled) ValidateEndpoint(recording.CommunicationsDevice, AudioFlow.Capture, nameof(recording.CommunicationsDevice), result);
        ValidateVolume(recording.IsVolumeEnabled, recording.VolumePercent, result);
        
        if (recording.IsChannelVolumeEnabled)
        {
            ValidateVolume(true, recording.VolumeLeft, result);
            ValidateVolume(true, recording.VolumeRight, result);
        }

        if (device is not null)
        {
            if (recording.IsVolumeEnabled && !device.Capabilities.IsVolumeSupported) AddUnsupported(result, "Recording volume is not supported.");
            if (recording.IsMuteEnabled && !device.Capabilities.IsMuteSupported) AddUnsupported(result, "Recording mute is not supported.");
        }
    }

    private void ValidateSystemAudioProfile(SystemAudioProfile system, AudioProfileValidationResult result)
    {
        if (!system.IsSystemAudioEnabled) return;
        if (!system.IsMonoAudioEnabled) result.Messages.Add(AudioOperationMessage.Warning(AudioMessageCode.NoEnabledSettings, "System audio profile is enabled, but no settings are enabled."));
    }

    private Models.AudioEndpointInfo? ValidateEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow, string propertyName, AudioProfileValidationResult result, bool allowDisabled = false)
    {
        if (!endpoint.IsEndpointEnabled)
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceMissing, $"{propertyName} does not contain a device reference."));
            return null;
        }

        if (endpoint.Flow != AudioFlow.Unknown && endpoint.Flow != expectedFlow)
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.InvalidAudioFlow, $"{propertyName} has flow {endpoint.Flow}, but expected {expectedFlow}."));
            return null;
        }

        Models.AudioEndpointInfo resolved = _deviceProvider.ResolveEndpoint(endpoint, expectedFlow);
        if (string.IsNullOrWhiteSpace(resolved.DeviceId))
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceNotFound, $"{propertyName} could not be found."));
            return null;
        }

        if (resolved.State.HasFlag(AudioDeviceState.NotPresent))
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceUnavailable, $"{propertyName} is not currently available. State: {resolved.State}."));
        }
        else if (resolved.State.HasFlag(AudioDeviceState.Disabled) && !allowDisabled)
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceUnavailable, $"{propertyName} is disabled. State: {resolved.State}."));
        }

        return resolved;
    }

    private static void ValidateVolume(bool isVolumeEnabled, decimal volumePercent, AudioProfileValidationResult result)
    {
        if (!isVolumeEnabled) return;
        if (volumePercent < 0 || volumePercent > 100) result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.InvalidVolume, "VolumePercent must be between 0 and 100."));
    }

    private static void AddUnsupported(AudioProfileValidationResult result, string message)
    {
        result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.UnsupportedSetting, message));
    }

    private static uint ReadRegistryPropertyUint(string deviceId, bool isRender, string keyName)
    {
        try
        {
            string dir = isRender ? "Render" : "Capture";
            using var key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\{dir}\\{deviceId}\\Properties");
            if (key?.GetValue(keyName) is byte[] bytes && bytes.Length >= 12)
            {
                return BitConverter.ToUInt32(bytes, 8); // Skip the unmanaged type descriptor prefix bytes
            }
        }
        catch {}
        return 0;
    }

    private static void CaptureRegistrySliders(string deviceId, bool isRender, Dictionary<string, string> targetDict)
    {
        try
        {
            string dir = isRender ? "Render" : "Capture";
            using var fxKey = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\{dir}\\{deviceId}\\FxProperties");
            if (fxKey != null)
            {
                foreach (string name in fxKey.GetValueNames())
                {
                    object? value = fxKey.GetValue(name);
                    if (value is int intVal)
                    {
                        targetDict[name] = $"int:{intVal}";
                    }
                    else if (value is byte[] bytes)
                    {
                        targetDict[name] = $"hex:{BitConverter.ToString(bytes).Replace("-", "")}";
                    }
                }
            }
        }
        catch {}
    }

    private void ApplyPropertiesViaPropertyStore(string deviceId, Dictionary<string, string> sliderDict)
    {
        if (sliderDict == null || sliderDict.Count == 0) return;
        try
        {
            var device = CoreAudioUtilities.GetDeviceById(deviceId);
            if (device.OpenPropertyStore(2, out IPropertyStore store) >= 0) // STGM_READWRITE (2)
            {
                foreach (var kvp in sliderDict)
                {
                    try
                    {
                        string cleanKey = kvp.Key.Replace("{", "").Replace("}", "");
                        string[] parts = cleanKey.Split(',');
                        if (parts.Length < 2) continue;

                        Guid fmtid = new Guid(parts[0]);
                        uint pid = uint.Parse(parts[1]);

                        PROPERTYKEY propKey = new PROPERTYKEY(fmtid, pid);
                        PROPVARIANT pv = default;

                        if (kvp.Value.StartsWith("int:"))
                        {
                            pv.vt = 19; // VT_UI4
                            pv.p = (IntPtr)uint.Parse(kvp.Value.Substring(4));
                            store.SetValue(in propKey, in pv);
                        }
                        else if (kvp.Value.StartsWith("hex:"))
                        {
                            pv.vt = 65; // VT_BLOB
                            byte[] raw = ConvertHexToBytes(kvp.Value.Substring(4));
                            IntPtr dataPtr = Marshal.AllocCoTaskMem(raw.Length);

                            try
                            {
                                Marshal.Copy(raw, 0, dataPtr, raw.Length);
                                pv.blobSize = (uint)raw.Length;
                                pv.blobData = dataPtr;

                                store.SetValue(in propKey, in pv);
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(dataPtr);
                            }
                        }
                    }
                    catch {}
                }
                store.Commit(); // Instantly deploy variables out to active Windows Audio graph lines
            }
        }
        catch {}
    }

    private static byte[] ConvertHexToBytes(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

internal static class StringExtensions
{
    public static string MakeSafe(string? input) => input ?? string.Empty;
}
