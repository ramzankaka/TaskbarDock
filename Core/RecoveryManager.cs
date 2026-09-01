using System;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using TaskbarDock.Diagnostics;
using TaskbarDock.WindowsIntegration;

namespace TaskbarDock.Core
{
    public class RecoveryState
    {
        public string LastMode { get; set; } = "windows";
        public bool DockWasActiveOnCrash { get; set; } = false;
        public DateTime HeartbeatUtc { get; set; } = DateTime.UtcNow;
        public int ProcessId { get; set; } = Environment.ProcessId;
    }

    public class RecoveryManager
    {
        private static readonly string _stateFilePath;
        private readonly DispatcherTimer _heartbeatTimer;
        private readonly TaskbarManager _taskbarManager;

        static RecoveryManager()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(localAppData, "TaskbarDock");
            Directory.CreateDirectory(dir);
            _stateFilePath = Path.Combine(dir, "recovery_state.json");
        }

        public RecoveryManager(TaskbarManager taskbarManager)
        {
            _taskbarManager = taskbarManager;
            _heartbeatTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _heartbeatTimer.Tick += (s, e) => WriteHeartbeat();
        }

        public bool CheckAndPerformEmergencyRecovery()
        {
            try
            {
                if (!File.Exists(_stateFilePath))
                    return false;

                string json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<RecoveryState>(json);
                if (state != null && state.DockWasActiveOnCrash)
                {
                    Logger.Recovery("Abnormal exit detected while Dock Mode was active! Executing emergency taskbar restoration...");
                    _taskbarManager.RestoreTaskbar();
                    
                    state.DockWasActiveOnCrash = false;
                    state.LastMode = "windows";
                    File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state));
                    Logger.Recovery("Emergency taskbar restoration completed.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error checking recovery state", ex);
            }

            return false;
        }

        public void SetDockActive(bool active)
        {
            try
            {
                var state = new RecoveryState
                {
                    LastMode = active ? "macos" : "windows",
                    DockWasActiveOnCrash = active,
                    HeartbeatUtc = DateTime.UtcNow,
                    ProcessId = Environment.ProcessId
                };

                File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state));

                if (active)
                {
                    _heartbeatTimer.Start();
                }
                else
                {
                    _heartbeatTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving recovery active state", ex);
            }
        }

        public static void ForceRestoreTaskbarSync()
        {
            try
            {
                var mgr = new TaskbarManager();
                mgr.RestoreTaskbar();

                if (File.Exists(_stateFilePath))
                {
                    var state = new RecoveryState
                    {
                        LastMode = "windows",
                        DockWasActiveOnCrash = false,
                        HeartbeatUtc = DateTime.UtcNow,
                        ProcessId = Environment.ProcessId
                    };
                    File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state));
                }
            }
            catch { }
        }

        private void WriteHeartbeat()
        {
            try
            {
                var state = new RecoveryState
                {
                    LastMode = "macos",
                    DockWasActiveOnCrash = true,
                    HeartbeatUtc = DateTime.UtcNow,
                    ProcessId = Environment.ProcessId
                };
                File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state));
            }
            catch { }
        }
    }
}
