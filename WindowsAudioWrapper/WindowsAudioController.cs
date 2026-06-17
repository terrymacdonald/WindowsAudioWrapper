namespace WindowsAudioWrapper;

using WindowsAudioWrapper.Models;
using WindowsAudioWrapper.Providers;

/// <summary>
/// Main controller for Windows audio profile capture, validation, and application.
/// </summary>
public sealed class WindowsAudioController : IWindowsAudioController
{
    private readonly IAudioDeviceProvider _deviceProvider;
    private readonly IDefaultAudioDeviceProvider _defaultDeviceProvider;
    private readonly IAudioVolumeProvider _volumeProvider;
    private readonly IAudioFormatProvider _formatProvider;
    private readonly ISpatialSoundProvider _spatialSoundProvider;
    private readonly IAudioEnhancementProvider _audioEnhancementProvider;
    private readonly ISystemAudioProvider _systemAudioProvider;
    private bool _disposed;

    public WindowsAudioController()
        : this(
            new AudioDeviceProvider(),
            new DefaultAudioDeviceProvider(),
            new AudioVolumeProvider(),
            new AudioFormatProvider(),
            new SpatialSoundProvider(),
            new AudioEnhancementProvider(),
            new SystemAudioProvider())
    {
    }

    internal WindowsAudioController(
        IAudioDeviceProvider deviceProvider,
        IDefaultAudioDeviceProvider defaultDeviceProvider,
        IAudioVolumeProvider volumeProvider,
        IAudioFormatProvider formatProvider,
        ISpatialSoundProvider spatialSoundProvider,
        IAudioEnhancementProvider audioEnhancementProvider,
        ISystemAudioProvider systemAudioProvider)
    {
        _deviceProvider = deviceProvider ?? throw new ArgumentNullException(nameof(deviceProvider));
        _defaultDeviceProvider = defaultDeviceProvider ?? throw new ArgumentNullException(nameof(defaultDeviceProvider));
        _volumeProvider = volumeProvider ?? throw new ArgumentNullException(nameof(volumeProvider));
        _formatProvider = formatProvider ?? throw new ArgumentNullException(nameof(formatProvider));
        _spatialSoundProvider = spatialSoundProvider ?? throw new ArgumentNullException(nameof(spatialSoundProvider));
        _audioEnhancementProvider = audioEnhancementProvider ?? throw new ArgumentNullException(nameof(audioEnhancementProvider));
        _systemAudioProvider = systemAudioProvider ?? throw new ArgumentNullException(nameof(systemAudioProvider));
    }

    public IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged)
    {
        ThrowIfDisposed();
        return _deviceProvider.GetPlaybackDevices(states);
    }

    public IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(
        AudioDeviceState states = AudioDeviceState.Active | AudioDeviceState.Unplugged)
    {
        ThrowIfDisposed();
        return _deviceProvider.GetRecordingDevices(states);
    }

    public AudioEndpointInfo GetDefaultPlaybackDevice()
    {
        ThrowIfDisposed();
        return _defaultDeviceProvider.GetDefaultPlaybackDevice();
    }

    public AudioEndpointInfo GetDefaultRecordingDevice()
    {
        ThrowIfDisposed();
        return _defaultDeviceProvider.GetDefaultRecordingDevice();
    }

    public AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice()
    {
        ThrowIfDisposed();
        return _defaultDeviceProvider.GetDefaultCommunicationsPlaybackDevice();
    }

    public AudioEndpointInfo GetDefaultCommunicationsRecordingDevice()
    {
        ThrowIfDisposed();
        return _defaultDeviceProvider.GetDefaultCommunicationsRecordingDevice();
    }

    public AudioProfile GetCurrentProfile()
    {
        ThrowIfDisposed();

        AudioProfile profile = new();
        CapturePlaybackProfile(profile.Playback);
        CaptureRecordingProfile(profile.Recording);
        CaptureSystemAudioProfile(profile.System);
        return profile;
    }

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

    public AudioProfileApplyResult ApplyProfile(AudioProfile profile)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(profile);
        profile.EnsureDefaults();

        AudioProfileApplyResult result = new();

        AudioProfileValidationResult validationResult = ValidateProfile(profile);
        foreach (AudioOperationMessage message in validationResult.Messages)
        {
            result.Messages.Add(message);
        }

        if (!validationResult.Successful)
        {
            result.Successful = false;
            return result;
        }

        try
        {
            if (profile.Playback.IsPlaybackEnabled)
            {
                ApplyPlaybackProfile(profile.Playback, result);
            }

            if (profile.Recording.IsRecordingEnabled)
            {
                ApplyRecordingProfile(profile.Recording, result);
            }

            if (profile.System.IsSystemAudioEnabled)
            {
                ApplySystemAudioProfile(profile.System, result);
            }
        }
        catch (Exception ex)
        {
            result.Successful = false;
            result.Messages.Add(AudioOperationMessage.Error(
                AudioMessageCode.UnexpectedError,
                $"Unexpected error applying audio profile: {ex.Message}"));
        }

        result.RecalculateSuccess();
        return result;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        (_deviceProvider as IDisposable)?.Dispose();
        (_defaultDeviceProvider as IDisposable)?.Dispose();
        (_volumeProvider as IDisposable)?.Dispose();
        (_formatProvider as IDisposable)?.Dispose();
        (_spatialSoundProvider as IDisposable)?.Dispose();
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
            playback.Device = AudioEndpointReference.FromEndpointInfo(defaultDevice);

            if (defaultDevice.Capabilities.IsVolumeSupported)
            {
                playback.IsVolumeEnabled = true;
                playback.VolumePercent = defaultDevice.VolumePercent;
            }

            if (defaultDevice.Capabilities.IsMuteSupported)
            {
                playback.IsMuteEnabled = true;
                playback.IsMuted = defaultDevice.IsMuted;
            }

            if (defaultDevice.Capabilities.IsFormatReadSupported)
            {
                playback.IsFormatEnabled = true;
                playback.Format = _formatProvider.GetFormat(defaultDevice.DeviceId);
            }

            if (defaultDevice.Capabilities.IsSpatialSoundReadSupported)
            {
                playback.IsSpatialSoundEnabled = true;
                playback.SpatialSound = _spatialSoundProvider.GetSpatialSound(defaultDevice.DeviceId);
            }

            if (defaultDevice.Capabilities.IsAudioEnhancementsReadSupported)
            {
                playback.IsAudioEnhancementsEnabled = true;
                playback.AudioEnhancements = _audioEnhancementProvider.GetAudioEnhancements(defaultDevice.DeviceId);
            }
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
            recording.Device = AudioEndpointReference.FromEndpointInfo(defaultDevice);

            if (defaultDevice.Capabilities.IsVolumeSupported)
            {
                recording.IsVolumeEnabled = true;
                recording.VolumePercent = defaultDevice.VolumePercent;
            }

            if (defaultDevice.Capabilities.IsMuteSupported)
            {
                recording.IsMuteEnabled = true;
                recording.IsMuted = defaultDevice.IsMuted;
            }

            if (defaultDevice.Capabilities.IsFormatReadSupported)
            {
                recording.IsFormatEnabled = true;
                recording.Format = _formatProvider.GetFormat(defaultDevice.DeviceId);
            }

            if (defaultDevice.Capabilities.IsAudioEnhancementsReadSupported)
            {
                recording.IsAudioEnhancementsEnabled = true;
                recording.AudioEnhancements = _audioEnhancementProvider.GetAudioEnhancements(defaultDevice.DeviceId);
            }

            if (defaultDevice.Capabilities.IsVoiceProcessingReadSupported)
            {
                recording.IsVoiceProcessingEnabled = true;
                recording.VoiceProcessing = _audioEnhancementProvider.GetVoiceProcessing(defaultDevice.DeviceId);
            }
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

        if (_systemAudioProvider.IsMonoAudioReadSupported)
        {
            system.IsMonoAudioEnabled = true;
            system.MonoAudio = _systemAudioProvider.GetMonoAudio();
        }
    }

    private void ApplyPlaybackProfile(PlaybackAudioProfile playback, AudioProfileApplyResult result)
    {
        if (playback.IsDefaultPlaybackDeviceEnabled)
        {
            _defaultDeviceProvider.SetDefaultPlaybackDevice(playback.Device);
        }

        if (playback.IsDefaultCommunicationsPlaybackDeviceEnabled)
        {
            _defaultDeviceProvider.SetDefaultCommunicationsPlaybackDevice(playback.CommunicationsDevice);
        }

        if (playback.IsVolumeEnabled)
        {
            _volumeProvider.SetVolumePercent(playback.Device, playback.VolumePercent);
        }

        if (playback.IsMuteEnabled)
        {
            _volumeProvider.SetMute(playback.Device, playback.IsMuted);
        }

        if (playback.IsFormatEnabled)
        {
            _formatProvider.SetFormat(playback.Device, playback.Format);
        }

        if (playback.IsSpatialSoundEnabled)
        {
            _spatialSoundProvider.SetSpatialSound(playback.Device, playback.SpatialSound);
        }

        if (playback.IsAudioEnhancementsEnabled)
        {
            _audioEnhancementProvider.SetAudioEnhancements(playback.Device, playback.AudioEnhancements);
        }

        result.Messages.Add(AudioOperationMessage.Info(
            AudioMessageCode.ProfileApplied,
            "Playback audio profile applied."));
    }

    private void ApplyRecordingProfile(RecordingAudioProfile recording, AudioProfileApplyResult result)
    {
        if (recording.IsDefaultRecordingDeviceEnabled)
        {
            _defaultDeviceProvider.SetDefaultRecordingDevice(recording.Device);
        }

        if (recording.IsDefaultCommunicationsRecordingDeviceEnabled)
        {
            _defaultDeviceProvider.SetDefaultCommunicationsRecordingDevice(recording.CommunicationsDevice);
        }

        if (recording.IsVolumeEnabled)
        {
            _volumeProvider.SetVolumePercent(recording.Device, recording.VolumePercent);
        }

        if (recording.IsMuteEnabled)
        {
            _volumeProvider.SetMute(recording.Device, recording.IsMuted);
        }

        if (recording.IsFormatEnabled)
        {
            _formatProvider.SetFormat(recording.Device, recording.Format);
        }

        if (recording.IsAudioEnhancementsEnabled)
        {
            _audioEnhancementProvider.SetAudioEnhancements(recording.Device, recording.AudioEnhancements);
        }

        if (recording.IsVoiceProcessingEnabled)
        {
            _audioEnhancementProvider.SetVoiceProcessing(recording.Device, recording.VoiceProcessing);
        }

        result.Messages.Add(AudioOperationMessage.Info(
            AudioMessageCode.ProfileApplied,
            "Recording audio profile applied."));
    }

    private void ApplySystemAudioProfile(SystemAudioProfile system, AudioProfileApplyResult result)
    {
        if (system.IsMonoAudioEnabled)
        {
            _systemAudioProvider.SetMonoAudio(system.MonoAudio);
        }

        result.Messages.Add(AudioOperationMessage.Info(
            AudioMessageCode.ProfileApplied,
            "System audio profile applied."));
    }

    private void ValidatePlaybackProfile(PlaybackAudioProfile playback, AudioProfileValidationResult result)
    {
        if (!playback.IsPlaybackEnabled)
        {
            return;
        }

        AudioEndpointInfo? device = null;
        AudioEndpointInfo? communicationsDevice = null;

        if (playback.IsDefaultPlaybackDeviceEnabled || playback.IsVolumeEnabled || playback.IsMuteEnabled || playback.IsFormatEnabled || playback.IsSpatialSoundEnabled || playback.IsAudioEnhancementsEnabled)
        {
            device = ValidateEndpoint(playback.Device, AudioFlow.Render, nameof(playback.Device), result);
        }

        if (playback.IsDefaultCommunicationsPlaybackDeviceEnabled)
        {
            communicationsDevice = ValidateEndpoint(playback.CommunicationsDevice, AudioFlow.Render, nameof(playback.CommunicationsDevice), result);
        }

        ValidateVolume(playback.IsVolumeEnabled, playback.VolumePercent, result);

        if (device is not null)
        {
            if (playback.IsVolumeEnabled && !device.Capabilities.IsVolumeSupported)
            {
                AddUnsupported(result, "Playback volume is not supported by the selected playback device.");
            }

            if (playback.IsMuteEnabled && !device.Capabilities.IsMuteSupported)
            {
                AddUnsupported(result, "Playback mute is not supported by the selected playback device.");
            }

            if (playback.IsFormatEnabled && !device.Capabilities.IsFormatSetSupported)
            {
                AddUnsupported(result, "Playback format setting is not supported by this version of WindowsAudioWrapper.");
            }

            if (playback.IsSpatialSoundEnabled && !device.Capabilities.IsSpatialSoundSetSupported)
            {
                AddUnsupported(result, "Spatial sound setting is not supported by this version of WindowsAudioWrapper.");
            }

            if (playback.IsAudioEnhancementsEnabled && !device.Capabilities.IsAudioEnhancementsSetSupported)
            {
                AddUnsupported(result, "Playback audio enhancement setting is not supported by this version of WindowsAudioWrapper.");
            }
        }

        _ = communicationsDevice;
    }

    private void ValidateRecordingProfile(RecordingAudioProfile recording, AudioProfileValidationResult result)
    {
        if (!recording.IsRecordingEnabled)
        {
            return;
        }

        AudioEndpointInfo? device = null;
        AudioEndpointInfo? communicationsDevice = null;

        if (recording.IsDefaultRecordingDeviceEnabled || recording.IsVolumeEnabled || recording.IsMuteEnabled || recording.IsFormatEnabled || recording.IsAudioEnhancementsEnabled || recording.IsVoiceProcessingEnabled)
        {
            device = ValidateEndpoint(recording.Device, AudioFlow.Capture, nameof(recording.Device), result);
        }

        if (recording.IsDefaultCommunicationsRecordingDeviceEnabled)
        {
            communicationsDevice = ValidateEndpoint(recording.CommunicationsDevice, AudioFlow.Capture, nameof(recording.CommunicationsDevice), result);
        }

        ValidateVolume(recording.IsVolumeEnabled, recording.VolumePercent, result);

        if (device is not null)
        {
            if (recording.IsVolumeEnabled && !device.Capabilities.IsVolumeSupported)
            {
                AddUnsupported(result, "Recording volume is not supported by the selected recording device.");
            }

            if (recording.IsMuteEnabled && !device.Capabilities.IsMuteSupported)
            {
                AddUnsupported(result, "Recording mute is not supported by the selected recording device.");
            }

            if (recording.IsFormatEnabled && !device.Capabilities.IsFormatSetSupported)
            {
                AddUnsupported(result, "Recording format setting is not supported by this version of WindowsAudioWrapper.");
            }

            if (recording.IsAudioEnhancementsEnabled && !device.Capabilities.IsAudioEnhancementsSetSupported)
            {
                AddUnsupported(result, "Recording audio enhancement setting is not supported by this version of WindowsAudioWrapper.");
            }

            if (recording.IsVoiceProcessingEnabled && !device.Capabilities.IsVoiceProcessingSetSupported)
            {
                AddUnsupported(result, "Voice processing setting is not supported by this version of WindowsAudioWrapper.");
            }
        }

        _ = communicationsDevice;
    }

    private void ValidateSystemAudioProfile(SystemAudioProfile system, AudioProfileValidationResult result)
    {
        if (!system.IsSystemAudioEnabled)
        {
            return;
        }

        if (system.IsMonoAudioEnabled && !_systemAudioProvider.IsMonoAudioSetSupported)
        {
            AddUnsupported(result, "Mono audio setting is not supported by this version of WindowsAudioWrapper.");
        }

        if (!system.IsMonoAudioEnabled)
        {
            result.Messages.Add(AudioOperationMessage.Warning(
                AudioMessageCode.NoEnabledSettings,
                "System audio profile is enabled, but no system audio settings are enabled."));
        }
    }

    private AudioEndpointInfo? ValidateEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow, string propertyName, AudioProfileValidationResult result)
    {
        if (!endpoint.IsEndpointEnabled)
        {
            result.Messages.Add(AudioOperationMessage.Error(
                AudioMessageCode.DeviceMissing,
                $"{propertyName} does not contain a device reference."));
            return null;
        }

        if (endpoint.Flow != AudioFlow.Unknown && endpoint.Flow != expectedFlow)
        {
            result.Messages.Add(AudioOperationMessage.Error(
                AudioMessageCode.InvalidAudioFlow,
                $"{propertyName} has flow {endpoint.Flow}, but expected {expectedFlow}."));
            return null;
        }

        AudioEndpointInfo resolved = _deviceProvider.ResolveEndpoint(endpoint, expectedFlow);
        if (string.IsNullOrWhiteSpace(resolved.DeviceId))
        {
            result.Messages.Add(AudioOperationMessage.Error(
                AudioMessageCode.DeviceNotFound,
                $"{propertyName} could not be found."));
            return null;
        }

        if (resolved.State.HasFlag(AudioDeviceState.Disabled) || resolved.State.HasFlag(AudioDeviceState.NotPresent))
        {
            result.Messages.Add(AudioOperationMessage.Error(
                AudioMessageCode.DeviceUnavailable,
                $"{propertyName} is not currently available. Device state: {resolved.State}."));
        }

        return resolved;
    }

    private static void ValidateVolume(bool isVolumeEnabled, decimal volumePercent, AudioProfileValidationResult result)
    {
        if (!isVolumeEnabled)
        {
            return;
        }

        if (volumePercent < 0 || volumePercent > 100)
        {
            result.Messages.Add(AudioOperationMessage.Error(
                AudioMessageCode.InvalidVolume,
                "VolumePercent must be between 0 and 100."));
        }
    }

    private static void AddUnsupported(AudioProfileValidationResult result, string message)
    {
        result.Messages.Add(AudioOperationMessage.Error(
            AudioMessageCode.UnsupportedSetting,
            message));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
