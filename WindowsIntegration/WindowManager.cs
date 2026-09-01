using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TaskbarDock.Diagnostics;
using static TaskbarDock.WindowsIntegration.NativeMethods;

namespace TaskbarDock.WindowsIntegration
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
    }

    public static class WindowManager
    {
        public static bool ActivateWindow(IntPtr hWnd, bool minimizeIfActive = true)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return false;

            try
            {
                IntPtr fg = GetForegroundWindow();
                if (fg == hWnd && minimizeIfActive)
                {
                    ShowWindowAsync(hWnd, SW_MINIMIZE);
                    return true;
                }

                ShowWindowAsync(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
                BringWindowToTop(hWnd);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to activate window handle {hWnd}", ex);
                return false;
            }
        }

        public static List<WindowInfo> GetTopLevelAppWindows()
        {
            var list = new List<WindowInfo>();
            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;

                    // Filter out toolwindows & cloaked windows
                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

                    // Check cloaked state (virtual desktops / UWP suspended)
                    DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int isCloaked, sizeof(int));
                    if (isCloaked != 0) return true;

                    var sbTitle = new StringBuilder(256);
                    GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
                    string title = sbTitle.ToString();

                    var sbClass = new StringBuilder(256);
                    GetClassName(hWnd, sbClass, sbClass.Capacity);
                    string className = sbClass.ToString();

                    // Filter known system overlays and taskbars
                    if (className == "Shell_TrayWnd" || className == "Shell_SecondaryTrayWnd" ||
                        className == "Progman" || className == "WorkerW" || className == "Windows.UI.Core.CoreWindow" && string.IsNullOrEmpty(title))
                    {
                        return true;
                    }

                    if (string.IsNullOrWhiteSpace(title) && className != "ApplicationFrameWindow")
                    {
                        return true;
                    }

                    GetWindowThreadProcessId(hWnd, out uint pid);
                    string procName = "";
                    string exePath = "";
                    try
                    {
                        var proc = Process.GetProcessById((int)pid);
                        procName = proc.ProcessName;
                        try { exePath = proc.MainModule?.FileName ?? ""; } catch { }
                    }
                    catch { }

                    list.Add(new WindowInfo
                    {
                        Handle = hWnd,
                        ProcessId = (int)pid,
                        ProcessName = procName,
                        ExecutablePath = exePath,
                        Title = title,
                        ClassName = className
                    });

                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.Error("Error enumerating windows", ex);
            }

            return list;
        }
    }
}
