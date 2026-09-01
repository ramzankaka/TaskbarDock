using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TaskbarDock.Diagnostics;
using static TaskbarDock.WindowsIntegration.NativeMethods;

namespace TaskbarDock.WindowsIntegration
{
    public class TaskbarManager
    {
        private static readonly object _lock = new();
        private bool _isTaskbarHidden;

        public bool IsTaskbarHidden => _isTaskbarHidden;

        public List<IntPtr> FindAllTaskbarHandles()
        {
            var handles = new List<IntPtr>();

            // Primary taskbar
            IntPtr primary = FindWindow("Shell_TrayWnd", null);
            if (primary != IntPtr.Zero)
            {
                handles.Add(primary);
            }

            // Secondary taskbars on multi-monitors
            IntPtr prev = IntPtr.Zero;
            while (true)
            {
                IntPtr secondary = FindWindowEx(IntPtr.Zero, prev, "Shell_SecondaryTrayWnd", null);
                if (secondary == IntPtr.Zero || handles.Contains(secondary))
                    break;
                handles.Add(secondary);
                prev = secondary;
            }

            return handles;
        }

        public bool HideTaskbar()
        {
            lock (_lock)
            {
                try
                {
                    Logger.Info("Hiding Windows Taskbar...");
                    var handles = FindAllTaskbarHandles();
                    if (handles.Count == 0)
                    {
                        Logger.Warn("No taskbar window handles found to hide.");
                        return false;
                    }

                    foreach (var h in handles)
                    {
                        ShowWindow(h, SW_HIDE);
                        SetWindowPos(h, IntPtr.Zero, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_HIDEWINDOW);
                    }

                    _isTaskbarHidden = true;
                    Logger.Info($"Windows Taskbar hidden successfully on {handles.Count} displays.");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to hide Windows Taskbar", ex);
                    return false;
                }
            }
        }

        public bool RestoreTaskbar()
        {
            lock (_lock)
            {
                try
                {
                    Logger.Info("Restoring Windows Taskbar...");
                    var handles = FindAllTaskbarHandles();
                    if (handles.Count == 0)
                    {
                        Logger.Warn("No taskbar window handles found when restoring. Checking Explorer state...");
                        EnsureExplorerRunning();
                        handles = FindAllTaskbarHandles();
                    }

                    foreach (var h in handles)
                    {
                        ShowWindow(h, SW_SHOW);
                        SetWindowPos(h, IntPtr.Zero, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                        RedrawWindow(h, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_INTERNALPAINT | RDW_ALLCHILDREN | RDW_UPDATENOW);
                    }

                    _isTaskbarHidden = false;
                    Logger.Info($"Windows Taskbar restored successfully on {handles.Count} displays.");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to restore Windows Taskbar", ex);
                    return false;
                }
            }
        }

        public bool VerifyTaskbarVisible()
        {
            IntPtr primary = FindWindow("Shell_TrayWnd", null);
            if (primary == IntPtr.Zero) return false;
            return IsWindowVisible(primary);
        }

        public static void EnsureExplorerRunning()
        {
            try
            {
                var explorers = Process.GetProcessesByName("explorer");
                if (explorers.Length == 0)
                {
                    Logger.Warn("Explorer process was not running. Starting Explorer...");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to verify/start explorer.exe", ex);
            }
        }

        public static async Task RestartExplorerAsync()
        {
            try
            {
                Logger.Warn("User requested Explorer restart for clean taskbar reset.");
                foreach (var p in Process.GetProcessesByName("explorer"))
                {
                    try { p.Kill(); } catch { }
                }
                await Task.Delay(1000);
                EnsureExplorerRunning();
            }
            catch (Exception ex)
            {
                Logger.Error("Error restarting Explorer", ex);
            }
        }
    }
}
