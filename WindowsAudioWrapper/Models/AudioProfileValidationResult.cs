namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the result of validating an AudioProfile before application.
/// </summary>
public sealed class AudioProfileValidationResult
{
    /// <summary>Gets or sets the overall severity of the validation result.</summary>
    public AudioValidationSeverity Severity { get; set; } = AudioValidationSeverity.Valid;

    /// <summary>Gets a value indicating whether the validation passed without fatal errors.</summary>
    public bool Successful => Severity != AudioValidationSeverity.Error;

    /// <summary>Gets or sets a list of messages detailing specific validation issues.</summary>
    public List<AudioOperationMessage> Messages { get; set; } = new();

    /// <summary>Recalculates the <see cref="Severity"/> property based on the highest severity message present.</summary>
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