WindowsAudioWrapper library files - profile-only skeleton

Public API concept:
- AudioProfile GetCurrentProfile()
- AudioProfileApplyResult ApplyProfile(AudioProfile profile)
- AudioProfileValidationResult ValidateProfile(AudioProfile profile)

There are no snapshot classes, restore methods, or capture options.
GetCurrentProfile() should eventually capture every readable Windows audio setting into an AudioProfile.
ApplyProfile() applies every enabled setting in an AudioProfile.

The low-level Core Audio, PolicyConfig, property store, spatial sound, enhancement, and mono audio implementations are intentionally placeholders in this skeleton.
