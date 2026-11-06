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
    private volatile bool _disposed;
    private bool _notificationsRegistered;
    private readonly Func<bool>? _tryRegisterNotifications;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly Action<string, bool> _setEndpointDisabled;
    private readonly Action<string, decimal, decimal> _setEndpointChannelVolumes;
    private readonly Action<string, Dictionary<string, string>> _applyEndpointProperties;

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
            lock (_notificationRegistrationLock)
            {
                EnsureNotificationRegistration();
                _audioDeviceConnected += value;
            }
        }
        remove
        {
            lock (_notificationRegistrationLock) _audioDeviceConnected -= value;
        }
    }

    /// <summary>
    /// Fires globally whenever any audio endpoint is plugged in, discovered, or becomes active in the system.
    /// </summary>
    public event EventHandler<AudioDeviceEventArgs>? AnyAudioDeviceConnected
    {
        add
        {
            lock (_notificationRegistrationLock)
            {
                EnsureNotificationRegistration();
                _anyAudioDeviceConnected += value;
            }
        }
        remove
        {
            lock (_notificationRegistrationLock) _anyAudioDeviceConnected -= value;
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
        ISystemAudioProvider systemAudioProvider,
        Func<bool>? tryRegisterNotifications = null,
        Action<string, bool>? setEndpointDisabled = null,
        Action<string, decimal, decimal>? setEndpointChannelVolumes = null,
        Action<string, Dictionary<string, string>>? applyEndpointProperties = null)
    {
        _tryRegisterNotifications = tryRegisterNotifications;
        _setEndpointDisabled = setEndpointDisabled ?? SetDeviceDisabled;
        _setEndpointChannelVolumes = setEndpointChannelVolumes ?? SetChannelVolumes;
        _applyEndpointProperties = applyEndpointProperties ?? ApplyPropertiesViaPropertyStore;
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
        => WaitForAudioDeviceToAppearAsync(device, timeoutMilliseconds).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<bool> WaitForAudioDeviceToAppearAsync(AudioEndpointReference device, int timeoutMilliseconds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        CancellationTokenSource linked;
        lock (_notificationRegistrationLock)
        {
            ThrowIfDisposed();
            linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCancellation.Token);
        }
        using (linked)
        using (SemaphoreSlim signal = new(0, 1))
        {
            object signalLock = new();
            bool closed = false;
            EventHandler<AudioDeviceEventArgs> handler = (_, _) =>
            {
                lock (signalLock) if (!closed && signal.CurrentCount == 0) signal.Release();
            };
            System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (linked.IsCancellationRequested) return false;
                if (CheckIfDeviceAlreadyActive(device)) return true;
                if (timeoutMilliseconds <= 0) return false;
                AnyAudioDeviceConnected += handler;
                while (!linked.IsCancellationRequested)
                {
                    if (CheckIfDeviceAlreadyActive(device)) return true;
                    int remaining = (int)Math.Max(0L, (long)timeoutMilliseconds - elapsed.ElapsedMilliseconds);
                    if (remaining == 0) return false;
                    // Notifications are hints; bounded rechecks also work when native registration fails.
                    await signal.WaitAsync(Math.Min(250, remaining), linked.Token).ConfigureAwait(false);
                }
                return false;
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested) { return false; }
            catch (ObjectDisposedException) when (_disposed) { return false; }
            finally
            {
                AnyAudioDeviceConnected -= handler;
                // A callback already copied by the event dispatcher must not release a disposed semaphore.
                lock (signalLock) closed = true;
            }
        }
    }

    /// <inheritdoc/>
    public void RegisterTargetDevice(AudioEndpointReference device)
    {
        ArgumentNullException.ThrowIfNull(device);
        lock (_notificationRegistrationLock)
        {
            EnsureNotificationRegistration();
            lock (_targetedDevicesLock)
            {
                if (!_targetedDevices.Any(target => target.Flow == device.Flow &&
                    string.Equals(target.DeviceId, device.DeviceId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(target.ContainerId, device.ContainerId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(target.FullName, device.FullName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(target.FriendlyName, device.FriendlyName, StringComparison.OrdinalIgnoreCase)))
                    _targetedDevices.Add(device);
            }
        }
    }

    /// <inheritdoc/>
    public void UnregisterTargetDevice(AudioEndpointReference device)
    {
        ArgumentNullException.ThrowIfNull(device);
        lock (_notificationRegistrationLock)
        {
            ThrowIfDisposed();
            lock (_targetedDevicesLock)
            {
                _targetedDevices.RemoveAll(target =>
                    (device.Flow == AudioFlow.Unknown || target.Flow == device.Flow) &&
                    (string.IsNullOrEmpty(device.DeviceId) || string.Equals(target.DeviceId, device.DeviceId, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(device.ContainerId) || string.Equals(target.ContainerId, device.ContainerId, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(device.FullName) || string.Equals(target.FullName, device.FullName, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(device.FriendlyName) || string.Equals(target.FriendlyName, device.FriendlyName, StringComparison.OrdinalIgnoreCase)));
            }
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

        if (profile.IsEndpointVisibilityTrackingEnabled) ApplyEndpointVisibilities(profile.EndpointVisibilities, result);
        if (profile.Playback.IsPlaybackEnabled) ApplyPlaybackProfile(profile.Playback, result);
        if (profile.Recording.IsRecordingEnabled) ApplyRecordingProfile(profile.Recording, result);
        // Mono audio is intentionally not applied as part of an audio profile.
        // Windows does not provide a non-disruptive, non-elevated way to apply it live.

        result.RecalculateSuccess();
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_notificationRegistrationLock)
        {
            if (_disposed) return;
            _disposed = true;
            _lifetimeCancellation.Cancel();
            _notificationRouter.DeviceNotificationReceived -= OnDeviceNotificationReceived;
            _audioDeviceConnected = null;
            _anyAudioDeviceConnected = null;
            lock (_targetedDevicesLock) _targetedDevices.Clear();
            try
            {
                if (_notificationsRegistered && _comEnumerator != null && _comCallbackPointer != IntPtr.Zero)
                    Marshal.ThrowExceptionForHR(_comEnumerator.UnregisterEndpointNotificationCallback(_comCallbackPointer));
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("WindowsAudioController/Dispose: Notification unregister failed: {0}", ex.Message); }
            finally
            {
                // GetOrCreateComInterfaceForObject returns an owned COM reference; release our reference after unregister.
                if (_comCallbackPointer != IntPtr.Zero) Marshal.Release(_comCallbackPointer);
                _comCallbackPointer = IntPtr.Zero;
                _comEnumerator = null;
                _notificationsRegistered = false;
            }
            foreach (object provider in new object[] { _deviceProvider, _defaultDeviceProvider, _volumeProvider, _formatProvider, _audioEnhancementProvider, _systemAudioProvider })
            {
                try { (provider as IDisposable)?.Dispose(); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("WindowsAudioController/Dispose: Provider cleanup failed: {0}", ex.Message); }
            }
            _lifetimeCancellation.Dispose();
        }
    }

    /// <summary>Offloads native notification processing so the COM callback returns promptly.</summary>
    private void OnDeviceNotificationReceived(string deviceId) => _ = Task.Run(() => ProcessDeviceNotification(deviceId));

    /// <summary>Processes a notification using live inventory; invokes subscribers outside all internal locks.</summary>
    internal void ProcessDeviceNotification(string deviceId)
    {
        AudioDeviceEventArgs? args = null;
        EventHandler<AudioDeviceEventArgs>? any = null;
        EventHandler<AudioDeviceEventArgs>? targeted = null;
        try
        {
            lock (_notificationRegistrationLock)
            {
                if (_disposed) return;
                AudioEndpointInfo[] inventory = _deviceProvider.GetPlaybackDevices(AudioDeviceState.All)
                    .Concat(_deviceProvider.GetRecordingDevices(AudioDeviceState.All)).ToArray();
                AudioEndpointInfo? found = inventory.FirstOrDefault(device => string.Equals(device.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
                if (found == null || !found.State.HasFlag(AudioDeviceState.Active)) return;
                args = new() { DeviceId = found.DeviceId, ContainerId = found.ContainerId, FriendlyName = found.FriendlyName, FullName = found.FullName, Flow = found.Flow };
                any = _anyAudioDeviceConnected;
                AudioEndpointReference[] targets;
                lock (_targetedDevicesLock) targets = _targetedDevices.ToArray();
                foreach (AudioEndpointReference target in targets)
                {
                    try
                    {
                        AudioEndpointInfo match = AudioEndpointResolver.Resolve(target, target.Flow, inventory);
                        if (match.Flow == found.Flow && string.Equals(match.DeviceId, found.DeviceId, StringComparison.OrdinalIgnoreCase) && match.State.HasFlag(AudioDeviceState.Active))
                        { targeted = _audioDeviceConnected; break; }
                    }
                    catch (AmbiguousAudioEndpointException) { /* An ambiguous target must not produce an arrival notification. */ }
                }
            }
        }
        catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("WindowsAudioController/ProcessDeviceNotification: Could not refresh endpoints: {0}", ex.Message); return; }
        foreach (EventHandler<AudioDeviceEventArgs>? subscribers in new[] { any, targeted })
        {
            if (subscribers == null) continue;
            foreach (EventHandler<AudioDeviceEventArgs> subscriber in subscribers.GetInvocationList())
            {
                if (_disposed) return;
                try { subscriber(this, args!); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("WindowsAudioController/ProcessDeviceNotification: Subscriber failed: {0}", ex.Message); }
            }
        }
    }

    /// <summary>Attempts notification registration; failed attempts remain retryable and waits retain polling.</summary>
    private void EnsureNotificationRegistration()
    {
        lock (_notificationRegistrationLock)
        {
            ThrowIfDisposed();
            if (_notificationsRegistered) return;
            try
            {
                if (_tryRegisterNotifications != null)
                { _notificationsRegistered = _tryRegisterNotifications(); return; }
                _comEnumerator = CoreAudioUtilities.CreateEnumerator();
                _comCallbackPointer = ComWrappers.GetOrCreateComInterfaceForObject(_notificationRouter, CreateComInterfaceFlags.None);
                Marshal.ThrowExceptionForHR(_comEnumerator.RegisterEndpointNotificationCallback(_comCallbackPointer));
                _notificationsRegistered = true;
            }
            catch (Exception ex)
            {
                if (_comCallbackPointer != IntPtr.Zero) Marshal.Release(_comCallbackPointer);
                _comCallbackPointer = IntPtr.Zero;
                _comEnumerator = null;
                _notificationsRegistered = false;
                System.Diagnostics.Trace.TraceWarning("WindowsAudioController/EnsureNotificationRegistration: Using availability rechecks after registration failure: {0}", ex.Message);
            }
        }
    }

    /// <summary>Checks unique live identity and active state while protecting provider lifetime.</summary>
    private bool CheckIfDeviceAlreadyActive(AudioEndpointReference target)
    {
        lock (_notificationRegistrationLock)
        {
            ThrowIfDisposed();
            IEnumerable<AudioEndpointInfo> inventory = target.Flow switch
            {
                AudioFlow.Render => _deviceProvider.GetPlaybackDevices(AudioDeviceState.All),
                AudioFlow.Capture => _deviceProvider.GetRecordingDevices(AudioDeviceState.All),
                _ => _deviceProvider.GetPlaybackDevices(AudioDeviceState.All).Concat(_deviceProvider.GetRecordingDevices(AudioDeviceState.All))
            };
            try { return AudioEndpointResolver.Resolve(target, target.Flow, inventory).State.HasFlag(AudioDeviceState.Active); }
            catch (AmbiguousAudioEndpointException) { return false; }
        }
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

    private void ApplyEndpointVisibilities(IReadOnlyCollection<AudioEndpointVisibility> endpointVisibilities, AudioProfileApplyResult result)
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

            AudioEndpointReference endpointReference = new()
            {
                DeviceId = endpointVisibility.DeviceId ?? string.Empty,
                ContainerId = endpointVisibility.ContainerId ?? string.Empty,
                FriendlyName = endpointVisibility.FriendlyName ?? string.Empty,
                Flow = endpointVisibility.Flow
            };

            AudioEndpointInfo resolvedEndpoint = ResolveEndpointForApply(endpointReference, endpointVisibility.Flow, result);
            if (resolvedEndpoint.State.HasFlag(AudioDeviceState.NotPresent))
            {
                result.Messages.Add(AudioOperationMessage.Info(
                    AudioMessageCode.DeviceUnavailable,
                    $"Endpoint visibility skipped because the captured {endpointVisibility.Flow} endpoint is not present: {endpointVisibility.FriendlyName ?? string.Empty}"));
                continue;
            }

            TryApplySetting(
                result,
                $"endpoint visibility for {endpointVisibility.FriendlyName ?? string.Empty}",
                () => _setEndpointDisabled(resolvedEndpoint.DeviceId, endpointVisibility.IsDisabled));
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
            uint channelMask = defaultDevice.HardwareDetails?.PhysicalSpeakersMask ?? 0;
            SpeakerConfigurationProfile speakerConfiguration = new()
            {
                ChannelMask = channelMask != 0 ? channelMask : playback.StreamFormat.ChannelMask,
                FullRangeSpeakersMask = defaultDevice.HardwareDetails?.FullRangeSpeakersMask ?? 0
            };
            playback.SpeakerConfiguration = speakerConfiguration;
            playback.IsSpeakerConfigurationEnabled = speakerConfiguration.IsConfigured;

            playback.IsAudioEnhancementsEnabled = true;
            playback.AudioEnhancements = _audioEnhancementProvider.GetAudioEnhancements(defaultDevice.DeviceId);
            
            CaptureChannelVolumes(defaultDevice.DeviceId, out float l, out float r);
            playback.VolumeLeft = (decimal)(l * 100f);
            playback.VolumeRight = (decimal)(r * 100f);
            playback.IsChannelVolumeEnabled = true;
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

        CaptureRoleEndpointFxProperties(playback.MultimediaDevice, true);
        CaptureRoleEndpointFxProperties(playback.ConsoleDevice, true);
        CaptureRoleEndpointFxProperties(playback.CommunicationsDevice, true);
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

            CaptureChannelVolumes(defaultDevice.DeviceId, out float l, out float r);
            recording.VolumeLeft = (decimal)(l * 100f);
            recording.VolumeRight = (decimal)(r * 100f);
            recording.IsChannelVolumeEnabled = true;
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

        CaptureRoleEndpointFxProperties(recording.MultimediaDevice, false);
        CaptureRoleEndpointFxProperties(recording.ConsoleDevice, false);
        CaptureRoleEndpointFxProperties(recording.CommunicationsDevice, false);
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

    private void ApplyPlaybackProfile(PlaybackAudioProfile playback, AudioProfileApplyResult result)
    {
        AudioEndpointReference multimedia = ResolveReferenceForApply(playback.MultimediaDevice, AudioFlow.Render, result);
        AudioEndpointReference console = ResolveReferenceForApply(playback.ConsoleDevice, AudioFlow.Render, result);
        AudioEndpointReference communications = ResolveReferenceForApply(playback.CommunicationsDevice, AudioFlow.Render, result);
        if (playback.IsDefaultConsolePlaybackDeviceEnabled && CanApplyEndpointSetting(console, AudioFlow.Render, "playback console device", result) && !IsCurrentDefaultConsolePlaybackDevice(console))
        {
            TryApplySetting(result, "playback console default device", () => _defaultDeviceProvider.SetDefaultConsolePlaybackDevice(console));
        }

        bool canApplyMultimediaDevice = CanApplyEndpointSetting(multimedia, AudioFlow.Render, "playback multimedia device", result);
        if (playback.IsDefaultPlaybackDeviceEnabled && canApplyMultimediaDevice && !IsCurrentDefaultPlaybackDevice(multimedia))
        {
            TryApplySetting(result, "playback multimedia default device", () => _defaultDeviceProvider.SetDefaultPlaybackDevice(multimedia));
        }

        if (playback.IsDefaultCommunicationsPlaybackDeviceEnabled && CanApplyEndpointSetting(communications, AudioFlow.Render, "playback communications device", result) && !IsCurrentDefaultCommunicationsPlaybackDevice(communications))
        {
            TryApplySetting(result, "playback communications default device", () => _defaultDeviceProvider.SetDefaultCommunicationsPlaybackDevice(communications));
        }

        if (playback.IsSpeakerConfigurationEnabled && canApplyMultimediaDevice && playback.SpeakerConfiguration.IsConfigured)
        {
            Dictionary<string, string> speakerProperties = new()
            {
                { "{1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E},3", $"int:{playback.SpeakerConfiguration.ChannelMask}" },
                { "{1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E},6", $"int:{playback.SpeakerConfiguration.FullRangeSpeakersMask}" }
            };
            TryApplySetting(result, "playback speaker configuration", () => _applyEndpointProperties(multimedia.DeviceId, speakerProperties));
        }

        if (playback.IsVolumeEnabled && canApplyMultimediaDevice && IsValidVolume(playback.VolumePercent, "playback volume", result)) TryApplySetting(result, "playback volume", () => _volumeProvider.SetVolumePercent(multimedia, playback.VolumePercent));
        if (playback.IsMuteEnabled && canApplyMultimediaDevice) TryApplySetting(result, "playback mute", () => _volumeProvider.SetMute(multimedia, playback.IsMuted));
        if (playback.IsFormatEnabled && canApplyMultimediaDevice)
        {
            AudioFormatProfile formatToApply = new()
            {
                SampleRate = playback.StreamFormat.SampleRate,
                BitsPerSample = playback.StreamFormat.BitsPerSample,
                Channels = playback.StreamFormat.Channels,
                ChannelMask = playback.IsSpeakerConfigurationEnabled && playback.SpeakerConfiguration.IsConfigured
                    ? playback.SpeakerConfiguration.ChannelMask
                    : playback.StreamFormat.ChannelMask,
                SampleFormat = playback.StreamFormat.SampleFormat,
                Mode = playback.StreamFormat.Mode
            };
            TryApplySetting(result, "playback format", () => _formatProvider.SetFormat(multimedia, formatToApply));
        }
        if (playback.IsAudioEnhancementsEnabled && canApplyMultimediaDevice) TryApplySetting(result, "playback audio enhancements", () => _audioEnhancementProvider.SetAudioEnhancements(multimedia, playback.AudioEnhancements));
        if (playback.IsChannelVolumeEnabled && canApplyMultimediaDevice &&
            IsValidVolume(playback.VolumeLeft, "playback left channel volume", result) &&
            IsValidVolume(playback.VolumeRight, "playback right channel volume", result))
        {
            TryApplySetting(result, "playback channel volumes", () => _setEndpointChannelVolumes(multimedia.DeviceId, playback.VolumeLeft, playback.VolumeRight));
        }
        
        ApplyRoleEndpointFxProperties(multimedia, AudioFlow.Render, "playback multimedia APO properties", result);
        ApplyRoleEndpointFxProperties(console, AudioFlow.Render, "playback console APO properties", result);
        ApplyRoleEndpointFxProperties(communications, AudioFlow.Render, "playback communications APO properties", result);

        result.Messages.Add(AudioOperationMessage.Info(AudioMessageCode.ProfileApplied, "Playback audio profile applied."));
    }

    private void ApplyRecordingProfile(RecordingAudioProfile recording, AudioProfileApplyResult result)
    {
        AudioEndpointReference multimedia = ResolveReferenceForApply(recording.MultimediaDevice, AudioFlow.Capture, result);
        AudioEndpointReference console = ResolveReferenceForApply(recording.ConsoleDevice, AudioFlow.Capture, result);
        AudioEndpointReference communications = ResolveReferenceForApply(recording.CommunicationsDevice, AudioFlow.Capture, result);
        if (recording.IsDefaultConsoleRecordingDeviceEnabled && CanApplyEndpointSetting(console, AudioFlow.Capture, "recording console device", result) && !IsCurrentDefaultConsoleRecordingDevice(console))
        {
            TryApplySetting(result, "recording console default device", () => _defaultDeviceProvider.SetDefaultConsoleRecordingDevice(console));
        }

        bool canApplyMultimediaDevice = CanApplyEndpointSetting(multimedia, AudioFlow.Capture, "recording multimedia device", result);
        if (recording.IsDefaultRecordingDeviceEnabled && canApplyMultimediaDevice && !IsCurrentDefaultRecordingDevice(multimedia))
        {
            TryApplySetting(result, "recording multimedia default device", () => _defaultDeviceProvider.SetDefaultRecordingDevice(multimedia));
        }

        if (recording.IsDefaultCommunicationsRecordingDeviceEnabled && CanApplyEndpointSetting(communications, AudioFlow.Capture, "recording communications device", result) && !IsCurrentDefaultCommunicationsRecordingDevice(communications))
        {
            TryApplySetting(result, "recording communications default device", () => _defaultDeviceProvider.SetDefaultCommunicationsRecordingDevice(communications));
        }

        if (recording.IsVolumeEnabled && canApplyMultimediaDevice && IsValidVolume(recording.VolumePercent, "recording volume", result)) TryApplySetting(result, "recording volume", () => _volumeProvider.SetVolumePercent(multimedia, recording.VolumePercent));
        if (recording.IsMuteEnabled && canApplyMultimediaDevice) TryApplySetting(result, "recording mute", () => _volumeProvider.SetMute(multimedia, recording.IsMuted));
        if (recording.IsFormatEnabled && canApplyMultimediaDevice) TryApplySetting(result, "recording format", () => _formatProvider.SetFormat(multimedia, recording.StreamFormat));
        if (recording.IsAudioEnhancementsEnabled && canApplyMultimediaDevice) TryApplySetting(result, "recording audio enhancements", () => _audioEnhancementProvider.SetAudioEnhancements(multimedia, recording.AudioEnhancements));
        if (recording.IsChannelVolumeEnabled && canApplyMultimediaDevice &&
            IsValidVolume(recording.VolumeLeft, "recording left channel volume", result) &&
            IsValidVolume(recording.VolumeRight, "recording right channel volume", result))
        {
            TryApplySetting(result, "recording channel volumes", () => _setEndpointChannelVolumes(multimedia.DeviceId, recording.VolumeLeft, recording.VolumeRight));
        }

        // Restore Channel Layout Configuration Masks securely via Property Store
        if (recording.IsFormatEnabled && canApplyMultimediaDevice && recording.StreamFormat.ChannelMask > 0)
        {
            var maskDict = new Dictionary<string, string> { { "{14242002-0320-4de4-9555-a7d82b73c286},3", $"int:{recording.StreamFormat.ChannelMask}" } };
            TryApplySetting(result, "recording channel mask property", () => _applyEndpointProperties(multimedia.DeviceId, maskDict));
        }
        ApplyRoleEndpointFxProperties(multimedia, AudioFlow.Capture, "recording multimedia APO properties", result);
        ApplyRoleEndpointFxProperties(console, AudioFlow.Capture, "recording console APO properties", result);
        ApplyRoleEndpointFxProperties(communications, AudioFlow.Capture, "recording communications APO properties", result);

        result.Messages.Add(AudioOperationMessage.Info(AudioMessageCode.ProfileApplied, "Recording audio profile applied."));
    }

    private static void CaptureRoleEndpointFxProperties(AudioEndpointReference endpoint, bool isRender)
    {
        endpoint.ApoFxProperties ??= new Dictionary<string, string>();
        endpoint.ApoFxProperties.Clear();

        if (string.IsNullOrWhiteSpace(endpoint.DeviceId))
        {
            return;
        }

        CaptureApoFxProperties(endpoint.DeviceId, isRender, endpoint.ApoFxProperties);
    }

    private void ApplyRoleEndpointFxProperties(AudioEndpointReference endpoint, AudioFlow expectedFlow, string settingName, AudioProfileApplyResult result)
    {
        endpoint.ApoFxProperties ??= new Dictionary<string, string>();
        if (endpoint.ApoFxProperties.Count == 0)
        {
            return;
        }

        if (!CanApplyEndpointSetting(endpoint, expectedFlow, settingName, result))
        {
            return;
        }

        TryApplySetting(result, settingName, () => _applyEndpointProperties(endpoint.DeviceId, endpoint.ApoFxProperties));
    }

    /// <summary>Copies saved per-role settings onto a resolved live identity without modifying the saved reference.</summary>
    private AudioEndpointReference ResolveReferenceForApply(AudioEndpointReference endpoint, AudioFlow flow, AudioProfileApplyResult result)
    {
        AudioEndpointInfo resolved = ResolveEndpointForApply(endpoint, flow, result);
        return new AudioEndpointReference
        {
            DeviceId = resolved.DeviceId, ContainerId = resolved.ContainerId, FriendlyName = resolved.FriendlyName,
            FullName = resolved.FullName, Flow = endpoint.Flow == AudioFlow.Unknown ? flow : endpoint.Flow,
            IsEndpointEnabled = endpoint.IsEndpointEnabled, HardwareDetails = endpoint.HardwareDetails,
            ApoFxProperties = new Dictionary<string, string>(endpoint.ApoFxProperties ?? new())
        };
    }

    private bool CanApplyEndpointSetting(AudioEndpointReference endpoint, AudioFlow expectedFlow, string settingName, AudioProfileApplyResult result)
    {
        if (endpoint.Flow != AudioFlow.Unknown && endpoint.Flow != expectedFlow)
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.InvalidAudioFlow, $"{settingName} has the wrong endpoint direction."));
            return false;
        }
        if (string.IsNullOrWhiteSpace(endpoint.DeviceId) && string.IsNullOrWhiteSpace(endpoint.ContainerId) && string.IsNullOrWhiteSpace(endpoint.FullName) && string.IsNullOrWhiteSpace(endpoint.FriendlyName))
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceMissing, $"{settingName} could not be applied because no endpoint reference was provided."));
            return false;
        }

        AudioEndpointInfo resolvedEndpoint = ResolveEndpointForApply(endpoint, expectedFlow, result, recordResolution: false);
        if (string.IsNullOrWhiteSpace(resolvedEndpoint.DeviceId) || resolvedEndpoint.State.HasFlag(AudioDeviceState.NotPresent))
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceUnavailable, $"{settingName} could not be applied because the endpoint is not present."));
            return false;
        }

        if (resolvedEndpoint.State.HasFlag(AudioDeviceState.Disabled))
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceUnavailable, $"{settingName} could not be applied because the endpoint is disabled."));
            return false;
        }

        return true;
    }

    private AudioEndpointInfo ResolveEndpointForApply(AudioEndpointReference endpoint, AudioFlow expectedFlow, AudioProfileApplyResult? result = null, bool recordResolution = true)
    {
        try
        {
            if (endpoint.Flow != AudioFlow.Unknown && endpoint.Flow != expectedFlow)
                return AudioEndpointResolver.Missing(endpoint, expectedFlow);
            AudioEndpointInfo resolved = _deviceProvider.ResolveEndpoint(endpoint, expectedFlow);
            if (recordResolution && result != null && !resolved.State.HasFlag(AudioDeviceState.NotPresent) && !string.IsNullOrWhiteSpace(endpoint.DeviceId) && !string.IsNullOrWhiteSpace(resolved.DeviceId))
                result.ResolvedDeviceIds[endpoint.DeviceId] = resolved.DeviceId;
            return resolved;
        }
        catch (AmbiguousAudioEndpointException ex)
        {
            result?.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.AmbiguousEndpoint, ex.Message));
            return AudioEndpointResolver.Missing(endpoint, expectedFlow);
        }
        catch
        {
            return new AudioEndpointInfo
            {
                DeviceId = endpoint.DeviceId,
                ContainerId = endpoint.ContainerId,
                FriendlyName = endpoint.FriendlyName,
                Flow = expectedFlow,
                State = AudioDeviceState.NotPresent
            };
        }
    }

    private static void TryApplySetting(AudioProfileApplyResult result, string settingName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.UnexpectedError, $"{settingName} could not be applied: {ex.Message}"));
        }
    }

    private static bool IsValidVolume(decimal volumePercent, string settingName, AudioProfileApplyResult result)
    {
        if (volumePercent >= 0 && volumePercent <= 100)
        {
            return true;
        }

        result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.InvalidVolume, $"{settingName} must be between 0 and 100."));
        return false;
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
        if (playback.IsDefaultPlaybackDeviceEnabled || playback.IsVolumeEnabled || playback.IsMuteEnabled || playback.IsFormatEnabled || playback.IsSpeakerConfigurationEnabled || playback.IsAudioEnhancementsEnabled || playback.IsChannelVolumeEnabled)
        {
            device = ValidateEndpoint(playback.MultimediaDevice, AudioFlow.Render, nameof(playback.MultimediaDevice), result);
        }

        if (playback.IsDefaultConsolePlaybackDeviceEnabled) ValidateEndpoint(playback.ConsoleDevice, AudioFlow.Render, nameof(playback.ConsoleDevice), result);
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
        if (recording.IsDefaultRecordingDeviceEnabled || recording.IsVolumeEnabled || recording.IsMuteEnabled || recording.IsFormatEnabled || recording.IsAudioEnhancementsEnabled || recording.IsChannelVolumeEnabled)
        {
            device = ValidateEndpoint(recording.MultimediaDevice, AudioFlow.Capture, nameof(recording.MultimediaDevice), result);
        }

        if (recording.IsDefaultConsoleRecordingDeviceEnabled) ValidateEndpoint(recording.ConsoleDevice, AudioFlow.Capture, nameof(recording.ConsoleDevice), result);
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

    private Models.AudioEndpointInfo? ValidateEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow, string propertyName, AudioProfileValidationResult result, bool allowDisabled = false)
    {
        if (string.IsNullOrWhiteSpace(endpoint.DeviceId) && string.IsNullOrWhiteSpace(endpoint.ContainerId) && string.IsNullOrWhiteSpace(endpoint.FullName) && string.IsNullOrWhiteSpace(endpoint.FriendlyName))
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceMissing, $"{propertyName} does not contain a device reference."));
            return null;
        }

        if (endpoint.Flow != AudioFlow.Unknown && endpoint.Flow != expectedFlow)
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.InvalidAudioFlow, $"{propertyName} has flow {endpoint.Flow}, but expected {expectedFlow}."));
            return null;
        }

        Models.AudioEndpointInfo resolved;
        try { resolved = _deviceProvider.ResolveEndpoint(endpoint, expectedFlow); }
        catch (AmbiguousAudioEndpointException ex)
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.AmbiguousEndpoint, ex.Message));
            return null;
        }
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

    private static void CaptureApoFxProperties(string deviceId, bool isRender, Dictionary<string, string> targetDict)
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

    private void ApplyPropertiesViaPropertyStore(string deviceId, Dictionary<string, string> properties)
    {
        if (properties == null || properties.Count == 0) return;
        try
        {
            var device = CoreAudioUtilities.GetDeviceById(deviceId);
            if (device.OpenPropertyStore(2, out IPropertyStore store) >= 0) // STGM_READWRITE (2)
            {
                foreach (var kvp in properties)
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
