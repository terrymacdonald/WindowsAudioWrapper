namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the result of an attempt to apply an audio profile to the system.
/// </summary>
public sealed class AudioProfileApplyResult
{
    /// <summary>Gets or sets a value indicating whether the profile was successfully applied without fatal errors.</summary>
    public bool Successful { get; set; } = true;

    /// <summary>Gets or sets a list of informational, warning, and error messages generated during application.</summary>
    public List<AudioOperationMessage> Messages { get; set; } = new();

    /// <summary>Maps requested nonempty endpoint IDs to the actual IDs resolved for this apply; it contains no native handles.</summary>
    public Dictionary<string, string> ResolvedDeviceIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Recalculates the <see cref="Successful"/> property based on the presence of Error messages.</summary>
    public void RecalculateSuccess()
    {
        Messages ??= new List<AudioOperationMessage>();
        Successful = !Messages.Any(message => message.Severity == AudioMessageSeverity.Error);
    }

    /// <summary>Creates an empty successful apply result.</summary>
    public static AudioProfileApplyResult Success() => new();

    /// <summary>Creates a failed apply result with one diagnostic error.</summary>
    public static AudioProfileApplyResult Error(AudioMessageCode code, string message) => new()
    {
        Successful = false,
        Messages = new List<AudioOperationMessage> { AudioOperationMessage.Error(code, message) }
    };
}