using Microsoft.Win32;

namespace BlackScreens;

/// <summary>
/// Current-user autostart via HKCU...\Run (no admin required).
/// </summary>
public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BlackScreens";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            key.SetValue(ValueName, QuoteIfNeeded(GetExecutablePath()));
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    private static string GetExecutablePath()
    {
        // Prefer the process path so single-file publishes point at the .exe, not a temp extract.
        string? path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Application.ExecutablePath;
        }

        return Path.GetFullPath(path);
    }

    private static string QuoteIfNeeded(string path) =>
        path.Contains(' ', StringComparison.Ordinal) ? $"\"{path}\"" : path;
}
