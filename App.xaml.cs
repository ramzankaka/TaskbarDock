using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using TaskbarDock.Core;
using TaskbarDock.Diagnostics;
using TaskbarDock.Dock;
using TaskbarDock.UI;
using TaskbarDock.WindowsIntegration;

namespace TaskbarDock
{
    public partial class App : System.Windows.Application
    {
        private const string MutexName = "TaskbarDock_SingleInstance_Mutex_90D5";
        private Mutex? _instanceMutex;

        private ConfigurationManager? _config;
        private TaskbarManager? _taskbar;
        private RecoveryManager? _recovery;
        private ModeManager? _modeManager;
        private DockItemManager? _itemManager;
        private RunningAppsTracker? _runningAppsTracker;
        private DockWindow? _dockWindow;
        private SettingsWindow? _settingsWindow;
        private TrayController? _trayController;
        private GlobalHotkeyManager? _hotkeyManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Contains("--restore-taskbar"))
            {
                RecoveryManager.ForceRestoreTaskbarSync();
                Environment.Exit(0);
                return;
            }

            _instanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show("TaskbarDock is already running in the system tray.", "TaskbarDock", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += (s, args) =>
            {
                Logger.Error("Dispatcher unhandled exception", args.Exception);
                EmergencySafetyRestore();
            };
            AppDomain.CurrentDomain.ProcessExit += (s, args) => EmergencySafetyRestore();
            SessionEnding += (s, args) => EmergencySafetyRestore();

            Logger.Info("=== TaskbarDock Starting ===");

            try
            {
                InitializeApp(e.Args);
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal exception during app startup", ex);
                EmergencySafetyRestore();
                Shutdown();
            }
        }

        private void InitializeApp(string[] args)
        {
            _taskbar = new TaskbarManager();
            _recovery = new RecoveryManager(_taskbar);
            _config = new ConfigurationManager();
            _config.LoadSettings();

            _modeManager = new ModeManager(_config, _taskbar, _recovery);
            _itemManager = new DockItemManager(_config);
            _runningAppsTracker = new RunningAppsTracker();

            _dockWindow = new DockWindow(_config, _itemManager, _runningAppsTracker);
            _modeManager.SetDockWindow(_dockWindow);

            _settingsWindow = new SettingsWindow(_config, _modeManager, _dockWindow, _taskbar);
            _trayController = new TrayController(_modeManager, _config, _taskbar, ShowSettings);

            _dockWindow.Loaded += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(_dockWindow).Handle;
                _hotkeyManager = new GlobalHotkeyManager();
                _hotkeyManager.Register(hwnd, _config.Settings.GlobalShortcut);
                _hotkeyManager.HotkeyPressed += () => _modeManager.ToggleMode();
            };

            bool recovered = _recovery.CheckAndPerformEmergencyRecovery();

            string initialMode = _config.Settings.StartMode;
            if (recovered || initialMode == "windows")
            {
                _modeManager.SwitchToMode(DockMode.WindowsTaskbar);
            }
            else if (initialMode == "macos" || (initialMode == "saved" && _config.Settings.Mode == "macos"))
            {
                _modeManager.SwitchToMode(DockMode.MacOSDock);
            }
            else
            {
                _modeManager.SwitchToMode(DockMode.WindowsTaskbar);
            }

            if (args.Contains("--settings"))
            {
                ShowSettings();
            }
        }

        private void ShowSettings()
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Show();
                _settingsWindow.Activate();
            }
        }

        private void EmergencySafetyRestore()
        {
            try
            {
                _taskbar?.RestoreTaskbar();
                _recovery?.SetDockActive(false);
            }
            catch { }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error("AppDomain unhandled exception", e.ExceptionObject as Exception);
            EmergencySafetyRestore();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            EmergencySafetyRestore();
            _trayController?.Dispose();
            _hotkeyManager?.Dispose();
            _instanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
