using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Uno.Gallery
{
    public static class WasmSpacingHelper
    {
        public static readonly DependencyProperty SpacingScaleProperty =
            DependencyProperty.RegisterAttached(
                "SpacingScale",
                typeof(double),
                typeof(WasmSpacingHelper),
                new PropertyMetadata(0.0, OnSpacingScaleChanged));

        public static double GetSpacingScale(DependencyObject obj)
        {
            return (double)obj.GetValue(SpacingScaleProperty);
        }

        public static void SetSpacingScale(DependencyObject obj, double value)
        {
            obj.SetValue(SpacingScaleProperty, value);
        }

        private static void OnSpacingScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not StackPanel panel)
                return;

            panel.Loaded -= OnPanelLoaded;
            panel.Loaded += OnPanelLoaded;

            if (panel.IsLoaded)
                ApplySpacing(panel);
        }

        private static void OnPanelLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is StackPanel panel)
                ApplySpacing(panel);
        }

        private static void ApplySpacing(StackPanel panel)
        {
            if (!OperatingSystem.IsBrowser())
                return;

            if (GetWasmSpacingApplied(panel))
                return;

            var scale = GetSpacingScale(panel);
            if (scale <= 0 || panel.Spacing <= 0)
                return;

            ApplyStackPanelSpacing(panel, panel.Spacing * scale);
            SetWasmSpacingApplied(panel, true);
        }

        private static void ApplyStackPanelSpacing(StackPanel panel, double spacing)
        {
            var isHorizontal = panel.Orientation == Orientation.Horizontal;
            var isFirst = true;

            foreach (var child in panel.Children)
            {
                if (child is not FrameworkElement element)
                    continue;

                if (isFirst)
                {
                    isFirst = false;
                    continue;
                }

                var margin = element.Margin;
                if (isHorizontal)
                {
                    element.Margin = new Thickness(margin.Left + spacing, margin.Top, margin.Right, margin.Bottom);
                }
                else
                {
                    element.Margin = new Thickness(margin.Left, margin.Top + spacing, margin.Right, margin.Bottom);
                }
            }
        }

        private static readonly DependencyProperty WasmSpacingAppliedProperty =
            DependencyProperty.RegisterAttached(
                "WasmSpacingApplied",
                typeof(bool),
                typeof(WasmSpacingHelper),
                new PropertyMetadata(false));

        private static bool GetWasmSpacingApplied(DependencyObject obj)
        {
            return (bool)obj.GetValue(WasmSpacingAppliedProperty);
        }

        private static void SetWasmSpacingApplied(DependencyObject obj, bool value)
        {
            obj.SetValue(WasmSpacingAppliedProperty, value);
        }
    }
}
