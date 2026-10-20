using System;
using System.Collections.ObjectModel;
using System.Linq;
using TaskbarDock.Core;
using TaskbarDock.Models;
using TaskbarDock.WindowsIntegration;

namespace TaskbarDock.Dock
{
    public class DockItemManager
    {
        private readonly ConfigurationManager _configManager;
        public ObservableCollection<DockItem> Items { get; } = new();

        public DockItemManager(ConfigurationManager configManager)
        {
            _configManager = configManager;
            ReloadItems();
        }

        public void ReloadItems()
        {
            Items.Clear();
            foreach (var item in _configManager.Settings.Items)
            {
                item.IconImage = IconExtractor.ExtractIcon(item.CustomIconPath ?? item.ExecutablePath, item.SystemAction);
                item.CurrentSize = _configManager.Settings.Dock.IconSize;
                Items.Add(item);
            }
        }

        public void AddItem(DockItem item)
        {
            item.IconImage = IconExtractor.ExtractIcon(item.CustomIconPath ?? item.ExecutablePath, item.SystemAction);
            item.CurrentSize = _configManager.Settings.Dock.IconSize;
            item.IsPinned = true;
            Items.Add(item);
            SavePinnedItems();
        }

        public void AddAppByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            string title = System.IO.Path.GetFileNameWithoutExtension(path);
            var item = new DockItem
            {
                Title = title,
                ExecutablePath = path,
                IsPinned = true,
                CurrentSize = _configManager.Settings.Dock.IconSize,
                IconImage = IconExtractor.ExtractIcon(path)
            };
            Items.Add(item);
            SavePinnedItems();
        }

        public void PinItem(DockItem item)
        {
            item.IsPinned = true;
            SavePinnedItems();
        }

        public void UnpinItem(DockItem item)
        {
            item.IsPinned = false;
            SavePinnedItems();

            if (!item.IsRunning)
            {
                Items.Remove(item);
            }
        }

        public void CloseAppWindows(DockItem item)
        {
            var handles = item.WindowHandles.ToList();
            foreach (var h in handles)
            {
                WindowManager.CloseWindow(h);
            }
        }

        public void SavePinnedItems()
        {
            _configManager.Settings.Items = Items.Where(i => i.IsPinned).ToList();
            _configManager.SaveSettings();
        }

        public void RemoveItem(DockItem item)
        {
            Items.Remove(item);
            SavePinnedItems();
        }

        public void MoveUp(DockItem item)
        {
            int idx = Items.IndexOf(item);
            if (idx > 0)
            {
                Items.Move(idx, idx - 1);
                SavePinnedItems();
            }
        }

        public void MoveDown(DockItem item)
        {
            int idx = Items.IndexOf(item);
            if (idx >= 0 && idx < Items.Count - 1)
            {
                Items.Move(idx, idx + 1);
                SavePinnedItems();
            }
        }

        public void ResetDefaults()
        {
            _configManager.Settings.Items = _configManager.GetDefaultDockItems();
            _configManager.SaveSettings();
            ReloadItems();
        }

        public void UpdateRunningWindows(List<WindowInfo> windows)
        {
            void Apply()
            {
                var matchedHwnds = new HashSet<IntPtr>();

                // First pass: Match existing dock items (pinned or already tracked unpinned)
                foreach (var item in Items)
                {
                    item.WindowHandles.Clear();
                    item.ProcessIds.Clear();

                    string targetExe = !string.IsNullOrWhiteSpace(item.ExecutablePath)
                        ? System.IO.Path.GetFileNameWithoutExtension(item.ExecutablePath).ToLowerInvariant()
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
                        matchedHwnds.Add(m.Handle);
                    }
                }

                // Second pass: Remove unpinned items that are no longer running
                var unpinnedToRemove = Items.Where(i => !i.IsPinned && !i.IsRunning).ToList();
                foreach (var rem in unpinnedToRemove)
                {
                    Items.Remove(rem);
                }

                // Third pass: For remaining windows that were not matched to any dock item, add dynamic unpinned items!
                var unmatchedWindows = windows.Where(w => !matchedHwnds.Contains(w.Handle)).ToList();
                var groupedByApp = unmatchedWindows
                    .GroupBy(w => !string.IsNullOrEmpty(w.ExecutablePath) ? w.ExecutablePath.ToLowerInvariant() : w.ProcessName.ToLowerInvariant());

                foreach (var group in groupedByApp)
                {
                    var firstWin = group.First();
                    // Check if already in Items (maybe newly added)
                    bool alreadyExists = Items.Any(i =>
                        (!string.IsNullOrEmpty(i.ExecutablePath) && !string.IsNullOrEmpty(firstWin.ExecutablePath) && i.ExecutablePath.Equals(firstWin.ExecutablePath, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(firstWin.ProcessName) && i.Title.Equals(firstWin.ProcessName, StringComparison.OrdinalIgnoreCase)));

                    if (!alreadyExists)
                    {
                        string title = !string.IsNullOrWhiteSpace(firstWin.Title) ? firstWin.Title : firstWin.ProcessName;
                        if (title.Length > 24)
                            title = firstWin.ProcessName;

                        var dynamicItem = new DockItem
                        {
                            Title = !string.IsNullOrWhiteSpace(title) ? title : "Application",
                            CustomTitle = firstWin.ProcessName,
                            ExecutablePath = firstWin.ExecutablePath,
                            IsPinned = false,
                            IsRunning = true,
                            CurrentSize = _configManager.Settings.Dock.IconSize,
                            IconImage = IconExtractor.ExtractIconFromWindow(firstWin.Handle, firstWin.ExecutablePath)
                        };

                        foreach (var win in group)
                        {
                            dynamicItem.WindowHandles.Add(win.Handle);
                            if (!dynamicItem.ProcessIds.Contains(win.ProcessId))
                                dynamicItem.ProcessIds.Add(win.ProcessId);
                        }

                        Items.Add(dynamicItem);
                    }
                }
            }

            if (System.Windows.Application.Current != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(Apply);
            }
            else
            {
                Apply();
            }
        }
    }
}
