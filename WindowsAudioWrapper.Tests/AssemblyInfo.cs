using Xunit;

// Real hardware tests should not run in parallel because several of them temporarily
// change global Windows audio settings such as volume, mute, and default devices.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
