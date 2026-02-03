namespace Flowery.Controls
{
    /// <summary>
    /// Loading animation variant styles.
    /// </summary>
    public enum DaisyLoadingVariant
    {
        /// <summary>Spinner animation (default) - rotating arc</summary>
        Spinner,
        /// <summary>Dots animation - three bouncing dots</summary>
        Dots,
        /// <summary>Ring animation - rotating ring</summary>
        Ring,
        /// <summary>Pulse animation - breathing/pulsing effect</summary>
        Pulse,
        /// <summary>Ball animation - single bouncing ball</summary>
        Ball,
        /// <summary>Bars animation - three vertical bars</summary>
        Bars,
        /// <summary>Infinity animation - infinity loop</summary>
        Infinity,
        /// <summary>Orbit animation - dots orbiting around a square</summary>
        Orbit,
        /// <summary>Snake animation - segments moving back and forth</summary>
        Snake,
        /// <summary>Wave animation - dots moving like a wave</summary>
        Wave,
        /// <summary>Bounce animation - 2x2 squares highlighting sequence</summary>
        Bounce,
        /// <summary>Matrix animation - dot matrix wave</summary>
        Matrix,
        /// <summary>Matrix inward animation - wave from inner to outer</summary>
        MatrixInward,
        /// <summary>Matrix outward animation - wave from outer to inner</summary>
        MatrixOutward,
        /// <summary>Matrix vertical animation - wave from top row to bottom row</summary>
        MatrixVertical,
        /// <summary>Matrix rain animation - falling dots in columns</summary>
        MatrixRain,
        /// <summary>Hourglass animation - hourglass sand flow</summary>
        Hourglass,
        /// <summary>Signal sweep animation - scanning bar</summary>
        SignalSweep,
        /// <summary>Bit flip animation - binary on/off dots</summary>
        BitFlip,
        /// <summary>Packet burst animation - center pulse with burst dots</summary>
        PacketBurst,
        /// <summary>Comet trail animation - dot with trailing dots in a circle</summary>
        CometTrail,
        /// <summary>Heartbeat animation - EKG pulse line</summary>
        Heartbeat,
        /// <summary>Tunnel zoom animation - expanding rings</summary>
        TunnelZoom,
        /// <summary>Glitch reveal animation - flashing columns</summary>
        GlitchReveal,
        /// <summary>Ripple matrix animation - 3x3 ripple wave</summary>
        RippleMatrix,
        /// <summary>Cursor blink animation - terminal cursor</summary>
        CursorBlink,
        /// <summary>Countdown spinner animation - 12 dots in clock arrangement</summary>
        CountdownSpinner,
        /// <summary>Business: document page flip on (sheet enters stack)</summary>
        DocumentFlipOn,
        /// <summary>Business: document page flip off (sheet exits stack)</summary>
        DocumentFlipOff,
        /// <summary>Business: mail sending animation</summary>
        MailSend,
        /// <summary>Business: cloud upload animation</summary>
        CloudUpload,
        /// <summary>Business: cloud download animation</summary>
        CloudDownload,
        /// <summary>Business: invoice stamped paid</summary>
        InvoiceStamp,
        /// <summary>Business: chart pulse / activity</summary>
        ChartPulse,
        /// <summary>Business: calendar tick</summary>
        CalendarTick,
        /// <summary>Business: approval flow / workflow nodes</summary>
        ApprovalFlow,
        /// <summary>Business: briefcase bounce/spin</summary>
        BriefcaseSpin,
        /// <summary>Business: battery charging fill</summary>
        BatteryCharging,
        /// <summary>Business: battery emptying fill</summary>
        BatteryEmptying,
        /// <summary>Business: traffic light (up)</summary>
        TrafficLightUp,
        /// <summary>Business: traffic light (right)</summary>
        TrafficLightRight,
        /// <summary>Business: traffic light (down)</summary>
        TrafficLightDown,
        /// <summary>Business: traffic light (left)</summary>
        TrafficLightLeft,
        /// <summary>Retro Win95: two folders with papers flying between them</summary>
        Win95FileCopy,
        /// <summary>Retro Win95: papers flying into recycle bin</summary>
        Win95Delete,
        /// <summary>Retro Win95: flashlight/magnifying glass searching files</summary>
        Win95Search,
        /// <summary>Retro Win95: papers flying out of recycle bin</summary>
        Win95EmptyRecycle
    }

    /// <summary>
    /// A Loading control styled after DaisyUI's Loading component.
    /// Shows an animation to indicate that something is loading.
    /// Uses Composition API for hardware-accelerated animations.
    /// </summary>
    public partial class DaisyLoading : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Compositor? _compositor;
        private Visual? _spinnerVisual;
        private UIElement? _spinnerElement;
        private bool _isAnimating;
        private readonly List<Visual> _activeVisuals = [];
        private readonly List<Visual> _activeTranslationVisuals = [];
        private readonly List<Storyboard> _activeStoryboards = [];
        private readonly List<DispatcherTimer> _activeTimers = [];

        private enum RippleGroup
        {
            Center,
            Ring1,
            Ring2
        }

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(DaisyLoadingVariant), typeof(DaisyLoading),
                new PropertyMetadata(DaisyLoadingVariant.Spinner, OnAppearanceChanged));

        public DaisyLoadingVariant Variant
        {
            get => (DaisyLoadingVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(DaisySize), typeof(DaisyLoading),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(nameof(Color), typeof(DaisyColor), typeof(DaisyLoading),
                new PropertyMetadata(DaisyColor.Default, OnAppearanceChanged));

        public DaisyColor Color
        {
            get => (DaisyColor)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty PixelSizeProperty =
            DependencyProperty.Register(nameof(PixelSize), typeof(double), typeof(DaisyLoading),
                new PropertyMetadata(double.NaN, OnAppearanceChanged));

        public double PixelSize
        {
            get => (double)GetValue(PixelSizeProperty);
            set => SetValue(PixelSizeProperty, value);
        }

        #endregion

        public DaisyLoading()
        {
            DefaultStyleKey = typeof(DaisyLoading);
            IsTabStop = false;
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyLoading loading)
            {
                loading.RebuildVisual();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
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
            _isAnimating = true;

            var pixelSize = PixelSize;
            var size = !double.IsNaN(pixelSize) && pixelSize > 0
                ? pixelSize
                : DaisyResourceLookup.GetSizeValue(Size);

            _rootGrid = new Grid
            {
                Width = size,
                Height = size
            };

            switch (Variant)
            {
                case DaisyLoadingVariant.Spinner:
                    BuildSpinnerVisual(size);
                    break;
                case DaisyLoadingVariant.Ring:
                    BuildRingVisual(size);
                    break;
                case DaisyLoadingVariant.Dots:
                    BuildDotsVisual(size);
                    break;
                case DaisyLoadingVariant.Pulse:
                    BuildPulseVisual(size);
                    break;
                case DaisyLoadingVariant.Ball:
                    BuildBallVisual(size);
                    break;
                case DaisyLoadingVariant.Bars:
                    BuildBarsVisual(size);
                    break;
                case DaisyLoadingVariant.Infinity:
                    BuildInfinityVisual(size);
                    break;
                case DaisyLoadingVariant.Orbit:
                    BuildOrbitVisual(size);
                    break;
                case DaisyLoadingVariant.Snake:
                    BuildSnakeVisual(size);
                    break;
                case DaisyLoadingVariant.Wave:
                    BuildWaveVisual(size);
                    break;
                case DaisyLoadingVariant.Bounce:
                    BuildBounceVisual(size);
                    break;
                case DaisyLoadingVariant.Matrix:
                    BuildMatrixVisual(size);
                    break;
                case DaisyLoadingVariant.MatrixInward:
                    BuildMatrixInwardVisual(size);
                    break;
                case DaisyLoadingVariant.MatrixOutward:
                    BuildMatrixOutwardVisual(size);
                    break;
                case DaisyLoadingVariant.MatrixVertical:
                    BuildMatrixVerticalVisual(size);
                    break;
                case DaisyLoadingVariant.MatrixRain:
                    BuildMatrixRainVisual(size);
                    break;
                case DaisyLoadingVariant.Hourglass:
                    BuildHourglassVisual(size);
                    break;
                case DaisyLoadingVariant.SignalSweep:
                    BuildSignalSweepVisual(size);
                    break;
                case DaisyLoadingVariant.BitFlip:
                    BuildBitFlipVisual(size);
                    break;
                case DaisyLoadingVariant.PacketBurst:
                    BuildPacketBurstVisual(size);
                    break;
                case DaisyLoadingVariant.CometTrail:
                    BuildCometTrailVisual(size);
                    break;
                case DaisyLoadingVariant.Heartbeat:
                    BuildHeartbeatVisual(size);
                    break;
                case DaisyLoadingVariant.TunnelZoom:
                    BuildTunnelZoomVisual(size);
                    break;
                case DaisyLoadingVariant.GlitchReveal:
                    BuildGlitchRevealVisual(size);
                    break;
                case DaisyLoadingVariant.RippleMatrix:
                    BuildRippleMatrixVisual(size);
                    break;
                case DaisyLoadingVariant.CursorBlink:
                    BuildCursorBlinkVisual(size);
                    break;
                case DaisyLoadingVariant.CountdownSpinner:
                    BuildCountdownSpinnerVisual(size);
                    break;
                case DaisyLoadingVariant.DocumentFlipOn:
                    BuildDocumentFlipOnVisual(size);
                    break;
                case DaisyLoadingVariant.DocumentFlipOff:
                    BuildDocumentFlipOffVisual(size);
                    break;
                case DaisyLoadingVariant.MailSend:
                    BuildMailSendVisual(size);
                    break;
                case DaisyLoadingVariant.CloudUpload:
                    BuildCloudUploadVisual(size);
                    break;
                case DaisyLoadingVariant.CloudDownload:
                    BuildCloudDownloadVisual(size);
                    break;
                case DaisyLoadingVariant.InvoiceStamp:
                    BuildInvoiceStampVisual(size);
                    break;
                case DaisyLoadingVariant.ChartPulse:
                    BuildChartPulseVisual(size);
                    break;
                case DaisyLoadingVariant.CalendarTick:
                    BuildCalendarTickVisual(size);
                    break;
                case DaisyLoadingVariant.ApprovalFlow:
                    BuildApprovalFlowVisual(size);
                    break;
                case DaisyLoadingVariant.BriefcaseSpin:
                    BuildBriefcaseSpinVisual(size);
                    break;
                case DaisyLoadingVariant.BatteryCharging:
                    BuildBatteryChargingVisual(size);
                    break;
                case DaisyLoadingVariant.BatteryEmptying:
                    BuildBatteryEmptyingVisual(size);
                    break;
                case DaisyLoadingVariant.TrafficLightUp:
                    BuildTrafficLightUpVisual(size);
                    break;
                case DaisyLoadingVariant.TrafficLightRight:
                    BuildTrafficLightRightVisual(size);
                    break;
                case DaisyLoadingVariant.TrafficLightDown:
                    BuildTrafficLightDownVisual(size);
                    break;
                case DaisyLoadingVariant.TrafficLightLeft:
                    BuildTrafficLightLeftVisual(size);
                    break;
                case DaisyLoadingVariant.Win95FileCopy:
                    BuildWin95FileCopyVisual(size);
                    break;
                case DaisyLoadingVariant.Win95Delete:
                    BuildWin95DeleteVisual(size);
                    break;
                case DaisyLoadingVariant.Win95Search:
                    BuildWin95SearchVisual(size);
                    break;
                case DaisyLoadingVariant.Win95EmptyRecycle:
                    BuildWin95EmptyRecycleVisual(size);
                    break;
                default:
                    BuildSpinnerVisual(size);
                    break;
            }

            Content = _rootGrid;
        }

        private void StartAnimation()
        {
            _isAnimating = true;
        }

        private void StopAnimation()
        {
            _isAnimating = false;
            PlatformCompatibility.StopRotationAnimation(_spinnerElement, _spinnerVisual, null);
            _spinnerVisual = null;
            _spinnerElement = null;

            foreach (var visual in _activeVisuals)
            {
                visual.StopAnimation("Opacity");
                visual.StopAnimation("Scale");
                visual.StopAnimation("Offset");
                visual.StopAnimation("RotationAngle");
            }

            foreach (var visual in _activeTranslationVisuals)
            {
                try
                {
                    visual.StopAnimation("Translation");
                }
                catch
                {
                }
            }

            _activeVisuals.Clear();
            _activeTranslationVisuals.Clear();

            foreach (var storyboard in _activeStoryboards)
            {
                storyboard.Stop();
            }
            _activeStoryboards.Clear();

            foreach (var timer in _activeTimers)
            {
                timer.Stop();
            }
            _activeTimers.Clear();
        }

        private void TrackVisual(Visual visual)
        {
            _activeVisuals.Add(visual);
        }

        private void TrackTranslationVisual(Visual visual)
        {
            _activeTranslationVisuals.Add(visual);
        }

        private void TrackStoryboard(Storyboard? storyboard)
        {
            if (storyboard != null)
            {
                _activeStoryboards.Add(storyboard);
            }
        }

        private void TrackTimer(DispatcherTimer timer)
        {
            _activeTimers.Add(timer);
        }

        private Brush GetColorBrush()
        {
            var resourceKey = Color switch
            {
                DaisyColor.Primary => "DaisyPrimaryBrush",
                DaisyColor.Secondary => "DaisySecondaryBrush",
                DaisyColor.Accent => "DaisyAccentBrush",
                DaisyColor.Neutral => "DaisyNeutralBrush",
                DaisyColor.Info => "DaisyInfoBrush",
                DaisyColor.Success => "DaisySuccessBrush",
                DaisyColor.Warning => "DaisyWarningBrush",
                DaisyColor.Error => "DaisyErrorBrush",
                _ => "DaisyBaseContentBrush"
            };

            if (Application.Current.Resources.TryGetValue(resourceKey, out var brush) && brush is Brush b)
            {
                return b;
            }

            // Fallback
            return new SolidColorBrush(Colors.White);
        }
    }
}
