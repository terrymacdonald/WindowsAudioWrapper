WindowsAudioWrapper library files

Public API concept:
- AudioProfile GetCurrentProfile()
- AudioProfileApplyResult ApplyProfile(AudioProfile profile)
- AudioProfileValidationResult ValidateProfile(AudioProfile profile)

This library is a safe, complete wrapper over Windows Core Audio, PolicyConfig, and related APIs.
It allows developers to programmatically grab, apply, and change audio devices, audio outputs, inputs, and settings.

The GetCurrentProfile() method captures all available audio settings into an easily serializable AudioProfile DTO. 
ApplyProfile() applies every enabled setting from the provided AudioProfile back to the Windows environment safely.