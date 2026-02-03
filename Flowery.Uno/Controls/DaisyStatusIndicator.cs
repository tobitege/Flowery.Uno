namespace Flowery.Controls
{
    /// <summary>
    /// Animation variant styles for the status indicator.
    /// </summary>
    public enum DaisyStatusIndicatorVariant
    {
        /// <summary>Static dot with no animation (default)</summary>
        Default,
        /// <summary>Ping animation - expanding ring that fades out</summary>
        Ping,
        /// <summary>Bounce animation - dot bounces up and down</summary>
        Bounce,
        /// <summary>Pulse animation - breathing/pulsing opacity effect</summary>
        Pulse,
        /// <summary>Blink animation - simple on/off blinking</summary>
        Blink,
        /// <summary>Ripple animation - multiple expanding rings</summary>
        Ripple,
        /// <summary>Heartbeat animation - double-pulse like a heartbeat</summary>
        Heartbeat,
        /// <summary>Spin animation - rotating dot indicator</summary>
        Spin,
        /// <summary>Wave animation - wave-like scale effect</summary>
        Wave,
        /// <summary>Glow animation - glowing halo effect</summary>
        Glow,
        /// <summary>Morph animation - shape morphing effect</summary>
        Morph,
        /// <summary>Orbit animation - small dot orbiting around</summary>
        Orbit,
        /// <summary>Radar animation - radar sweep effect</summary>
        Radar,
        /// <summary>Sonar animation - sonar ping effect</summary>
        Sonar,
        /// <summary>Beacon animation - lighthouse beacon sweep</summary>
        Beacon,
        /// <summary>Shake animation - horizontal shake effect</summary>
        Shake,
        /// <summary>Wobble animation - wobbling rotation effect</summary>
        Wobble,
        /// <summary>Pop animation - pop in/out scale effect</summary>
        Pop,
        /// <summary>Flicker animation - random flickering effect</summary>
        Flicker,
        /// <summary>Breathe animation - slow breathing scale</summary>
        Breathe,
        /// <summary>Ring animation - expanding ring outline</summary>
        Ring,
        /// <summary>Flash animation - quick flash effect</summary>
        Flash,
        /// <summary>Swing animation - pendulum swing effect</summary>
        Swing,
        /// <summary>Jiggle animation - jiggling effect</summary>
        Jiggle,
        /// <summary>Throb animation - throbbing intensity effect</summary>
        Throb,
        /// <summary>Twinkle animation - star-like twinkling</summary>
        Twinkle,
        /// <summary>Splash animation - splash ripple effect</summary>
        Splash,
        /// <summary>Status: battery level indicator</summary>
        Battery,
        /// <summary>Status: traffic light (vertical)</summary>
        TrafficLightVertical,
        /// <summary>Status: traffic light (horizontal)</summary>
        TrafficLightHorizontal,
        /// <summary>Status: WiFi/WLAN signal strength indicator (3 bars)</summary>
        WifiSignal,
        /// <summary>Status: Cellular/5G signal strength indicator (5 bars)</summary>
        CellularSignal
    }

    /// <summary>
    /// A status indicator control that displays a small colored dot to represent status.
    /// Uses Composition API for hardware-accelerated animations.
    /// </summary>
    public partial class DaisyStatusIndicator : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Compositor? _compositor;
        private Visual? _dotVisual;
        private UIElement? _dotElement;
        private bool _isAnimating;
        private readonly List<Visual> _activeVisuals = [];
        private readonly List<Storyboard> _activeStoryboards = [];

        // Track animated elements for restart support
        private readonly List<RingAnimationData> _ringAnimations = [];
        private UIElement? _animatedContainer; // Single container for glow/orbit/spin/radar/beacon
        private double _cachedSize;

        private record RingAnimationData(UIElement Element, double Size, float StartOpacity, float EndOpacity, float EndScale, int DurationMs, int DelayMs = 0);

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(DaisyStatusIndicatorVariant), typeof(DaisyStatusIndicator),
                new PropertyMetadata(DaisyStatusIndicatorVariant.Default, OnAppearanceChanged));

        public DaisyStatusIndicatorVariant Variant
        {
            get => (DaisyStatusIndicatorVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(DaisySize), typeof(DaisyStatusIndicator),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(DaisyColor), typeof(DaisyStatusIndicator),
                new PropertyMetadata(DaisyColor.Neutral, OnAppearanceChanged));

        public DaisyColor Color
        {
            get => (DaisyColor)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty BatteryChargePercentProperty =
            DependencyProperty.Register(nameof(BatteryChargePercent), typeof(double), typeof(DaisyStatusIndicator),
                new PropertyMetadata(100d, OnAppearanceChanged));

        public double BatteryChargePercent
        {
            get => (double)GetValue(BatteryChargePercentProperty);
            set => SetValue(BatteryChargePercentProperty, value);
        }

        public static readonly DependencyProperty TrafficLightActiveProperty =
            DependencyProperty.Register(nameof(TrafficLightActive), typeof(DaisyTrafficLightState), typeof(DaisyStatusIndicator),
                new PropertyMetadata(DaisyTrafficLightState.Green, OnAppearanceChanged));

        public DaisyTrafficLightState TrafficLightActive
        {
            get => (DaisyTrafficLightState)GetValue(TrafficLightActiveProperty);
            set => SetValue(TrafficLightActiveProperty, value);
        }

        public static readonly DependencyProperty SignalStrengthProperty =
            DependencyProperty.Register(nameof(SignalStrength), typeof(int), typeof(DaisyStatusIndicator),
                new PropertyMetadata(3, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the signal strength for WifiSignal (1-3) or CellularSignal (1-5) variants.
        /// Values are clamped to valid ranges when rendered.
        /// </summary>
        public int SignalStrength
        {
            get => (int)GetValue(SignalStrengthProperty);
            set => SetValue(SignalStrengthProperty, value);
        }

        #endregion

        public DaisyStatusIndicator()
        {
            DefaultStyleKey = typeof(DaisyStatusIndicator);
            IsTabStop = false;
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyStatusIndicator indicator)
            {
                indicator.RebuildVisual();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                // Restart animations on existing visual tree without rebuilding
                RestartAnimations();
                return;
            }

            RebuildVisual();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            StopAnimation();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            RebuildVisual();
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

        private void RebuildVisual()
        {
            StopAnimation();

            // Clear animation tracking for fresh start
            _ringAnimations.Clear();
            _animatedContainer = null;

            var size = DaisyResourceLookup.GetSizeValue(Size);
            _cachedSize = size;
            var brush = GetColorBrush();
            var isStatusGlyph = IsStatusGlyphVariant();
            var usesEffectPadding = UsesEffectPadding();
            var padding = usesEffectPadding
                ? DaisyResourceLookup.GetStatusIndicatorEffectPadding(Size)
                : DaisyResourceLookup.GetStatusIndicatorPadding(Size);
            var containerSize = isStatusGlyph ? size : size + (padding * 2);

            _rootGrid = new Grid
            {
                Width = containerSize,
                Height = containerSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (isStatusGlyph)
            {
                _isAnimating = true;
                switch (Variant)
                {
                    case DaisyStatusIndicatorVariant.Battery:
                        BuildBatteryStatusVisual(size, brush);
                        break;
                    case DaisyStatusIndicatorVariant.TrafficLightVertical:
                        BuildTrafficLightStatusVisual(size, rotationAngle: 0);
                        break;
                    case DaisyStatusIndicatorVariant.TrafficLightHorizontal:
                        BuildTrafficLightStatusVisual(size, rotationAngle: 90);
                        break;
                    case DaisyStatusIndicatorVariant.WifiSignal:
                        BuildWifiSignalVisual(size, brush);
                        break;
                    case DaisyStatusIndicatorVariant.CellularSignal:
                        BuildCellularSignalVisual(size, brush);
                        break;
                }

                Content = _rootGrid;
                return;
            }

            // Main dot
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _rootGrid.Children.Add(dot);
            _dotElement = dot;

            _isAnimating = true;

            // Build variant-specific overlay visuals
            BuildVariantOverlay(dot, size, brush);

            // Set up animation for dot variants
            dot.Loaded += (s, e) =>
            {
                // In Uno/WinUI, animating Visual.Offset for layout-driven elements can be overwritten by layout.
                // Use Translation for motion-based variants.
                PlatformCompatibility.TrySetIsTranslationEnabled(dot, true);
                _dotVisual = ElementCompositionPreview.GetElementVisual(dot);
                TrackVisual(_dotVisual);
                _compositor ??= _dotVisual.Compositor;
                _dotVisual.CenterPoint = new Vector3((float)(size / 2), (float)(size / 2), 0);
                StartDotAnimation();
            };

            Content = _rootGrid;
        }

        private bool IsStatusGlyphVariant() =>
            Variant == DaisyStatusIndicatorVariant.Battery ||
            Variant == DaisyStatusIndicatorVariant.TrafficLightVertical ||
            Variant == DaisyStatusIndicatorVariant.TrafficLightHorizontal ||
            Variant == DaisyStatusIndicatorVariant.WifiSignal ||
            Variant == DaisyStatusIndicatorVariant.CellularSignal;

        private bool UsesEffectPadding() =>
            Variant == DaisyStatusIndicatorVariant.Ping ||
            Variant == DaisyStatusIndicatorVariant.Ripple ||
            Variant == DaisyStatusIndicatorVariant.Sonar ||
            Variant == DaisyStatusIndicatorVariant.Splash ||
            Variant == DaisyStatusIndicatorVariant.Glow ||
            Variant == DaisyStatusIndicatorVariant.Ring ||
            Variant == DaisyStatusIndicatorVariant.Radar ||
            Variant == DaisyStatusIndicatorVariant.Beacon ||
            Variant == DaisyStatusIndicatorVariant.Orbit ||
            Variant == DaisyStatusIndicatorVariant.Spin ||
            Variant == DaisyStatusIndicatorVariant.Bounce;
    }
}
