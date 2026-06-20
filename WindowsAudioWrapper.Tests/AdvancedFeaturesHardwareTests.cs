using System;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

/// <summary>
/// Focuses integration vectors checking advanced framework operations like system enhancement switches,
/// registry-based audio accessibility modifiers, and marshalling structure reapplies.
/// </summary>
[Collection(AudioHardwareCollection.Name)]
public sealed class AdvancedFeaturesHardwareTests
{
    /// <summary>
    /// Validates that modifying global mono mixing accessibility properties via the registry succeeds and reverts accurately.
    /// </summary>
    [SkippableFact]
    public void ApplyProfile_ShouldChangeAndRestoreSystemMonoAudio_ViaRegistry()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        bool targetMonoState = !originalProfile.System.MonoAudio;
        
        AudioProfile changedProfile = HardwareTestHelpers.CloneProfile(originalProfile);
        changedProfile.System.IsSystemAudioEnabled = true;
        changedProfile.System.IsMonoAudioEnabled = true;
        changedProfile.System.MonoAudio = targetMonoState;

        try
        {
            AudioProfileApplyResult applyResult = HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(changedProfile),
                "system mono audio set");

            HardwareTestHelpers.AssertApplySucceeded(applyResult);

            AudioProfile currentProfile = HardwareTestHelpers.RunOrSkip(
                controller.GetCurrentProfile,
                "current audio profile capture after mono change");

            Assert.Equal(targetMonoState, currentProfile.System.MonoAudio);
        }
        finally
        {
            HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(originalProfile),
                "system mono audio restore");
        }
    }

    /// <summary>
    /// Verifies that toggling the system audio processing object processing state works on endpoints where APO features are active.
    /// </summary>
    [SkippableFact]
    public void ApplyProfile_ShouldToggleAudioEnhancements_WhenEndpointSupportsAPOs()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        Skip.If(!originalProfile.Playback.AudioEnhancements.AreEnhancementsSupported, 
            "Skipping because the current default playback hardware does not support system enhancements.");

        bool targetToggleState = !originalProfile.Playback.AudioEnhancements.DisableAllEnhancements;

        AudioProfile changedProfile = HardwareTestHelpers.CloneProfile(originalProfile);
        changedProfile.Playback.AudioEnhancements.DisableAllEnhancements = targetToggleState;

        try
        {
            AudioProfileApplyResult applyResult = HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(changedProfile),
                "audio enhancements toggle set");

            HardwareTestHelpers.AssertApplySucceeded(applyResult);

            AudioProfile currentProfile = HardwareTestHelpers.RunOrSkip(
                controller.GetCurrentProfile,
                "current audio profile capture after enhancement toggle");

            Assert.Equal(targetToggleState, currentProfile.Playback.AudioEnhancements.DisableAllEnhancements);
        }
        finally
        {
            HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(originalProfile),
                "audio enhancements restore");
        }
    }

    /// <summary>
    /// Confirms that marshalling the internal wave extensible data blocks back down into unmanaged property stores maps without faults.
    /// </summary>
    [SkippableFact]
    public void ApplyProfile_ShouldReapplySharedStreamFormatWithoutFaults()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        Skip.If(originalProfile.Playback.StreamFormat.SampleRate == 0, 
            "Skipping because no active stream format parameters could be extracted from the device.");

        AudioProfile changedProfile = HardwareTestHelpers.CloneProfile(originalProfile);

        AudioProfileApplyResult applyResult = HardwareTestHelpers.RunOrSkip(
            () => controller.ApplyProfile(changedProfile),
            "stream format roundtrip reapply");

        HardwareTestHelpers.AssertApplySucceeded(applyResult);
    }
}