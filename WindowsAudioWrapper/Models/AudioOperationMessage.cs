namespace WindowsAudioWrapper.Models;

public sealed class AudioOperationMessage
{
    public AudioMessageSeverity Severity { get; set; } = AudioMessageSeverity.Info;

    public AudioMessageCode Code { get; set; } = AudioMessageCode.None;

    public string Message { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public static AudioOperationMessage Info(AudioMessageCode code, string message)
    {
        return new AudioOperationMessage
        {
            Severity = AudioMessageSeverity.Info,
            Code = code,
            Message = message
        };
    }

    public static AudioOperationMessage Warning(AudioMessageCode code, string message)
    {
        return new AudioOperationMessage
        {
            Severity = AudioMessageSeverity.Warning,
            Code = code,
            Message = message
        };
    }

    public static AudioOperationMessage Error(AudioMessageCode code, string message)
    {
        return new AudioOperationMessage
        {
            Severity = AudioMessageSeverity.Error,
            Code = code,
            Message = message
        };
    }
}
