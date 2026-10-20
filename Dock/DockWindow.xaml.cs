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
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;

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
        public Action? RequestOpenSettings { get; set; }
        public Action? RequestToggleMode { get; set; }

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
            _autoHideController.SmartDesktopAutoHide = _config.Settings.Behavior.SmartDesktopAutoHide;
            _autoHideController.RevealDelayMs = _config.Settings.Behavior.RevealDelayMs;
            _autoHideController.HideDelayMs = _config.Settings.Behavior.HideDelayMs;

            if (_autoHideController.AutoHideEnabled || _autoHideController.SmartDesktopAutoHide)
                _autoHideController.Start();
            else
                _autoHideController.Stop();

            ApplyTheme(dockCfg.Theme);
            RepositionDock();
        }

        public void ApplyTheme(string? theme)
        {
            theme ??= "Current";
            switch (theme.ToLowerInvariant())
            {
                case "glassyliquid":
                case "liquid":
                    // Glassy Liquid Modern: Fluid refraction, multi-layered specular gleam, cyan liquid aura
                    var liquidBg = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF), 0.0),
                            new GradientStop(Color.FromArgb(0x28, 0x18, 0x2C, 0x4E), 0.08),
                            new GradientStop(Color.FromArgb(0x45, 0x0E, 0x1B, 0x33), 0.5),
                            new GradientStop(Color.FromArgb(0x60, 0x09, 0x12, 0x24), 1.0)
                        }
                    };
                    var liquidBorder = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0xC0, 0x90, 0xE0, 0xFF), 0.0),
                            new GradientStop(Color.FromArgb(0x80, 0xC0, 0x84, 0xFC), 0.4),
                            new GradientStop(Color.FromArgb(0x60, 0x00, 0xE5, 0xFF), 0.8),
                            new GradientStop(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF), 1.0)
                        }
                    };
                    DockContainerBorder.Background = liquidBg;
                    DockContainerBorder.BorderBrush = liquidBorder;
                    DockContainerBorder.BorderThickness = new Thickness(1.2);
                    DockDropShadow.Color = Color.FromRgb(0x00, 0xE5, 0xFF);
                    DockDropShadow.Opacity = 0.35;
                    DockDropShadow.BlurRadius = 32;
                    UpdateDotColors(new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)), Color.FromRgb(0x00, 0xE5, 0xFF));
                    break;

                case "translucentdark":
                case "dark":
                    // Translucent Dark: Deep obsidian acrylic, crisp silver edge, high-contrast dark style
                    var darkBg = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0x45, 0x0A, 0x0A, 0x0D), 0.0),
                            new GradientStop(Color.FromArgb(0x65, 0x05, 0x05, 0x07), 1.0)
                        }
                    };
                    var darkBorder = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF), 0.0),
                            new GradientStop(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF), 1.0)
                        }
                    };
                    DockContainerBorder.Background = darkBg;
                    DockContainerBorder.BorderBrush = darkBorder;
                    DockContainerBorder.BorderThickness = new Thickness(1);
                    DockDropShadow.Color = Color.FromRgb(0, 0, 0);
                    DockDropShadow.Opacity = 0.65;
                    DockDropShadow.BlurRadius = 35;
                    UpdateDotColors(new SolidColorBrush(Color.FromRgb(0xEA, 0xEA, 0xEA)), Color.FromRgb(0xFF, 0xFF, 0xFF));
                    break;

                case "translucentwhite":
                case "white":
                case "light":
                    // Translucent White: Apple-style frosted ice glass, bright translucent sheen
                    var whiteBg = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF), 0.0),
                            new GradientStop(Color.FromArgb(0x9E, 0xE5, 0xEB, 0xF5), 1.0)
                        }
                    };
                    var whiteBorder = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0xEE, 0xFF, 0xFF, 0xFF), 0.0),
                            new GradientStop(Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF), 0.5),
                            new GradientStop(Color.FromArgb(0x75, 0xBF, 0xCB, 0xDA), 1.0)
                        }
                    };
                    DockContainerBorder.Background = whiteBg;
                    DockContainerBorder.BorderBrush = whiteBorder;
                    DockContainerBorder.BorderThickness = new Thickness(1);
                    DockDropShadow.Color = Color.FromRgb(0, 0, 0);
                    DockDropShadow.Opacity = 0.22;
                    DockDropShadow.BlurRadius = 25;
                    UpdateDotColors(new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x26)), Color.FromArgb(0x60, 0, 0, 0));
                    break;

                case "current":
                default:
                    // Classic macOS Dark Glass
                    var classicBg = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0x55, 0x22, 0x22, 0x26), 0.0),
                            new GradientStop(Color.FromArgb(0x77, 0x16, 0x16, 0x18), 1.0)
                        }
                    };
                    var classicBorder = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF), 0.0),
                            new GradientStop(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF), 0.5),
                            new GradientStop(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF), 1.0)
                        }
                    };
                    DockContainerBorder.Background = classicBg;
                    DockContainerBorder.BorderBrush = classicBorder;
                    DockContainerBorder.BorderThickness = new Thickness(1);
                    DockDropShadow.Color = Color.FromRgb(0, 0, 0);
                    DockDropShadow.Opacity = 0.42;
                    DockDropShadow.BlurRadius = 28;
                    UpdateDotColors(new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)), Color.FromRgb(0xFF, 0xFF, 0xFF));
                    break;
            }
        }

        private void UpdateDotColors(System.Windows.Media.Brush fill, System.Windows.Media.Color glow)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < DockItemsControl.Items.Count; i++)
                {
                    var container = DockItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                    if (container != null)
                    {
                        var itemView = FindVisualChild<DockItemView>(container);
                        itemView?.SetDotTheme(fill, glow);
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent is T match) return match;
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var res = FindVisualChild<T>(child);
                if (res != null) return res;
            }
            return null;
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

        private Rect? GetItemScreenRect(DockItem item)
        {
            try
            {
                int index = DockItemsControl.Items.IndexOf(item);
                if (index >= 0)
                {
                    var container = DockItemsControl.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                    if (container != null)
                    {
                        Point screenPoint = container.PointToScreen(new Point(0, 0));
                        return new Rect(screenPoint.X, screenPoint.Y, container.ActualWidth, container.ActualHeight);
                    }
                }
            }
            catch { }
            return null;
        }

        private void OnDockItemClicked(DockItem item)
        {
            Rect? screenRect = GetItemScreenRect(item);
            ApplicationLauncher.LaunchOrActivate(
                item,
                _config.Settings.Behavior.ActivateRunningApp,
                _config.Settings.Behavior.MinimizeOnSecondClick,
                screenRect);
        }

        private void OnDockItemPinToggleRequested(DockItem item)
        {
            if (item.IsPinned)
                _itemManager.UnpinItem(item);
            else
                _itemManager.PinItem(item);
        }

        private void OnDockItemCloseRequested(DockItem item)
        {
            _itemManager.CloseAppWindows(item);
        }

        private void OnDockItemRemoveRequested(DockItem item)
        {
            _itemManager.RemoveItem(item);
        }

        private void OnMenuAddAppClicked(object sender, RoutedEventArgs e)
        {
            PromptAddApp();
        }

        private void PromptAddApp()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Applications and Shortcuts (*.exe;*.lnk)|*.exe;*.lnk|All files (*.*)|*.*",
                Title = "Select Application to Add to Dock"
            };

            if (dlg.ShowDialog() == true)
            {
                _itemManager.AddAppByPath(dlg.FileName);
                RepositionDock();
            }
        }

        private void OnDockDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDockDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        _itemManager.AddAppByPath(file);
                    }
                    RepositionDock();
                }
            }
        }

        private void OnSelectThemeClassic(object sender, RoutedEventArgs e) => SetTheme("Current");
        private void OnSelectThemeLiquid(object sender, RoutedEventArgs e) => SetTheme("GlassyLiquid");
        private void OnSelectThemeTranslucentDark(object sender, RoutedEventArgs e) => SetTheme("TranslucentDark");
        private void OnSelectThemeTranslucentWhite(object sender, RoutedEventArgs e) => SetTheme("TranslucentWhite");

        private void SetTheme(string themeName)
        {
            _config.Settings.Dock.Theme = themeName;
            _config.SaveSettings();
            ApplyTheme(themeName);
        }

        private void OnMenuSettingsClicked(object sender, RoutedEventArgs e)
        {
            RequestOpenSettings?.Invoke();
        }

        private void OnMenuSwitchModeClicked(object sender, RoutedEventArgs e)
        {
            RequestToggleMode?.Invoke();
        }

        private void OnRunningAppsChanged()
        {
            Dispatcher.Invoke(() =>
            {
                // Reapply dot theme if items were dynamically added
                ApplyTheme(_config.Settings.Dock.Theme);
            });
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
            _runningAppsTracker.Start(_itemManager);
            if (_config.Settings.Behavior.AutoHide || _config.Settings.Behavior.SmartDesktopAutoHide)
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
