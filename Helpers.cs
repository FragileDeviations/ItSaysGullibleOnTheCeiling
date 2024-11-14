using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Gullible;

internal static class Helpers
{
    public static string GetVlcPathFromRegistry()
    {
        string[] registryKeys =
        [
            @"SOFTWARE\VideoLAN\VLC",
            @"SOFTWARE\WOW6432Node\VideoLAN\VLC"
        ];

        foreach (var key in registryKeys)
        {
            using var vlcKey = Registry.LocalMachine.OpenSubKey(key);
            if (vlcKey == null) continue;
            if (vlcKey.GetValue("InstallDir") is not string installDir || !Directory.Exists(installDir)) continue;
            Console.WriteLine($"Found VLC at {installDir}");
            return installDir;
        }

        return null;
    }

    public static void ForceShutdown()
    {
        try
        {
            Process.Start("shutdown.exe", "/s /t 300");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while trying to shut down: {ex.Message}");
        }
    }
}