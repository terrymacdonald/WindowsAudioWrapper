using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class AudioEnhancementEffectsTests
{
    [SkippableFact]
    public void GetCurrentProfile_ShouldCaptureActiveEffectsGuidsList_WhenEffectsAreReported()
    {
        using WindowsAudioController controller = new();

        AudioProfile profile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        profile.EnsureDefaults();
        Skip.If(!profile.Playback.MultimediaDevice.IsEndpointEnabled, "Skipping because the playback multimedia device was not captured.");

        IReadOnlyList<AUDIO_EFFECT> effects = CoreAudioUtilities.GetAudioEffects(profile.Playback.MultimediaDevice.DeviceId);
        Skip.If(effects.Count == 0, "Skipping because this endpoint does not report Windows audio effects.");

        string[] expectedActiveEffectIds = effects
            .Where(effect => effect.id != Guid.Empty && effect.state == AudioEffectState.On)
            .Select(effect => effect.id.ToString("D"))
            .OrderBy(effectId => effectId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] actualActiveEffectIds = profile.Playback.AudioEnhancements.ActiveEffectsGuidsList
            .OrderBy(effectId => effectId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(profile.Playback.AudioEnhancements.AreEnhancementsSupported);
        Assert.Equal(expectedActiveEffectIds, actualActiveEffectIds);
    }

    [SkippableFact]
    public void SetAudioEnhancements_ShouldApplyMutableActiveEffectStates_WhenEffectsAreControllable()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        originalProfile.EnsureDefaults();
        Skip.If(!originalProfile.Playback.MultimediaDevice.IsEndpointEnabled, "Skipping because the playback multimedia device was not captured.");

        string deviceId = originalProfile.Playback.MultimediaDevice.DeviceId;
        AudioEnhancementProfile originalEnhancements = originalProfile.Playback.AudioEnhancements;
        IReadOnlyList<AUDIO_EFFECT> originalEffects = CoreAudioUtilities.GetAudioEffects(deviceId);
        Skip.If(originalEffects.Count == 0, "Skipping because this endpoint does not report Windows audio effects.");

        AUDIO_EFFECT mutableEffect = originalEffects.FirstOrDefault(effect => effect.id != Guid.Empty && effect.canSetState);
        Skip.If(mutableEffect.id == Guid.Empty, "Skipping because this endpoint does not expose any mutable Windows audio effects.");

        bool shouldEnableEffect = mutableEffect.state != AudioEffectState.On;
        var targetEnhancements = new AudioEnhancementProfile
        {
            AreEnhancementsSupported = true,
            DisableAllEnhancements = false,
            ActiveEffectsGuidsList = originalEffects
                .Where(effect => effect.id != Guid.Empty && effect.state == AudioEffectState.On && effect.id != mutableEffect.id)
                .Select(effect => effect.id.ToString("D"))
                .ToList()
        };

        if (shouldEnableEffect)
        {
            targetEnhancements.ActiveEffectsGuidsList.Add(mutableEffect.id.ToString("D"));
        }

        try
        {
            HardwareTestHelpers.RunOrSkip(
                () => controller.SetAudioEnhancements(deviceId, targetEnhancements),
                "audio effect state apply");

            IReadOnlyList<AUDIO_EFFECT> changedEffects = CoreAudioUtilities.GetAudioEffects(deviceId);
            AUDIO_EFFECT changedEffect = changedEffects.FirstOrDefault(effect => effect.id == mutableEffect.id);

            Assert.NotEqual(Guid.Empty, changedEffect.id);
            Assert.Equal(shouldEnableEffect ? AudioEffectState.On : AudioEffectState.Off, changedEffect.state);
        }
        finally
        {
            try
            {
                controller.SetAudioEnhancements(deviceId, originalEnhancements);
            }
            catch
            {
                // Best-effort restore only; the test failure path should remain visible.
            }
        }
    }
}
