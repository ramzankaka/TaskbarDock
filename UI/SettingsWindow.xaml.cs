using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TaskbarDock.Core;
using TaskbarDock.Diagnostics;
using TaskbarDock.Dock;
using TaskbarDock.Models;
using TaskbarDock.WindowsIntegration;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using MessageBox = System.Windows.MessageBox;

namespace TaskbarDock.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationManager _config;
        private readonly ModeManager _modeManager;
        private readonly DockWindow _dockWindow;
        private readonly TaskbarManager _taskbar;
        private bool _isInitializing = true;

        public SettingsWindow(ConfigurationManager config, ModeManager modeManager, DockWindow dockWindow, TaskbarManager taskbar)
        {
            _config = config;
            _modeManager = modeManager;
            _dockWindow = dockWindow;
            _taskbar = taskbar;

            InitializeComponent();
            LoadValuesFromConfig();
            _isInitializing = false;
        }

        private void LoadValuesFromConfig()
        {
            var s = _config.Settings;
            ChkStartup.IsChecked = StartupManager.IsStartupEnabled();
            ChkMinimizeToTray.IsChecked = s.MinimizeToTray;

            SliderIconSize.Value = s.Dock.IconSize;
            ChkMagnification.IsChecked = s.Dock.MagnificationEnabled;
            SliderMaxScale.Value = s.Dock.MaxMagnification;
            SliderOpacity.Value = s.Dock.DockOpacity;
            SliderRadius.Value = s.Dock.CornerRadius;
            SliderBottomSpacing.Value = s.Dock.BottomSpacing;

            ChkAutoHide.IsChecked = s.Behavior.AutoHide;
            ChkActivateRunning.IsChecked = s.Behavior.ActivateRunningApp;
            ChkMinimizeOnSecondClick.IsChecked = s.Behavior.MinimizeOnSecondClick;
            ChkShowIndicators.IsChecked = s.Behavior.ShowRunningIndicators;
            ChkBounceOnLaunch.IsChecked = s.Behavior.BounceOnLaunch;

            ListDockApps.ItemsSource = _dockWindow.ItemManager.Items;
        }

        private void OnSettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            var s = _config.Settings;
            if (ChkStartup.IsChecked.HasValue)
                StartupManager.SetStartup(ChkStartup.IsChecked.Value);

            s.MinimizeToTray = ChkMinimizeToTray.IsChecked ?? true;

            s.Dock.IconSize = SliderIconSize.Value;
            s.Dock.MagnificationEnabled = ChkMagnification.IsChecked ?? true;
            s.Dock.MaxMagnification = SliderMaxScale.Value;
            s.Dock.DockOpacity = SliderOpacity.Value;
            s.Dock.CornerRadius = SliderRadius.Value;
            s.Dock.BottomSpacing = SliderBottomSpacing.Value;

            s.Behavior.AutoHide = ChkAutoHide.IsChecked ?? false;
            s.Behavior.ActivateRunningApp = ChkActivateRunning.IsChecked ?? true;
            s.Behavior.MinimizeOnSecondClick = ChkMinimizeOnSecondClick.IsChecked ?? true;
            s.Behavior.ShowRunningIndicators = ChkShowIndicators.IsChecked ?? true;
            s.Behavior.BounceOnLaunch = ChkBounceOnLaunch.IsChecked ?? true;

            _config.SaveSettings();
            _dockWindow.ApplyConfigStyles();
        }

        private void OnToggleModeClicked(object sender, RoutedEventArgs e)
        {
            _modeManager.ToggleMode();
        }

        private void OnAddAppClicked(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Application to Add to Dock"
            };

            if (dlg.ShowDialog() == true)
            {
                string title = Path.GetFileNameWithoutExtension(dlg.FileName);
                var item = new DockItem
                {
                    Title = title,
                    ExecutablePath = dlg.FileName,
                    IsPinned = true
                };
                _dockWindow.ItemManager.AddItem(item);
            }
        }

        private void OnRemoveAppClicked(object sender, RoutedEventArgs e)
        {
            if (ListDockApps.SelectedItem is DockItem item)
            {
                _dockWindow.ItemManager.RemoveItem(item);
            }
        }

        private void OnMoveUpClicked(object sender, RoutedEventArgs e)
        {
            if (ListDockApps.SelectedItem is DockItem item)
            {
                _dockWindow.ItemManager.MoveUp(item);
            }
        }

        private void OnMoveDownClicked(object sender, RoutedEventArgs e)
        {
            if (ListDockApps.SelectedItem is DockItem item)
            {
                _dockWindow.ItemManager.MoveDown(item);
            }
        }

        private void OnResetAppsClicked(object sender, RoutedEventArgs e)
        {
            _dockWindow.ItemManager.ResetDefaults();
        }

        private void OnEmergencyRestoreClicked(object sender, RoutedEventArgs e)
        {
            _modeManager.SwitchToMode(DockMode.WindowsTaskbar);
            _taskbar.RestoreTaskbar();
            MessageBox.Show("Windows Taskbar restoration command executed.", "Taskbar Restored", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void OnRestartExplorerClicked(object sender, RoutedEventArgs e)
        {
            await TaskbarManager.RestartExplorerAsync();
            MessageBox.Show("Explorer restart completed.", "Explorer Restarted", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnOpenLogsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Logger.LogDirectory,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void OnResetAllClicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset all settings to default values?", "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _config.ResetToDefaults();
                LoadValuesFromConfig();
                _dockWindow.ApplyConfigStyles();
                _dockWindow.ItemManager.ReloadItems();
            }
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
