using System;
using System.Drawing;
using System.Windows.Forms;
using TaskbarDock.Core;
using TaskbarDock.Diagnostics;
using TaskbarDock.Dock;
using TaskbarDock.WindowsIntegration;

namespace TaskbarDock.UI
{
    public class TrayController : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ModeManager _modeManager;
        private readonly ConfigurationManager _config;
        private readonly TaskbarManager _taskbar;
        private readonly Action _openSettingsAction;

        private ToolStripMenuItem? _switchModeItem;

        public TrayController(ModeManager modeManager, ConfigurationManager config, TaskbarManager taskbar, Action openSettingsAction)
        {
            _modeManager = modeManager;
            _config = config;
            _taskbar = taskbar;
            _openSettingsAction = openSettingsAction;

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "TaskbarDock - Windows 11 Taskbar to macOS Dock",
                Visible = true
            };

            _modeManager.ModeChanged += OnModeChanged;
            BuildContextMenu();
        }

        private void BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            _switchModeItem = new ToolStripMenuItem(
                _modeManager.CurrentMode == DockMode.MacOSDock ? "Switch to Windows Taskbar" : "Switch to macOS Dock",
                null,
                (s, e) => _modeManager.ToggleMode());
            _switchModeItem.Font = new Font(_switchModeItem.Font, FontStyle.Bold);

            var settingsItem = new ToolStripMenuItem("Settings", null, (s, e) => _openSettingsAction());
            var restoreTaskbarItem = new ToolStripMenuItem("Emergency Restore Taskbar", null, (s, e) =>
            {
                _modeManager.SwitchToMode(DockMode.WindowsTaskbar);
                _taskbar.RestoreTaskbar();
            });

            var restartExplorerItem = new ToolStripMenuItem("Restart Explorer", null, async (s, e) =>
            {
                await TaskbarManager.RestartExplorerAsync();
            });

            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) =>
            {
                Logger.Info("User initiated Exit from system tray.");
                _modeManager.SwitchToMode(DockMode.WindowsTaskbar);
                _taskbar.RestoreTaskbar();
                System.Windows.Application.Current.Shutdown();
            });

            menu.Items.Add(_switchModeItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(settingsItem);
            menu.Items.Add(restoreTaskbarItem);
            menu.Items.Add(restartExplorerItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => _openSettingsAction();
        }

        private void OnModeChanged(DockMode mode)
        {
            if (_switchModeItem != null)
            {
                _switchModeItem.Text = mode == DockMode.MacOSDock 
                    ? "Switch to Windows Taskbar" 
                    : "Switch to macOS Dock";
            }
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
