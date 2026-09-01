using System;
using System.Collections.Generic;

namespace TaskbarDock.Models
{
    public class AppSettings
    {
        public string Mode { get; set; } = "windows"; // "windows" or "macos"
        public bool StartWithWindows { get; set; } = false;
        public string StartMode { get; set; } = "saved"; // "saved", "windows", "macos"
        public bool MinimizeToTray { get; set; } = true;
        public string GlobalShortcut { get; set; } = "Ctrl+Alt+D";

        public DockVisualConfig Dock { get; set; } = new();
        public DockBehaviorConfig Behavior { get; set; } = new();
        public MonitorConfig Monitor { get; set; } = new();
        public List<DockItem> Items { get; set; } = new();
    }

    public class DockVisualConfig
    {
        public double IconSize { get; set; } = 56.0;
        public bool MagnificationEnabled { get; set; } = true;
        public double MaxMagnification { get; set; } = 1.65;
        public double MagnificationRange { get; set; } = 140.0;
        public double AnimationSpeed { get; set; } = 1.0;
        public double DockOpacity { get; set; } = 0.88;
        public bool BlurEnabled { get; set; } = true;
        public string BlurType { get; set; } = "Acrylic"; // "Acrylic", "Mica", "Transparent"
        public double CornerRadius { get; set; } = 18.0;
        public double BottomSpacing { get; set; } = 10.0;
        public bool ShadowEnabled { get; set; } = true;
        public string Theme { get; set; } = "System"; // "System", "Dark", "Light"
    }

    public class DockBehaviorConfig
    {
        public bool AutoHide { get; set; } = false;
        public int RevealDelayMs { get; set; } = 100;
        public int HideDelayMs { get; set; } = 350;
        public bool ActivateRunningApp { get; set; } = true;
        public bool MinimizeOnSecondClick { get; set; } = true;
        public bool ShowRunningIndicators { get; set; } = true;
        public bool BounceOnLaunch { get; set; } = true;
    }

    public class MonitorConfig
    {
        public string Mode { get; set; } = "Primary"; // "Primary", "All", "Selected"
        public string SelectedMonitorDevice { get; set; } = string.Empty;
    }
}
