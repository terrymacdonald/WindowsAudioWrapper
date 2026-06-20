namespace WindowsAudioWrapper.Models;

public class AudioEnhancementProfile
{
    public bool AreEnhancementsSupported { get; set; }
    public bool DisableAllEnhancements { get; set; }
    public List<string> ActiveEffectsGuidsList { get; set; } = new();
}