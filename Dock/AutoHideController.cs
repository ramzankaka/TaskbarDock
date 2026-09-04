using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using TaskbarDock.Diagnostics;
using TaskbarDock.Models;
using static TaskbarDock.WindowsIntegration.NativeMethods;

namespace TaskbarDock.Dock
{
    public class AutoHideController
    {
        private readonly Window _dockWindow;
        private readonly DispatcherTimer _edgeCheckTimer;
        private bool _isCurrentlyHidden;
        private DateTime _lastEdgeTouchTime = DateTime.MinValue;
        private DateTime _lastDockLeaveTime = DateTime.MinValue;
        private IntPtr _hwnd = IntPtr.Zero;

        public bool AutoHideEnabled { get; set; } = false;
        public int RevealDelayMs { get; set; } = 80;
        public int HideDelayMs { get; set; } = 350;
        public double TargetBottom { get; set; }
        public double HiddenOffset { get; set; } = 80.0;

        public event Action<bool>? VisibilityChanged;

        public AutoHideController(Window dockWindow)
        {
            _dockWindow = dockWindow;
            _edgeCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            _edgeCheckTimer.Tick += (s, e) => CheckMouseProximity();
        }

        public void Start()
        {
            if (_hwnd == IntPtr.Zero)
            {
                var helper = new WindowInteropHelper(_dockWindow);
                _hwnd = helper.Handle;
            }
            _edgeCheckTimer.Start();
        }

        public void Stop()
        {
            _edgeCheckTimer.Stop();
            _isCurrentlyHidden = false;
            VisibilityChanged?.Invoke(true);
        }

        private void CheckMouseProximity()
        {
            if (!AutoHideEnabled || !_dockWindow.IsVisible) return;

            if (_hwnd == IntPtr.Zero)
            {
                var helper = new WindowInteropHelper(_dockWindow);
                _hwnd = helper.Handle;
                if (_hwnd == IntPtr.Zero) return;
            }

            if (!GetCursorPos(out POINT pt)) return;

            // Get physical monitor where the cursor is currently located
            IntPtr hMonitor = MonitorFromPoint(pt, 2 /* MONITOR_DEFAULTTONEAREST */);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(hMonitor, ref mi)) return;

            // Physical bottom edge of the monitor
            int monitorBottom = mi.rcMonitor.Bottom;

            // Only consider it touching the edge if pointer is at the very bottom (last 2 physical pixels)
            bool isAtBottomEdge = pt.Y >= monitorBottom - 2;

            if (_isCurrentlyHidden)
            {
                // When hidden: ONLY reveal when pointer physically touches the bottom edge of the screen
                if (isAtBottomEdge)
                {
                    if (_lastEdgeTouchTime == DateTime.MinValue)
                        _lastEdgeTouchTime = DateTime.UtcNow;

                    if ((DateTime.UtcNow - _lastEdgeTouchTime).TotalMilliseconds >= RevealDelayMs)
                    {
                        _isCurrentlyHidden = false;
                        _lastEdgeTouchTime = DateTime.MinValue;
                        _lastDockLeaveTime = DateTime.MinValue;
                        VisibilityChanged?.Invoke(true);
                    }
                }
                else
                {
                    _lastEdgeTouchTime = DateTime.MinValue;
                }
            }
            else
            {
                // When already revealed: Keep open while hovering the dock or bottom edge
                bool isOverDock = false;
                if (GetWindowRect(_hwnd, out RECT winRect))
                {
                    // Check if cursor is over the active dock window area
                    isOverDock = pt.X >= winRect.Left && pt.X <= winRect.Right &&
                                 pt.Y >= winRect.Top && pt.Y <= winRect.Bottom + 5;
                }

                if (isOverDock || isAtBottomEdge)
                {
                    _lastDockLeaveTime = DateTime.MinValue;
                }
                else
                {
                    if (_lastDockLeaveTime == DateTime.MinValue)
                        _lastDockLeaveTime = DateTime.UtcNow;

                    if ((DateTime.UtcNow - _lastDockLeaveTime).TotalMilliseconds >= HideDelayMs)
                    {
                        _isCurrentlyHidden = true;
                        _lastDockLeaveTime = DateTime.MinValue;
                        _lastEdgeTouchTime = DateTime.MinValue;
                        VisibilityChanged?.Invoke(false);
                    }
                }
            }
        }
    }
}
