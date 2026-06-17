namespace WindowsAudioWrapper.Models;

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

public enum AudioFlow
{
    Unknown = 0,
    Render = 1,
    Capture = 2
}

public enum AudioSampleFormat
{
    Unknown = 0,
    Pcm = 1,
    IeeeFloat = 2
}

public enum AudioFormatMode
{
    Unknown = 0,
    Shared = 1,
    Exclusive = 2
}

public enum SpatialSoundMode
{
    Unknown = 0,
    Off = 1,
    WindowsSonic = 2,
    DolbyAtmos = 3,
    Dts = 4,
    Other = 99
}

public enum AudioMessageSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public enum AudioValidationSeverity
{
    Valid = 0,
    Warning = 1,
    Error = 2
}

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
