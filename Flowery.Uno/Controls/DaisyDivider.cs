using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Flowery.Theming;
using Windows.Foundation;

namespace Flowery.Controls
{
    /// <summary>
    /// A Divider control styled after DaisyUI's Divider component.
    /// Supports horizontal/vertical orientation, content placement, and multiple visual styles.
    /// </summary>
    public partial class DaisyDivider : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private TextBlock? _textBlock;
        private Border? _textContainer;
        private bool _neumorphicOptOutApplied;

        // Style-specific elements (created as needed)
        private Rectangle? _line1;
        private Rectangle? _line2;        // For Inset/Double
        private Path? _linePath;          // For Wave/Tapered
        private Ellipse? _ornamentShape;  // For Ornament
        private Rectangle? _glowLine;     // For Glow shadow
        private Storyboard? _glowStoryboard; // For Glow pulse animation

        public DaisyDivider()
        {
            DefaultStyleKey = typeof(DaisyDivider);
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            IsTabStop = false;

            // Build visual tree immediately in constructor
            RebuildVisualTree();
        }

        private void ApplyAll()
        {
            ApplySizing();
            ApplyColors();
        }

        #region Dependency Properties

        public static readonly DependencyProperty HorizontalProperty = DependencyProperty.Register(
            nameof(Horizontal), typeof(bool), typeof(DaisyDivider), new PropertyMetadata(false, OnLayoutChanged));

        public bool Horizontal
        {
            get => (bool)GetValue(HorizontalProperty);
            set => SetValue(HorizontalProperty, value);
        }

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color), typeof(DaisyDividerColor), typeof(DaisyDivider), new PropertyMetadata(DaisyDividerColor.Default, OnAppearanceChanged));

        public DaisyDividerColor Color
        {
            get => (DaisyDividerColor)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty PlacementProperty = DependencyProperty.Register(
            nameof(Placement), typeof(DaisyDividerPlacement), typeof(DaisyDivider), new PropertyMetadata(DaisyDividerPlacement.Default, OnLayoutChanged));

        public DaisyDividerPlacement Placement
        {
            get => (DaisyDividerPlacement)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public static readonly DependencyProperty DividerTextProperty = DependencyProperty.Register(
            nameof(DividerText), typeof(string), typeof(DaisyDivider), new PropertyMetadata(null, OnTextChanged));

        public string? DividerText
        {
            get => (string?)GetValue(DividerTextProperty);
            set => SetValue(DividerTextProperty, value);
        }

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            nameof(Size), typeof(DaisySize), typeof(DaisyDivider), new PropertyMetadata(DaisySize.Small, OnSizeChanged));

        /// <summary>
        /// Gets or sets the size of the divider.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty TextBackgroundProperty = DependencyProperty.Register(
            nameof(TextBackground), typeof(Brush), typeof(DaisyDivider), new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the background brush for the divider text container.
        /// When null (default), uses DaisyBase100Brush. Set this to match
        /// the parent container's background when using dividers inside cards.
        /// </summary>
        public Brush? TextBackground
        {
            get => (Brush?)GetValue(TextBackgroundProperty);
            set => SetValue(TextBackgroundProperty, value);
        }

        public static readonly DependencyProperty DividerStyleProperty = DependencyProperty.Register(
            nameof(DividerStyle), typeof(DaisyDividerStyle), typeof(DaisyDivider), new PropertyMetadata(DaisyDividerStyle.Solid, OnStyleChanged));

        /// <summary>
        /// Gets or sets the visual style of the divider.
        /// </summary>
        public DaisyDividerStyle DividerStyle
        {
            get => (DaisyDividerStyle)GetValue(DividerStyleProperty);
            set => SetValue(DividerStyleProperty, value);
        }

        public static readonly DependencyProperty NeumorphicOptOutProperty = DependencyProperty.Register(
            nameof(NeumorphicOptOut), typeof(bool), typeof(DaisyDivider), new PropertyMetadata(true, OnNeumorphicOptOutChanged));

        /// <summary>
        /// Gets or sets whether this divider opts out of neumorphic effects.
        /// When true (default), the divider disables neumorphic rendering unless explicitly enabled.
        /// </summary>
        public bool NeumorphicOptOut
        {
            get => (bool)GetValue(NeumorphicOptOutProperty);
            set => SetValue(NeumorphicOptOutProperty, value);
        }

        public static readonly DependencyProperty OrnamentProperty = DependencyProperty.Register(
            nameof(Ornament), typeof(DaisyDividerOrnament), typeof(DaisyDivider), new PropertyMetadata(DaisyDividerOrnament.Diamond, OnStyleChanged));

        /// <summary>
        /// Gets or sets the ornament shape when DividerStyle is Ornament.
        /// </summary>
        public DaisyDividerOrnament Ornament
        {
            get => (DaisyDividerOrnament)GetValue(OrnamentProperty);
            set => SetValue(OrnamentProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
            nameof(Thickness), typeof(double), typeof(DaisyDivider), new PropertyMetadata(1.0, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the thickness of the divider line(s) in pixels.
        /// Default is 2.
        /// </summary>
        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        #endregion

        #region Property Changed Handlers

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDivider divider)
            {
                divider.ApplyColors();
            }
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDivider divider)
            {
                divider.RebuildVisualTree();
            }
        }

        private static void OnStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDivider divider)
            {
                divider.RebuildVisualTree();
            }
        }

        private static void OnNeumorphicOptOutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDivider divider)
            {
                divider.ApplyNeumorphicOptOut();
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDivider divider)
            {
                divider.UpdateTextVisibility();
            }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDivider divider)
            {
                divider.RebuildVisualTree();
            }
        }

        #endregion

        protected override void OnLoaded()
        {
            base.OnLoaded();
            ApplyNeumorphicOptOut();
            ApplyAll();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        #region Sizing Helpers

        private DaisySize EffectiveSize =>
            FlowerySizeManager.ShouldIgnoreGlobalSize(this) ? Size : FlowerySizeManager.CurrentSize;

        private double GetThickness()
        {
            return Thickness;
        }

        private double GetFontSize()
        {
            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(EffectiveSize);
            return DaisyResourceLookup.GetDouble($"DaisyDivider{sizeKey}FontSize", 12d);
        }

        private double GetTextPadding()
        {
            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(EffectiveSize);
            return DaisyResourceLookup.GetDouble($"DaisyDivider{sizeKey}TextPadding", 12d);
        }

        private double GetMargin()
        {
            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(EffectiveSize);
            return DaisyResourceLookup.GetDouble($"DaisyDivider{sizeKey}Margin", 3d);
        }

        #endregion

        private void ApplyNeumorphicOptOut()
        {
            if (NeumorphicOptOut)
            {
                var localValue = ReadLocalValue(DaisyNeumorphic.IsEnabledProperty);
                if (localValue == DependencyProperty.UnsetValue || (_neumorphicOptOutApplied && localValue is bool b && b == false))
                {
                    DaisyNeumorphic.SetIsEnabled(this, false);
                    _neumorphicOptOutApplied = true;
                    return;
                }

                if (localValue is bool b2 && b2)
                {
                    _neumorphicOptOutApplied = false;
                }

                return;
            }

            if (_neumorphicOptOutApplied)
            {
                DaisyNeumorphic.SetIsEnabled(this, null);
                _neumorphicOptOutApplied = false;
            }
        }

        #region Color/Brush Helpers

        private Brush GetLineBrush()
        {
            // Get color name for resource lookup
            var colorName = Color switch
            {
                DaisyDividerColor.Primary => "Primary",
                DaisyDividerColor.Secondary => "Secondary",
                DaisyDividerColor.Accent => "Accent",
                DaisyDividerColor.Success => "Success",
                DaisyDividerColor.Warning => "Warning",
                DaisyDividerColor.Info => "Info",
                DaisyDividerColor.Error => "Error",
                DaisyDividerColor.Neutral => "Neutral",
                _ => ""
            };

            // Check for lightweight styling overrides
            var lineOverride = !string.IsNullOrEmpty(colorName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyDivider", $"{colorName}LineBrush")
                : null;
            lineOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyDivider", "LineBrush");

            if (lineOverride != null) return lineOverride;

            string brushKey = Color switch
            {
                DaisyDividerColor.Default => "DaisyBase300Brush",
                DaisyDividerColor.Neutral => "DaisyNeutralBrush",
                DaisyDividerColor.Primary => "DaisyPrimaryBrush",
                DaisyDividerColor.Secondary => "DaisySecondaryBrush",
                DaisyDividerColor.Accent => "DaisyAccentBrush",
                DaisyDividerColor.Success => "DaisySuccessBrush",
                DaisyDividerColor.Warning => "DaisyWarningBrush",
                DaisyDividerColor.Info => "DaisyInfoBrush",
                DaisyDividerColor.Error => "DaisyErrorBrush",
                _ => "DaisyBaseContentBrush"
            };

            return DaisyResourceLookup.GetBrush(brushKey) ?? new SolidColorBrush(Colors.Gray);
        }

        private double GetLineOpacity()
        {
            return Color == DaisyDividerColor.Neutral ? 0.7 : 1.0;
        }

        private Brush GetTextForeground()
        {
            var textFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyDivider", "TextForeground");
            return textFgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush") ?? new SolidColorBrush(Colors.White);
        }

        private Brush GetTextBackgroundBrush()
        {
            var textBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyDivider", "TextBackground");
            return TextBackground
                ?? textBgOverride
                ?? DaisyResourceLookup.GetBrush("DaisyBase100Brush")
                ?? new SolidColorBrush(Colors.Transparent);
        }

        /// <summary>
        /// Creates a gradient brush that fades from full opacity at center to transparent at edges.
        /// </summary>
        private LinearGradientBrush CreateCenterFadeGradient(Brush baseBrush)
        {
            var color = GetBrushColor(baseBrush);
            var transparentColor = Windows.UI.Color.FromArgb(0, color.R, color.G, color.B);

            var gradient = new LinearGradientBrush
            {
                StartPoint = Horizontal ? new Point(0.5, 0) : new Point(0, 0.5),
                EndPoint = Horizontal ? new Point(0.5, 1) : new Point(1, 0.5),
                GradientStops =
                {
                    new GradientStop { Color = transparentColor, Offset = 0 },
                    new GradientStop { Color = color, Offset = 0.3 },
                    new GradientStop { Color = color, Offset = 0.7 },
                    new GradientStop { Color = transparentColor, Offset = 1 }
                }
            };
            return gradient;
        }

        /// <summary>
        /// Extracts the color from a brush (handles SolidColorBrush).
        /// </summary>
        private static Windows.UI.Color GetBrushColor(Brush brush)
        {
            if (brush is SolidColorBrush scb)
                return scb.Color;
            return Colors.Gray;
        }

        /// <summary>
        /// Creates a shadow brush (darker version for Inset style).
        /// </summary>
        private static SolidColorBrush CreateShadowBrush(double opacity = 0.4)
        {
            return new SolidColorBrush(Windows.UI.Color.FromArgb((byte)(255 * opacity), 0, 0, 0));
        }

        /// <summary>
        /// Creates a highlight brush (lighter version for Inset style).
        /// </summary>
        private static SolidColorBrush CreateHighlightBrush(double opacity = 0.25)
        {
            return new SolidColorBrush(Windows.UI.Color.FromArgb((byte)(255 * opacity), 255, 255, 255));
        }

        #endregion

        #region Visual Tree Construction

        private void ClearVisualElements()
        {
            _line1 = null;
            _line2 = null;
            _linePath = null;
            _ornamentShape = null;
            _glowLine = null;
            _textBlock = null;
            _textContainer = null;

            // Stop and clear glow animation
            StopGlowAnimation();
        }

        private void StopGlowAnimation()
        {
            if (_glowStoryboard != null)
            {
                _glowStoryboard.Stop();
                _glowStoryboard = null;
            }
        }

        private void RebuildVisualTree()
        {
            ClearVisualElements();

            double thickness = GetThickness();
            double margin = GetMargin();
            double textPadding = GetTextPadding();

            // Create root grid
            _rootGrid = new Grid
            {
                HorizontalAlignment = Horizontal ? HorizontalAlignment.Center : HorizontalAlignment.Stretch,
                VerticalAlignment = Horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Center,
                Margin = Horizontal ? new Thickness(margin, 0, margin, 0) : new Thickness(0, margin, 0, margin)
            };

            // Set minimum dimensions based on style
            // Styles that need more height/width for patterns, paths, or ornaments
            double waveMinSize = thickness * 6;

            if (Horizontal)
            {
                // Vertical divider: Needs min width for patterns
                bool needsExtraWidth = DividerStyle == DaisyDividerStyle.Wave ||
                                       DividerStyle == DaisyDividerStyle.Tapered ||
                                       DividerStyle == DaisyDividerStyle.Ornament;
                _rootGrid.MinWidth = needsExtraWidth ? waveMinSize : Math.Max(thickness, 2);
            }
            else
            {
                // Horizontal divider: Needs min height for patterns
                bool needsExtraHeight = DividerStyle == DaisyDividerStyle.Wave ||
                                        DividerStyle == DaisyDividerStyle.Tapered ||
                                        DividerStyle == DaisyDividerStyle.Ornament;
                _rootGrid.MinHeight = needsExtraHeight ? waveMinSize : Math.Max(thickness, 2);
            }

            // Build style-specific visuals
            switch (DividerStyle)
            {
                case DaisyDividerStyle.Solid:
                    BuildSolidVisual(thickness);
                    break;
                case DaisyDividerStyle.Inset:
                    BuildInsetVisual(thickness);
                    break;
                case DaisyDividerStyle.Gradient:
                    BuildGradientVisual(thickness);
                    break;
                case DaisyDividerStyle.Ornament:
                    BuildOrnamentVisual(thickness);
                    break;
                case DaisyDividerStyle.Wave:
                    BuildWaveVisual(thickness);
                    break;
                case DaisyDividerStyle.Glow:
                    BuildGlowVisual(thickness);
                    break;
                case DaisyDividerStyle.Dashed:
                    BuildDashedVisual(thickness);
                    break;
                case DaisyDividerStyle.Dotted:
                    BuildDottedVisual(thickness);
                    break;
                case DaisyDividerStyle.Tapered:
                    BuildTaperedVisual(thickness);
                    break;
                case DaisyDividerStyle.Double:
                    BuildDoubleVisual(thickness);
                    break;
            }

            // Add text container (for all styles)
            BuildTextContainer(textPadding);

            Content = _rootGrid;
            UpdateTextVisibility();
            ApplyColors();
        }

        #endregion

        #region Style Builders

        /// <summary>
        /// Solid: Simple single line (default).
        /// </summary>
        private void BuildSolidVisual(double thickness)
        {
            _line1 = CreateBasicLine(thickness);
            _rootGrid!.Children.Add(_line1);
        }

        /// <summary>
        /// Inset: Dual-line 3D embossed effect (groove) - shadow on top, highlight below.
        /// </summary>
        private void BuildInsetVisual(double thickness)
        {
            // For a visible 3D groove effect on ANY background:
            // - Use a 2-row/column grid to GUARANTEE separation
            // - Top line: semi-transparent dark (works on light bg)
            // - Bottom line: semi-transparent light (works on dark bg)

            double lineThickness = Math.Max(1.0, thickness);
            double gap = Math.Max(1, lineThickness * 0.5);

            // Create inner container with explicit rows/columns for guaranteed separation
            var insetContainer = new Grid();

            if (Horizontal)
            {
                // Vertical orientation: two columns
                insetContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(lineThickness) });
                insetContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap) });
                insetContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(lineThickness) });
                insetContainer.HorizontalAlignment = HorizontalAlignment.Center;
                insetContainer.VerticalAlignment = VerticalAlignment.Stretch;

                // Shadow line (left)
                _line1 = new Rectangle
                {
                    Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 0, 0, 0)), // 31% black
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                Grid.SetColumn(_line1, 0);

                // Highlight line (right)
                _line2 = new Rectangle
                {
                    Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 255, 255, 255)), // 55% white
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                Grid.SetColumn(_line2, 2);
            }
            else
            {
                // Horizontal orientation: two rows
                insetContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(lineThickness) });
                insetContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gap) });
                insetContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(lineThickness) });
                insetContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
                insetContainer.VerticalAlignment = VerticalAlignment.Center;

                // Shadow line (top) - darker
                _line1 = new Rectangle
                {
                    Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 0, 0, 0)), // 31% black
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                Grid.SetRow(_line1, 0);

                // Highlight line (bottom) - lighter
                _line2 = new Rectangle
                {
                    Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 255, 255, 255)), // 55% white
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                Grid.SetRow(_line2, 2);
            }

            insetContainer.Children.Add(_line1);
            insetContainer.Children.Add(_line2);
            _rootGrid!.Children.Add(insetContainer);
        }

        /// <summary>
        /// Gradient: Fades from center to transparent edges.
        /// </summary>
        private void BuildGradientVisual(double thickness)
        {
            _line1 = CreateBasicLine(thickness);
            // Gradient brush applied in ApplyColors
            _rootGrid!.Children.Add(_line1);
        }

        /// <summary>
        /// Ornament: Line with decorative center shape.
        /// </summary>
        private void BuildOrnamentVisual(double thickness)
        {
            double ornamentSize = thickness * 4;

            // Create a 3-column/row layout for line - ornament - line
            if (Horizontal)
            {
                _rootGrid!.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                _line1 = CreateBasicLine(thickness);
                _line1.VerticalAlignment = VerticalAlignment.Stretch;
                Grid.SetRow(_line1, 0);

                _line2 = CreateBasicLine(thickness);
                _line2.VerticalAlignment = VerticalAlignment.Stretch;
                Grid.SetRow(_line2, 2);
            }
            else
            {
                _rootGrid!.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                _line1 = CreateBasicLine(thickness);
                _line1.HorizontalAlignment = HorizontalAlignment.Stretch;
                Grid.SetColumn(_line1, 0);

                _line2 = CreateBasicLine(thickness);
                _line2.HorizontalAlignment = HorizontalAlignment.Stretch;
                Grid.SetColumn(_line2, 2);
            }

            // Create ornament shape
            var ornament = CreateOrnamentShape(ornamentSize);
            ornament.HorizontalAlignment = HorizontalAlignment.Center;
            ornament.VerticalAlignment = VerticalAlignment.Center;

            if (Horizontal)
            {
                Grid.SetRow(ornament, 1);
                ornament.Margin = new Thickness(0, 4, 0, 4);
            }
            else
            {
                Grid.SetColumn(ornament, 1);
                ornament.Margin = new Thickness(4, 0, 4, 0);
            }

            _rootGrid.Children.Add(_line1);
            _rootGrid.Children.Add(ornament);
            _rootGrid.Children.Add(_line2);
        }

        /// <summary>
        /// Wave: Curved/wavy line using Path inside a Border for sizing.
        /// </summary>
        private void BuildWaveVisual(double thickness)
        {
            // Use a Border as wrapper to get proper sizing - Path alone has no size without geometry
            var waveContainer = new Border
            {
                HorizontalAlignment = Horizontal ? HorizontalAlignment.Center : HorizontalAlignment.Stretch,
                VerticalAlignment = Horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Center,
                MinHeight = Horizontal ? 0 : thickness * 6,
                MinWidth = Horizontal ? thickness * 6 : 0
            };

            _linePath = new Path
            {
                StrokeThickness = thickness,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.None, // Don't stretch - we'll create exact geometry
                UseLayoutRounding = true
            };

            waveContainer.Child = _linePath;

            // Listen to container size changes instead of path
            waveContainer.SizeChanged += OnWaveContainerSizeChanged;

            _rootGrid!.Children.Add(waveContainer);
        }

        private void OnWaveContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_linePath == null) return;

            double width = e.NewSize.Width;
            double height = e.NewSize.Height;

            if (width <= 0 || height <= 0) return;

            UpdateWaveGeometry(width, height);
        }

        private void UpdateWaveGeometry(double width, double height)
        {
            if (_linePath == null) return;

            double thickness = GetThickness();
            double amplitude = thickness * 1.5;
            int waveCount = Horizontal ? (int)Math.Max(4, height / 25) : (int)Math.Max(4, width / 25);

            var geometry = new PathGeometry();
            var figure = new PathFigure { IsClosed = false };

            if (Horizontal)
            {
                // Vertical wave
                double centerX = width / 2;
                figure.StartPoint = new Point(centerX, 0);
                double segmentHeight = height / waveCount;

                for (int i = 0; i < waveCount; i++)
                {
                    double yStart = segmentHeight * i;
                    double yMid = yStart + segmentHeight / 2;
                    double yEnd = yStart + segmentHeight;
                    double xOffset = (i % 2 == 0) ? amplitude : -amplitude;

                    var bezier = new BezierSegment
                    {
                        Point1 = new Point(centerX + xOffset, yMid * 0.5 + yStart * 0.5),
                        Point2 = new Point(centerX + xOffset, yMid * 0.5 + yEnd * 0.5),
                        Point3 = new Point(centerX, yEnd)
                    };
                    figure.Segments.Add(bezier);
                }
            }
            else
            {
                // Horizontal wave
                double centerY = height / 2;
                figure.StartPoint = new Point(0, centerY);
                double segmentWidth = width / waveCount;

                for (int i = 0; i < waveCount; i++)
                {
                    double xStart = segmentWidth * i;
                    double xMid = xStart + segmentWidth / 2;
                    double xEnd = xStart + segmentWidth;
                    double yOffset = (i % 2 == 0) ? -amplitude : amplitude;

                    var bezier = new BezierSegment
                    {
                        Point1 = new Point(xMid * 0.5 + xStart * 0.5, centerY + yOffset),
                        Point2 = new Point(xMid * 0.5 + xEnd * 0.5, centerY + yOffset),
                        Point3 = new Point(xEnd, centerY)
                    };
                    figure.Segments.Add(bezier);
                }
            }

            geometry.Figures.Add(figure);
            _linePath.Data = geometry;
        }

        /// <summary>
        /// Glow: Neon effect with blur shadow and pulsing animation.
        /// </summary>
        private void BuildGlowVisual(double thickness)
        {
            // "Glow" shadow (thicker, with pulsing opacity)
            _glowLine = CreateBasicLine(thickness * 4);
            _glowLine.Opacity = 0.2;
            _glowLine.RadiusX = thickness * 2.5;
            _glowLine.RadiusY = thickness * 2.5;

            // Main line (bright core)
            _line1 = CreateBasicLine(thickness);

            _rootGrid!.Children.Add(_glowLine);
            _rootGrid.Children.Add(_line1);

            // Start glow pulse animation when loaded
            _glowLine.Loaded += (s, e) => StartGlowAnimation();
        }

        private void StartGlowAnimation()
        {
            if (_glowLine == null) return;

            // Use explicit keyframes to ensure smooth pulsation in all environments (avoiding potential AutoReverse bugs)
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(2400)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            var ease = new SineEase { EasingMode = EasingMode.EaseInOut };

            animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.2, EasingFunction = ease });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1200)), Value = 0.6, EasingFunction = ease });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2400)), Value = 0.2, EasingFunction = ease });

            Storyboard.SetTarget(animation, _glowLine);
            Storyboard.SetTargetProperty(animation, "Opacity");

            _glowStoryboard = new Storyboard();
            _glowStoryboard.Children.Add(animation);
            _glowStoryboard.Begin();
        }

        /// <summary>
        /// Dashed: Dashed line pattern.
        /// </summary>
        private void BuildDashedVisual(double thickness)
        {
            _linePath = CreateStrokedLine(thickness, new DoubleCollection { 4, 2 });
            _rootGrid!.Children.Add(_linePath);
        }

        /// <summary>
        /// Dotted: Dotted line pattern.
        /// </summary>
        private void BuildDottedVisual(double thickness)
        {
            _linePath = CreateStrokedLine(thickness, new DoubleCollection { 1, 2 });
            _linePath.StrokeStartLineCap = PenLineCap.Round;
            _linePath.StrokeEndLineCap = PenLineCap.Round;
            _linePath.StrokeDashCap = PenLineCap.Round;
            _rootGrid!.Children.Add(_linePath);
        }

        /// <summary>
        /// Tapered: Thick center tapering to thin ends.
        /// </summary>
        private void BuildTaperedVisual(double _)
        {
            _linePath = new Path
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.None, // Exact geometry calculation handled in SizeChanged
                UseLayoutRounding = false // Allow sub-pixel tapering
            };

            _linePath.SizeChanged += OnTaperedPathSizeChanged;
            _rootGrid!.Children.Add(_linePath);
        }

        private void OnTaperedPathSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_linePath == null) return;

            double width = e.NewSize.Width;
            double height = e.NewSize.Height;
            double thickness = GetThickness();

            if (width <= 0 || height <= 0) return;

            var geometry = new PathGeometry();
            var figure = new PathFigure { IsClosed = true, IsFilled = true };

            if (Horizontal)
            {
                // Vertical tapered: thin at top/bottom, thick in middle
                double centerX = width / 2;
                double midY = height / 2;
                double thinWidth = Math.Max(0.5, thickness / 3);
                double thickWidth = thickness;

                figure.StartPoint = new Point(centerX, 0);
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(centerX + thinWidth / 2, 0),
                    Point2 = new Point(centerX + thickWidth / 2, midY - height * 0.2),
                    Point3 = new Point(centerX + thickWidth / 2, midY)
                });
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(centerX + thickWidth / 2, midY + height * 0.2),
                    Point2 = new Point(centerX + thinWidth / 2, height),
                    Point3 = new Point(centerX, height)
                });
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(centerX - thinWidth / 2, height),
                    Point2 = new Point(centerX - thickWidth / 2, midY + height * 0.2),
                    Point3 = new Point(centerX - thickWidth / 2, midY)
                });
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(centerX - thickWidth / 2, midY - height * 0.2),
                    Point2 = new Point(centerX - thinWidth / 2, 0),
                    Point3 = new Point(centerX, 0)
                });
            }
            else
            {
                // Horizontal tapered: thin at left/right, thick in middle
                double centerY = height / 2;
                double midX = width / 2;
                double thinHeight = Math.Max(0.5, thickness / 3);
                double thickHeight = thickness;

                figure.StartPoint = new Point(0, centerY);
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(0, centerY - thinHeight / 2),
                    Point2 = new Point(midX - width * 0.2, centerY - thickHeight / 2),
                    Point3 = new Point(midX, centerY - thickHeight / 2)
                });
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(midX + width * 0.2, centerY - thickHeight / 2),
                    Point2 = new Point(width, centerY - thinHeight / 2),
                    Point3 = new Point(width, centerY)
                });
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(width, centerY + thinHeight / 2),
                    Point2 = new Point(midX + width * 0.2, centerY + thickHeight / 2),
                    Point3 = new Point(midX, centerY + thickHeight / 2)
                });
                figure.Segments.Add(new BezierSegment
                {
                    Point1 = new Point(midX - width * 0.2, centerY + thickHeight / 2),
                    Point2 = new Point(0, centerY + thinHeight / 2),
                    Point3 = new Point(0, centerY)
                });
            }

            geometry.Figures.Add(figure);
            _linePath.Data = geometry;
        }

        /// <summary>
        /// Double: Two parallel lines with gap.
        /// </summary>
        private void BuildDoubleVisual(double thickness)
        {
            double gap = thickness;
            double lineThickness = thickness;

            _line1 = CreateBasicLine(lineThickness);
            _line2 = CreateBasicLine(lineThickness);

            if (Horizontal)
            {
                // Vertical: two lines side by side
                _line1.HorizontalAlignment = HorizontalAlignment.Left;
                _line2.HorizontalAlignment = HorizontalAlignment.Right;
                _rootGrid!.MinWidth = lineThickness * 2 + gap;
            }
            else
            {
                // Horizontal: two lines stacked
                _line1.VerticalAlignment = VerticalAlignment.Top;
                _line2.VerticalAlignment = VerticalAlignment.Bottom;
                _rootGrid!.MinHeight = lineThickness * 2 + gap;
            }

            _rootGrid.Children.Add(_line1);
            _rootGrid.Children.Add(_line2);
        }

        #endregion

        #region Element Factory Helpers

        /// <summary>
        /// Creates a basic Rectangle line element.
        /// </summary>
        private Rectangle CreateBasicLine(double thickness)
        {
            var rect = new Rectangle
            {
                UseLayoutRounding = true,
                Fill = new SolidColorBrush(Colors.Gray) // Will be updated in ApplyColors
            };

            if (Horizontal)
            {
                // Vertical line
                rect.Width = thickness;
                rect.Height = double.NaN;
                rect.HorizontalAlignment = HorizontalAlignment.Center;
                rect.VerticalAlignment = VerticalAlignment.Stretch;
            }
            else
            {
                // Horizontal line
                rect.Height = thickness;
                rect.Width = double.NaN;
                rect.HorizontalAlignment = HorizontalAlignment.Stretch;
                rect.VerticalAlignment = VerticalAlignment.Center;
            }

            return rect;
        }

        /// <summary>
        /// Creates a Path-based line with stroke dash pattern.
        /// </summary>
        private Path CreateStrokedLine(double thickness, DoubleCollection dashArray)
        {
            var path = new Path
            {
                StrokeThickness = thickness,
                StrokeDashArray = dashArray,
                UseLayoutRounding = true,
                Stretch = Stretch.Fill
            };

            // Create a simple line geometry
            if (Horizontal)
            {
                path.Data = new LineGeometry
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 100)
                };
                path.HorizontalAlignment = HorizontalAlignment.Center;
                path.VerticalAlignment = VerticalAlignment.Stretch;
            }
            else
            {
                path.Data = new LineGeometry
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(100, 0)
                };
                path.HorizontalAlignment = HorizontalAlignment.Stretch;
                path.VerticalAlignment = VerticalAlignment.Center;
            }

            return path;
        }

        /// <summary>
        /// Creates the ornament shape based on the Ornament property.
        /// </summary>
        private FrameworkElement CreateOrnamentShape(double size)
        {
            switch (Ornament)
            {
                case DaisyDividerOrnament.Circle:
                    _ornamentShape = new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Fill = new SolidColorBrush(Colors.Gray)
                    };
                    return _ornamentShape;

                case DaisyDividerOrnament.Star:
                    return CreateStarPath(size);

                case DaisyDividerOrnament.Square:
                    return new Rectangle
                    {
                        Width = size,
                        Height = size,
                        Fill = new SolidColorBrush(Colors.Gray)
                    };

                case DaisyDividerOrnament.Diamond:
                default:
                    // Diamond is a rotated square
                    var diamond = new Rectangle
                    {
                        Width = size * 0.7,
                        Height = size * 0.7,
                        Fill = new SolidColorBrush(Colors.Gray),
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = new RotateTransform { Angle = 45 }
                    };
                    return diamond;
            }
        }

        /// <summary>
        /// Creates a simple 4-point star path.
        /// </summary>
        private static Path CreateStarPath(double size)
        {
            double r = size / 2;
            double innerR = r * 0.4;
            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                IsClosed = true,
                IsFilled = true,
                StartPoint = new Point(r, 0) // Top point
            };

            // 4-point star
            double[] angles = [0, 45, 90, 135, 180, 225, 270, 315];
            foreach (var angle in angles)
            {
                double rad = angle * Math.PI / 180;
                double radius = (angle % 90 == 0) ? r : innerR;
                double x = r + radius * Math.Sin(rad);
                double y = r - radius * Math.Cos(rad);
                figure.Segments.Add(new LineSegment { Point = new Point(x, y) });
            }

            geometry.Figures.Add(figure);

            return new Path
            {
                Data = geometry,
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Colors.Gray),
                Stretch = Stretch.Uniform
            };
        }

        /// <summary>
        /// Builds the text container (shared by all styles).
        /// </summary>
        private void BuildTextContainer(double textPadding)
        {
            _textContainer = new Border
            {
                Visibility = Visibility.Collapsed
            };

            _textBlock = new TextBlock
            {
                FontSize = GetFontSize(),
                Opacity = 0.8,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _textContainer.Child = _textBlock;

            // Position based on orientation and placement
            if (Horizontal)
            {
                _textContainer.HorizontalAlignment = HorizontalAlignment.Center;
                _textContainer.Padding = new Thickness(0, textPadding, 0, textPadding);
                _textContainer.VerticalAlignment = Placement switch
                {
                    DaisyDividerPlacement.Start => VerticalAlignment.Top,
                    DaisyDividerPlacement.End => VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Center
                };
            }
            else
            {
                _textContainer.VerticalAlignment = VerticalAlignment.Center;
                _textContainer.Padding = new Thickness(textPadding, 0, textPadding, 0);
                _textContainer.HorizontalAlignment = Placement switch
                {
                    DaisyDividerPlacement.Start => HorizontalAlignment.Left,
                    DaisyDividerPlacement.End => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Center
                };
            }

            _rootGrid!.Children.Add(_textContainer);
        }

        #endregion

        #region Apply Methods

        private void UpdateTextVisibility()
        {
            if (_textBlock == null || _textContainer == null) return;

            var text = DividerText;
            if (!string.IsNullOrEmpty(text))
            {
                _textBlock.Text = text;
                _textContainer.Visibility = Visibility.Visible;
            }
            else
            {
                _textContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplySizing()
        {
            // Sizing is applied during RebuildVisualTree, trigger a rebuild
            RebuildVisualTree();
        }

        private void ApplyColors()
        {
            var brush = GetLineBrush();
            var opacity = GetLineOpacity();

            // Apply to line elements
            if (_line1 != null)
            {
                if (DividerStyle == DaisyDividerStyle.Gradient)
                {
                    _line1.Fill = CreateCenterFadeGradient(brush);
                }
                else if (DividerStyle != DaisyDividerStyle.Inset) // Inset uses shadow/highlight
                {
                    _line1.Fill = brush;
                }
                _line1.Opacity = opacity;
            }

            if (_line2 != null && DividerStyle != DaisyDividerStyle.Inset)
            {
                _line2.Fill = brush;
                _line2.Opacity = opacity;
            }

            // Apply to path elements
            if (_linePath != null)
            {
                if (DividerStyle == DaisyDividerStyle.Tapered)
                {
                    _linePath.Fill = brush;
                }
                else
                {
                    _linePath.Stroke = brush;
                }
                _linePath.Opacity = opacity;
            }

            // Apply to glow elements
            if (_glowLine != null)
            {
                _glowLine.Fill = brush;
            }

            // Apply to ornament
            ApplyColorToOrnament(brush, opacity);

            // Text styling
            if (_textBlock != null)
            {
                _textBlock.Foreground = GetTextForeground();
            }
            if (_textContainer != null)
            {
                _textContainer.Background = GetTextBackgroundBrush();
            }
        }

        private void ApplyColorToOrnament(Brush brush, double opacity)
        {
            if (_ornamentShape != null)
            {
                _ornamentShape.Fill = brush;
                _ornamentShape.Opacity = opacity;
            }

            // Also check for Rectangle/Path ornaments in the grid
            if (_rootGrid == null) return;

            foreach (var child in _rootGrid.Children)
            {
                if (child == _line1 || child == _line2 || child == _textContainer) continue;

                if (child is Rectangle rect && child != _glowLine)
                {
                    rect.Fill = brush;
                    rect.Opacity = opacity;
                }
                else if (child is Path path && path != _linePath)
                {
                    path.Fill = brush;
                    path.Opacity = opacity;
                }
            }
        }

        #endregion
    }
}
