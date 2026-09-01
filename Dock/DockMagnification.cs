using System;
using System.Collections.Generic;
using TaskbarDock.Models;

namespace TaskbarDock.Dock
{
    public class DockMagnification
    {
        public bool Enabled { get; set; } = true;
        public double MaxMagnification { get; set; } = 1.65;
        public double MagnificationRange { get; set; } = 140.0;
        public double BaseIconSize { get; set; } = 56.0;

        public void CalculateMagnification(double mouseX, IReadOnlyList<(DockItem Item, double CenterX)> items)
        {
            if (!Enabled || items.Count == 0)
            {
                ResetMagnification(items);
                return;
            }

            foreach (var (item, centerX) in items)
            {
                double distance = Math.Abs(mouseX - centerX);
                if (distance <= MagnificationRange)
                {
                    // Parabolic Gaussian-like smooth curve
                    double factor = Math.Cos((distance / MagnificationRange) * (Math.PI / 2.0));
                    factor = Math.Pow(factor, 1.8);
                    double scale = 1.0 + (MaxMagnification - 1.0) * factor;
                    item.TargetScale = scale;
                    item.CurrentSize = BaseIconSize * scale;
                }
                else
                {
                    item.TargetScale = 1.0;
                    item.CurrentSize = BaseIconSize;
                }
            }
        }

        public void ResetMagnification(IReadOnlyList<(DockItem Item, double CenterX)> items)
        {
            foreach (var (item, _) in items)
            {
                item.TargetScale = 1.0;
                item.CurrentSize = BaseIconSize;
            }
        }
    }
}
