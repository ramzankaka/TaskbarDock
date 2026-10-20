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
        public static bool ActivateWindow(IntPtr hWnd, bool minimizeIfActive = true, System.Windows.Rect? dockItemRect = null)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return false;

            try
            {
                IntPtr fg = GetForegroundWindow();
                if (fg == hWnd && minimizeIfActive)
                {
                    if (dockItemRect.HasValue)
                        AnimateWindowTransition(hWnd, dockItemRect.Value, true);

                    ShowWindowAsync(hWnd, SW_MINIMIZE);
                    return true;
                }

                if (IsIconic(hWnd))
                {
                    if (dockItemRect.HasValue)
                        AnimateWindowTransition(hWnd, dockItemRect.Value, false);
                    ShowWindow(hWnd, SW_RESTORE);
                }
                else
                {
                    ShowWindowAsync(hWnd, SW_SHOW);
                }

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

        public static void CloseWindow(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero && IsWindow(hWnd))
            {
                try
                {
                    PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to close window handle {hWnd}", ex);
                }
            }
        }

        public static void AnimateWindowTransition(IntPtr hWnd, System.Windows.Rect dockItemRect, bool isMinimizing)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return;
            try
            {
                if (GetWindowRect(hWnd, out RECT winRect))
                {
                    var dockRect = new RECT
                    {
                        Left = (int)dockItemRect.Left,
                        Top = (int)dockItemRect.Top,
                        Right = (int)dockItemRect.Right,
                        Bottom = (int)dockItemRect.Bottom
                    };

                    if (isMinimizing)
                        DrawAnimatedRects(hWnd, IDANI_CAPTION, ref winRect, ref dockRect);
                    else
                        DrawAnimatedRects(hWnd, IDANI_CAPTION, ref dockRect, ref winRect);
                }
            }
            catch { }
        }

        public static bool HasVisibleDesktopWindows(IntPtr dockHwnd)
        {
            bool hasVisibleWindows = false;
            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    if (hWnd == dockHwnd) return true;
                    if (!IsWindowVisible(hWnd)) return true;
                    if (IsIconic(hWnd)) return true; // Minimized windows are not showing on desktop!

                    // Filter out toolwindows & cloaked windows
                    int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                    if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

                    // Cloaked windows (other virtual desktops, suspended UWP)
                    DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int isCloaked, sizeof(int));
                    if (isCloaked != 0) return true;

                    var sbClass = new StringBuilder(256);
                    GetClassName(hWnd, sbClass, sbClass.Capacity);
                    string className = sbClass.ToString();

                    if (className == "Shell_TrayWnd" || className == "Shell_SecondaryTrayWnd" ||
                        className == "Progman" || className == "WorkerW" ||
                        className == "Windows.UI.Core.CoreWindow" || className == "ImmersiveLauncher" ||
                        className == "XamlExplorerHostIslandWindow")
                    {
                        return true;
                    }

                    if (!GetWindowRect(hWnd, out RECT rc)) return true;
                    // Ignore zero size or tiny utility windows
                    if (rc.Width < 100 || rc.Height < 100) return true;

                    var sbTitle = new StringBuilder(256);
                    GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
                    string title = sbTitle.ToString();

                    if (string.IsNullOrWhiteSpace(title) && className != "ApplicationFrameWindow")
                    {
                        return true;
                    }

                    // A non-minimized normal application window is present on desktop!
                    hasVisibleWindows = true;
                    return false; // Stop enumeration
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.Error("Error checking visible desktop windows", ex);
            }

            return hasVisibleWindows;
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
