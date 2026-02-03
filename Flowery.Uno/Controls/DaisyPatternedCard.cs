using System;
using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Microsoft.UI.Dispatching;

namespace Flowery.Controls
{
    /// <summary>
    /// A DaisyCard with pattern and ornament layer support.
    /// Extends DaisyCard with decorative pattern backgrounds and corner ornaments.
    /// </summary>
    [Bindable]
    public partial class DaisyPatternedCard : DaisyCard
    {
        private Canvas? _patternLayer;
        private Canvas? _ornamentLayer;
        private bool _isRebuildPending;

        public DaisyPatternedCard()
        {
            DefaultStyleKey = typeof(DaisyPatternedCard);
            SizeChanged += OnSizeChanged;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _patternLayer = GetTemplateChild("PART_PatternLayer") as Canvas;
            _ornamentLayer = GetTemplateChild("PART_OrnamentLayer") as Canvas;

            RebuildDecorativeLayers();
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
        }

        private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            RebuildDecorativeLayers();
        }

        #region DeferPatternGeneration
        public static readonly DependencyProperty DeferPatternGenerationProperty =
            DependencyProperty.Register(
                nameof(DeferPatternGeneration),
                typeof(bool),
                typeof(DaisyPatternedCard),
                new PropertyMetadata(false, OnDeferPatternGenerationChanged));

        /// <summary>
        /// When true, pattern and ornament generation is deferred until set to false.
        /// Useful for WASM where creating multiple cards in sequence can cause frame drops.
        /// Set to true before adding to visual tree, then false when ready to render.
        /// </summary>
        public bool DeferPatternGeneration
        {
            get => (bool)GetValue(DeferPatternGenerationProperty);
            set => SetValue(DeferPatternGenerationProperty, value);
        }

        private static void OnDeferPatternGenerationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPatternedCard card && e.NewValue is false)
            {
                // Deferred generation now enabled - rebuild directly (not via dispatcher)
                // This allows WASM animation frames to actually interleave
                card.RebuildDecorativeLayersDirect();
            }
        }
        #endregion

        #region Pattern
        public static readonly DependencyProperty PatternProperty =
            DependencyProperty.Register(
                nameof(Pattern),
                typeof(DaisyCardPattern),
                typeof(DaisyPatternedCard),
                new PropertyMetadata(DaisyCardPattern.None, OnPatternChanged));

        /// <summary>
        /// The background pattern of the card.
        /// </summary>
        public DaisyCardPattern Pattern
        {
            get => (DaisyCardPattern)GetValue(PatternProperty);
            set => SetValue(PatternProperty, value);
        }
        #endregion

        #region PatternMode
        public static readonly DependencyProperty PatternModeProperty =
            DependencyProperty.Register(
                nameof(PatternMode),
                typeof(FloweryPatternMode),
                typeof(DaisyPatternedCard),
                new PropertyMetadata(FloweryPatternMode.SvgAsset, OnPatternChanged));

        /// <summary>
        /// How the pattern is rendered. Generated = custom geometry per card size (default).
        /// Tiled = seamless repeating tile (faster for large cards).
        /// </summary>
        public FloweryPatternMode PatternMode
        {
            get => (FloweryPatternMode)GetValue(PatternModeProperty);
            set => SetValue(PatternModeProperty, value);
        }
        #endregion

        #region Ornament
        public static readonly DependencyProperty OrnamentProperty =
            DependencyProperty.Register(
                nameof(Ornament),
                typeof(DaisyCardOrnament),
                typeof(DaisyPatternedCard),
                new PropertyMetadata(DaisyCardOrnament.None, OnPatternChanged));

        /// <summary>
        /// Decorative ornaments for the card corners.
        /// </summary>
        public DaisyCardOrnament Ornament
        {
            get => (DaisyCardOrnament)GetValue(OrnamentProperty);
            set => SetValue(OrnamentProperty, value);
        }
        #endregion

        private static void OnPatternChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyPatternedCard card)
            {
                card.RebuildDecorativeLayers();
            }
        }

        /// <summary>
        /// Rebuild directly without dispatcher - used when DeferPatternGeneration becomes false.
        /// </summary>
        private void RebuildDecorativeLayersDirect()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            RebuildPatternLayer();
            RebuildOrnamentLayer();
        }

        private void RebuildDecorativeLayers()
        {
            // Skip if deferred - will be called directly when DeferPatternGeneration is set to false
            if (DeferPatternGeneration) return;
            if (ActualWidth <= 0 || ActualHeight <= 0) return;

            if (_isRebuildPending) return;

            _isRebuildPending = true;
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                _isRebuildPending = false;
                if (DeferPatternGeneration) return;
                if (ActualWidth <= 0 || ActualHeight <= 0) return;

                RebuildPatternLayer();
                RebuildOrnamentLayer();
            });
        }



        private void RebuildPatternLayer()
        {
            if (_patternLayer == null) return;
            _patternLayer.Children.Clear();

            if (Pattern == DaisyCardPattern.None) return;

            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0) return;

            // Patterns use the Foreground brush (which is the VariantContentBrush)
            var patternBrush = Foreground;

            // Check if we should use asset mode (PNG files - fast, pre-rasterized)
            if (PatternMode == FloweryPatternMode.SvgAsset && FloweryPatternSvgLoader.HasSvgAsset(Pattern))
            {
                var tiledCanvas = FloweryPatternSvgLoader.CreateTiledCanvas(Pattern, width, height);
                if (tiledCanvas != null)
                {
                    _patternLayer.Children.Add(tiledCanvas);
                    return;
                }
                // Fall through to geometry mode if asset loading failed
            }

            // All patterns now use FloweryPatternTileGenerator
            if (FloweryPatternTileGenerator.SupportsTiling(Pattern))
            {
                var tiledCanvas = FloweryPatternTileGenerator.CreateTiledPatternCanvas(Pattern, patternBrush, width, height);
                if (tiledCanvas != null)
                {
                    _patternLayer.Children.Add(tiledCanvas);
                }
            }
        }

        private void RebuildOrnamentLayer()
        {
            if (_ornamentLayer == null) return;
            _ornamentLayer.Children.Clear();

            if (Ornament == DaisyCardOrnament.None) return;

            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0) return;

            var accent = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");

            switch (Ornament)
            {
                case DaisyCardOrnament.Corners:
                    BuildCornersOrnament(width, height, accent);
                    break;
                case DaisyCardOrnament.Brackets:
                    BuildBracketsOrnament(width, height, accent);
                    break;
                case DaisyCardOrnament.Industrial:
                    BuildIndustrialOrnament(width, height, accent);
                    break;
            }
        }

        private void BuildCornersOrnament(double width, double height, Brush brush)
        {
            double size = 16;
            // Top Left
            AddTriangle(0, 0, size, size, brush, 0);
            // Top Right
            AddTriangle(width - size, 0, size, size, brush, 90);
            // Bottom Right
            AddTriangle(width - size, height - size, size, size, brush, 180);
            // Bottom Left
            AddTriangle(0, height - size, size, size, brush, 270);
        }

        private void AddTriangle(double x, double y, double w, double h, Brush brush, double angle)
        {
            var poly = new Microsoft.UI.Xaml.Shapes.Polygon
            {
                Points = new PointCollection { new Point(0, 0), new Point(w, 0), new Point(0, h) },
                Fill = brush,
                Opacity = 0.6,
                RenderTransform = new RotateTransform { Angle = angle, CenterX = w/2, CenterY = h/2 }
            };
            Canvas.SetLeft(poly, x);
            Canvas.SetTop(poly, y);
            _ornamentLayer?.Children.Add(poly);
        }

        private void BuildBracketsOrnament(double width, double height, Brush brush)
        {
            double size = 20;
            double thickness = 3;
            // Top Left
            AddBracket(0, 0, size, size, thickness, brush, 0, 0);
            // Top Right
            AddBracket(width - size, 0, size, size, thickness, brush, 1, 0);
            // Bottom Left
            AddBracket(0, height - size, size, size, thickness, brush, 0, 1);
            // Bottom Right
            AddBracket(width - size, height - size, size, size, thickness, brush, 1, 1);
        }

        private void AddBracket(double x, double y, double w, double h, double t, Brush brush, int hDir, int vDir)
        {
            var g = new Microsoft.UI.Xaml.Shapes.Path
            {
                Stroke = brush,
                StrokeThickness = t,
                Opacity = 0.8,
                Data = GeometryFromString(hDir == 0 
                    ? (vDir == 0 ? $"M {w},0 L 0,0 L 0,{h}" : $"M {w},{h} L 0,{h} L 0,0")
                    : (vDir == 0 ? $"M 0,0 L {w},0 L {w},{h}" : $"M 0,{h} L {w},{h} L {w},0"))
            };
            Canvas.SetLeft(g, x);
            Canvas.SetTop(g, y);
            _ornamentLayer?.Children.Add(g);
        }

        private void BuildIndustrialOrnament(double width, double height, Brush brush)
        {
            // Small hex/bolt like markers or tech lines
            double size = 12;
            AddBolt(6, 6, size, brush);
            AddBolt(width - 6 - size, 6, size, brush);
            AddBolt(width - 6 - size, height - 6 - size, size, brush);
            AddBolt(6, height - 6 - size, size, brush);
        }

        private void AddBolt(double x, double y, double size, Brush brush)
        {
            var el = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = size, Height = size, Stroke = brush, StrokeThickness = 2, Opacity = 0.5 };
            var inner = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = size/2, Height = size/2, Fill = brush, Opacity = 0.7 };
            Canvas.SetLeft(el, x); Canvas.SetTop(el, y);
            Canvas.SetLeft(inner, x + size/4); Canvas.SetTop(inner, y + size/4);
            _ornamentLayer?.Children.Add(el);
            _ornamentLayer?.Children.Add(inner);
        }

        private static Geometry GeometryFromString(string pathData)
        {
            return (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Geometry), pathData);
        }
    }
}
