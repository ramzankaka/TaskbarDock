using System;
using System.Windows;

namespace TaskbarDock.Models
{
    public class MonitorInfo
    {
        public string DeviceName { get; set; } = string.Empty;
        public Rect Bounds { get; set; }
        public Rect WorkArea { get; set; }
        public bool IsPrimary { get; set; }
        public double DpiScaleX { get; set; } = 1.0;
        public double DpiScaleY { get; set; } = 1.0;
        public int DpiX { get; set; } = 96;
        public int DpiY { get; set; } = 96;

        public override string ToString() =>
            $"{DeviceName} ({(IsPrimary ? "Primary, " : "")}{Bounds.Width}x{Bounds.Height} @ {DpiScaleX:P0})";
    }
}
