namespace WindowsAudioWrapper.Models;

/// <summary>Identifies the connection and enablement state of an audio endpoint.</summary>
[Flags]
public enum AudioDeviceState
{
    /// <summary>The endpoint state is unknown.</summary>
    Unknown = 0,
    /// <summary>The endpoint is active.</summary>
    Active = 1,
    /// <summary>The endpoint is disabled.</summary>
    Disabled = 2,
    /// <summary>The endpoint is not present.</summary>
    NotPresent = 4,
    /// <summary>The endpoint is unplugged.</summary>
    Unplugged = 8,
    /// <summary>Includes every defined endpoint state except unknown.</summary>
    All = Active | Disabled | NotPresent | Unplugged
}

/// <summary>Identifies whether an audio endpoint renders or captures audio.</summary>
public enum AudioFlow
{
    /// <summary>The endpoint direction is unknown.</summary>
    Unknown = 0,
    /// <summary>The endpoint renders playback audio.</summary>
    Render = 1,
    /// <summary>The endpoint captures recording audio.</summary>
    Capture = 2
}

/// <summary>Identifies the encoding used for audio samples.</summary>
public enum AudioSampleFormat
{
    /// <summary>The sample format is unknown.</summary>
    Unknown = 0,
    /// <summary>Samples use pulse-code modulation.</summary>
    Pcm = 1,
    /// <summary>Samples use IEEE floating-point encoding.</summary>
    IeeeFloat = 2
}

/// <summary>Identifies the audio stream mode represented by a format.</summary>
public enum AudioFormatMode
{
    /// <summary>The format mode is unknown.</summary>
    Unknown = 0,
    /// <summary>The format uses shared mode.</summary>
    Shared = 1
}

/// <summary>Identifies the severity of a profile operation message.</summary>
public enum AudioMessageSeverity
{
    /// <summary>The message is informational.</summary>
    Info = 0,
    /// <summary>The message reports a warning.</summary>
    Warning = 1,
    /// <summary>The message reports an error.</summary>
    Error = 2
}

/// <summary>Identifies the severity of a profile validation result.</summary>
public enum AudioValidationSeverity
{
    /// <summary>The profile setting is valid.</summary>
    Valid = 0,
    /// <summary>The profile setting has a warning.</summary>
    Warning = 1,
    /// <summary>The profile setting has an error.</summary>
    Error = 2
}

/// <summary>Identifies the reason for a profile operation message.</summary>
public enum AudioMessageCode
{
    /// <summary>No specific message code applies.</summary>
    None = 0,
    /// <summary>The profile was applied.</summary>
    ProfileApplied = 1,
    /// <summary>The profile does not specify a required device.</summary>
    DeviceMissing = 2,
    /// <summary>The requested device was not found.</summary>
    DeviceNotFound = 3,
    /// <summary>The requested device is unavailable.</summary>
    DeviceUnavailable = 4,
    /// <summary>The requested audio direction is invalid.</summary>
    InvalidAudioFlow = 5,
    /// <summary>The requested volume is invalid.</summary>
    InvalidVolume = 6,
    /// <summary>The requested setting is unsupported.</summary>
    UnsupportedSetting = 7,
    /// <summary>The profile has no enabled settings to apply.</summary>
    NoEnabledSettings = 8,
    /// <summary>Multiple endpoints match fallback criteria; no endpoint was selected.</summary>
    AmbiguousEndpoint = 9,
    /// <summary>An unexpected error occurred.</summary>
    UnexpectedError = 99
}
