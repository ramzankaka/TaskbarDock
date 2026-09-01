using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskbarDock.Diagnostics;
using static TaskbarDock.WindowsIntegration.NativeMethods;

namespace TaskbarDock.WindowsIntegration
{
    public static class IconExtractor
    {
        public static ImageSource? ExtractIcon(string? path, string? systemAction = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(systemAction))
                {
                    var sysIcon = GetSystemActionIcon(systemAction);
                    if (sysIcon != null) return sysIcon;
                }

                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return GetDefaultAppIcon();
                }

                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".ico")
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(path, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }

                // Extract large icon from EXE/DLL
                IntPtr[] largeIcons = new IntPtr[1];
                IntPtr[] smallIcons = new IntPtr[1];
                uint count = ExtractIconEx(path, 0, largeIcons, smallIcons, 1);

                if (count > 0 && largeIcons[0] != IntPtr.Zero)
                {
                    using var icon = (Icon)Icon.FromHandle(largeIcons[0]).Clone();
                    DestroyIcon(largeIcons[0]);
                    if (smallIcons[0] != IntPtr.Zero) DestroyIcon(smallIcons[0]);

                    var bs = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    return bs;
                }

                // Fallback to Shell associated icon
                using var assocIcon = Icon.ExtractAssociatedIcon(path);
                if (assocIcon != null)
                {
                    var bs = Imaging.CreateBitmapSourceFromHIcon(
                        assocIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    return bs;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to extract icon for path: {path}", ex);
            }

            return GetDefaultAppIcon();
        }

        private static ImageSource? GetSystemActionIcon(string action)
        {
            try
            {
                string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

                switch (action.ToLowerInvariant())
                {
                    case "start":
                        return CreateVectorGlyph("M12,2L2,12h3v8h6v-6h2v6h6v-8h3L12,2z", System.Windows.Media.Brushes.DodgerBlue);
                    case "explorer":
                        string expPath = Path.Combine(windir, "explorer.exe");
                        return ExtractIcon(expPath);
                    case "notepad":
                        string npPath = Path.Combine(sys32, "notepad.exe");
                        return ExtractIcon(npPath);
                    case "calc":
                        string calcPath = Path.Combine(sys32, "calc.exe");
                        return ExtractIcon(calcPath);
                    case "terminal":
                        return CreateVectorGlyph("M2,4h20v16H2V4zm2,2v12h16V6H4zm3,3l4,3-4,3v-2l1.5-1L7,11V9zm6,5h5v2h-5v-2z", System.Windows.Media.Brushes.DarkSlateGray);
                    case "settings":
                        return CreateVectorGlyph("M19.14,12.94c0.04,-0.3 0.06,-0.61 0.06,-0.94c0,-0.32 -0.02,-0.64 -0.07,-0.94l2.03,-1.58c0.18,-0.14 0.23,-0.41 0.12,-0.61l-1.92,-3.32c-0.12,-0.22 -0.37,-0.29 -0.59,-0.22l-2.39,0.96c-0.5,-0.38 -1.03,-0.7 -1.62,-0.94L14.4,2.81c-0.04,-0.24 -0.24,-0.41 -0.48,-0.41h-3.84c-0.24,0 -0.43,0.17 -0.47,0.41L9.25,5.35C8.66,5.59 8.12,5.92 7.63,6.29L5.24,5.33c-0.22,-0.08 -0.47,0 -0.59,0.22L2.74,8.87c-0.12,0.21 -0.08,0.47 0.12,0.61l2.03,1.58c-0.05,0.3 -0.09,0.63 -0.09,0.94s0.02,0.64 0.07,0.94l-2.03,1.58c-0.18,0.14 -0.23,0.41 -0.12,0.61l1.92,3.32c0.12,0.22 0.37,0.29 0.59,0.22l2.39,-0.96c0.5,0.38 1.03,0.7 1.62,0.94l0.36,2.54c0.05,0.24 0.24,0.41 0.48,0.41h3.84c0.24,0 0.44,-0.17 0.47,-0.41l0.36,-2.54c0.59,-0.24 1.13,-0.56 1.62,-0.94l2.39,0.96c0.22,0.08 0.47,0 0.59,-0.22l1.92,-3.32c0.12,-0.22 0.07,-0.47 -0.12,-0.61L19.14,12.94zM12,15.5c-1.93,0 -3.5,-1.57 -3.5,-3.5s1.57,-3.5 3.5,-3.5s3.5,1.57 3.5,3.5S13.93,15.5 12,15.5z", System.Windows.Media.Brushes.RoyalBlue);
                    case "recyclebin":
                        return CreateVectorGlyph("M6,19c0,1.1 0.9,2 2,2h8c1.1,0 2,-0.9 2,-2V7H6v12zM19,4h-3.5l-1,-1h-5l-1,1H5v2h14V4z", System.Windows.Media.Brushes.MediumSlateBlue);
                    case "store":
                        return CreateVectorGlyph("M20,6h-4V4c0,-1.11 -0.89,-2 -2,-2h-4c-1.11,0 -2,0.89 -2,2v2H4c-1.11,0 -1.99,0.89 -1.99,2L2,19c0,1.11 0.89,2 2,2h16c1.11,0 2,-0.89 2,-2V8c0,-1.11 -0.89,-2 -2,-2zm-6,-2v2h-4V4h4z", System.Windows.Media.Brushes.CornflowerBlue);
                }
            }
            catch { }

            return null;
        }

        private static ImageSource CreateVectorGlyph(string pathData, System.Windows.Media.Brush fill)
        {
            var geometry = Geometry.Parse(pathData);
            var drawing = new GeometryDrawing(fill, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Transparent, 0), geometry);
            var drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();
            return drawingImage;
        }

        private static ImageSource GetDefaultAppIcon()
        {
            return CreateVectorGlyph("M4,4h16v16H4V4zm2,2v12h12V6H6z", System.Windows.Media.Brushes.SlateGray);
        }
    }
}
