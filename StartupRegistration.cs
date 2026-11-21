using Microsoft.Win32;
using System.IO;

namespace GoogleDesktopRemake;

public static class StartupRegistration
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "GoogleDesktopRevived";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        string? value = key?.GetValue(ValueName) as string;
        return string.Equals(value, GetCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("Could not open the current-user startup registry key.");
            key.SetValue(ValueName, GetCommand(), RegistryValueKind.String);
            return;
        }

        using var existingKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        existingKey?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    static string GetCommand()
    {
        string? executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath) ||
            !string.Equals(Path.GetFileName(executablePath), "Google Desktop Revived.exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Run the published Google Desktop Revived.exe to add this app to startup.");
        }

        return $"\"{executablePath}\"";
    }
}
