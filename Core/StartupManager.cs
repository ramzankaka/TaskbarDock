using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using TaskbarDock.Diagnostics;

namespace TaskbarDock.Core
{
    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "TaskbarDock";

        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to query startup registry key", ex);
                return false;
            }
        }

        public static bool SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) return false;

                if (enable)
                {
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\" --startup");
                        Logger.Info("Registered TaskbarDock in Windows Startup.");
                        return true;
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    Logger.Info("Removed TaskbarDock from Windows Startup.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to modify startup registry key", ex);
            }

            return false;
        }
    }
}
