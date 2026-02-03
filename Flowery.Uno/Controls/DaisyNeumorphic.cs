using System;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// Provides attached properties for neumorphic visual effects.
    /// This allows any control to opt-in to neumorphic effects without requiring a specific base class.
    /// </summary>
    public static class DaisyNeumorphic
    {
        #region IsEnabled

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool?),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(null, OnPropertyChanged));

        public static void SetIsEnabled(DependencyObject element, bool? value) => element.SetValue(IsEnabledProperty, value);
        public static bool? GetIsEnabled(DependencyObject element) => (bool?)element.GetValue(IsEnabledProperty);

        #endregion

        #region Mode

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(DaisyNeumorphicMode?),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(null, OnPropertyChanged));

        public static void SetMode(DependencyObject element, DaisyNeumorphicMode? value) => element.SetValue(ModeProperty, value);
        public static DaisyNeumorphicMode? GetMode(DependencyObject element) => (DaisyNeumorphicMode?)element.GetValue(ModeProperty);

        #endregion

        #region Intensity

        public static readonly DependencyProperty IntensityProperty =
            DependencyProperty.RegisterAttached(
                "Intensity",
                typeof(double?),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(null, OnPropertyChanged));

        public static void SetIntensity(DependencyObject element, double? value) => element.SetValue(IntensityProperty, value);
        public static double? GetIntensity(DependencyObject element) => (double?)element.GetValue(IntensityProperty);

        #endregion

        #region BlurRadius

        public static readonly DependencyProperty BlurRadiusProperty =
            DependencyProperty.RegisterAttached(
                "BlurRadius",
                typeof(double),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(4.0, OnPropertyChanged));

        public static void SetBlurRadius(DependencyObject element, double value) => element.SetValue(BlurRadiusProperty, value);
        public static double GetBlurRadius(DependencyObject element) => (double)element.GetValue(BlurRadiusProperty);

        #endregion

        #region Offset

        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.RegisterAttached(
                "Offset",
                typeof(double),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(4.0, OnPropertyChanged));

        public static void SetOffset(DependencyObject element, double value) => element.SetValue(OffsetProperty, value);
        public static double GetOffset(DependencyObject element) => (double)element.GetValue(OffsetProperty);

        #endregion

        #region DarkShadowColor

        public static readonly DependencyProperty DarkShadowColorProperty =
            DependencyProperty.RegisterAttached(
                "DarkShadowColor",
                typeof(Color?),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(null, OnPropertyChanged));

        public static void SetDarkShadowColor(DependencyObject element, Color? value) => element.SetValue(DarkShadowColorProperty, value);
        public static Color? GetDarkShadowColor(DependencyObject element) => (Color?)element.GetValue(DarkShadowColorProperty);

        #endregion

        #region RimLightEnabled

        public static readonly DependencyProperty RimLightEnabledProperty =
            DependencyProperty.RegisterAttached(
                "RimLightEnabled",
                typeof(bool?),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(null, OnPropertyChanged));

        public static void SetRimLightEnabled(DependencyObject element, bool? value) => element.SetValue(RimLightEnabledProperty, value);
        public static bool? GetRimLightEnabled(DependencyObject element) => (bool?)element.GetValue(RimLightEnabledProperty);

        #endregion

        #region SurfaceGradientEnabled

        public static readonly DependencyProperty SurfaceGradientEnabledProperty =
            DependencyProperty.RegisterAttached(
                "SurfaceGradientEnabled",
                typeof(bool?),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(null, OnPropertyChanged));

        public static void SetSurfaceGradientEnabled(DependencyObject element, bool? value) => element.SetValue(SurfaceGradientEnabledProperty, value);
        public static bool? GetSurfaceGradientEnabled(DependencyObject element) => (bool?)element.GetValue(SurfaceGradientEnabledProperty);

        #endregion

        #region LightShadowColor

        public static readonly DependencyProperty LightShadowColorProperty =
            DependencyProperty.RegisterAttached(
                "LightShadowColor",
                typeof(Color?),
                typeof(DaisyNeumorphic),
                new PropertyMetadata(null, OnPropertyChanged));

        public static void SetLightShadowColor(DependencyObject element, Color? value) => element.SetValue(LightShadowColorProperty, value);
        public static Color? GetLightShadowColor(DependencyObject element) => (Color?)element.GetValue(LightShadowColorProperty);

        #endregion

        public static bool? GetScopeEnabled(DependencyObject element)
        {
            DependencyObject? current = element;
            while (current != null)
            {
                var scope = DaisyBaseContentControl.GetNeumorphicScopeEnabled(current);
                if (scope.HasValue)
                    return scope.Value;

                var next = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
                if (next == null && current is FrameworkElement fe)
                {
                    next = fe.Parent;
                }
                current = next;
            }

            return null;
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButton button)
            {
                button.RequestNeumorphicRefresh();
            }
            else if (d is DaisyComboBoxBase comboBox)
            {
                comboBox.RequestNeumorphicRefresh();
            }
            else if (d is DaisyBaseContentControl baseControl)
            {
                baseControl.RequestNeumorphicRefresh();
            }
        }
    }
}
