namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents a message generated during validation or application of an audio profile.
/// </summary>
public sealed class AudioOperationMessage
{
    /// <summary>Gets or sets the severity of the message.</summary>
    public AudioMessageSeverity Severity { get; set; } = AudioMessageSeverity.Info;

    /// <summary>Gets or sets the specific code associated with the message.</summary>
    public AudioMessageCode Code { get; set; } = AudioMessageCode.None;

    /// <summary>Gets or sets the human-readable message text.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the ID of the device associated with this message, if applicable.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the device associated with this message, if applicable.</summary>
    public string DeviceName { get; set; } = string.Empty;

    public static AudioOperationMessage Info(AudioMessageCode code, string message) => new()
    {
        Severity = AudioMessageSeverity.Info,
        Code = code,
        Message = message
    };

    public static AudioOperationMessage Warning(AudioMessageCode code, string message) => new()
    {
        Severity = AudioMessageSeverity.Warning,
        Code = code,
        Message = message
    };

    public static AudioOperationMessage Error(AudioMessageCode code, string message) => new()
    {
        Severity = AudioMessageSeverity.Error,
        Code = code,
        Message = message
    };
}