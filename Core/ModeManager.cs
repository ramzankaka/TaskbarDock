using System;
using System.Windows;
using TaskbarDock.Diagnostics;
using TaskbarDock.Dock;
using TaskbarDock.WindowsIntegration;

namespace TaskbarDock.Core
{
    public enum DockMode
    {
        WindowsTaskbar,
        MacOSDock
    }

    public class ModeManager
    {
        private readonly ConfigurationManager _config;
        private readonly TaskbarManager _taskbar;
        private readonly RecoveryManager _recovery;
        private DockWindow? _dockWindow;
        private DockMode _currentMode = DockMode.WindowsTaskbar;

        public DockMode CurrentMode => _currentMode;
        public event Action<DockMode>? ModeChanged;

        public ModeManager(ConfigurationManager config, TaskbarManager taskbar, RecoveryManager recovery)
        {
            _config = config;
            _taskbar = taskbar;
            _recovery = recovery;
        }

        public void SetDockWindow(DockWindow window)
        {
            _dockWindow = window;
        }

        public bool SwitchToMode(DockMode mode)
        {
            try
            {
                Logger.Info($"Switching mode from {_currentMode} to {mode}...");

                if (mode == DockMode.MacOSDock)
                {
                    // 1. Mark recovery state as active
                    _recovery.SetDockActive(true);

                    // 2. Hide Windows Taskbar
                    _taskbar.HideTaskbar();

                    // 3. Show macOS Dock Window
                    if (_dockWindow != null)
                    {
                        _dockWindow.ShowDock();
                    }

                    _currentMode = DockMode.MacOSDock;
                    _config.Settings.Mode = "macos";
                    _config.SaveSettings();
                    Logger.Info("Successfully transitioned to macOS Dock Mode.");
                }
                else
                {
                    // 1. Hide Dock Window
                    if (_dockWindow != null)
                    {
                        _dockWindow.HideDock();
                    }

                    // 2. Restore Windows Taskbar
                    _taskbar.RestoreTaskbar();

                    // 3. Update recovery state
                    _recovery.SetDockActive(false);

                    _currentMode = DockMode.WindowsTaskbar;
                    _config.Settings.Mode = "windows";
                    _config.SaveSettings();
                    Logger.Info("Successfully transitioned to Windows Taskbar Mode.");
                }

                ModeChanged?.Invoke(_currentMode);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed mode transition to {mode}", ex);
                // Emergency fail-safe
                _taskbar.RestoreTaskbar();
                _recovery.SetDockActive(false);
                return false;
            }
        }

        public void ToggleMode()
        {
            if (_currentMode == DockMode.WindowsTaskbar)
                SwitchToMode(DockMode.MacOSDock);
            else
                SwitchToMode(DockMode.WindowsTaskbar);
        }
    }
}
