namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the current state of a Windows audio device.
/// </summary>
[Flags]
public enum AudioDeviceState
{
    Unknown = 0,
    Active = 1,
    Disabled = 2,
    NotPresent = 4,
    Unplugged = 8,
    All = Active | Disabled | NotPresent | Unplugged
}

/// <summary>
/// Represents the data flow direction of an audio endpoint.
/// </summary>
public enum AudioFlow
{
    Unknown = 0,
    Render = 1,
    Capture = 2
}

/// <summary>
/// Represents the underlying sample format of the audio stream.
/// </summary>
public enum AudioSampleFormat
{
    Unknown = 0,
    Pcm = 1,
    IeeeFloat = 2
}

/// <summary>
/// Represents how the audio stream interacts with the Windows audio engine.
/// </summary>
public enum AudioFormatMode
{
    Unknown = 0,
    Shared = 1,
    Exclusive = 2
}

/// <summary>
/// Represents the spatial sound format applied to a device.
/// </summary>
public enum SpatialSoundMode
{
    Unknown = 0,
    Off = 1,
    WindowsSonic = 2,
    DolbyAtmos = 3,
    Dts = 4,
    Other = 99
}

/// <summary>
/// Defines the severity of an audio operation message.
/// </summary>
public enum AudioMessageSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

/// <summary>
/// Defines the severity of an audio profile validation result.
/// </summary>
public enum AudioValidationSeverity
{
    Valid = 0,
    Warning = 1,
    Error = 2
}

/// <summary>
/// Defines specific codes for audio operation results.
/// </summary>
public enum AudioMessageCode
{
    None = 0,
    ProfileApplied = 1,
    DeviceMissing = 2,
    DeviceNotFound = 3,
    DeviceUnavailable = 4,
    InvalidAudioFlow = 5,
    InvalidVolume = 6,
    UnsupportedSetting = 7,
    NoEnabledSettings = 8,
    UnexpectedError = 99
}