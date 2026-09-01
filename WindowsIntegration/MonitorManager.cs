using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using TaskbarDock.Diagnostics;
using TaskbarDock.Models;
using static TaskbarDock.WindowsIntegration.NativeMethods;

namespace TaskbarDock.WindowsIntegration
{
    public static class MonitorManager
    {
        public static List<MonitorInfo> GetAllMonitors()
        {
            var monitors = new List<MonitorInfo>();
            int index = 1;

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    uint dpiX = 96, dpiY = 96;
                    try
                    {
                        GetDpiForMonitor(hMonitor, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                    }
                    catch
                    {
                        dpiX = 96;
                        dpiY = 96;
                    }

                    bool isPrimary = (mi.dwFlags & 1) != 0;
                    monitors.Add(new MonitorInfo
                    {
                        DeviceName = $"Display {index++}",
                        Bounds = new Rect(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Width, mi.rcMonitor.Height),
                        WorkArea = new Rect(mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Width, mi.rcWork.Height),
                        IsPrimary = isPrimary,
                        DpiScaleX = dpiX / 96.0,
                        DpiScaleY = dpiY / 96.0,
                        DpiX = (int)dpiX,
                        DpiY = (int)dpiY
                    });
                }
                return true;
            }, IntPtr.Zero);

            if (monitors.Count == 0)
            {
                // Fallback to primary screen
                monitors.Add(new MonitorInfo
                {
                    DeviceName = "Primary Display",
                    Bounds = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight),
                    WorkArea = new Rect(0, 0, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height),
                    IsPrimary = true,
                    DpiScaleX = 1.0,
                    DpiScaleY = 1.0
                });
            }

            return monitors;
        }

        public static MonitorInfo GetPrimaryMonitor()
        {
            var monitors = GetAllMonitors();
            return monitors.Find(m => m.IsPrimary) ?? monitors[0];
        }
    }
}
