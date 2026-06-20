namespace WindowsAudioWrapper;

using System;
using System.Collections.Generic;
using WindowsAudioWrapper.Models;
using WindowsAudioWrapper.Providers;

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
    public AudioEndpointInfo GetDefaultRecordingDevice() { ThrowIfDisposed(); return _defaultDeviceProvider.GetDefaultRecordingDevice(); }
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

    /// <inheritdoc/>
    public void SetDefaultRecordingDevice(string deviceId)
    {
        ThrowIfDisposed();
        var reference = new AudioEndpointReference { DeviceId = deviceId ?? string.Empty };
        _defaultDeviceProvider.SetDefaultRecordingDevice(reference);
    }

    // --- Macro Profile Processing Block ---

    /// <inheritdoc/>
    public AudioProfile GetCurrentProfile()
    {
        ThrowIfDisposed();
        AudioProfile profile = new();
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
        (_deviceProvider as IDisposable)?.Dispose();
        (_defaultDeviceProvider as IDisposable)?.Dispose();
        (_volumeProvider as IDisposable)?.Dispose();
        (_formatProvider as IDisposable)?.Dispose();
        (_audioEnhancementProvider as IDisposable)?.Dispose();
        (_systemAudioProvider as IDisposable)?.Dispose();
        _disposed = true;
    }

    private void CapturePlaybackProfile(PlaybackAudioProfile playback)
    {
        AudioEndpointInfo defaultDevice = GetDefaultPlaybackDevice();
        AudioEndpointInfo communicationsDevice = GetDefaultCommunicationsPlaybackDevice();

        playback.IsPlaybackEnabled = true;

        if (defaultDevice.IsAvailable)
        {
            playback.IsDefaultPlaybackDeviceEnabled = true;
            playback.TargetDevice = AudioEndpointReference.FromEndpointInfo(defaultDevice);

            playback.IsVolumeEnabled = defaultDevice.Capabilities.IsVolumeSupported;
            if (playback.IsVolumeEnabled) playback.VolumePercent = defaultDevice.VolumePercent;

            playback.IsMuteEnabled = defaultDevice.Capabilities.IsMuteSupported;
            if (playback.IsMuteEnabled) playback.IsMuted = defaultDevice.IsMuted;

            playback.IsFormatEnabled = true;
            playback.StreamFormat = _formatProvider.GetFormat(defaultDevice.DeviceId);

            playback.IsAudioEnhancementsEnabled = true;
            playback.AudioEnhancements = _audioEnhancementProvider.GetAudioEnhancements(defaultDevice.DeviceId);
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
        AudioEndpointInfo communicationsDevice = GetDefaultCommunicationsRecordingDevice();

        recording.IsRecordingEnabled = true;

        if (defaultDevice.IsAvailable)
        {
            recording.IsDefaultRecordingDeviceEnabled = true;
            recording.TargetDevice = AudioEndpointReference.FromEndpointInfo(defaultDevice);

            recording.IsVolumeEnabled = defaultDevice.Capabilities.IsVolumeSupported;
            if (recording.IsVolumeEnabled) recording.VolumePercent = defaultDevice.VolumePercent;

            recording.IsMuteEnabled = defaultDevice.Capabilities.IsMuteSupported;
            if (recording.IsMuteEnabled) recording.IsMuted = defaultDevice.IsMuted;

            recording.IsFormatEnabled = true;
            recording.StreamFormat = _formatProvider.GetFormat(defaultDevice.DeviceId);

            recording.IsAudioEnhancementsEnabled = true;
            recording.AudioEnhancements = _audioEnhancementProvider.GetAudioEnhancements(defaultDevice.DeviceId);
        }

        if (communicationsDevice.IsAvailable)
        {
            recording.IsDefaultCommunicationsRecordingDeviceEnabled = true;
            recording.CommunicationsDevice = AudioEndpointReference.FromEndpointInfo(communicationsDevice);
        }
    }

    private void CaptureSystemAudioProfile(SystemAudioProfile system)
    {
        system.IsSystemAudioEnabled = true;
        system.IsMonoAudioEnabled = true;
        system.MonoAudio = _systemAudioProvider.GetMonoAudio();
    }

    private void ApplyPlaybackProfile(PlaybackAudioProfile playback, AudioProfileApplyResult result)
    {
        if (playback.IsDefaultPlaybackDeviceEnabled) _defaultDeviceProvider.SetDefaultPlaybackDevice(playback.TargetDevice);
        if (playback.IsDefaultCommunicationsPlaybackDeviceEnabled) _defaultDeviceProvider.SetDefaultCommunicationsPlaybackDevice(playback.CommunicationsDevice);
        if (playback.IsVolumeEnabled) _volumeProvider.SetVolumePercent(playback.TargetDevice, playback.VolumePercent);
        if (playback.IsMuteEnabled) _volumeProvider.SetMute(playback.TargetDevice, playback.IsMuted);
        if (playback.IsFormatEnabled) _formatProvider.SetFormat(playback.TargetDevice, playback.StreamFormat);
        if (playback.IsAudioEnhancementsEnabled) _audioEnhancementProvider.SetAudioEnhancements(playback.TargetDevice, playback.AudioEnhancements);

        result.Messages.Add(AudioOperationMessage.Info(AudioMessageCode.ProfileApplied, "Playback audio profile applied."));
    }

    private void ApplyRecordingProfile(RecordingAudioProfile recording, AudioProfileApplyResult result)
    {
        if (recording.IsDefaultRecordingDeviceEnabled) _defaultDeviceProvider.SetDefaultRecordingDevice(recording.TargetDevice);
        if (recording.IsDefaultCommunicationsRecordingDeviceEnabled) _defaultDeviceProvider.SetDefaultCommunicationsRecordingDevice(recording.CommunicationsDevice);
        if (recording.IsVolumeEnabled) _volumeProvider.SetVolumePercent(recording.TargetDevice, recording.VolumePercent);
        if (recording.IsMuteEnabled) _volumeProvider.SetMute(recording.TargetDevice, recording.IsMuted);
        if (recording.IsFormatEnabled) _formatProvider.SetFormat(recording.TargetDevice, recording.StreamFormat);
        if (recording.IsAudioEnhancementsEnabled) _audioEnhancementProvider.SetAudioEnhancements(recording.TargetDevice, recording.AudioEnhancements);

        result.Messages.Add(AudioOperationMessage.Info(AudioMessageCode.ProfileApplied, "Recording audio profile applied."));
    }

    private void ApplySystemAudioProfile(SystemAudioProfile system, AudioProfileApplyResult result)
    {
        if (system.IsMonoAudioEnabled) _systemAudioProvider.SetMonoAudio(system.MonoAudio);
        result.Messages.Add(AudioOperationMessage.Info(AudioMessageCode.ProfileApplied, "System audio profile applied."));
    }

    private void ValidatePlaybackProfile(PlaybackAudioProfile playback, AudioProfileValidationResult result)
    {
        if (!playback.IsPlaybackEnabled) return;

        WindowsAudioWrapper.Models.AudioEndpointInfo? device = null;
        if (playback.IsDefaultPlaybackDeviceEnabled || playback.IsVolumeEnabled || playback.IsMuteEnabled || playback.IsFormatEnabled || playback.IsAudioEnhancementsEnabled)
        {
            device = ValidateEndpoint(playback.TargetDevice, AudioFlow.Render, nameof(playback.TargetDevice), result);
        }

        if (playback.IsDefaultCommunicationsPlaybackDeviceEnabled) ValidateEndpoint(playback.CommunicationsDevice, AudioFlow.Render, nameof(playback.CommunicationsDevice), result);
        ValidateVolume(playback.IsVolumeEnabled, playback.VolumePercent, result);

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
        if (recording.IsDefaultRecordingDeviceEnabled || recording.IsVolumeEnabled || recording.IsMuteEnabled || recording.IsFormatEnabled || recording.IsAudioEnhancementsEnabled)
        {
            device = ValidateEndpoint(recording.TargetDevice, AudioFlow.Capture, nameof(recording.TargetDevice), result);
        }

        if (recording.IsDefaultCommunicationsRecordingDeviceEnabled) ValidateEndpoint(recording.CommunicationsDevice, AudioFlow.Capture, nameof(recording.CommunicationsDevice), result);
        ValidateVolume(recording.IsVolumeEnabled, recording.VolumePercent, result);

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

    private Models.AudioEndpointInfo? ValidateEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow, string propertyName, AudioProfileValidationResult result)
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

        if (resolved.State.HasFlag(AudioDeviceState.Disabled) || resolved.State.HasFlag(AudioDeviceState.NotPresent))
        {
            result.Messages.Add(AudioOperationMessage.Error(AudioMessageCode.DeviceUnavailable, $"{propertyName} is not currently available. State: {resolved.State}."));
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

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}