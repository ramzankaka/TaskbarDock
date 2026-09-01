using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TaskbarDock.Core;
using TaskbarDock.Diagnostics;
using TaskbarDock.Models;
using TaskbarDock.WindowsIntegration;
using static TaskbarDock.WindowsIntegration.NativeMethods;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace TaskbarDock.Dock
{
    public partial class DockWindow : Window
    {
        private readonly ConfigurationManager _config;
        private readonly DockItemManager _itemManager;
        private readonly DockMagnification _magnification = new();
        private readonly AutoHideController _autoHideController;
        private readonly RunningAppsTracker _runningAppsTracker;

        public DockItemManager ItemManager => _itemManager;

        public DockWindow(ConfigurationManager config, DockItemManager itemManager, RunningAppsTracker runningAppsTracker)
        {
            _config = config;
            _itemManager = itemManager;
            _runningAppsTracker = runningAppsTracker;
            _autoHideController = new AutoHideController(this);

            DataContext = _itemManager;
            InitializeComponent();

            ApplyConfigStyles();

            _autoHideController.VisibilityChanged += OnAutoHideVisibilityChanged;
            _runningAppsTracker.RunningAppsChanged += OnRunningAppsChanged;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

            RepositionDock();
        }

        public void ApplyConfigStyles()
        {
            var dockCfg = _config.Settings.Dock;
            _magnification.Enabled = dockCfg.MagnificationEnabled;
            _magnification.MaxMagnification = dockCfg.MaxMagnification;
            _magnification.MagnificationRange = dockCfg.MagnificationRange;
            _magnification.BaseIconSize = dockCfg.IconSize;

            DockContainerBorder.CornerRadius = new CornerRadius(dockCfg.CornerRadius);
            DockContainerBorder.Opacity = dockCfg.DockOpacity;

            _autoHideController.AutoHideEnabled = _config.Settings.Behavior.AutoHide;
            _autoHideController.RevealDelayMs = _config.Settings.Behavior.RevealDelayMs;
            _autoHideController.HideDelayMs = _config.Settings.Behavior.HideDelayMs;

            if (_autoHideController.AutoHideEnabled)
                _autoHideController.Start();
            else
                _autoHideController.Stop();

            RepositionDock();
        }

        public void RepositionDock()
        {
            try
            {
                var monitor = MonitorManager.GetPrimaryMonitor();
                double screenWidth = monitor.Bounds.Width / monitor.DpiScaleX;
                double screenHeight = monitor.Bounds.Height / monitor.DpiScaleY;

                Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Arrange(new Rect(0, 0, DesiredSize.Width, DesiredSize.Height));

                double dockWidth = ActualWidth > 0 ? ActualWidth : DesiredSize.Width;
                double dockHeight = ActualHeight > 0 ? ActualHeight : DesiredSize.Height;

                Left = (screenWidth - dockWidth) / 2.0;
                Top = screenHeight - dockHeight - _config.Settings.Dock.BottomSpacing;

                _autoHideController.TargetBottom = Top;
            }
            catch (Exception ex)
            {
                Logger.Warn("Error repositioning dock window", ex);
            }
        }

        private void OnWindowMouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_magnification.Enabled) return;

            var pos = e.GetPosition(this);
            var itemPairs = new List<(DockItem Item, double CenterX)>();

            for (int i = 0; i < DockItemsControl.Items.Count; i++)
            {
                var container = DockItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null && DockItemsControl.Items[i] is DockItem item)
                {
                    var itemPos = container.TransformToAncestor(this).Transform(new Point(0, 0));
                    double centerX = itemPos.X + container.ActualWidth / 2.0;
                    itemPairs.Add((item, centerX));
                }
            }

            _magnification.CalculateMagnification(pos.X, itemPairs);
        }

        private void OnWindowMouseLeave(object sender, WpfMouseEventArgs e)
        {
            var itemPairs = _itemManager.Items.Select(it => (it, 0.0)).ToList();
            _magnification.ResetMagnification(itemPairs);
        }

        private void OnDockItemClicked(DockItem item)
        {
            ApplicationLauncher.LaunchOrActivate(
                item,
                _config.Settings.Behavior.ActivateRunningApp,
                _config.Settings.Behavior.MinimizeOnSecondClick);
        }

        private void OnDockItemRemoveRequested(DockItem item)
        {
            _itemManager.RemoveItem(item);
        }

        private void OnRunningAppsChanged()
        {
            Dispatcher.Invoke(() => { });
        }

        private void OnAutoHideVisibilityChanged(bool isVisible)
        {
            Dispatcher.Invoke(() =>
            {
                double targetY = isVisible ? 0 : (ActualHeight + 40);
                var anim = new DoubleAnimation(targetY, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                DockSlideTransform.BeginAnimation(TranslateTransform.YProperty, anim);
            });
        }

        public void ShowDock()
        {
            Show();
            RepositionDock();
            _runningAppsTracker.Start(_itemManager.Items.ToList());
            if (_config.Settings.Behavior.AutoHide)
                _autoHideController.Start();
        }

        public void HideDock()
        {
            _autoHideController.Stop();
            _runningAppsTracker.Stop();
            Hide();
        }
    }
}
