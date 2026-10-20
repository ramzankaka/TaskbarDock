using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using TaskbarDock.Models;

namespace TaskbarDock.Dock
{
    public partial class DockItemView : System.Windows.Controls.UserControl
    {
        public event Action<DockItem>? ItemClicked;
        public event Action<DockItem>? ItemRemoveRequested;
        public event Action<DockItem>? ItemPinToggleRequested;
        public event Action<DockItem>? ItemCloseRequested;

        public DockItem Item => (DockItem)DataContext;

        public DockItemView()
        {
            InitializeComponent();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DockItem item)
            {
                ItemClicked?.Invoke(item);
            }
        }

        public void PlayLaunchBounce()
        {
            // Bouncing disabled per user request: icons remain firmly grounded and active indicator dot displays underneath
        }

        public void PlayScalePop()
        {
            // Scale pop disabled per user request
        }

        public void SetDotTheme(System.Windows.Media.Brush fill, System.Windows.Media.Color glow)
        {
            try
            {
                RunningDot.Fill = fill;
                DotShadow.Color = glow;
            }
            catch { }
        }

        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (DataContext is DockItem item)
            {
                MenuTitle.Header = item.DisplayName;
                MenuPinToggle.Header = item.IsPinned ? "Unpin from Dock" : "Pin to Dock";
                MenuClose.Visibility = item.IsRunning ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnMenuOpen(object sender, RoutedEventArgs e)
        {
            if (DataContext is DockItem item)
                ItemClicked?.Invoke(item);
        }

        private void OnMenuOpenNew(object sender, RoutedEventArgs e)
        {
            if (DataContext is DockItem item)
                ApplicationLauncher.LaunchNewInstance(item);
        }

        private void OnMenuPinToggle(object sender, RoutedEventArgs e)
        {
            if (DataContext is DockItem item)
                ItemPinToggleRequested?.Invoke(item);
        }

        private void OnMenuClose(object sender, RoutedEventArgs e)
        {
            if (DataContext is DockItem item)
                ItemCloseRequested?.Invoke(item);
        }

        private void OnMenuRemove(object sender, RoutedEventArgs e)
        {
            if (DataContext is DockItem item)
                ItemRemoveRequested?.Invoke(item);
        }
    }
}
