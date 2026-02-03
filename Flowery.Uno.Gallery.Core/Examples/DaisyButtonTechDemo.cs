using System;
using System.Numerics;
using Flowery.Controls;
using Flowery.Helpers;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Windows.UI;

namespace Flowery.Uno.Gallery.Examples
{
    public enum DaisyButtonTechMode
    {
        None,
        SetElevation,
        DropShadow,
        ThemeShadow
    }

    public sealed partial class DaisyButtonTechDemo : DaisyButton
    {
        public static bool EnableDebug { get; set; } = true;

        public static readonly DependencyProperty TechModeProperty =
            DependencyProperty.Register(
                nameof(TechMode),
                typeof(DaisyButtonTechMode),
                typeof(DaisyButtonTechDemo),
                new PropertyMetadata(DaisyButtonTechMode.None, OnTechModeChanged));

        private Border? _buttonBorder;
        private Canvas? _themeShadowReceiver;
        private SpriteVisual? _dropShadowVisual;
        private DropShadow? _dropShadow;
        private ShapeVisual? _dropShadowMaskVisual;
        private ThemeShadow? _themeShadow;
        private bool _pendingApplyTech;

        public DaisyButtonTechMode TechMode
        {
            get => (DaisyButtonTechMode)GetValue(TechModeProperty);
            set => SetValue(TechModeProperty, value);
        }

        public DaisyButtonTechDemo()
        {
            DefaultStyleKey = typeof(DaisyButton);
            NeumorphicEnabled = false;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            LayoutUpdated += OnLayoutUpdated;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _buttonBorder = GetTemplateChild("ButtonBorder") as Border;

            // Use the host Canvas already defined in the DaisyButton template
            _themeShadowReceiver = GetTemplateChild("PART_NeumorphicHost") as Canvas;

            _buttonBorder ??= FindChildByType<Border>(this);

            ApplyTech();
        }

        private static T? FindChildByType<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                var result = FindChildByType<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return default;
        }

        protected override bool ShouldSyncTemplateResources()
        {
            return false;
        }

        private static void OnTechModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButtonTechDemo control && control.IsLoaded)
            {
                control.ApplyTech();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTech();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ClearTech();
            LayoutUpdated -= OnLayoutUpdated;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth > 0 && ActualHeight > 0 && _pendingApplyTech)
            {
                _pendingApplyTech = false;
                ApplyTech();
            }
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            if (ActualWidth > 0 && ActualHeight > 0 && _pendingApplyTech)
            {
                _pendingApplyTech = false;
                ApplyTech();
                LayoutUpdated -= OnLayoutUpdated;
            }
        }

        private void ApplyTech()
        {
            if (_buttonBorder == null || ActualWidth <= 0 || ActualHeight <= 0)
            {
                _pendingApplyTech = true;
                return;
            }

            ClearTech();

            // Sync corner radius manually
            _buttonBorder.CornerRadius = CornerRadius;

            if (!OperatingSystem.IsWindows())
            {
                // WASM: Direct SetElevation path remains the Golden Path
                global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(_buttonBorder, 6);
                return;
            }

            switch (TechMode)
            {
                case DaisyButtonTechMode.SetElevation:
                    global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(_buttonBorder, 6);
                    break;
                case DaisyButtonTechMode.DropShadow:
                    ApplyDropShadow();
                    break;
                case DaisyButtonTechMode.ThemeShadow:
                    ApplyThemeShadow();
                    break;
                default:
                    break;
            }
        }

        private void ClearTech()
        {
            if (_buttonBorder != null)
            {
                _buttonBorder.Shadow = null;
                _buttonBorder.Translation = new Vector3(0, 0, 0);
                global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(_buttonBorder, 0);

                try
                {
                    ElementCompositionPreview.SetElementChildVisual(_buttonBorder, null!);
                }
                catch { }
            }

            _dropShadow = null;
            _dropShadowVisual = null;
            _dropShadowMaskVisual = null;
            _themeShadow = null;

        }

        private static void Log(string message)
        {
            if (!EnableDebug)
                return;
            FloweryDiagnostics.Log($"[DaisyButtonTechDemo] {message}");
        }

        private void ApplyDropShadow()
        {
            if (_buttonBorder == null)
                return;

            try
            {
                var hostVisual = ElementCompositionPreview.GetElementVisual(_buttonBorder);
                var compositor = hostVisual.Compositor;

                _dropShadowVisual ??= compositor.CreateSpriteVisual();
                _dropShadow ??= compositor.CreateDropShadow();

                _dropShadow.BlurRadius = 12f;
                _dropShadow.Offset = new Vector3(3f, 3f, 0f);
                _dropShadow.Color = Color.FromArgb(140, 0, 0, 0);
                _dropShadowVisual.Shadow = _dropShadow;

                ApplyRoundedMask(compositor, _dropShadow, ref _dropShadowMaskVisual,
                    (float)ActualWidth, (float)ActualHeight, (float)CornerRadius.TopLeft);

                _dropShadowVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
                ElementCompositionPreview.SetElementChildVisual(_buttonBorder, _dropShadowVisual);
            }
            catch (Exception ex)
            {
                Log($"ApplyDropShadow Failed: {ex.Message}");
            }
        }

        private void ApplyThemeShadow()
        {
            if (_buttonBorder == null || _themeShadowReceiver == null)
                return;

            bool isThemeShadowAvailable;
            try
            {
                isThemeShadowAvailable = ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.ThemeShadow, Uno.UI");
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (!isThemeShadowAvailable)
                return;

            try
            {
                _themeShadow = new ThemeShadow();
                _themeShadow.Receivers.Add(_themeShadowReceiver);

                _buttonBorder.Shadow = _themeShadow;
                _buttonBorder.Translation = new Vector3(0, 0, 6f);
            }
            catch (Exception ex)
            {
                Log($"ApplyThemeShadow Failed: {ex.Message}");
            }
        }

        private static void ApplyRoundedMask(
            Compositor compositor,
            DropShadow shadow,
            ref ShapeVisual? maskVisual,
            float width,
            float height,
            float cornerRadius)
        {
            if (maskVisual == null)
            {
                maskVisual = compositor.CreateShapeVisual();
                var geometry = compositor.CreateRoundedRectangleGeometry();
                var shape = compositor.CreateSpriteShape(geometry);
                shape.FillBrush = compositor.CreateColorBrush(Microsoft.UI.Colors.White);
                maskVisual.Shapes.Add(shape);
            }

            maskVisual.Size = new Vector2(width, height);
            if (maskVisual.Shapes[0] is CompositionSpriteShape spriteShape &&
                spriteShape.Geometry is CompositionRoundedRectangleGeometry roundedRect)
            {
                roundedRect.Size = new Vector2(width, height);
                roundedRect.CornerRadius = new Vector2(cornerRadius, cornerRadius);
            }

            var surface = compositor.CreateVisualSurface();
            surface.SourceVisual = maskVisual;
            surface.SourceSize = new Vector2(width, height);

            var maskBrush = compositor.CreateSurfaceBrush(surface);
            shadow.Mask = maskBrush;
        }
    }
}
