namespace WindowsAudioWrapper.Models;

public sealed class AudioProfileValidationResult
{
    public AudioValidationSeverity Severity { get; set; } = AudioValidationSeverity.Valid;

    public bool Successful => Severity != AudioValidationSeverity.Error;

    public List<AudioOperationMessage> Messages { get; set; } = new();

    public void RecalculateSeverity()
    {
        Messages ??= new List<AudioOperationMessage>();

        if (Messages.Any(message => message.Severity == AudioMessageSeverity.Error))
        {
            Severity = AudioValidationSeverity.Error;
        }
        else if (Messages.Any(message => message.Severity == AudioMessageSeverity.Warning))
        {
            Severity = AudioValidationSeverity.Warning;
        }
        else
        {
            Severity = AudioValidationSeverity.Valid;
        }
    }
}
