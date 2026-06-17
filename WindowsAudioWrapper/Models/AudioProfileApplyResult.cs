namespace WindowsAudioWrapper.Models;

public sealed class AudioProfileApplyResult
{
    public bool Successful { get; set; } = true;

    public List<AudioOperationMessage> Messages { get; set; } = new();

    public void RecalculateSuccess()
    {
        Messages ??= new List<AudioOperationMessage>();
        Successful = !Messages.Any(message => message.Severity == AudioMessageSeverity.Error);
    }

    public static AudioProfileApplyResult Success() => new();

    public static AudioProfileApplyResult Error(AudioMessageCode code, string message)
    {
        return new AudioProfileApplyResult
        {
            Successful = false,
            Messages = new List<AudioOperationMessage>
            {
                AudioOperationMessage.Error(code, message)
            }
        };
    }
}
