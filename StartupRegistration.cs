using Microsoft.Win32;
using System.IO;

namespace GoogleDesktopRemake;

public static class StartupRegistration
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "GoogleDesktopRevived";
    static readonly string InstalledDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GoogleDesktopRevived", "Startup");
    static readonly string InstalledExecutable = Path.Combine(InstalledDirectory, "GoogleDesktopRebuilt.exe");

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        string? value = key?.GetValue(ValueName) as string;
        return string.Equals(value, GetInstalledCommand(), StringComparison.OrdinalIgnoreCase) &&
               File.Exists(InstalledExecutable);
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            string? runningExecutable = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(runningExecutable) ||
                !string.Equals(Path.GetExtension(runningExecutable), ".exe", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(runningExecutable))
            {
                throw new InvalidOperationException("Could not find the running app executable to add it to startup.");
            }

            Directory.CreateDirectory(InstalledDirectory);
            if (!string.Equals(runningExecutable, InstalledExecutable, StringComparison.OrdinalIgnoreCase))
                File.Copy(runningExecutable, InstalledExecutable, overwrite: true);

            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("Could not open the current-user startup registry key.");
            key.SetValue(ValueName, GetInstalledCommand(), RegistryValueKind.String);
            return;
        }

        using var existingKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        existingKey?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    static string GetInstalledCommand() => $"\"{InstalledExecutable}\"";
}
