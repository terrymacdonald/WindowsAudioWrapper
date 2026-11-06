namespace WindowsAudioWrapper.Models;

/// <summary>Contains the audio enhancement settings for an endpoint.</summary>
public class AudioEnhancementProfile
{
    /// <summary>Gets or sets whether the endpoint supports audio enhancements.</summary>
    public bool AreEnhancementsSupported { get; set; }
    /// <summary>Gets or sets whether all audio enhancements should be disabled.</summary>
    public bool DisableAllEnhancements { get; set; }
    /// <summary>Gets or sets the identifiers of active audio effects.</summary>
    public List<string> ActiveEffectsGuidsList { get; set; } = new();
}
