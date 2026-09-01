using System;
using System.Windows;
using System.Windows.Threading;
using TaskbarDock.Diagnostics;
using TaskbarDock.Models;
using static TaskbarDock.WindowsIntegration.NativeMethods;
using Point = System.Windows.Point;

namespace TaskbarDock.Dock
{
    public class AutoHideController
    {
        private readonly Window _dockWindow;
        private readonly DispatcherTimer _edgeCheckTimer;
        private bool _isCurrentlyHidden;
        private DateTime _lastEdgeTouchTime = DateTime.MinValue;
        private DateTime _lastDockLeaveTime = DateTime.MinValue;

        public bool AutoHideEnabled { get; set; } = false;
        public int RevealDelayMs { get; set; } = 100;
        public int HideDelayMs { get; set; } = 350;
        public double TargetBottom { get; set; }
        public double HiddenOffset { get; set; } = 80.0;

        public event Action<bool>? VisibilityChanged;

        public AutoHideController(Window dockWindow)
        {
            _dockWindow = dockWindow;
            _edgeCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(40)
            };
            _edgeCheckTimer.Tick += (s, e) => CheckMouseProximity();
        }

        public void Start()
        {
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

            GetCursorPos(out POINT pt);

            // Check if cursor is on bottom edge of screen (within 4 pixels)
            double screenBottom = SystemParameters.PrimaryScreenHeight;
            bool isAtBottomEdge = pt.Y >= screenBottom - 4;

            // Check if cursor is over the dock window bounds
            var rect = new Rect(_dockWindow.Left, _dockWindow.Top, _dockWindow.ActualWidth, _dockWindow.ActualHeight);
            bool isOverDock = rect.Contains(new Point(pt.X, pt.Y));

            if (isAtBottomEdge || isOverDock)
            {
                _lastDockLeaveTime = DateTime.MinValue;
                if (_isCurrentlyHidden)
                {
                    if (_lastEdgeTouchTime == DateTime.MinValue)
                        _lastEdgeTouchTime = DateTime.UtcNow;

                    if ((DateTime.UtcNow - _lastEdgeTouchTime).TotalMilliseconds >= RevealDelayMs)
                    {
                        _isCurrentlyHidden = false;
                        VisibilityChanged?.Invoke(true);
                    }
                }
            }
            else
            {
                _lastEdgeTouchTime = DateTime.MinValue;
                if (!_isCurrentlyHidden)
                {
                    if (_lastDockLeaveTime == DateTime.MinValue)
                        _lastDockLeaveTime = DateTime.UtcNow;

                    if ((DateTime.UtcNow - _lastDockLeaveTime).TotalMilliseconds >= HideDelayMs)
                    {
                        _isCurrentlyHidden = true;
                        VisibilityChanged?.Invoke(false);
                    }
                }
            }
        }
    }
}
