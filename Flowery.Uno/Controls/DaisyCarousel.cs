using System;
using System.Collections.Generic;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Flowery.Services;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A carousel/slider control styled after DaisyUI's Carousel component.
    /// Supports prev/next navigation with animated transitions.
    /// </summary>
    public partial class DaisyCarousel : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _contentContainer;
        private RectangleGeometry? _contentClip;
        private Grid? _slideContainer; // Holds slides for transitions (replaces ContentPresenter)
        private Border? _currentSlideWrapper; // Wrapper for current slide
        private Border? _previousSlideWrapper; // Wrapper for previous slide during transition
        private DaisyButton? _previousButton;
        private DaisyButton? _nextButton;
        private readonly List<UIElement> _items = [];
        private int _selectedIndex;
        private int _previousIndex = -1; // Track previous index for event
        private FlowerySlideEffect _lastAppliedEffect = FlowerySlideEffect.None; // Track resolved effect
        private FlowerySlideTransition _lastAppliedTransition = FlowerySlideTransition.None; // Track resolved transition
        private bool _isTransitioning; // Prevent overlapping transitions
        private bool _updatePending;   // Track if an update was requested during transition

        // Slideshow controller handles auto-advance logic
        private readonly FlowerySlideshowController _slideshowController;
        // For Kiosk mode random effect selection
        private static readonly Random _effectRandom = new();
        private static readonly FlowerySlideEffect[] _kioskEffects =
        [
            FlowerySlideEffect.ZoomIn,
            FlowerySlideEffect.ZoomOut,
            FlowerySlideEffect.PanLeft,
            FlowerySlideEffect.PanRight,
            FlowerySlideEffect.PanUp,
            FlowerySlideEffect.PanDown,
            FlowerySlideEffect.PanAndZoom,
            FlowerySlideEffect.Pulse,
            FlowerySlideEffect.Breath,
            FlowerySlideEffect.Throw
        ];

        /// <summary>
        /// Fired when the carousel navigates to a new slide.
        /// </summary>
        public event EventHandler<FlowerySlideChangedEventArgs>? SlideChanged;

        public FlowerySlideEffect LastAppliedEffect => _lastAppliedEffect;
        public FlowerySlideTransition LastAppliedTransition => _lastAppliedTransition;

        public DaisyCarousel()
        {
            DefaultStyleKey = typeof(DaisyCarousel);
            
            // Initialize the slideshow controller with callbacks
            _slideshowController = new FlowerySlideshowController(
                navigateAction: index => SelectedIndex = index,
                getItemCount: () => _items.Count,
                getCurrentIndex: () => _selectedIndex);

            SizeChanged += OnSizeChanged;
        }

        #region Dependency Properties
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyCarousel),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCarousel carousel)
            {
                carousel.UpdateAutomationProperties();
            }
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedIndex),
                typeof(int),
                typeof(DaisyCarousel),
                new PropertyMetadata(0, OnSelectedIndexChanged));

        /// <summary>
        /// Gets or sets the index of the currently displayed item.
        /// </summary>
        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, ValueCoercion.Index(value, _items.Count));
        }

        public static readonly DependencyProperty ShowNavigationProperty =
            DependencyProperty.Register(
                nameof(ShowNavigation),
                typeof(bool),
                typeof(DaisyCarousel),
                new PropertyMetadata(true, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets whether navigation buttons are shown.
        /// </summary>
        public bool ShowNavigation
        {
            get => (bool)GetValue(ShowNavigationProperty);
            set => SetValue(ShowNavigationProperty, value);
        }

        public static readonly DependencyProperty WrapAroundProperty =
            DependencyProperty.Register(
                nameof(WrapAround),
                typeof(bool),
                typeof(DaisyCarousel),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets whether navigation wraps around (last→first, first→last).
        /// When enabled, both navigation buttons are always visible.
        /// </summary>
        public bool WrapAround
        {
            get => (bool)GetValue(WrapAroundProperty);
            set => SetValue(WrapAroundProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyCarousel),
                new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        /// <summary>
        /// Gets or sets the carousel orientation. Affects button placement and swipe direction.
        /// Default is Horizontal (left/right navigation). Vertical uses top/bottom navigation.
        /// </summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty SwipeThresholdProperty =
            DependencyProperty.Register(
                nameof(SwipeThreshold),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(50.0));

        /// <summary>
        /// Gets or sets the minimum swipe distance in pixels to trigger navigation.
        /// Default is 50 pixels.
        /// </summary>
        public double SwipeThreshold
        {
            get => (double)GetValue(SwipeThresholdProperty);
            set => SetValue(SwipeThresholdProperty, Math.Max(10.0, value));
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(FlowerySlideshowMode),
                typeof(DaisyCarousel),
                new PropertyMetadata(FlowerySlideshowMode.Manual, OnModeChanged));

        /// <summary>
        /// Gets or sets the carousel navigation mode.
        /// Manual: User navigates via buttons. Slideshow: Auto-advances sequentially. Random: Auto-advances in random order.
        /// </summary>
        public FlowerySlideshowMode Mode
        {
            get => (FlowerySlideshowMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly DependencyProperty SlideIntervalProperty =
            DependencyProperty.Register(
                nameof(SlideInterval),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(3.0, OnSlideIntervalChanged));

        /// <summary>
        /// Gets or sets the interval in seconds between automatic slide transitions.
        /// Only applies when Mode is Slideshow or Random. Default is 3 seconds.
        /// </summary>
        public double SlideInterval
        {
            get => (double)GetValue(SlideIntervalProperty);
            set => SetValue(SlideIntervalProperty, Math.Max(0.5, value));
        }

        public static readonly DependencyProperty SlideEffectProperty =
            DependencyProperty.Register(
                nameof(SlideEffect),
                typeof(FlowerySlideEffect),
                typeof(DaisyCarousel),
                new PropertyMetadata(FlowerySlideEffect.None, OnSlideEffectChanged));

        /// <summary>
        /// Gets or sets the visual effect applied to slides during display.
        /// Default is None. Effects like PanAndZoom add subtle pan/zoom motion.
        /// </summary>
        public FlowerySlideEffect SlideEffect
        {
            get => (FlowerySlideEffect)GetValue(SlideEffectProperty);
            set => SetValue(SlideEffectProperty, value);
        }

        public static readonly DependencyProperty ZoomIntensityProperty =
            DependencyProperty.Register(
                nameof(ZoomIntensity),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(0.15, OnSlideEffectChanged));

        /// <summary>
        /// Gets or sets the zoom intensity for ZoomIn/ZoomOut effects (0.0 to 0.5).
        /// A value of 0.15 means zoom to 115%. Default is 0.15.
        /// </summary>
        public double ZoomIntensity
        {
            get => (double)GetValue(ZoomIntensityProperty);
            set => SetValue(ZoomIntensityProperty, Math.Clamp(value, 0.0, 0.5));
        }

        public static readonly DependencyProperty PanDistanceProperty =
            DependencyProperty.Register(
                nameof(PanDistance),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(50.0, OnSlideEffectChanged));

        /// <summary>
        /// Gets or sets the pan distance in pixels for Pan effects (0 to 200).
        /// Default is 50 pixels.
        /// </summary>
        public double PanDistance
        {
            get => (double)GetValue(PanDistanceProperty);
            set => SetValue(PanDistanceProperty, Math.Clamp(value, 0.0, 200.0));
        }

        public static readonly DependencyProperty PulseIntensityProperty =
            DependencyProperty.Register(
                nameof(PulseIntensity),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(0.08, OnSlideEffectChanged));

        /// <summary>
        /// Gets or sets the pulse intensity for the Pulse effect (0.0 to 0.2).
        /// A value of 0.08 means pulse to 108% and back. Default is 0.08.
        /// </summary>
        public double PulseIntensity
        {
            get => (double)GetValue(PulseIntensityProperty);
            set => SetValue(PulseIntensityProperty, Math.Clamp(value, 0.0, 0.2));
        }

        public static readonly DependencyProperty PanAndZoomLockZoomProperty =
            DependencyProperty.Register(
                nameof(PanAndZoomLockZoom),
                typeof(bool),
                typeof(DaisyCarousel),
                new PropertyMetadata(false, OnSlideEffectChanged));

        /// <summary>
        /// Gets or sets whether Pan And Zoom zoom is locked (pan only). Default is false.
        /// </summary>
        public bool PanAndZoomLockZoom
        {
            get => (bool)GetValue(PanAndZoomLockZoomProperty);
            set => SetValue(PanAndZoomLockZoomProperty, value);
        }

        public static readonly DependencyProperty PanAndZoomPanSpeedProperty =
            DependencyProperty.Register(
                nameof(PanAndZoomPanSpeed),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(4.0, OnSlideEffectChanged));

        /// <summary>
        /// Gets or sets the Pan And Zoom pan speed in pixels per second (0 to 10). Default is 4.
        /// </summary>
        public double PanAndZoomPanSpeed
        {
            get => (double)GetValue(PanAndZoomPanSpeedProperty);
            set => SetValue(PanAndZoomPanSpeedProperty, Math.Clamp(value, 0.0, 10.0));
        }

        public static readonly DependencyProperty PanAndZoomCycleSecondsProperty =
            DependencyProperty.Register(
                nameof(PanAndZoomCycleSeconds),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(6.0, OnSlideEffectChanged));

        /// <summary>
        /// Gets or sets the Pan And Zoom cycle duration in seconds. Default is 6.
        /// </summary>
        public double PanAndZoomCycleSeconds
        {
            get => (double)GetValue(PanAndZoomCycleSecondsProperty);
            set => SetValue(PanAndZoomCycleSecondsProperty, Math.Max(0.5, value));
        }

        public static readonly DependencyProperty SlideTransitionProperty =
            DependencyProperty.Register(
                nameof(SlideTransition),
                typeof(FlowerySlideTransition),
                typeof(DaisyCarousel),
                new PropertyMetadata(FlowerySlideTransition.None, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the transition animation used when navigating between slides.
        /// Default is None (instant snap). Transitions like Fade, Slide, Push provide visual movement.
        /// </summary>
        public FlowerySlideTransition SlideTransition
        {
            get => (FlowerySlideTransition)GetValue(SlideTransitionProperty);
            set => SetValue(SlideTransitionProperty, value);
        }

        public static readonly DependencyProperty TransitionDurationProperty =
            DependencyProperty.Register(
                nameof(TransitionDuration),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(0.4, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the duration of slide transitions in seconds.
        /// Default is 0.4 seconds (400ms). Range: 0.1 to 2.0 seconds.
        /// </summary>
        public double TransitionDuration
        {
            get => (double)GetValue(TransitionDurationProperty);
            set => SetValue(TransitionDurationProperty, Math.Clamp(value, 0.1, 2.0));
        }

        public static readonly DependencyProperty TransitionSliceCountProperty =
            DependencyProperty.Register(
                nameof(TransitionSliceCount),
                typeof(int),
                typeof(DaisyCarousel),
                new PropertyMetadata(8, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the number of slices for Slices/Blinds transitions. Default 8.
        /// </summary>
        public int TransitionSliceCount
        {
            get => (int)GetValue(TransitionSliceCountProperty);
            set => SetValue(TransitionSliceCountProperty, Math.Clamp(value, 2, 50));
        }

        public static readonly DependencyProperty TransitionStaggerSlicesProperty =
            DependencyProperty.Register(
                nameof(TransitionStaggerSlices),
                typeof(bool),
                typeof(DaisyCarousel),
                new PropertyMetadata(true, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets whether slice animations should be staggered. Default true.
        /// </summary>
        public bool TransitionStaggerSlices
        {
            get => (bool)GetValue(TransitionStaggerSlicesProperty);
            set => SetValue(TransitionStaggerSlicesProperty, value);
        }

        public static readonly DependencyProperty TransitionSliceStaggerMsProperty =
            DependencyProperty.Register(
                nameof(TransitionSliceStaggerMs),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(50.0, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the stagger delay between slices in milliseconds. Default 50ms.
        /// </summary>
        public double TransitionSliceStaggerMs
        {
            get => (double)GetValue(TransitionSliceStaggerMsProperty);
            set => SetValue(TransitionSliceStaggerMsProperty, Math.Clamp(value, 10.0, 200.0));
        }

        public static readonly DependencyProperty TransitionCheckerboardSizeProperty =
            DependencyProperty.Register(
                nameof(TransitionCheckerboardSize),
                typeof(int),
                typeof(DaisyCarousel),
                new PropertyMetadata(6, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the grid size for Checkerboard transition. Default 6.
        /// </summary>
        public int TransitionCheckerboardSize
        {
            get => (int)GetValue(TransitionCheckerboardSizeProperty);
            set => SetValue(TransitionCheckerboardSizeProperty, Math.Clamp(value, 2, 20));
        }

        public static readonly DependencyProperty TransitionBlurRadiusProperty =
            DependencyProperty.Register(
                nameof(TransitionBlurRadius),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(20.0, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the maximum blur radius for blur transitions. Default 20.
        /// </summary>
        public double TransitionBlurRadius
        {
            get => (double)GetValue(TransitionBlurRadiusProperty);
            set => SetValue(TransitionBlurRadiusProperty, Math.Clamp(value, 5.0, 100.0));
        }

        public static readonly DependencyProperty TransitionPixelateSizeProperty =
            DependencyProperty.Register(
                nameof(TransitionPixelateSize),
                typeof(int),
                typeof(DaisyCarousel),
                new PropertyMetadata(20, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the pixel block size for Pixelate transition. Default 20.
        /// </summary>
        public int TransitionPixelateSize
        {
            get => (int)GetValue(TransitionPixelateSizeProperty);
            set => SetValue(TransitionPixelateSizeProperty, Math.Clamp(value, 4, 100));
        }

        public static readonly DependencyProperty TransitionDissolveDensityProperty =
            DependencyProperty.Register(
                nameof(TransitionDissolveDensity),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(0.5, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the noise density for Dissolve transition. Default 0.5.
        /// </summary>
        public double TransitionDissolveDensity
        {
            get => (double)GetValue(TransitionDissolveDensityProperty);
            set => SetValue(TransitionDissolveDensityProperty, Math.Clamp(value, 0.1, 1.0));
        }

        public static readonly DependencyProperty TransitionFlipAngleProperty =
            DependencyProperty.Register(
                nameof(TransitionFlipAngle),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(90.0, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the rotation angle for flip transitions in degrees. Default 90.
        /// </summary>
        public double TransitionFlipAngle
        {
            get => (double)GetValue(TransitionFlipAngleProperty);
            set => SetValue(TransitionFlipAngleProperty, Math.Clamp(value, 0.0, 180.0));
        }

        public static readonly DependencyProperty TransitionPerspectiveDepthProperty =
            DependencyProperty.Register(
                nameof(TransitionPerspectiveDepth),
                typeof(double),
                typeof(DaisyCarousel),
                new PropertyMetadata(1000.0, OnSlideTransitionChanged));

        /// <summary>
        /// Gets or sets the perspective depth for 3D transitions. Default 1000.
        /// </summary>
        public double TransitionPerspectiveDepth
        {
            get => (double)GetValue(TransitionPerspectiveDepthProperty);
            set => SetValue(TransitionPerspectiveDepthProperty, Math.Max(1.0, value));
        }

        /// <summary>
        /// Gets the total number of items in the carousel.
        /// </summary>
        public int ItemCount => _items.Count;

        #endregion

        #region Callbacks

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCarousel carousel)
            {
                carousel.UpdateSelectedItem();
                carousel.UpdateButtonVisibility();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCarousel carousel)
            {
                carousel.ApplyAll();
            }
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCarousel carousel)
            {
                carousel.UpdateAutoAdvanceTimer();
                carousel.UpdateButtonVisibility();
            }
        }

        private static void OnSlideIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCarousel carousel)
            {
                carousel.UpdateAutoAdvanceTimer();
            }
        }

        private static void OnSlideEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCarousel carousel)
            {
                carousel.ApplySlideEffect();
            }
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCarousel carousel)
            {
                carousel.UpdateButtonOrientation();
            }
        }

        private static void OnSlideTransitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCarousel carousel)
            {
                carousel.UpdateAutoAdvanceTimer();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                ApplyAll();
                UpdateAutoAdvanceTimer();
                return;
            }

            BuildVisualTree();
            ApplyAll();
            UpdateAutoAdvanceTimer();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            StopAutoAdvanceTimer();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateContentClip();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Capture user content
            var userContent = Content;
            Content = null;

            _rootGrid = new Grid
            {
                // Enable focus and keyboard navigation
                IsTabStop = true
            };

            // Content container with clipping
            _contentContainer = new Border
            {
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Slide container Grid - holds both old and new slides during transitions
            _slideContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _contentContainer.Child = _slideContainer;

            _rootGrid.Children.Add(_contentContainer);

            // Collect carousel items from user content
            _items.Clear();
            if (userContent is Panel panel)
            {
                while (panel.Children.Count > 0)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);
                    _items.Add(child);
                }
            }
            else if (userContent is UIElement element)
            {
                _items.Add(element);
            }

            // Previous button (placement depends on orientation)
            _previousButton = new DaisyButton
            {
                Margin = new Thickness(8),
                Opacity = 0.8,
                Shape = DaisyButtonShape.Circle,
                IsTabStop = false
            };
            _previousButton.Click += OnPreviousClick;
            _rootGrid.Children.Add(_previousButton);

            // Next button (placement depends on orientation)
            _nextButton = new DaisyButton
            {
                Margin = new Thickness(8),
                Opacity = 0.8,
                Shape = DaisyButtonShape.Circle,
                IsTabStop = false
            };
            _nextButton.Click += OnNextClick;
            _rootGrid.Children.Add(_nextButton);

            // Set up button orientation
            UpdateButtonOrientation();

            // Enable gesture support
            SetupGestureSupport();

            // Enable keyboard navigation
            _rootGrid.KeyDown += OnKeyDown;

            Content = _rootGrid;
            UpdateAutomationProperties();

            UpdateContentClip();

            UpdateSelectedItem();
            UpdateButtonVisibility();
        }

        private void UpdateAutomationProperties()
        {
            if (_rootGrid == null)
            {
                return;
            }

            var name = string.IsNullOrWhiteSpace(AccessibleText) ? "Carousel" : AccessibleText;
            AutomationProperties.SetName(_rootGrid, name);
        }

        private void UpdateContentClip()
        {
            if (_contentContainer == null)
            {
                return;
            }

            double width = _contentContainer.ActualWidth;
            double height = _contentContainer.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                _contentContainer.Clip = null;
                return;
            }

            _contentClip ??= new RectangleGeometry();
            _contentClip.Rect = new Windows.Foundation.Rect(0, 0, width, height);
            _contentContainer.Clip = _contentClip;
        }

        private void UpdateButtonOrientation()
        {
            if (_previousButton == null || _nextButton == null)
                return;

            if (Orientation == Orientation.Horizontal)
            {
                // Left/Right buttons
                _previousButton.HorizontalAlignment = HorizontalAlignment.Left;
                _previousButton.VerticalAlignment = VerticalAlignment.Center;
                _previousButton.Content = FloweryPathHelpers.CreateChevron(true); // Left chevron

                _nextButton.HorizontalAlignment = HorizontalAlignment.Right;
                _nextButton.VerticalAlignment = VerticalAlignment.Center;
                _nextButton.Content = FloweryPathHelpers.CreateChevron(false); // Right chevron
            }
            else
            {
                // Top/Bottom buttons
                _previousButton.HorizontalAlignment = HorizontalAlignment.Center;
                _previousButton.VerticalAlignment = VerticalAlignment.Top;
                _previousButton.Content = FloweryPathHelpers.CreateVerticalChevron(true); // Up chevron

                _nextButton.HorizontalAlignment = HorizontalAlignment.Center;
                _nextButton.VerticalAlignment = VerticalAlignment.Bottom;
                _nextButton.Content = FloweryPathHelpers.CreateVerticalChevron(false); // Down chevron
            }

            ApplyColors();
        }

        private void SetupGestureSupport()
        {
            if (_rootGrid == null)
                return;

            // Enable manipulation for swipe gestures
            _rootGrid.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rootGrid.ManipulationCompleted += OnManipulationCompleted;
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var delta = e.Cumulative.Translation;

            if (Orientation == Orientation.Horizontal)
            {
                // Horizontal swipe
                if (delta.X < -SwipeThreshold)
                    Next();
                else if (delta.X > SwipeThreshold)
                    Previous();
            }
            else
            {
                // Vertical swipe
                if (delta.Y < -SwipeThreshold)
                    Next();
                else if (delta.Y > SwipeThreshold)
                    Previous();
            }
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_items.Count == 0)
                return;

            var handled = true;
            switch (e.Key)
            {
                case VirtualKey.Left when Orientation == Orientation.Horizontal:
                case VirtualKey.Up when Orientation == Orientation.Vertical:
                    Previous();
                    break;

                case VirtualKey.Right when Orientation == Orientation.Horizontal:
                case VirtualKey.Down when Orientation == Orientation.Vertical:
                    Next();
                    break;

                case VirtualKey.Home:
                    SelectedIndex = 0;
                    break;

                case VirtualKey.End:
                    SelectedIndex = _items.Count - 1;
                    break;

                case VirtualKey.PageUp:
                    Previous();
                    break;

                case VirtualKey.PageDown:
                    Next();
                    break;

                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        #endregion

        #region Navigation

        private void OnPreviousClick(object sender, RoutedEventArgs e)
        {
            Previous();
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            Next();
        }

        /// <summary>
        /// Navigate to the previous item.
        /// </summary>
        public void Previous()
        {
            _slideshowController.WrapAround = WrapAround;
            _slideshowController.Previous();
            _slideshowController.Restart();
        }

        /// <summary>
        /// Navigate to the next item.
        /// </summary>
        public void Next()
        {
            _slideshowController.WrapAround = WrapAround;
            _slideshowController.Next();
            _slideshowController.Restart();
        }

        private void UpdateSelectedItem()
        {
            if (_slideContainer == null || _items.Count == 0)
                return;

            var oldSlideElement = _currentSlideWrapper?.Child;

            // Prevent overlapping transitions
            if (_isTransitioning)
            {
                _updatePending = true;
                return;
            }

            _updatePending = false;

            var oldIndex = _previousIndex;
            _selectedIndex = ValueCoercion.Index(SelectedIndex, _items.Count);

            // Same slide? Nothing to do
            if (oldIndex == _selectedIndex && _currentSlideWrapper != null)
                return;

            // Determine transition to use
            var transition = SlideTransition;
            if (Mode == FlowerySlideshowMode.Kiosk)
            {
                transition = FlowerySlideTransition.Random;
            }
            var hasTransition = oldIndex != -1 && transition != FlowerySlideTransition.None && _currentSlideWrapper != null;

            // Stop any effect on the currently visible item before switching
            // Preserve transform if we're transitioning, so the old slide doesn't snap back
            if (ShouldManageSlideEffects())
            {
                if (hasTransition)
                    StopSlideEffectPreservingTransform();
                else
                    StopSlideEffect();
            }

            // Get the new slide element
            var newSlide = _items[_selectedIndex];

            // Create wrapper for the new slide
            var newWrapper = CreateSlideWrapper(newSlide);

            // First load or no transition configured - instant show
            if (!hasTransition)
            {
                _lastAppliedTransition = FlowerySlideTransition.None;
                if (oldSlideElement != null)
                {
                    FloweryTextFillEffects.TryStopEffect(oldSlideElement);
                }

                if (_currentSlideWrapper != null && !ReferenceEquals(_currentSlideWrapper, newWrapper))
                {
                    _currentSlideWrapper.Child = null;
                }

                // Clear any previous content
                _slideContainer.Children.Clear();
                _slideContainer.Children.Add(newWrapper);

                _previousSlideWrapper = null;
                _currentSlideWrapper = newWrapper;
                _previousIndex = _selectedIndex;

                // Apply slide effect to the newly visible item
                ApplySlideEffect();
                ApplyTextFillEffect();

                // Fire slide changed event
                SlideChanged?.Invoke(this, new FlowerySlideChangedEventArgs(oldIndex, _selectedIndex, _lastAppliedEffect, newSlide));
                return;
            }

            // Store the old wrapper for transition
            _previousSlideWrapper = _currentSlideWrapper;

            // Capture reference to old slide content for cleanup
            var oldSlideContent = _previousSlideWrapper?.Child;

            // Add new wrapper to container
            // The transition helper will manage Opacity and ZIndex as needed
            _slideContainer.Children.Insert(0, newWrapper);

            // Mark transitioning
            _isTransitioning = true;
            _previousIndex = _selectedIndex;

            // Apply transition
            var transitionParams = new FlowerySlideTransitionParams
            {
                Duration = TimeSpan.FromSeconds(TransitionDuration),
                SliceCount = TransitionSliceCount,
                StaggerSlices = TransitionStaggerSlices,
                SliceStaggerMs = TransitionSliceStaggerMs,
                CheckerboardSize = TransitionCheckerboardSize,
                PixelateSize = TransitionPixelateSize,
                DissolveDensity = TransitionDissolveDensity,
                FlipAngle = TransitionFlipAngle,
                PerspectiveDepth = TransitionPerspectiveDepth
            };

            _lastAppliedTransition = FlowerySlideTransitionHelpers.ApplyTransition(
                _slideContainer,
                _previousSlideWrapper,
                newWrapper,
                transition,
                transitionParams,
                onComplete: () =>
                {
                    // Cleanup after transition
                    _isTransitioning = false;

                    // Reset the OLD slide's transform now that transition is complete
                    // This ensures the slide is clean when reused
                    if (oldSlideContent != null)
                    {
                        if (ShouldManageSlideEffects())
                        {
                            FlowerySlideEffects.ResetSlideTransform(oldSlideContent);
                        }

                        FloweryTextFillEffects.TryStopEffect(oldSlideContent);
                    }

                    // Remove old wrapper from visual tree
                    if (_previousSlideWrapper != null)
                    {
                        // Detach old slide from wrapper before removing
                        _previousSlideWrapper.Child = null;
                        _slideContainer.Children.Remove(_previousSlideWrapper);
                        _previousSlideWrapper = null;
                    }

                    // Apply slide effect to the newly visible item
                    ApplySlideEffect();
                    ApplyTextFillEffect();

                    // Fire slide changed event (after transition completes)
                    SlideChanged?.Invoke(this, new FlowerySlideChangedEventArgs(oldIndex, _selectedIndex, _lastAppliedEffect, newSlide));

                    // If an update was requested while we were busy, process it now
                    if (_updatePending)
                    {
                        UpdateSelectedItem();
                    }
                });

            // Update current wrapper reference
            _currentSlideWrapper = newWrapper;
        }

        /// <summary>
        /// Creates a wrapper Border for a slide element.
        /// The wrapper allows transforms to be applied without affecting the original element.
        /// </summary>
        private Border CreateSlideWrapper(UIElement slide)
        {
            // Detach slide from any previous parent
            if (slide is FrameworkElement fe && fe.Parent != null)
            {
                switch (fe.Parent)
                {
                    case Border border when border != _contentContainer:
                        if (ReferenceEquals(border.Child, slide))
                        {
                            if (ShouldManageSlideEffects())
                            {
                                FlowerySlideEffects.StopEffect(slide);
                                FlowerySlideEffects.ResetSlideTransform(slide);
                            }

                            return border;
                        }
                        break;
                    case ContentControl cc:
                        if (ReferenceEquals(cc.Content, slide))
                        {
                            cc.Content = null;
                        }
                        break;
                    case ContentPresenter cp:
                        if (ReferenceEquals(cp.Content, slide))
                        {
                            cp.Content = null;
                        }
                        break;
                    case Panel panel when panel.Children.Contains(slide):
                        panel.Children.Remove(slide);
                        break;
                }
            }

            // Stop any lingering slide effects when the carousel owns those effects
            // Avoid resetting user-defined transforms when no slide effect is active.
            if (ShouldManageSlideEffects())
            {
                FlowerySlideEffects.StopEffect(slide);
                FlowerySlideEffects.ResetSlideTransform(slide);
            }

            return new Border
            {
                Child = slide,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };
        }

        private void UpdateButtonVisibility()
        {
            if (_previousButton == null || _nextButton == null)
                return;

            var showNav = ShowNavigation && _items.Count > 1;

            if (WrapAround)
            {
                // Always show both buttons when WrapAround is enabled
                _previousButton.Visibility = showNav ? Visibility.Visible : Visibility.Collapsed;
                _nextButton.Visibility = showNav ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                // Hide previous on first slide
                _previousButton.Visibility = showNav && _selectedIndex > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Hide next on last slide
                _nextButton.Visibility = showNav && _selectedIndex < _items.Count - 1
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        #endregion

        #region Auto-Advance Timer (via FlowerySlideshowController)

        private void UpdateAutoAdvanceTimer()
        {
            if (_slideshowController == null) return;
            _slideshowController.Mode = Mode;
            _slideshowController.Interval = GetAutoAdvanceInterval();
            _slideshowController.Start();
        }

        private void StopAutoAdvanceTimer() => _slideshowController?.Stop();

        #endregion

        private double GetAutoAdvanceInterval()
        {
            var interval = SlideInterval;
            if (ShouldIncludeTransitionInterval())
            {
                interval += TransitionDuration;
            }
            return interval;
        }

        private bool ShouldIncludeTransitionInterval()
        {
            if (Mode == FlowerySlideshowMode.Kiosk)
            {
                return true;
            }

            return SlideTransition != FlowerySlideTransition.None;
        }

        #region Apply Styling

        private void ApplyAll()
        {
            if (_previousButton == null || _nextButton == null)
                return;

            ApplyColors();
        }

        private void ApplyColors()
        {
            if (_previousButton == null || _nextButton == null)
                return;

            // Check for lightweight styling overrides
            var navFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyCarousel", "NavigationForeground");

            var neutralContentBrush = navFgOverride ?? DaisyResourceLookup.GetBrush("DaisyNeutralContentBrush");

            _previousButton.Variant = DaisyButtonVariant.Neutral;
            _nextButton.Variant = DaisyButtonVariant.Neutral;

            // Style the chevron paths
            if (_previousButton.Content is Microsoft.UI.Xaml.Shapes.Path prevPath)
            {
                prevPath.Stroke = neutralContentBrush;
            }
            if (_nextButton.Content is Microsoft.UI.Xaml.Shapes.Path nextPath)
            {
                nextPath.Stroke = neutralContentBrush;
            }
        }

        #endregion

        #region Slide Effects

#if HAS_UNO || WINDOWS
        private void ApplySlideEffect()
        {
            StopSlideEffect();

            // Determine which effect to use
            var effectToApply = SlideEffect;
            
            // In Random/Kiosk mode, pick a random effect from the pool (avoiding repeat)
            if (Mode is FlowerySlideshowMode.Random or FlowerySlideshowMode.Kiosk)
            {
                // Filter out the last applied effect to avoid repetition
                var availableEffects = _kioskEffects.Length > 1
                    ? [.. _kioskEffects.Where(e => e != _lastAppliedEffect)]
                    : _kioskEffects;
                effectToApply = availableEffects[_effectRandom.Next(availableEffects.Length)];
            }

            _lastAppliedEffect = effectToApply; // Default to what we're applying

            // Get the current slide from the wrapper
            if (effectToApply == FlowerySlideEffect.None || _currentSlideWrapper?.Child is not UIElement currentItem)
                return;

            // Use the shared attached property system
            FlowerySlideEffects.SetEffect(currentItem, effectToApply);
            double effectDuration = SlideInterval > 0 ? SlideInterval : 3.0;
            if (effectToApply == FlowerySlideEffect.PanAndZoom)
            {
                effectDuration = Math.Max(0.5, PanAndZoomCycleSeconds);
            }

            FlowerySlideEffects.SetDuration(currentItem, effectDuration);
            FlowerySlideEffects.SetZoomIntensity(currentItem, ZoomIntensity);
            FlowerySlideEffects.SetPanDistance(currentItem, PanDistance);
            FlowerySlideEffects.SetPulseIntensity(currentItem, PulseIntensity);
            FlowerySlideEffects.SetPanAndZoomLockZoom(currentItem, PanAndZoomLockZoom);
            FlowerySlideEffects.SetPanAndZoomPanSpeed(currentItem, PanAndZoomPanSpeed);
            FlowerySlideEffects.SetAutoStart(currentItem, false); // We'll start it manually
            _lastAppliedEffect = FlowerySlideEffects.StartEffect(currentItem);
        }

        private void StopSlideEffect()
        {
            if (_currentSlideWrapper?.Child is UIElement currentItem)
            {
                FlowerySlideEffects.StopEffect(currentItem);
            }
        }

        private void StopSlideEffectPreservingTransform()
        {
            if (_currentSlideWrapper?.Child is UIElement currentItem)
            {
                FlowerySlideEffects.StopEffectPreservingTransform(currentItem);
            }
        }
#else
        // Slide effects not supported on this platform
        private void ApplySlideEffect() { }
        private void StopSlideEffect() { }
        private void StopSlideEffectPreservingTransform() { }
#endif

        private void ApplyTextFillEffect()
        {
            if (_currentSlideWrapper?.Child is UIElement currentItem)
            {
                FloweryTextFillEffects.TryStartEffect(currentItem);
            }
        }

        #endregion

        private bool ShouldManageSlideEffects()
        {
            return SlideEffect != FlowerySlideEffect.None || Mode is FlowerySlideshowMode.Kiosk or FlowerySlideshowMode.Random;
        }
    }
}
