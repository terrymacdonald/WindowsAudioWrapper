namespace WindowsAudioWrapper.Providers;

using Microsoft.Win32;

internal sealed class SystemAudioProvider : ISystemAudioProvider
{
    private const string RegistryKeyPath = @"Software\Microsoft\Multimedia\Audio";
    private const string MonoValueName = "AccessibilityMonoMixState";

    public bool IsMonoAudioReadSupported => true;
    public bool IsMonoAudioSetSupported => true;

    public bool GetMonoAudio()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            if (key != null)
            {
                object? value = key.GetValue(MonoValueName);
                if (value is int intValue)
                {
                    return intValue == 1;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sets the mono audio state in the registry. 
    /// Note: Changes to this registry key may require restarting the Windows audio service or logging out/in to fully apply across all applications.
    /// </summary>
    public void SetMonoAudio(bool enabled)
    {
        using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, writable: true);
        if (key != null)
        {
            key.SetValue(MonoValueName, enabled ? 1 : 0, RegistryValueKind.DWord);
        }
    }
}