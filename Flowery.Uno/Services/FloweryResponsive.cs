using System;
using Microsoft.UI.Xaml;

namespace Flowery.Services
{
    /// <summary>
    /// Standard responsive breakpoints aligned with common CSS frameworks.
    /// </summary>
    public static class FloweryBreakpoints
    {
        public const double ExtraSmall = 480;
        public const double Small = 640;
        public const double Medium = 768;
        public const double Large = 1024;
        public const double ExtraLarge = 1280;
        public const double TwoXL = 1536;

        public static string GetBreakpointName(double width) => width switch
        {
            < ExtraSmall => "xs",
            < Small => "sm",
            < Medium => "md",
            < Large => "lg",
            < ExtraLarge => "xl",
            _ => "2xl"
        };
    }

    /// <summary>
    /// Provides responsive layout functionality via attached properties.
    /// Attach to a container and child elements can bind to the calculated ResponsiveMaxWidth.
    /// </summary>
    public static class FloweryResponsive
    {
        public const double DefaultPadding = 48;
        public const double MinimumWidth = 200;

        public static readonly DependencyProperty BaseMaxWidthProperty =
            DependencyProperty.RegisterAttached(
                "BaseMaxWidth",
                typeof(double),
                typeof(FloweryResponsive),
                new PropertyMetadata(430d));

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(FloweryResponsive),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty ResponsiveMaxWidthProperty =
            DependencyProperty.RegisterAttached(
                "ResponsiveMaxWidth",
                typeof(double),
                typeof(FloweryResponsive),
                new PropertyMetadata(430d));

        public static readonly DependencyProperty CurrentBreakpointProperty =
            DependencyProperty.RegisterAttached(
                "CurrentBreakpoint",
                typeof(string),
                typeof(FloweryResponsive),
                new PropertyMetadata("lg"));

        public static double GetBaseMaxWidth(DependencyObject element) => (double)element.GetValue(BaseMaxWidthProperty);
        public static void SetBaseMaxWidth(DependencyObject element, double value) => element.SetValue(BaseMaxWidthProperty, value);

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static double GetResponsiveMaxWidth(DependencyObject element) => (double)element.GetValue(ResponsiveMaxWidthProperty);
        public static void SetResponsiveMaxWidth(DependencyObject element, double value) => element.SetValue(ResponsiveMaxWidthProperty, value);

        public static string GetCurrentBreakpoint(DependencyObject element) => (string)element.GetValue(CurrentBreakpointProperty);
        public static void SetCurrentBreakpoint(DependencyObject element, string value) => element.SetValue(CurrentBreakpointProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe)
                return;

            if (e.NewValue is true)
            {
                fe.SizeChanged += OnSizeChanged;
                UpdateResponsiveProperties(fe);
            }
            else
            {
                fe.SizeChanged -= OnSizeChanged;
            }
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                UpdateResponsiveProperties(fe);
            }
        }

        private static void UpdateResponsiveProperties(FrameworkElement element)
        {
            var baseMaxWidth = GetBaseMaxWidth(element);
            var availableWidth = element.ActualWidth - DefaultPadding;

            var responsiveWidth = availableWidth > 0
                ? Math.Min(baseMaxWidth, Math.Max(MinimumWidth, availableWidth))
                : baseMaxWidth;

            SetResponsiveMaxWidth(element, responsiveWidth);
            SetCurrentBreakpoint(element, FloweryBreakpoints.GetBreakpointName(element.ActualWidth));
        }
    }
}
