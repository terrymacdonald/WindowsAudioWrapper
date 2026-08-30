namespace WindowsAudioWrapper.Providers;

using Microsoft.Win32;
using System.Runtime.InteropServices;

internal sealed class SystemAudioProvider : ISystemAudioProvider
{
    private const string RegistryKeyPath = @"Software\Microsoft\Multimedia\Audio";
    private const string MonoValueName = "AccessibilityMonoMixState";
    private const uint WmSettingChange = 0x001A;
    private static readonly IntPtr HwndBroadcast = new(0xffff);

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
    /// Sets the mono audio state in the registry and notifies running applications of the setting change.
    /// </summary>
    public void SetMonoAudio(bool enabled)
    {
        using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, writable: true);
        if (key != null)
        {
            key.SetValue(MonoValueName, enabled ? 1 : 0, RegistryValueKind.DWord);
            SendMessageTimeout(HwndBroadcast, WmSettingChange, IntPtr.Zero, MonoValueName, 0, 1000, out _);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        string lParam,
        uint flags,
        uint timeout,
        out IntPtr result);
}
