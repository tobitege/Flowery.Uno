using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class ShowcaseExamples : ScrollableExamplePage
    {
        private SpriteVisual? _dropShadowVisual;
        private DropShadow? _dropShadow;
        private bool _rawDropShadowAttached;
        private bool _rawDropShadowSupported = true;

        public ShowcaseExamples()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override Microsoft.UI.Xaml.Controls.ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnSlideCompleted(object? sender, EventArgs e)
        {
            // Handle slide completion - could trigger an action here
            System.Diagnostics.Debug.WriteLine("Slide completed!");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyRawElevation();
            ApplyRawDropShadow();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (FindName("RawDropShadowTarget") is FrameworkElement target)
            {
                target.SizeChanged -= OnRawDropShadowSizeChanged;
            }

            if (FindName("RawDropShadowLayer") is FrameworkElement shadowLayer)
            {
                if (_rawDropShadowAttached)
                {
                    try
                    {
                        ElementCompositionPreview.SetElementChildVisual(shadowLayer, null!);
                    }
                    catch (NotImplementedException)
                    {
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }

            _rawDropShadowAttached = false;
            _dropShadow = null;
            _dropShadowVisual = null;

        }

        private void ApplyRawElevation()
        {
            if (FindName("RawElevationTarget") is UIElement element)
            {
                global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(element, 16);
            }
        }

        private void ApplyRawDropShadow()
        {
            if (!_rawDropShadowSupported)
                return;

            if (!OperatingSystem.IsWindows())
            {
                _rawDropShadowSupported = false;
                return;
            }

            if (FindName("RawDropShadowLayer") is not FrameworkElement shadowLayer ||
                FindName("RawDropShadowTarget") is not FrameworkElement target)
                return;

            try
            {
                var hostVisual = ElementCompositionPreview.GetElementVisual(shadowLayer);
                var compositor = hostVisual.Compositor;

                _dropShadowVisual ??= compositor.CreateSpriteVisual();
                _dropShadow ??= compositor.CreateDropShadow();

                _dropShadow.BlurRadius = 18f;
                _dropShadow.Offset = new Vector3(6f, 6f, 0f);
                _dropShadow.Color = Color.FromArgb(140, 0, 0, 0);
                _dropShadow.Mask ??= compositor.CreateColorBrush(Colors.White);
                _dropShadowVisual.Shadow = _dropShadow;

                UpdateRawDropShadowSize(target, shadowLayer);
                ElementCompositionPreview.SetElementChildVisual(shadowLayer, _dropShadowVisual);
                _rawDropShadowAttached = true;
            }
            catch (NotImplementedException)
            {
                _rawDropShadowSupported = false;
                return;
            }
            catch (InvalidOperationException)
            {
                _rawDropShadowSupported = false;
                return;
            }

            target.SizeChanged -= OnRawDropShadowSizeChanged;
            target.SizeChanged += OnRawDropShadowSizeChanged;
        }

        private void OnRawDropShadowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement target &&
                FindName("RawDropShadowLayer") is FrameworkElement shadowLayer)
            {
                UpdateRawDropShadowSize(target, shadowLayer);
            }
        }

        private void UpdateRawDropShadowSize(FrameworkElement target, FrameworkElement shadowLayer)
        {
            if (_dropShadowVisual == null) return;

            var width = (float)target.ActualWidth;
            var height = (float)target.ActualHeight;
            if (width <= 0 || height <= 0) return;

            _dropShadowVisual.Size = new Vector2(width, height);
            shadowLayer.Width = target.ActualWidth;
            shadowLayer.Height = target.ActualHeight;
        }

    }
}
