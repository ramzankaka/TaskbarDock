using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaskbarDock.Diagnostics;
using TaskbarDock.Models;
using TaskbarDock.WindowsIntegration;

namespace TaskbarDock.Core
{
    public class ConfigurationManager
    {
        private static readonly string _configPath;
        private static readonly string _backupPath;
        private AppSettings _settings = new();

        public AppSettings Settings => _settings;

        static ConfigurationManager()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(localAppData, "TaskbarDock");
            Directory.CreateDirectory(dir);
            _configPath = Path.Combine(dir, "settings.json");
            _backupPath = Path.Combine(dir, "settings.json.bak");
        }

        public static string ConfigDirectory => Path.GetDirectoryName(_configPath)!;

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });

                    if (loaded != null)
                    {
                        _settings = ValidateAndSanitize(loaded);
                        Logger.Info("Configuration loaded successfully.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to parse settings.json. Backing up corrupted file and loading safe defaults.", ex);
                try
                {
                    if (File.Exists(_configPath))
                        File.Copy(_configPath, _backupPath, true);
                }
                catch { }
            }

            _settings = GetDefaultSettings();
            SaveSettings();
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_configPath, json);
                Logger.Debug("Settings saved to disk.");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings to disk", ex);
            }
        }

        public void ExportSettings(string targetPath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(targetPath, json);
        }

        public void ImportSettings(string sourcePath)
        {
            string json = File.ReadAllText(sourcePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            if (loaded != null)
            {
                _settings = ValidateAndSanitize(loaded);
                SaveSettings();
            }
        }

        public void ResetToDefaults()
        {
            _settings = GetDefaultSettings();
            SaveSettings();
        }

        private AppSettings ValidateAndSanitize(AppSettings s)
        {
            s.Dock.IconSize = Math.Clamp(s.Dock.IconSize, 32.0, 128.0);
            s.Dock.MaxMagnification = Math.Clamp(s.Dock.MaxMagnification, 1.0, 2.5);
            s.Dock.MagnificationRange = Math.Clamp(s.Dock.MagnificationRange, 60.0, 300.0);
            s.Dock.DockOpacity = Math.Clamp(s.Dock.DockOpacity, 0.2, 1.0);
            s.Dock.CornerRadius = Math.Clamp(s.Dock.CornerRadius, 0.0, 40.0);
            s.Dock.BottomSpacing = Math.Clamp(s.Dock.BottomSpacing, 0.0, 50.0);

            if (s.Items == null || s.Items.Count == 0)
            {
                s.Items = GetDefaultDockItems();
            }

            return s;
        }

        public AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                Mode = "windows",
                StartWithWindows = false,
                StartMode = "saved",
                MinimizeToTray = true,
                GlobalShortcut = "Ctrl+Alt+D",
                Dock = new DockVisualConfig(),
                Behavior = new DockBehaviorConfig(),
                Monitor = new MonitorConfig(),
                Items = GetDefaultDockItems()
            };
        }

        public List<DockItem> GetDefaultDockItems()
        {
            var list = new List<DockItem>
            {
                new DockItem { Title = "Start", IsSystemItem = true, SystemAction = "start", IsPinned = true },
                new DockItem { Title = "File Explorer", IsSystemItem = true, SystemAction = "explorer", ExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), IsPinned = true },
                new DockItem { Title = "Microsoft Edge", ExecutablePath = GetEdgePath(), IsPinned = true },
                new DockItem { Title = "Windows Terminal", IsSystemItem = true, SystemAction = "terminal", IsPinned = true },
                new DockItem { Title = "Notepad", IsSystemItem = true, SystemAction = "notepad", ExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe"), IsPinned = true },
                new DockItem { Title = "Calculator", IsSystemItem = true, SystemAction = "calc", ExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "calc.exe"), IsPinned = true },
                new DockItem { Title = "Settings", IsSystemItem = true, SystemAction = "settings", IsPinned = true },
                new DockItem { Title = "Microsoft Store", IsSystemItem = true, SystemAction = "store", IsPinned = true },
                new DockItem { Title = "Recycle Bin", IsSystemItem = true, SystemAction = "recyclebin", IsPinned = true }
            };

            return list;
        }

        private static string GetEdgePath()
        {
            string p = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            if (File.Exists(p)) return p;
            p = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
            if (File.Exists(p)) return p;
            return "";
        }
    }
}
