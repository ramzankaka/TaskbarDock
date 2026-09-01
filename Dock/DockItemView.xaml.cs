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

        public DockItem Item => (DockItem)DataContext;

        public DockItemView()
        {
            InitializeComponent();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DockItem item)
            {
                PlayLaunchBounce();
                ItemClicked?.Invoke(item);
            }
        }

        public void PlayLaunchBounce()
        {
            try
            {
                var anim = new DoubleAnimationUsingKeyFrames();
                anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                anim.KeyFrames.Add(new SplineDoubleKeyFrame(-18, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)), new KeySpline(0.1, 0.9, 0.2, 1.0)));
                anim.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), new KeySpline(0.8, 0.0, 0.9, 0.2)));
                anim.KeyFrames.Add(new SplineDoubleKeyFrame(-10, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420)), new KeySpline(0.1, 0.9, 0.2, 1.0)));
                anim.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(550)), new KeySpline(0.8, 0.0, 0.9, 0.2)));

                BounceTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, anim);
            }
            catch { }
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

        private void OnMenuRemove(object sender, RoutedEventArgs e)
        {
            if (DataContext is DockItem item)
                ItemRemoveRequested?.Invoke(item);
        }
    }
}
