using System;
using System.Diagnostics;
using System.IO;
using osucc.Core;

namespace LazerLens.Utilities
{
    /// <summary>
    /// Cross-platform helper for opening directories and selecting files in desktop file managers (Windows, Linux, macOS).
    /// </summary>
    public static class PlatformHelper
    {
        public static void OpenDirectory(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath)) return;

            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = directoryPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{directoryPath}\"",
                        UseShellExecute = false
                    });
                }
                else // Linux / BSD / Unix
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "xdg-open",
                            Arguments = $"\"{directoryPath}\"",
                            UseShellExecute = false
                        });
                    }
                    catch
                    {
                        new osu.Framework.Platform.NativeStorage(directoryPath).PresentExternally();
                    }
                }
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PlatformHelper: Failed to open directory '{directoryPath}': {ex.Message}");
                try
                {
                    new osu.Framework.Platform.NativeStorage(directoryPath).PresentExternally();
                }
                catch { }
            }
        }

        public static void OpenAndSelectFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                var fallbackDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(fallbackDir))
                    OpenDirectory(fallbackDir);
                return;
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"-R \"{filePath}\"",
                        UseShellExecute = false
                    });
                }
                else // Linux
                {
                    string dir = Path.GetDirectoryName(filePath) ?? filePath;
                    bool launched = false;

                    try
                    {
                        using var nautilus = Process.Start(new ProcessStartInfo
                        {
                            FileName = "nautilus",
                            Arguments = $"--select \"{filePath}\"",
                            UseShellExecute = false
                        });
                        launched = nautilus != null;
                    }
                    catch { }

                    if (!launched)
                    {
                        try
                        {
                            using var dolphin = Process.Start(new ProcessStartInfo
                            {
                                FileName = "dolphin",
                                Arguments = $"--select \"{filePath}\"",
                                UseShellExecute = false
                            });
                            launched = dolphin != null;
                        }
                        catch { }
                    }

                    if (!launched)
                    {
                        OpenDirectory(dir);
                    }
                }
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PlatformHelper: Failed to open/select file '{filePath}': {ex.Message}");
                var dir = Path.GetDirectoryName(filePath) ?? filePath;
                OpenDirectory(dir);
            }
        }
    }
}
