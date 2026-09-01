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
            Items.Add(item);
            _configManager.Settings.Items = Items.ToList();
            _configManager.SaveSettings();
        }

        public void RemoveItem(DockItem item)
        {
            Items.Remove(item);
            _configManager.Settings.Items = Items.ToList();
            _configManager.SaveSettings();
        }

        public void MoveUp(DockItem item)
        {
            int idx = Items.IndexOf(item);
            if (idx > 0)
            {
                Items.Move(idx, idx - 1);
                _configManager.Settings.Items = Items.ToList();
                _configManager.SaveSettings();
            }
        }

        public void MoveDown(DockItem item)
        {
            int idx = Items.IndexOf(item);
            if (idx >= 0 && idx < Items.Count - 1)
            {
                Items.Move(idx, idx + 1);
                _configManager.Settings.Items = Items.ToList();
                _configManager.SaveSettings();
            }
        }

        public void ResetDefaults()
        {
            _configManager.Settings.Items = _configManager.GetDefaultDockItems();
            _configManager.SaveSettings();
            ReloadItems();
        }
    }
}
