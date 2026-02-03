using Microsoft.UI.Dispatching;
using Flowery.Uno.Gallery.Localization;

namespace Flowery.Uno.Gallery.Examples
{
    [Microsoft.UI.Xaml.Data.Bindable]
    public class CarouselDemoItem
    {
        public string Name { get; set; } = string.Empty;
        public FlowerySlideTransition Transition { get; set; }
        public FlowerySlideEffect Effect { get; set; }
        public int SliceCount { get; set; } = 8;
        public double Duration { get; set; } = 0.6;
    }

    public sealed partial class CarouselExamples : ScrollableExamplePage
    {
        private static readonly Dictionary<FlowerySlideTransition, CarouselTransitionDefaults> CarouselTransitionDefaultsByKind =
            new()
            {
                [FlowerySlideTransition.BlindsHorizontal] = new CarouselTransitionDefaults(SliceCount: 10m, Stagger: true, StaggerMs: 50m),
                [FlowerySlideTransition.BlindsVertical] = new CarouselTransitionDefaults(SliceCount: 10m, Stagger: true, StaggerMs: 50m),
                [FlowerySlideTransition.SlicesHorizontal] = new CarouselTransitionDefaults(SliceCount: 12m, Stagger: true, StaggerMs: 50m),
                [FlowerySlideTransition.SlicesVertical] = new CarouselTransitionDefaults(SliceCount: 12m, Stagger: true, StaggerMs: 50m),
                [FlowerySlideTransition.Checkerboard] = new CarouselTransitionDefaults(SliceCount: 8m, Stagger: true, StaggerMs: 50m),
                [FlowerySlideTransition.Spiral] = new CarouselTransitionDefaults(SliceCount: 8m, Stagger: true, StaggerMs: 50m),
                [FlowerySlideTransition.MatrixRain] = new CarouselTransitionDefaults(SliceCount: 16m, Stagger: true, StaggerMs: 50m),
                [FlowerySlideTransition.Wormhole] = new CarouselTransitionDefaults(SliceCount: 8m, Stagger: true, StaggerMs: 50m),
                [FlowerySlideTransition.Dissolve] = new CarouselTransitionDefaults(SliceCount: 12m, Stagger: true, StaggerMs: 50m, DissolveDensity: 0.5m),
                [FlowerySlideTransition.Pixelate] = new CarouselTransitionDefaults(SliceCount: 10m, Stagger: true, StaggerMs: 50m, PixelateSize: 20m),
                [FlowerySlideTransition.FlipHorizontal] = new CarouselTransitionDefaults(FlipAngle: 90m),
                [FlowerySlideTransition.FlipVertical] = new CarouselTransitionDefaults(FlipAngle: 90m),
                [FlowerySlideTransition.CubeLeft] = new CarouselTransitionDefaults(FlipAngle: 90m),
                [FlowerySlideTransition.CubeRight] = new CarouselTransitionDefaults(FlipAngle: 90m)
            };

        private bool _isUpdatingEffectDropdown; // Guard to prevent feedback loop
        private bool _isUpdatingTransitionDropdown;
        private bool _isApplyingSettings;
        private bool _hasRestoredSettings;
        private readonly Random _carouselRandom = new();
        private DispatcherQueueTimer? _carouselTimer;
        private FlowerySlideshowMode _carouselMode = FlowerySlideshowMode.Manual;
        private FlowerySlideEffect _lastRandomEffect = FlowerySlideEffect.None;
        private FlowerySlideTransition _lastRandomTransition = FlowerySlideTransition.None;
        private FlowerySlideshowMode _textFillCarouselMode;
        private bool _textFillCarouselHasSnapshot;

        public GalleryLocalization Localization { get; } = GalleryLocalization.Instance;

        public CarouselExamples()
        {
            InitializeComponent();
            SetDebugDefines();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged += OnThemeChanged;
            ApplyThemeColors();

            if (InteractiveCarousel != null)
            {
                InteractiveCarousel.SlideChanged += OnCarouselSlideChanged;
            }

            if (CarouselModeSelect != null)
            {
                CarouselModeSelect.ManualSelectionChanged += CarouselModeSelect_ManualSelectionChanged;
            }

            if (CarouselTransitionSelect != null)
            {
                CarouselTransitionSelect.ManualSelectionChanged += CarouselTransitionSelect_ManualSelectionChanged;
            }

            if (CarouselDurationSelect != null)
            {
                CarouselDurationSelect.ManualSelectionChanged += CarouselDurationSelect_ManualSelectionChanged;
            }

            if (CarouselEffectSelect != null)
            {
                CarouselEffectSelect.ManualSelectionChanged += CarouselEffectSelect_ManualSelectionChanged;
            }

            if (TextFillCarousel != null)
            {
                TextFillCarousel.SlideChanged += OnTextFillCarouselSlideChanged;
            }

            if (CarouselIntervalBox != null)
            {
                CarouselIntervalBox.Value = 3m;
                CarouselIntervalBox.Minimum = 1m;
                CarouselIntervalBox.Maximum = 300m;
                CarouselIntervalBox.Increment = 1m;

                CarouselIntervalBox.RegisterPropertyChangedCallback(
                    DaisyNumericUpDown.ValueProperty,
                    OnCarouselIntervalValueChanged);
            }

            if (CarouselSliceCountBox != null)
            {
                CarouselSliceCountBox.Value = 8m;
                CarouselSliceCountBox.Minimum = 2m;
                CarouselSliceCountBox.Maximum = 50m;
                CarouselSliceCountBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnCarouselTransitionParamValueChanged);
            }

            if (CarouselStaggerMsBox != null)
            {
                CarouselStaggerMsBox.Value = 50m;
                CarouselStaggerMsBox.Minimum = 10m;
                CarouselStaggerMsBox.Maximum = 200m;
                CarouselStaggerMsBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnCarouselTransitionParamValueChanged);
            }

            if (CarouselPixelateSizeBox != null)
            {
                CarouselPixelateSizeBox.Value = 20m;
                CarouselPixelateSizeBox.Minimum = 4m;
                CarouselPixelateSizeBox.Maximum = 100m;
                CarouselPixelateSizeBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnCarouselTransitionParamValueChanged);
            }

            if (CarouselDissolveDensityBox != null)
            {
                CarouselDissolveDensityBox.Value = 0.5m;
                CarouselDissolveDensityBox.Minimum = 0.1m;
                CarouselDissolveDensityBox.Maximum = 1.0m;
                CarouselDissolveDensityBox.Increment = 0.05m;
                CarouselDissolveDensityBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnCarouselTransitionParamValueChanged);
            }

            if (CarouselFlipAngleBox != null)
            {
                CarouselFlipAngleBox.Value = 90m;
                CarouselFlipAngleBox.Minimum = 0m;
                CarouselFlipAngleBox.Maximum = 180m;
                CarouselFlipAngleBox.Increment = 5m;
                CarouselFlipAngleBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, OnCarouselTransitionParamValueChanged);
            }

            InitializeTextFillControls();
            GalleryLocalization.CultureChanged += OnCultureChanged;
            RefreshLocalizationBindings();
            RestoreCarouselSettings();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DaisyThemeManager.ThemeChanged -= OnThemeChanged;

            if (InteractiveCarousel != null)
            {
                InteractiveCarousel.SlideChanged -= OnCarouselSlideChanged;
            }

            if (CarouselModeSelect != null)
            {
                CarouselModeSelect.ManualSelectionChanged -= CarouselModeSelect_ManualSelectionChanged;
            }

            if (CarouselTransitionSelect != null)
            {
                CarouselTransitionSelect.ManualSelectionChanged -= CarouselTransitionSelect_ManualSelectionChanged;
            }

            if (CarouselDurationSelect != null)
            {
                CarouselDurationSelect.ManualSelectionChanged -= CarouselDurationSelect_ManualSelectionChanged;
            }

            if (CarouselEffectSelect != null)
            {
                CarouselEffectSelect.ManualSelectionChanged -= CarouselEffectSelect_ManualSelectionChanged;
            }

            if (TextFillCarousel != null)
            {
                TextFillCarousel.SlideChanged -= OnTextFillCarouselSlideChanged;
            }

            GalleryLocalization.CultureChanged -= OnCultureChanged;
            StopCarouselTimer();

            PersistCarouselSettings();
            PersistTextFillSettings();
        }

        private void OnCultureChanged(object? sender, string cultureName)
        {
            RefreshLocalizationBindings();
        }

        private void RefreshLocalizationBindings()
        {
            if (MainScrollViewer == null)
                return;

            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(RefreshLocalizationBindingsCore);
                return;
            }
            RefreshLocalizationBindingsCore();
        }

        private void RefreshLocalizationBindingsCore()
        {
            if (MainScrollViewer == null)
                return;

            MainScrollViewer.DataContext = null;
            MainScrollViewer.DataContext = Localization;

            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, EnsureCarouselDefaults);
                return;
            }
            EnsureCarouselDefaults();
        }

        private void OnThemeChanged(object? sender, string themeName)
        {
            ApplyThemeColors();
        }

        private void ApplyThemeColors()
        {
            if (Slide1Border != null)
                Slide1Border.Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
            if (Slide1Text1 != null)
                Slide1Text1.Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush");
            if (Slide1Text2 != null)
                Slide1Text2.Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush");

            if (Slide2Border != null)
                Slide2Border.Background = DaisyResourceLookup.GetBrush("DaisySecondaryBrush");
            if (Slide2Text1 != null)
                Slide2Text1.Foreground = DaisyResourceLookup.GetBrush("DaisySecondaryContentBrush");
            if (Slide2Text2 != null)
                Slide2Text2.Foreground = DaisyResourceLookup.GetBrush("DaisySecondaryContentBrush");

            if (Slide3Border != null)
                Slide3Border.Background = DaisyResourceLookup.GetBrush("DaisyAccentBrush");
            if (Slide3Text1 != null)
                Slide3Text1.Foreground = DaisyResourceLookup.GetBrush("DaisyAccentContentBrush");
            if (Slide3Text2 != null)
                Slide3Text2.Foreground = DaisyResourceLookup.GetBrush("DaisyAccentContentBrush");
        }

        private void CarouselModeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = sender;
            _ = e;

            ApplyCarouselModeSelection();
        }

        private void CarouselModeSelect_ManualSelectionChanged(object? sender, object? selectedItem)
        {
            _ = sender;
            _ = selectedItem;

            ApplyCarouselModeSelection();
        }

        private void CarouselInfiniteToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isApplyingSettings)
                return;

            if (InteractiveCarouselEnabledToggle?.IsOn != true)
            {
                PersistCarouselSettings();
                return;
            }

            if (InteractiveCarousel == null || CarouselInfiniteToggle == null)
                return;

            InteractiveCarousel.WrapAround = CarouselInfiniteToggle.IsChecked == true;
            UpdateCarouselAutoCycle();
            PersistCarouselSettings();
        }

        private void CarouselEffectSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = sender;
            _ = e;

            ApplyCarouselEffectSelection();
        }

        private void CarouselEffectSelect_ManualSelectionChanged(object? sender, object? selectedItem)
        {
            _ = sender;
            _ = selectedItem;

            ApplyCarouselEffectSelection();
        }

        private void CarouselTransitionSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = sender;
            _ = e;

            ApplyCarouselTransitionSelection();
        }

        private void CarouselTransitionSelect_ManualSelectionChanged(object? sender, object? selectedItem)
        {
            _ = sender;
            _ = selectedItem;

            ApplyCarouselTransitionSelection();
        }

        private void CarouselDurationSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = sender;
            _ = e;

            ApplyCarouselDurationSelection();
        }

        private void CarouselDurationSelect_ManualSelectionChanged(object? sender, object? selectedItem)
        {
            _ = sender;
            _ = selectedItem;

            ApplyCarouselDurationSelection();
        }

        private void CarouselTransitionParam_Changed(object sender, RoutedEventArgs e)
        {
            if (_isApplyingSettings)
                return;

            ApplyTransitionParams();
            PersistCarouselSettings();
        }

        private void InteractiveCarouselEnabledToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isApplyingSettings)
                return;

            if (InteractiveCarousel == null || InteractiveCarouselEnabledToggle == null)
            {
                return;
            }

            bool isOn = InteractiveCarouselEnabledToggle.IsOn;
            InteractiveCarousel.IsEnabled = isOn;

            if (!isOn)
            {
                InteractiveCarousel.Mode = FlowerySlideshowMode.Manual;
                InteractiveCarousel.SlideEffect = FlowerySlideEffect.None;
                StopCarouselTimer();
                PersistCarouselSettings();
                return;
            }

            ApplyInteractiveCarouselSettings();
            PersistCarouselSettings();
        }

        private void ApplyInteractiveCarouselSettings()
        {
            if (InteractiveCarousel == null)
            {
                return;
            }

            _carouselMode = GetSelectedCarouselMode();
            InteractiveCarousel.Mode = FlowerySlideshowMode.Manual;

            if (CarouselTransitionSelect?.SelectedItem is DaisySelectItem transitionItem)
            {
                InteractiveCarousel.SlideTransition = FlowerySlideTransitionParser.Parse(transitionItem.Tag?.ToString());
            }

            if (CarouselDurationSelect?.SelectedItem is DaisySelectItem durationItem &&
                double.TryParse(durationItem.Tag?.ToString(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var duration))
            {
                InteractiveCarousel.TransitionDuration = duration;
            }

            if (CarouselEffectSelect?.SelectedItem is DaisySelectItem effectItem)
            {
                InteractiveCarousel.SlideEffect = FlowerySlideEffectParser.Parse(effectItem.Tag?.ToString());
            }

            if (CarouselIntervalBox?.Value is decimal val)
            {
                InteractiveCarousel.SlideInterval = (double)val;
            }

            if (CarouselInfiniteToggle != null)
            {
                InteractiveCarousel.WrapAround = CarouselInfiniteToggle.IsChecked == true;
            }

            if (_carouselMode == FlowerySlideshowMode.Random)
            {
                _lastRandomEffect = InteractiveCarousel.SlideEffect;
                _lastRandomTransition = InteractiveCarousel.SlideTransition;
            }

            ApplyTransitionParams();
            UpdateCarouselAutoCycle();
        }

        private void ApplyCarouselModeSelection()
        {
            if (_isApplyingSettings)
            {
                return;
            }

            PersistCarouselSettings();

            if (InteractiveCarouselEnabledToggle?.IsOn != true)
            {
                return;
            }

            if (InteractiveCarousel == null || CarouselModeSelect?.SelectedItem is not DaisySelectItem item)
                return;

            _carouselMode = FlowerySlideshowModeParser.Parse(item.Tag?.ToString());
            InteractiveCarousel.Mode = FlowerySlideshowMode.Manual;

            ApplyCarouselEffectSelection();

            if (_carouselMode == FlowerySlideshowMode.Random)
            {
                _lastRandomEffect = InteractiveCarousel.SlideEffect;
                _lastRandomTransition = InteractiveCarousel.SlideTransition;
            }

            UpdateCarouselAutoCycle();
        }

        private void ApplyCarouselTransitionSelection()
        {
            if (_isApplyingSettings || _isUpdatingTransitionDropdown)
            {
                return;
            }

            PersistCarouselSettings();

            if (InteractiveCarouselEnabledToggle?.IsOn != true)
            {
                return;
            }

            if (InteractiveCarousel == null || CarouselTransitionSelect?.SelectedItem is not DaisySelectItem item)
                return;

            InteractiveCarousel.SlideTransition = FlowerySlideTransitionParser.Parse(item.Tag?.ToString());
            _lastRandomTransition = InteractiveCarousel.SlideTransition;
            NormalizeCarouselParamBoxes();
            ApplyTransitionParams();
            UpdateCarouselAutoCycle();
        }

        private void ApplyCarouselDurationSelection()
        {
            if (_isApplyingSettings)
            {
                return;
            }

            PersistCarouselSettings();

            if (InteractiveCarouselEnabledToggle?.IsOn != true)
            {
                return;
            }

            if (InteractiveCarousel == null || CarouselDurationSelect?.SelectedItem is not DaisySelectItem item)
                return;

            if (double.TryParse(item.Tag?.ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var duration))
            {
                InteractiveCarousel.TransitionDuration = duration;
            }

            UpdateCarouselTimerInterval();
            UpdateCarouselAutoCycle();
        }

        private void ApplyCarouselEffectSelection()
        {
            if (_isUpdatingEffectDropdown || _isApplyingSettings)
                return;

            PersistCarouselSettings();

            if (InteractiveCarouselEnabledToggle?.IsOn != true)
                return;

            if (InteractiveCarousel == null || CarouselEffectSelect?.SelectedItem is not DaisySelectItem item)
                return;

            InteractiveCarousel.SlideEffect = FlowerySlideEffectParser.Parse(item.Tag?.ToString());
            _lastRandomEffect = InteractiveCarousel.SlideEffect;
            UpdateCarouselAutoCycle();
        }

        private void ApplyTransitionParams()
        {
            if (InteractiveCarousel == null || InteractiveCarouselEnabledToggle?.IsOn != true)
            {
                return;
            }

            if (CarouselSliceCountBox != null)
            {
                var val = (int)(CarouselSliceCountBox.Value ?? 8m);
                InteractiveCarousel.TransitionSliceCount = val;
                InteractiveCarousel.TransitionCheckerboardSize = val;
            }

            if (CarouselStaggerToggle != null)
                InteractiveCarousel.TransitionStaggerSlices = CarouselStaggerToggle.IsChecked == true;

            if (CarouselStaggerMsBox != null)
                InteractiveCarousel.TransitionSliceStaggerMs = (double)(CarouselStaggerMsBox.Value ?? 50m);

            if (CarouselPixelateSizeBox != null)
                InteractiveCarousel.TransitionPixelateSize = (int)(CarouselPixelateSizeBox.Value ?? 20m);

            if (CarouselDissolveDensityBox != null)
                InteractiveCarousel.TransitionDissolveDensity = (double)(CarouselDissolveDensityBox.Value ?? 1m);

            if (CarouselFlipAngleBox != null)
                InteractiveCarousel.TransitionFlipAngle = (double)(CarouselFlipAngleBox.Value ?? 90m);
        }

        private void NormalizeCarouselParamBoxes()
        {
            if (_isApplyingSettings)
                return;

            if (!TryGetTransitionDefaults(GetSelectedTransition(), out var defaults))
                return;

            _isApplyingSettings = true;
            try
            {
                if (defaults.SliceCount.HasValue)
                    NormalizeCarouselNumericBox(CarouselSliceCountBox, defaults.SliceCount.Value);

                if (defaults.Stagger.HasValue
                    && CarouselStaggerToggle is { } staggerToggle
                    && staggerToggle.IsChecked is null)
                {
                    staggerToggle.IsChecked = defaults.Stagger.Value;
                }

                if (defaults.StaggerMs.HasValue)
                    NormalizeCarouselNumericBox(CarouselStaggerMsBox, defaults.StaggerMs.Value);

                if (defaults.PixelateSize.HasValue)
                    NormalizeCarouselNumericBox(CarouselPixelateSizeBox, defaults.PixelateSize.Value);

                if (defaults.DissolveDensity.HasValue)
                    NormalizeCarouselNumericBox(CarouselDissolveDensityBox, defaults.DissolveDensity.Value);

                if (defaults.FlipAngle.HasValue)
                    NormalizeCarouselNumericBox(CarouselFlipAngleBox, defaults.FlipAngle.Value);
            }
            finally
            {
                _isApplyingSettings = false;
            }
        }

        private FlowerySlideshowMode GetSelectedCarouselMode()
        {
            if (CarouselModeSelect?.SelectedItem is DaisySelectItem item)
            {
                return FlowerySlideshowModeParser.Parse(item.Tag?.ToString());
            }

            return FlowerySlideshowMode.Manual;
        }

        private void UpdateCarouselAutoCycle()
        {
            if (_isApplyingSettings)
                return;

            if (!ShouldRunCarouselTimer())
            {
                StopCarouselTimer();
                return;
            }

            EnsureCarouselTimer();
            UpdateCarouselTimerInterval();

            if (_carouselTimer is { IsRunning: false })
            {
                _carouselTimer.Start();
            }
        }

        private bool ShouldRunCarouselTimer()
        {
            if (InteractiveCarouselEnabledToggle?.IsOn != true)
                return false;

            if (InteractiveCarousel == null)
                return false;

            if (!IsCarouselAutoMode(_carouselMode))
                return false;

            if (InteractiveCarousel.ItemCount <= 1)
                return false;

            return CarouselInfiniteToggle?.IsChecked == true || !IsCarouselAtEnd();
        }

        private static bool IsCarouselAutoMode(FlowerySlideshowMode mode)
        {
            return mode is FlowerySlideshowMode.Slideshow or FlowerySlideshowMode.Kiosk or FlowerySlideshowMode.Random;
        }

        private bool IsCarouselAtEnd()
        {
            if (InteractiveCarousel == null)
                return true;

            if (InteractiveCarousel.ItemCount == 0)
                return true;

            return InteractiveCarousel.SelectedIndex >= InteractiveCarousel.ItemCount - 1;
        }

        private void EnsureCarouselTimer()
        {
            if (_carouselTimer != null)
                return;

            var queue = DispatcherQueue;
            if (queue == null)
                return;

            _carouselTimer = queue.CreateTimer();
            _carouselTimer.Tick += OnCarouselTimerTick;
        }

        private void StopCarouselTimer()
        {
            if (_carouselTimer == null)
                return;

            _carouselTimer.Stop();
        }

        private void OnCarouselTimerTick(DispatcherQueueTimer sender, object args)
        {
            _ = args;

            if (!ShouldRunCarouselTimer())
            {
                sender.Stop();
                return;
            }

            UpdateCarouselTimerInterval();
            ApplyCarouselCycleSelection();
            InteractiveCarousel?.Next();
        }

        private void UpdateCarouselTimerInterval()
        {
            if (_carouselTimer == null)
                return;

            var seconds = GetCarouselCycleSeconds();
            if (seconds < 1)
                seconds = 1;

            _carouselTimer.Interval = TimeSpan.FromSeconds(seconds);
        }

        private double GetCarouselCycleSeconds()
        {
            var interval = (double)(CarouselIntervalBox?.Value ?? 3m);
            if (interval < 1)
                interval = 1;

            var transitionSeconds = InteractiveCarousel?.TransitionDuration ?? GetSelectedTransitionSeconds();
            return interval + Math.Max(transitionSeconds, 0.1);
        }

        private double GetSelectedTransitionSeconds()
        {
            if (CarouselDurationSelect?.SelectedItem is DaisySelectItem item
                && double.TryParse(item.Tag?.ToString(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var seconds))
            {
                return Math.Max(seconds, 0.1d);
            }

            return 0.4d;
        }

        private void ApplyCarouselCycleSelection()
        {
            if (InteractiveCarousel == null)
                return;

            switch (_carouselMode)
            {
                case FlowerySlideshowMode.Slideshow:
                    SetCarouselEffect(GetNextSequentialEffect());
                    break;
                case FlowerySlideshowMode.Kiosk:
                    SetCarouselEffect(GetNextSequentialEffect());
                    SetCarouselTransition(PickRandomTransition(avoidRepeat: false));
                    break;
                case FlowerySlideshowMode.Random:
                    SetCarouselEffect(PickRandomEffect(avoidRepeat: true));
                    SetCarouselTransition(PickRandomTransition(avoidRepeat: true));
                    break;
            }
        }

        private List<FlowerySlideEffect> GetEffectCycle()
        {
            var effects = new List<FlowerySlideEffect>();
            if (CarouselEffectSelect == null)
                return effects;

            foreach (var item in CarouselEffectSelect.Items.OfType<DaisySelectItem>())
            {
                effects.Add(FlowerySlideEffectParser.Parse(item.Tag?.ToString()));
            }

            return effects;
        }

        private List<FlowerySlideTransition> GetTransitionPool()
        {
            var transitions = new List<FlowerySlideTransition>();
            if (CarouselTransitionSelect == null)
                return transitions;

            foreach (var item in CarouselTransitionSelect.Items.OfType<DaisySelectItem>())
            {
                var transition = FlowerySlideTransitionParser.Parse(item.Tag?.ToString());
                if (transition == FlowerySlideTransition.Random)
                    continue;

                transitions.Add(transition);
            }

            return transitions;
        }

        private FlowerySlideEffect GetNextSequentialEffect()
        {
            var effects = GetEffectCycle();
            if (effects.Count == 0)
                return FlowerySlideEffect.None;

            var current = InteractiveCarousel?.SlideEffect
                ?? FlowerySlideEffectParser.Parse(GetSelectedTag(CarouselEffectSelect));
            var index = effects.IndexOf(current);
            if (index < 0)
                index = 0;

            var nextIndex = index + 1;
            if (nextIndex >= effects.Count)
                nextIndex = 0;

            return effects[nextIndex];
        }

        private FlowerySlideEffect PickRandomEffect(bool avoidRepeat)
        {
            var effects = GetEffectCycle();
            if (effects.Count == 0)
                return FlowerySlideEffect.None;

            if (!avoidRepeat || effects.Count == 1)
            {
                return effects[_carouselRandom.Next(effects.Count)];
            }

            FlowerySlideEffect next;
            do
            {
                next = effects[_carouselRandom.Next(effects.Count)];
            } while (next == _lastRandomEffect);

            _lastRandomEffect = next;
            return next;
        }

        private FlowerySlideTransition PickRandomTransition(bool avoidRepeat)
        {
            var transitions = GetTransitionPool();
            if (transitions.Count == 0)
                return FlowerySlideTransition.None;

            if (!avoidRepeat || transitions.Count == 1)
            {
                return transitions[_carouselRandom.Next(transitions.Count)];
            }

            FlowerySlideTransition next;
            do
            {
                next = transitions[_carouselRandom.Next(transitions.Count)];
            } while (next == _lastRandomTransition);

            _lastRandomTransition = next;
            return next;
        }

        private void SetCarouselEffect(FlowerySlideEffect effect)
        {
            if (InteractiveCarousel != null)
            {
                InteractiveCarousel.SlideEffect = effect;
            }

            if (CarouselEffectSelect == null)
                return;

            _isUpdatingEffectDropdown = true;
            try
            {
                SelectByTag(CarouselEffectSelect, effect.ToString());
            }
            finally
            {
                _isUpdatingEffectDropdown = false;
            }
        }

        private void SetCarouselTransition(FlowerySlideTransition transition)
        {
            if (InteractiveCarousel != null)
            {
                InteractiveCarousel.SlideTransition = transition;
            }

            if (CarouselTransitionSelect == null)
                return;

            _isUpdatingTransitionDropdown = true;
            try
            {
                SelectByTag(CarouselTransitionSelect, transition.ToString());
            }
            finally
            {
                _isUpdatingTransitionDropdown = false;
            }
        }

        private FlowerySlideTransition GetSelectedTransition()
        {
            return CarouselTransitionSelect?.SelectedItem is DaisySelectItem item
                ? FlowerySlideTransitionParser.Parse(item.Tag?.ToString())
                : FlowerySlideTransition.None;
        }

        private static bool TryGetTransitionDefaults(FlowerySlideTransition transition, out CarouselTransitionDefaults defaults)
        {
            return CarouselTransitionDefaultsByKind.TryGetValue(transition, out defaults);
        }

        private static void NormalizeCarouselNumericBox(DaisyNumericUpDown? box, decimal fallback)
        {
            if (box == null)
                return;

            var value = box.Value ?? fallback;
            var min = box.Minimum;
            var max = box.Maximum;
            var normalized = value < min || value > max
                ? Math.Clamp(fallback, min, max)
                : value;

            if (normalized != value)
            {
                box.Value = normalized;
            }
        }

        private readonly record struct CarouselTransitionDefaults(
            decimal? SliceCount = null,
            bool? Stagger = null,
            decimal? StaggerMs = null,
            decimal? PixelateSize = null,
            decimal? DissolveDensity = null,
            decimal? FlipAngle = null);

        private void OnCarouselIntervalValueChanged(DependencyObject sender, DependencyProperty dp)
        {
            _ = sender;
            _ = dp;

            if (_isApplyingSettings)
                return;

            var intervalBox = CarouselIntervalBox;
            if (intervalBox == null)
                return;

            var normalizedInterval = NormalizeIntervalValue(intervalBox.Value, fallback: 3m);
            if (intervalBox.Value != normalizedInterval)
            {
                _isApplyingSettings = true;
                try
                {
                    intervalBox.Value = normalizedInterval;
                }
                finally
                {
                    _isApplyingSettings = false;
                }

                return;
            }

            if (InteractiveCarousel != null
                && InteractiveCarouselEnabledToggle?.IsOn == true
                && intervalBox.Value is decimal val)
            {
                InteractiveCarousel.SlideInterval = (double)val;
            }

            UpdateCarouselTimerInterval();
            UpdateCarouselAutoCycle();
            PersistCarouselSettings();
        }

        private void OnCarouselTransitionParamValueChanged(DependencyObject sender, DependencyProperty dp)
        {
            _ = sender;
            _ = dp;

            if (_isApplyingSettings)
                return;

            ApplyTransitionParams();
            PersistCarouselSettings();
        }

        private void InitializeTextFillControls()
        {
            if (TextFillDurationBox != null)
            {
                TextFillDurationBox.Value = 6m;
                TextFillDurationBox.Minimum = 0.2m;
                TextFillDurationBox.Maximum = 20m;
                TextFillDurationBox.Increment = 0.2m;
                TextFillDurationBox.FormatString = "0.##";
                TextFillDurationBox.MaxDecimalPlaces = 2;
                TextFillDurationBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, TextFillNumericValueChanged);
            }

            if (TextFillPanXBox != null)
            {
                TextFillPanXBox.Value = 0.25m;
                TextFillPanXBox.Minimum = 0m;
                TextFillPanXBox.Maximum = 1m;
                TextFillPanXBox.Increment = 0.05m;
                TextFillPanXBox.FormatString = "0.##";
                TextFillPanXBox.MaxDecimalPlaces = 2;
                TextFillPanXBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, TextFillNumericValueChanged);
            }

            if (TextFillPanYBox != null)
            {
                TextFillPanYBox.Value = 0.1m;
                TextFillPanYBox.Minimum = 0m;
                TextFillPanYBox.Maximum = 1m;
                TextFillPanYBox.Increment = 0.05m;
                TextFillPanYBox.FormatString = "0.##";
                TextFillPanYBox.MaxDecimalPlaces = 2;
                TextFillPanYBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, TextFillNumericValueChanged);
            }

            if (TextFillScaleBox != null)
            {
                TextFillScaleBox.Value = 1m;
                TextFillScaleBox.Minimum = 0.5m;
                TextFillScaleBox.Maximum = 2m;
                TextFillScaleBox.Increment = 0.05m;
                TextFillScaleBox.FormatString = "0.##";
                TextFillScaleBox.MaxDecimalPlaces = 2;
                TextFillScaleBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, TextFillNumericValueChanged);
            }

            if (TextFillRotationBox != null)
            {
                TextFillRotationBox.Value = 0m;
                TextFillRotationBox.Minimum = -45m;
                TextFillRotationBox.Maximum = 45m;
                TextFillRotationBox.Increment = 1m;
                TextFillRotationBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, TextFillNumericValueChanged);
            }

            if (TextFillOffsetXBox != null)
            {
                TextFillOffsetXBox.Value = 0m;
                TextFillOffsetXBox.Minimum = -60m;
                TextFillOffsetXBox.Maximum = 60m;
                TextFillOffsetXBox.Increment = 1m;
                TextFillOffsetXBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, TextFillNumericValueChanged);
            }

            if (TextFillOffsetYBox != null)
            {
                TextFillOffsetYBox.Value = 0m;
                TextFillOffsetYBox.Minimum = -60m;
                TextFillOffsetYBox.Maximum = 60m;
                TextFillOffsetYBox.Increment = 1m;
                TextFillOffsetYBox.RegisterPropertyChangedCallback(DaisyNumericUpDown.ValueProperty, TextFillNumericValueChanged);
            }

            ApplyTextFillSettings();
        }

        private void RestoreCarouselSettings()
        {
            if (_hasRestoredSettings)
                return;

            _hasRestoredSettings = true;
            _isApplyingSettings = true;
            try
            {
                var settings = GallerySettings.LoadCarouselSettings();
                if (settings != null)
                {
                    if (InteractiveCarouselEnabledToggle != null)
                        InteractiveCarouselEnabledToggle.IsOn = settings.Enabled;

                    SelectByTag(CarouselModeSelect, settings.ModeTag);
                    SelectByTag(CarouselTransitionSelect, settings.TransitionTag);
                    SelectByTag(CarouselDurationSelect, settings.DurationTag);
                    SelectByTag(CarouselEffectSelect, settings.EffectTag);

                    if (CarouselIntervalBox != null)
                        CarouselIntervalBox.Value = settings.IntervalSeconds;
                    if (CarouselInfiniteToggle != null)
                        CarouselInfiniteToggle.IsChecked = settings.Infinite;
                    if (CarouselSliceCountBox != null)
                        CarouselSliceCountBox.Value = settings.SliceCount;
                    if (CarouselStaggerToggle != null)
                        CarouselStaggerToggle.IsChecked = settings.Stagger;
                    if (CarouselStaggerMsBox != null)
                        CarouselStaggerMsBox.Value = settings.StaggerMs;
                    if (CarouselPixelateSizeBox != null)
                        CarouselPixelateSizeBox.Value = settings.PixelateSize;
                    if (CarouselDissolveDensityBox != null)
                        CarouselDissolveDensityBox.Value = settings.DissolveDensity;
                    if (CarouselFlipAngleBox != null)
                        CarouselFlipAngleBox.Value = settings.FlipAngle;
                }

                NormalizeIntervalBox(CarouselIntervalBox, fallback: 3m);

                var textFill = GallerySettings.LoadCarouselTextFillSettings();
                if (textFill != null)
                {
                    if (TextFillCarouselEnabledToggle != null)
                        TextFillCarouselEnabledToggle.IsOn = textFill.Enabled;
                    if (TextFillAnimateToggle != null)
                        TextFillAnimateToggle.IsChecked = textFill.Animate;
                    if (TextFillAutoReverseToggle != null)
                        TextFillAutoReverseToggle.IsChecked = textFill.AutoReverse;
                    if (TextFillDurationBox != null)
                        TextFillDurationBox.Value = textFill.Duration;
                    if (TextFillPanXBox != null)
                        TextFillPanXBox.Value = textFill.PanX;
                    if (TextFillPanYBox != null)
                        TextFillPanYBox.Value = textFill.PanY;
                    if (TextFillScaleBox != null)
                        TextFillScaleBox.Value = textFill.Scale;
                    if (TextFillRotationBox != null)
                        TextFillRotationBox.Value = textFill.Rotation;
                    if (TextFillOffsetXBox != null)
                        TextFillOffsetXBox.Value = textFill.OffsetX;
                    if (TextFillOffsetYBox != null)
                        TextFillOffsetYBox.Value = textFill.OffsetY;
                }
            }
            finally
            {
                _isApplyingSettings = false;
            }

            EnsureCarouselDefaults();
            InteractiveCarouselEnabledToggle_Changed(this, new RoutedEventArgs());
            TextFillCarouselEnabledToggle_Changed(this, new RoutedEventArgs());
            ApplyTextFillSettings();
        }

        private void PersistCarouselSettings()
        {
            if (_isApplyingSettings)
                return;

            var intervalSeconds = NormalizeIntervalValue(CarouselIntervalBox?.Value, fallback: 3m);
            var settings = new CarouselSettings(
                Enabled: InteractiveCarouselEnabledToggle?.IsOn ?? true,
                ModeTag: GetSelectedTag(CarouselModeSelect),
                TransitionTag: GetSelectedTag(CarouselTransitionSelect),
                DurationTag: GetSelectedTag(CarouselDurationSelect),
                EffectTag: GetSelectedTag(CarouselEffectSelect),
                IntervalSeconds: intervalSeconds,
                Infinite: CarouselInfiniteToggle?.IsChecked == true,
                SliceCount: CarouselSliceCountBox?.Value ?? 8m,
                Stagger: CarouselStaggerToggle?.IsChecked == true,
                StaggerMs: CarouselStaggerMsBox?.Value ?? 50m,
                PixelateSize: CarouselPixelateSizeBox?.Value ?? 20m,
                DissolveDensity: CarouselDissolveDensityBox?.Value ?? 0.5m,
                FlipAngle: CarouselFlipAngleBox?.Value ?? 90m);

            GallerySettings.SaveCarouselSettings(settings);
        }

        private void PersistTextFillSettings()
        {
            if (_isApplyingSettings)
                return;

            var settings = new CarouselTextFillSettings(
                Enabled: TextFillCarouselEnabledToggle?.IsOn ?? true,
                Animate: TextFillAnimateToggle?.IsChecked == true,
                AutoReverse: TextFillAutoReverseToggle?.IsChecked == true,
                Duration: TextFillDurationBox?.Value ?? 6m,
                PanX: TextFillPanXBox?.Value ?? 0.25m,
                PanY: TextFillPanYBox?.Value ?? 0.1m,
                Scale: TextFillScaleBox?.Value ?? 1m,
                Rotation: TextFillRotationBox?.Value ?? 0m,
                OffsetX: TextFillOffsetXBox?.Value ?? 0m,
                OffsetY: TextFillOffsetYBox?.Value ?? 0m);

            GallerySettings.SaveCarouselTextFillSettings(settings);
        }

        private void TextFillSettings_Changed(object? sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            if (_isApplyingSettings)
                return;

            ApplyTextFillSettings();
            PersistTextFillSettings();
        }

        private void TextFillCarouselEnabledToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isApplyingSettings)
                return;

            if (TextFillCarousel == null || TextFillCarouselEnabledToggle == null)
            {
                return;
            }

            bool isOn = TextFillCarouselEnabledToggle.IsOn;
            TextFillCarousel.IsEnabled = isOn;

            if (isOn)
            {
                if (_textFillCarouselHasSnapshot)
                {
                    TextFillCarousel.Mode = _textFillCarouselMode;
                }

                PersistTextFillSettings();
                return;
            }

            _textFillCarouselMode = TextFillCarousel.Mode;
            _textFillCarouselHasSnapshot = true;

            TextFillCarousel.Mode = FlowerySlideshowMode.Manual;
            PersistTextFillSettings();
        }

        private void OnTextFillCarouselSlideChanged(object? sender, FlowerySlideChangedEventArgs e)
        {
            _ = sender;
            _ = e;

            ApplyTextFillSettings();
        }

        private void TextFillNumericValueChanged(DependencyObject sender, DependencyProperty dp)
        {
            _ = sender;
            _ = dp;

            if (_isApplyingSettings)
                return;

            ApplyTextFillSettings();
            PersistTextFillSettings();
        }

        private void ApplyTextFillSettings()
        {
            bool animate = TextFillAnimateToggle?.IsChecked == true;
            bool autoReverse = TextFillAutoReverseToggle?.IsChecked == true;
            double duration = (double)(TextFillDurationBox?.Value ?? 6m);
            double panX = (double)(TextFillPanXBox?.Value ?? 0.25m);
            double panY = (double)(TextFillPanYBox?.Value ?? 0.1m);
            double scale = (double)(TextFillScaleBox?.Value ?? 1m);
            double rotation = (double)(TextFillRotationBox?.Value ?? 0m);
            double offsetX = (double)(TextFillOffsetXBox?.Value ?? 0m);
            double offsetY = (double)(TextFillOffsetYBox?.Value ?? 0m);

            foreach (var textBlock in GetTextFillTargets())
            {
                FloweryTextFillEffects.SetDuration(textBlock, duration);
                FloweryTextFillEffects.SetPanX(textBlock, panX);
                FloweryTextFillEffects.SetPanY(textBlock, panY);
                FloweryTextFillEffects.SetAutoReverse(textBlock, autoReverse);
                FloweryTextFillEffects.SetAnimate(textBlock, animate);

                ApplyTextTransform(textBlock, scale, rotation, offsetX, offsetY);

                if (animate)
                {
                    FloweryTextFillEffects.StartEffect(textBlock);
                }
                else
                {
                    FloweryTextFillEffects.StopEffect(textBlock);
                }
            }
        }

        private void EnsureCarouselDefaults()
        {
            EnsureCarouselSelection(CarouselModeSelect, defaultIndex: 1);
            EnsureCarouselSelection(CarouselTransitionSelect, defaultIndex: 0);
            EnsureCarouselSelection(CarouselDurationSelect, defaultIndex: 2);
            EnsureCarouselSelection(CarouselEffectSelect, defaultIndex: 1);
        }

        private static void EnsureCarouselSelection(DaisySelect? select, int defaultIndex)
        {
            if (select == null || select.Items.Count == 0)
                return;

            var targetIndex = select.SelectedIndex >= 0
                ? select.SelectedIndex
                : Math.Min(defaultIndex, select.Items.Count - 1);

            if (targetIndex < 0)
                return;

            select.SelectedIndex = -1;
            select.SelectedIndex = targetIndex;
            select.SelectedItem = select.Items[targetIndex];
        }

        private static decimal NormalizeIntervalValue(decimal? value, decimal fallback)
        {
            if (!value.HasValue)
            {
                return fallback;
            }

            var interval = value.Value;
            return interval < 1m ? fallback : interval;
        }

        private static void NormalizeIntervalBox(DaisyNumericUpDown? box, decimal fallback)
        {
            if (box == null)
                return;

            var normalized = NormalizeIntervalValue(box.Value, fallback);
            if (box.Value != normalized)
            {
                box.Value = normalized;
            }
        }

        private static string? GetSelectedTag(DaisySelect? select)
        {
            return select?.SelectedItem is DaisySelectItem item ? item.Tag?.ToString() : null;
        }

        private static void SelectByTag(DaisySelect? select, string? tag)
        {
            if (select == null || string.IsNullOrWhiteSpace(tag))
                return;

            for (int i = 0; i < select.Items.Count; i++)
            {
                if (select.Items[i] is DaisySelectItem item
                    && string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    if (select.SelectedIndex != i)
                    {
                        select.SelectedIndex = i;
                    }
                    return;
                }
            }
        }

        private static void ApplyTextTransform(TextBlock textBlock, double scale, double rotation, double offsetX, double offsetY)
        {
            if (textBlock.RenderTransform is not CompositeTransform transform)
            {
                transform = new CompositeTransform();
                textBlock.RenderTransform = transform;
            }

            transform.ScaleX = scale;
            transform.ScaleY = scale;
            transform.Rotation = rotation;
            transform.TranslateX = offsetX;
            transform.TranslateY = offsetY;
            textBlock.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        }

        private IEnumerable<TextBlock> GetTextFillTargets()
        {
            if (TextFillText1 is { } text1)
                yield return text1;

            if (TextFillText2 is { } text2)
                yield return text2;

            if (TextFillText3 is { } text3)
                yield return text3;
        }

        private void OnCarouselSlideChanged(object? sender, FlowerySlideChangedEventArgs e)
        {
            _ = sender;
            _ = e;

            if (_carouselMode == FlowerySlideshowMode.Random)
            {
                _lastRandomEffect = e.AppliedEffect;
                if (InteractiveCarousel != null)
                {
                    _lastRandomTransition = InteractiveCarousel.LastAppliedTransition;
                }
            }

            UpdateCarouselAutoCycle();
        }

        private void MultiDemoButton_Click(object sender, RoutedEventArgs e)
        {
            var items = new List<CarouselDemoItem>();

            foreach (var transition in Enum.GetValues<FlowerySlideTransition>())
            {
                if (transition is FlowerySlideTransition.None or FlowerySlideTransition.Random)
                    continue;

                if (FlowerySlideTransitionHelpers.IsWasmCompositionTransition(transition))
                    continue;

                var item = new CarouselDemoItem
                {
                    Name = transition.ToString(),
                    Transition = transition,
                    Effect = FlowerySlideEffect.None,
                    Duration = 2
                };

                if (transition == FlowerySlideTransition.MatrixRain) item.SliceCount = 16;
                if (transition == FlowerySlideTransition.Wormhole) item.SliceCount = 30;
                if (transition is FlowerySlideTransition.Checkerboard or FlowerySlideTransition.Spiral) item.SliceCount = 10;

                items.Add(item);
            }

            var effectLabel = Localization["Gallery_Carousel_Matrix_EffectLabel"];
            foreach (var effect in Enum.GetValues<FlowerySlideEffect>())
            {
                if (effect == FlowerySlideEffect.None)
                    continue;

                items.Add(new CarouselDemoItem
                {
                    Name = string.Format(CultureInfo.CurrentUICulture, effectLabel, effect),
                    Transition = FlowerySlideTransition.PushLeft,
                    Effect = effect,
                    Duration = 0.4
                });
            }

            MultiDemoGrid.ItemsSource = items;
            MultiDemoModal.IsOpen = true;
        }

        private void CloseMultiDemo_Click(object sender, RoutedEventArgs e)
        {
            MultiDemoModal.IsOpen = false;
        }

        private void SetDebugDefines()
        {
            if (DebugDefinesText == null)
            {
                return;
            }

            DebugDefinesText.Text = BuildDefinesText();
        }

        private static string BuildDefinesText()
        {
            var defines = new List<string>();
#if FLOWERY_BROWSER_BUILD
            defines.Add("FLOWERY_BROWSER_BUILD");
#endif
#if __WASM__
            defines.Add("__WASM__");
#endif
#if HAS_UNO_SKIA
            defines.Add("HAS_UNO_SKIA");
#endif
#if __UNO_SKIA__
            defines.Add("__UNO_SKIA__");
#endif
#if __SKIA__
            defines.Add("__SKIA__");
#endif
#if HAS_UNO_SKIA_WEBASSEMBLY_BROWSER
            defines.Add("HAS_UNO_SKIA_WEBASSEMBLY_BROWSER");
#endif
#if __UNO_SKIA_WEBASSEMBLY_BROWSER__
            defines.Add("__UNO_SKIA_WEBASSEMBLY_BROWSER__");
#endif

            if (defines.Count == 0)
            {
                return "Defines: (none)";
            }

            return $"Defines: {string.Join(", ", defines)}";
        }
    }
}
