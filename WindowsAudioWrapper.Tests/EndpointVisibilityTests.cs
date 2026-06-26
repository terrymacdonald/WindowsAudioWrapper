using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class EndpointVisibilityTests
{
    [SkippableFact]
    public void ApplyProfile_ShouldReapplyCapturedEndpointVisibilitiesSuccessfully()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        originalProfile.EnsureDefaults();
        Skip.If(originalProfile.EndpointVisibilities.Count == 0, "Skipping because no endpoint visibility states were captured.");

        AudioProfile visibilityOnlyProfile = new()
        {
            EndpointVisibilities = originalProfile.EndpointVisibilities
                .Select(endpoint => new AudioEndpointVisibility
                {
                    DeviceId = endpoint.DeviceId,
                    ContainerId = endpoint.ContainerId,
                    FriendlyName = endpoint.FriendlyName,
                    Flow = endpoint.Flow,
                    IsDisabled = endpoint.IsDisabled
                })
                .ToList(),
            IsEndpointVisibilityTrackingEnabled = true
        };

        AudioProfileApplyResult result = HardwareTestHelpers.RunOrSkip(
            () => controller.ApplyProfile(visibilityOnlyProfile),
            "endpoint visibility reapply");

        HardwareTestHelpers.AssertApplySucceeded(result);

        AudioProfile currentProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture after endpoint visibility reapply");

        foreach (AudioEndpointVisibility expected in visibilityOnlyProfile.EndpointVisibilities)
        {
            AudioEndpointVisibility actual = Assert.Single(
                currentProfile.EndpointVisibilities,
                endpoint => endpoint.DeviceId.Equals(expected.DeviceId, StringComparison.OrdinalIgnoreCase));

            Assert.Equal(expected.IsDisabled, actual.IsDisabled);
        }
    }
}
