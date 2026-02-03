// Preserved Windows-specific implementation. Disabled while standardizing on Skia.
// To re-enable, restore the original #if WINDOWS guard.
#if WINDOWS
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// Windows-specific implementation of neumorphic shadows.
    /// </summary>
    /// <remarks>
    /// Uses the Composition API DropShadow for a reliable, single-shadow path.
    /// </remarks>
    public sealed partial class DaisyNeumorphicHelper
    {
        /// <summary>
        /// When true, applies a rounded rectangle mask to DropShadow to match button corner radius.
        /// </summary>
        public static bool UseRoundedShadowMask { get; set; } = true;

        // ---------------------------------------------------------------------
        // DropShadow mode fields (Composition API)
        // ---------------------------------------------------------------------
        private SpriteVisual? _darkShadowVisual;
        private SpriteVisual? _lightShadowVisual;
        private ShapeVisual? _darkMaskVisual;
        private ShapeVisual? _lightMaskVisual;
        private bool _windowsShadowsAttached;

        partial void OnApplyDirectElevation(DaisyNeumorphicMode mode, double intensity, double elevation, ref bool handled)
        {
            if (_owner == null || !_owner.IsLoaded)
            {
                handled = false;
                return;
            }

            try
            {
                ApplyWindowsDropShadows(mode, intensity, elevation);
                handled = true;
            }
            catch
            {
                // If shadow API fails, fall back to default SetElevation path
                handled = false;
            }
        }

        partial void OnDisposeWindowsShadows()
        {
            DisposeWindowsShadowVisuals();
        }

        private void ApplyWindowsDropShadows(DaisyNeumorphicMode mode, double intensity, double elevation)
        {
            var ownerVisual = ElementCompositionPreview.GetElementVisual(_owner);
            var compositor = ownerVisual.Compositor;

            // Calculate shadow parameters based on mode
            var finalElevation = GetDirectElevation(mode, intensity, elevation);

            float blurRadius = (float)(finalElevation * 1.5);
            float offset = (float)(finalElevation * 0.6);

            // Get shadow color from design tokens (per-element global fallback)
            var darkBaseColor = DaisyNeumorphic.GetDarkShadowColor(_owner)
                ?? DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor;

            // Apply intensity to alpha channel (boosted for visibility on Windows)
            byte darkAlpha = (byte)Math.Clamp(160 * intensity, 60, 220);
            // Get owner's current size
            float width = (float)_owner.ActualWidth;
            float height = (float)_owner.ActualHeight;

            if (width <= 0 || height <= 0) return;

            // Create or update dark shadow (bottom-right)
            if (_darkShadowVisual == null)
            {
                _darkShadowVisual = compositor.CreateSpriteVisual();
                var darkShadow = compositor.CreateDropShadow();
                _darkShadowVisual.Shadow = darkShadow;
            }

            var darkDropShadow = (DropShadow)_darkShadowVisual.Shadow!;
            darkDropShadow.BlurRadius = blurRadius;
            darkDropShadow.Offset = new Vector3(offset, offset, 0);
            darkDropShadow.Color = Color.FromArgb(darkAlpha, darkBaseColor.R, darkBaseColor.G, darkBaseColor.B);
            _darkShadowVisual.Size = new Vector2(width, height);

            if (UseRoundedShadowMask)
            {
                var cornerRadius = GetOwnerCornerRadius();
                if (cornerRadius > 0)
                {
                    ApplyRoundedMask(compositor, darkDropShadow, ref _darkMaskVisual, width, height, cornerRadius);
                }
                else
                {
                    darkDropShadow.Mask ??= compositor.CreateColorBrush(Colors.White);
                }
            }
            else
            {
                darkDropShadow.Mask ??= compositor.CreateColorBrush(Colors.White);
            }

            if (_lightShadowVisual != null)
            {
                _lightShadowVisual.Shadow = null;
                _lightShadowVisual = null;
            }

            if (_lightMaskVisual != null)
            {
                _lightMaskVisual.Shapes.Clear();
                _lightMaskVisual = null;
            }

            // Attach shadow visuals behind the owner if not already attached
            if (!_windowsShadowsAttached)
            {
                AttachShadowVisuals(ownerVisual);
                _windowsShadowsAttached = true;
            }

            // Subscribe to size changes if not already
            _owner.SizeChanged -= OnOwnerSizeChangedWindows;
            _owner.SizeChanged += OnOwnerSizeChangedWindows;
        }

        private float GetOwnerCornerRadius()
        {
            // Get corner radius from owner - use the main file's GetCornerRadius logic
            if (_owner is Microsoft.UI.Xaml.Controls.Control control)
                return (float)control.CornerRadius.TopLeft;
            if (_owner is Microsoft.UI.Xaml.Controls.Border border)
                return (float)border.CornerRadius.TopLeft;
            if (_owner is Microsoft.UI.Xaml.Controls.Grid grid)
                return (float)grid.CornerRadius.TopLeft;
            return 0f;
        }

        private void ApplyRoundedMask(
            Compositor compositor,
            DropShadow shadow,
            ref ShapeVisual? maskVisual,
            float width,
            float height,
            float cornerRadius)
        {
            // Create or update the mask shape visual
            if (maskVisual == null)
            {
                maskVisual = compositor.CreateShapeVisual();
                var geometry = compositor.CreateRoundedRectangleGeometry();
                var shape = compositor.CreateSpriteShape(geometry);
                shape.FillBrush = compositor.CreateColorBrush(Color.FromArgb(255, 255, 255, 255));
                maskVisual.Shapes.Add(shape);
            }

            // Update mask size and corner radius
            maskVisual.Size = new Vector2(width, height);
            if (maskVisual.Shapes[0] is CompositionSpriteShape spriteShape &&
                spriteShape.Geometry is CompositionRoundedRectangleGeometry roundedRect)
            {
                roundedRect.Size = new Vector2(width, height);
                roundedRect.CornerRadius = new Vector2(cornerRadius, cornerRadius);
            }

            // Create a CompositionVisualSurface to render the shape, then use as mask brush
            var surface = compositor.CreateVisualSurface();
            surface.SourceVisual = maskVisual;
            surface.SourceSize = new Vector2(width, height);

            var maskBrush = compositor.CreateSurfaceBrush(surface);
            shadow.Mask = maskBrush;
        }

        private void UpdateMaskSize(ShapeVisual? maskVisual, float width, float height, float cornerRadius)
        {
            if (maskVisual == null) return;

            maskVisual.Size = new Vector2(width, height);
            if (maskVisual.Shapes.Count > 0 &&
                maskVisual.Shapes[0] is CompositionSpriteShape spriteShape &&
                spriteShape.Geometry is CompositionRoundedRectangleGeometry roundedRect)
            {
                roundedRect.Size = new Vector2(width, height);
                roundedRect.CornerRadius = new Vector2(cornerRadius, cornerRadius);
            }
        }

        private void AttachShadowVisuals(Visual ownerVisual)
        {
            if (_darkShadowVisual != null && _lightShadowVisual == null)
            {
                ElementCompositionPreview.SetElementChildVisual(_owner, _darkShadowVisual);
                return;
            }

            var compositor = ownerVisual.Compositor;
            var container = compositor.CreateContainerVisual();

            if (_lightShadowVisual != null)
            {
                container.Children.InsertAtBottom(_lightShadowVisual);
            }

            if (_darkShadowVisual != null)
            {
                container.Children.InsertAtTop(_darkShadowVisual);
            }

            ElementCompositionPreview.SetElementChildVisual(_owner, container);
        }

        private void OnOwnerSizeChangedWindows(object sender, SizeChangedEventArgs e)
        {
            float width = (float)e.NewSize.Width;
            float height = (float)e.NewSize.Height;

            if (width <= 0 || height <= 0) return;

            if (_darkShadowVisual != null)
            {
                _darkShadowVisual.Size = new Vector2(width, height);
            }

            if (_lightShadowVisual != null)
            {
                _lightShadowVisual.Size = new Vector2(width, height);
            }

            // Update mask sizes if using rounded masks
            if (UseRoundedShadowMask)
            {
                var cornerRadius = GetOwnerCornerRadius();
                UpdateMaskSize(_darkMaskVisual, width, height, cornerRadius);
                UpdateMaskSize(_lightMaskVisual, width, height, cornerRadius);
            }
        }

        // ---------------------------------------------------------------------
        // Cleanup
        // ---------------------------------------------------------------------

        private void DisposeWindowsShadowVisuals()
        {
            // Clean up based on which mode was active
            DisposeDropShadowVisuals();
        }

        private void DisposeDropShadowVisuals()
        {
            _owner.SizeChanged -= OnOwnerSizeChangedWindows;

            if (_darkShadowVisual != null)
            {
                _darkShadowVisual.Shadow = null;
                _darkShadowVisual = null;
            }

            if (_lightShadowVisual != null)
            {
                _lightShadowVisual.Shadow = null;
                _lightShadowVisual = null;
            }

            // Clean up mask visuals
            if (_darkMaskVisual != null)
            {
                _darkMaskVisual.Shapes.Clear();
                _darkMaskVisual = null;
            }

            if (_lightMaskVisual != null)
            {
                _lightMaskVisual.Shapes.Clear();
                _lightMaskVisual = null;
            }

            // Clear the child visual
            try
            {
                ElementCompositionPreview.SetElementChildVisual(_owner, null);
            }
            catch
            {
                // Ignore if already disposed or not attached
            }

            _windowsShadowsAttached = false;
        }
    }
}
#endif


