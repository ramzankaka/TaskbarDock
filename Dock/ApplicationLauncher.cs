using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TaskbarDock.Diagnostics;
using TaskbarDock.Models;
using TaskbarDock.WindowsIntegration;
using static TaskbarDock.WindowsIntegration.NativeMethods;

namespace TaskbarDock.Dock
{
    public static class ApplicationLauncher
    {
        public static void LaunchOrActivate(DockItem item, bool activateRunning = true, bool minimizeIfActive = true)
        {
            try
            {
                if (activateRunning && item.IsRunning && item.WindowHandles.Count > 0)
                {
                    // If multiple windows exist, cycle or activate the foremost
                    var validHwnds = item.WindowHandles.Where(h => IsWindow(h)).ToList();
                    if (validHwnds.Count > 0)
                    {
                        IntPtr target = validHwnds[0];
                        WindowManager.ActivateWindow(target, minimizeIfActive);
                        return;
                    }
                }

                // Launch new instance
                LaunchNewInstance(item);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to launch or activate item: {item.Title}", ex);
            }
        }

        public static void LaunchNewInstance(DockItem item)
        {
            try
            {
                if (item.IsSystemItem)
                {
                    switch (item.SystemAction.ToLowerInvariant())
                    {
                        case "start":
                            // Trigger Windows Start Menu
                            keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
                            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                            return;

                        case "explorer":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                UseShellExecute = true
                            });
                            return;

                        case "notepad":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "notepad.exe",
                                UseShellExecute = true
                            });
                            return;

                        case "calc":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "calc.exe",
                                UseShellExecute = true
                            });
                            return;

                        case "terminal":
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = "wt.exe",
                                    UseShellExecute = true
                                });
                            }
                            catch
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    UseShellExecute = true
                                });
                            }
                            return;

                        case "settings":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "ms-settings:",
                                UseShellExecute = true
                            });
                            return;

                        case "store":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "ms-windows-store:",
                                UseShellExecute = true
                            });
                            return;

                        case "recyclebin":
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = "shell:RecycleBinFolder",
                                UseShellExecute = true
                            });
                            return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.ExecutablePath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = item.ExecutablePath,
                        Arguments = item.Arguments ?? "",
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(item.ExecutablePath) ?? ""
                    };
                    Process.Start(psi);
                    Logger.Info($"Launched application: {item.Title} ({item.ExecutablePath})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to launch new instance of {item.Title}", ex);
            }
        }
    }
}
