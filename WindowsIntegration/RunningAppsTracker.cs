using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using TaskbarDock.Models;

namespace TaskbarDock.WindowsIntegration
{
    public class RunningAppsTracker
    {
        private readonly DispatcherTimer _timer;
        private List<DockItem> _dockItems = new();

        public event Action? RunningAppsChanged;

        public RunningAppsTracker()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _timer.Tick += (s, e) => CheckRunningApps();
        }

        public void Start(List<DockItem> items)
        {
            _dockItems = items;
            _timer.Start();
            CheckRunningApps();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void UpdateItemsList(List<DockItem> items)
        {
            _dockItems = items;
            CheckRunningApps();
        }

        private void CheckRunningApps()
        {
            var windows = WindowManager.GetTopLevelAppWindows();

            foreach (var item in _dockItems)
            {
                item.WindowHandles.Clear();
                item.ProcessIds.Clear();

                if (string.IsNullOrWhiteSpace(item.ExecutablePath) && string.IsNullOrWhiteSpace(item.SystemAction))
                {
                    item.IsRunning = false;
                    continue;
                }

                string targetExe = !string.IsNullOrWhiteSpace(item.ExecutablePath) 
                    ? Path.GetFileNameWithoutExtension(item.ExecutablePath).ToLowerInvariant() 
                    : "";

                var matched = windows.Where(w =>
                {
                    if (item.IsSystemItem)
                    {
                        if (item.SystemAction == "explorer" && (w.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase) && w.ClassName == "CabinetWClass")) return true;
                        if (item.SystemAction == "terminal" && (w.ProcessName.Contains("WindowsTerminal", StringComparison.OrdinalIgnoreCase) || w.ProcessName.Equals("cmd", StringComparison.OrdinalIgnoreCase) || w.ProcessName.Equals("powershell", StringComparison.OrdinalIgnoreCase))) return true;
                        if (item.SystemAction == "notepad" && w.ProcessName.Contains("notepad", StringComparison.OrdinalIgnoreCase)) return true;
                        if (item.SystemAction == "calc" && (w.ProcessName.Contains("CalculatorApp", StringComparison.OrdinalIgnoreCase) || w.ProcessName.Contains("calc", StringComparison.OrdinalIgnoreCase))) return true;
                    }

                    if (!string.IsNullOrEmpty(targetExe) && w.ProcessName.Equals(targetExe, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (!string.IsNullOrEmpty(item.ExecutablePath) && !string.IsNullOrEmpty(w.ExecutablePath) &&
                        item.ExecutablePath.Equals(w.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                        return true;

                    return false;
                }).ToList();

                item.IsRunning = matched.Count > 0;
                foreach (var m in matched)
                {
                    item.WindowHandles.Add(m.Handle);
                    if (!item.ProcessIds.Contains(m.ProcessId))
                        item.ProcessIds.Add(m.ProcessId);
                }
            }

            RunningAppsChanged?.Invoke();
        }
    }
}
