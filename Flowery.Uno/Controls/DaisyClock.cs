using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Flowery.Helpers;
using Flowery.Localization;
using Flowery.Theming;

#nullable enable

namespace Flowery.Controls
{
    /// <summary>
    /// Clock display mode.
    /// </summary>
    public enum ClockMode
    {
        /// <summary>Shows current time</summary>
        Clock,
        /// <summary>Countdown timer mode</summary>
        Timer,
        /// <summary>Stopwatch mode (elapsed time)</summary>
        Stopwatch
    }

    /// <summary>
    /// Label format for time units.
    /// </summary>
    public enum ClockLabelFormat
    {
        /// <summary>No labels, just numbers (04:53:16)</summary>
        None,
        /// <summary>Short labels (4h 53m 16s)</summary>
        Short,
        /// <summary>Long labels (4 hours 53 minutes 16 seconds)</summary>
        Long
    }

    /// <summary>
    /// Visual style for clock display.
    /// </summary>
    public enum ClockStyle
    {
        /// <summary>Uses DaisyCountdown controls for each time unit segment (hours, minutes, seconds)</summary>
        Segmented,
        /// <summary>Flip-clock style using TextBlocks in DaisyJoin (like bedside clocks with flaps)</summary>
        Flip,
        /// <summary>Plain text display - just a simple TextBlock showing the time</summary>
        Text
    }

    /// <summary>
    /// A comprehensive clock control that displays time with optional labels, supports
    /// timer and stopwatch modes, and can manage alarms. "Batteries included" replacement
    /// for manual DaisyCountdown composition.
    /// </summary>
    public partial class DaisyClock : DaisyBaseContentControl
    {
        private ManagedTimer? _clockTimer;
        private ManagedTimer? _timerTimer;
        private ManagedTimer? _stopwatchTimer;

        // Visual elements
        private UIElement? _rootContainer; // Either StackPanel, DaisyJoin, or TextBlock
        private DaisyJoin? _daisyJoin;
        private StackPanel? _joinPanel; // Panel inside DaisyJoin to hold children
        private StackPanel? _rootPanel;
        private DaisyCountdown? _hoursCountdown;
        private DaisyCountdown? _minutesCountdown;
        private DaisyCountdown? _secondsCountdown;
        private TextBlock? _hoursText;
        private TextBlock? _minutesText;
        private TextBlock? _secondsText;
        private TextBlock? _timeText; // For plain Text style
        private UIElement? _separator1;
        private UIElement? _separator2;
        private TextBlock? _hoursLabel;
        private TextBlock? _minutesLabel;
        private TextBlock? _secondsLabel;
        private TextBlock? _amPmLabel;

        // Stopwatch state
        private DateTime _stopwatchStartTime;
        private TimeSpan _stopwatchPausedElapsed;
        private readonly ObservableCollection<LapTime> _laps = new();
        private TimeSpan _lastLapTime;

        // Timer state
        private TimeSpan _timerRemaining;

        public DaisyClock()
        {
            DefaultStyleKey = typeof(DaisyClock);
            IsTabStop = false;

            // Initialize read-only laps collection
            Laps = new ReadOnlyObservableCollection<LapTime>(_laps);
        }

        #region Dependency Properties

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(ClockMode),
                typeof(DaisyClock),
                new PropertyMetadata(ClockMode.Clock, OnModeChanged));

        /// <summary>
        /// Gets or sets the clock mode (Clock, Timer, Stopwatch).
        /// </summary>
        public ClockMode Mode
        {
            get => (ClockMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyClock),
                new PropertyMetadata(Orientation.Horizontal, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the layout orientation (Horizontal or Vertical).
        /// </summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty ShowSecondsProperty =
            DependencyProperty.Register(
                nameof(ShowSeconds),
                typeof(bool),
                typeof(DaisyClock),
                new PropertyMetadata(true, OnLayoutChanged));

        /// <summary>
        /// Gets or sets whether to display seconds.
        /// </summary>
        public bool ShowSeconds
        {
            get => (bool)GetValue(ShowSecondsProperty);
            set => SetValue(ShowSecondsProperty, value);
        }

        public static readonly DependencyProperty ShowMillisecondsProperty =
            DependencyProperty.Register(
                nameof(ShowMilliseconds),
                typeof(bool),
                typeof(DaisyClock),
                new PropertyMetadata(false, OnLayoutChanged));

        /// <summary>
        /// Gets or sets whether to display milliseconds.
        /// Primarily useful for Stopwatch mode for sub-second precision.
        /// </summary>
        public bool ShowMilliseconds
        {
            get => (bool)GetValue(ShowMillisecondsProperty);
            set => SetValue(ShowMillisecondsProperty, value);
        }

        public static readonly DependencyProperty LabelFormatProperty =
            DependencyProperty.Register(
                nameof(LabelFormat),
                typeof(ClockLabelFormat),
                typeof(DaisyClock),
                new PropertyMetadata(ClockLabelFormat.None, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the label format (None, Short, Long).
        /// </summary>
        public ClockLabelFormat LabelFormat
        {
            get => (ClockLabelFormat)GetValue(LabelFormatProperty);
            set => SetValue(LabelFormatProperty, value);
        }

        public static readonly DependencyProperty SeparatorProperty =
            DependencyProperty.Register(
                nameof(Separator),
                typeof(string),
                typeof(DaisyClock),
                new PropertyMetadata(":", OnLayoutChanged));

        /// <summary>
        /// Gets or sets the separator between time units.
        /// </summary>
        public string Separator
        {
            get => (string)GetValue(SeparatorProperty);
            set => SetValue(SeparatorProperty, value);
        }

        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register(
                nameof(Format),
                typeof(ClockFormat),
                typeof(DaisyClock),
                new PropertyMetadata(ClockFormat.TwentyFourHour, OnFormatChanged));

        /// <summary>
        /// Gets or sets the clock format (12h or 24h).
        /// </summary>
        public ClockFormat Format
        {
            get => (ClockFormat)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        public static readonly DependencyProperty ShowAmPmProperty =
            DependencyProperty.Register(
                nameof(ShowAmPm),
                typeof(bool),
                typeof(DaisyClock),
                new PropertyMetadata(false, OnLayoutChanged));

        /// <summary>
        /// Gets or sets whether to show AM/PM suffix (only in 12h mode).
        /// </summary>
        public bool ShowAmPm
        {
            get => (bool)GetValue(ShowAmPmProperty);
            set => SetValue(ShowAmPmProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyClock),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of the clock display.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(DaisyClock),
                new PropertyMetadata(6.0, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the spacing between time units.
        /// </summary>
        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public static readonly DependencyProperty UseJoinProperty =
            DependencyProperty.Register(
                nameof(UseJoin),
                typeof(bool),
                typeof(DaisyClock),
                new PropertyMetadata(false, OnLayoutChanged));

        /// <summary>
        /// When true, uses DaisyJoin for a polished segmented appearance with
        /// shared borders and rounded corners. Works best with LabelFormat.None.
        /// </summary>
        public bool UseJoin
        {
            get => (bool)GetValue(UseJoinProperty);
            set => SetValue(UseJoinProperty, value);
        }

        public static readonly DependencyProperty DisplayStyleProperty =
            DependencyProperty.Register(
                nameof(DisplayStyle),
                typeof(ClockStyle),
                typeof(DaisyClock),
                new PropertyMetadata(ClockStyle.Segmented, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the visual style. Segmented uses DaisyCountdown controls,
        /// Flip uses TextBlocks in DaisyJoin for a bedside flip-clock look,
        /// Text uses a simple TextBlock for plain text display.
        /// </summary>
        public ClockStyle DisplayStyle
        {
            get => (ClockStyle)GetValue(DisplayStyleProperty);
            set => SetValue(DisplayStyleProperty, value);
        }

        public static readonly DependencyProperty TimerDurationProperty =
            DependencyProperty.Register(
                nameof(TimerDuration),
                typeof(TimeSpan),
                typeof(DaisyClock),
                new PropertyMetadata(TimeSpan.FromMinutes(5), OnTimerDurationChanged));

        /// <summary>
        /// Gets or sets the timer duration (for Timer mode).
        /// </summary>
        public TimeSpan TimerDuration
        {
            get => (TimeSpan)GetValue(TimerDurationProperty);
            set => SetValue(TimerDurationProperty, value);
        }

        public static readonly DependencyProperty TimerRemainingProperty =
            DependencyProperty.Register(
                nameof(TimerRemaining),
                typeof(TimeSpan),
                typeof(DaisyClock),
                new PropertyMetadata(TimeSpan.Zero));

        /// <summary>
        /// Gets the remaining time in Timer mode.
        /// </summary>
        public TimeSpan TimerRemaining
        {
            get => (TimeSpan)GetValue(TimerRemainingProperty);
            private set => SetValue(TimerRemainingProperty, value);
        }

        public static readonly DependencyProperty StopwatchElapsedProperty =
            DependencyProperty.Register(
                nameof(StopwatchElapsed),
                typeof(TimeSpan),
                typeof(DaisyClock),
                new PropertyMetadata(TimeSpan.Zero));

        /// <summary>
        /// Gets the elapsed time in Stopwatch mode.
        /// </summary>
        public TimeSpan StopwatchElapsed
        {
            get => (TimeSpan)GetValue(StopwatchElapsedProperty);
            private set => SetValue(StopwatchElapsedProperty, value);
        }

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register(
                nameof(IsRunning),
                typeof(bool),
                typeof(DaisyClock),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets whether the timer or stopwatch is currently running.
        /// </summary>
        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            private set => SetValue(IsRunningProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the timer finishes counting down to zero.
        /// </summary>
        public event EventHandler? TimerFinished;

        /// <summary>
        /// Raised each second when the timer is running.
        /// </summary>
        public event EventHandler<TimeSpan>? TimerTick;

        /// <summary>
        /// Raised each second when the stopwatch is running.
        /// </summary>
        public event EventHandler<TimeSpan>? StopwatchTick;

        /// <summary>
        /// Raised when a lap is recorded.
        /// </summary>
        public event EventHandler<LapTime>? LapRecorded;

        #endregion

        #region Lap Recording

        /// <summary>
        /// Gets the collection of recorded lap times (read-only).
        /// </summary>
        public ReadOnlyObservableCollection<LapTime> Laps { get; }

        /// <summary>
        /// Records a lap at the current stopwatch elapsed time.
        /// Only works in Stopwatch mode when running.
        /// </summary>
        public void RecordLap()
        {
            if (Mode != ClockMode.Stopwatch || !IsRunning)
                return;

            var currentElapsed = StopwatchElapsed;
            var lapDuration = currentElapsed - _lastLapTime;
            var lapNumber = _laps.Count + 1;

            var lap = new LapTime(lapNumber, DateTimeOffset.Now, currentElapsed, lapDuration);
            _laps.Add(lap);
            _lastLapTime = currentElapsed;

            LapRecorded?.Invoke(this, lap);
        }

        /// <summary>
        /// Clears all recorded laps.
        /// </summary>
        public void ClearLaps()
        {
            _laps.Clear();
            _lastLapTime = TimeSpan.Zero;
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyClock clock)
            {
                clock.StopAllTimers();
                clock.UpdateTimerForMode();
                clock.UpdateDisplay();
            }
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyClock clock)
            {
                clock.RebuildVisualTree();
            }
        }

        private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyClock clock)
            {
                clock.UpdateDisplay();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyClock clock)
            {
                clock.ApplyAll();
            }
        }

        private static void OnTimerDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyClock clock && clock.Mode == ClockMode.Timer && !clock.IsRunning)
            {
                clock._timerRemaining = clock.TimerDuration;
                clock.TimerRemaining = clock._timerRemaining;
                clock.UpdateDisplay();
            }
        }

        #endregion

        #region Lifecycle

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootContainer != null || _timeText != null)
            {
                ApplyAll();
                UpdateTimerForMode();
                FloweryLocalization.CultureChanged += OnCultureChanged;
                return;
            }

            RebuildVisualTree();
            ApplyAll();
            UpdateTimerForMode();

            // Subscribe to localization changes
            FloweryLocalization.CultureChanged += OnCultureChanged;
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            StopAllTimers();
            FloweryLocalization.CultureChanged -= OnCultureChanged;
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
            UpdateDisplay();
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

        private void OnCultureChanged(object? sender, string e)
        {
            UpdateLabels();
            UpdateFlowDirection();
        }

        #endregion

        #region Visual Tree

        private void RebuildVisualTree()
        {
            var showLabels = LabelFormat != ClockLabelFormat.None;
            var showSeparators = LabelFormat == ClockLabelFormat.None;
            var isFlipStyle = DisplayStyle == ClockStyle.Flip;
            var isTextStyle = DisplayStyle == ClockStyle.Text;
            var useJoinLayout = (UseJoin || isFlipStyle) && !showLabels && !isTextStyle; // Flip uses join, Text never does

            // Reset all references
            _daisyJoin = null;
            _joinPanel = null;
            _rootPanel = null;
            _hoursCountdown = null;
            _minutesCountdown = null;
            _secondsCountdown = null;
            _hoursText = null;
            _minutesText = null;
            _secondsText = null;
            _timeText = null;
            _separator1 = null;
            _separator2 = null;

            // Text style: just a single TextBlock, no container needed
            if (isTextStyle)
            {
                _timeText = CreatePlainTextBlock();
                _rootContainer = null;
                Content = _timeText;
                ApplyAll();
                UpdateDisplay();
                return;
            }

            if (useJoinLayout)
            {
                // Create a StackPanel to hold children, then set it as DaisyJoin's Content
                _joinPanel = new StackPanel
                {
                    Orientation = Orientation,
                    Spacing = 0
                };
                _daisyJoin = new DaisyJoin
                {
                    Orientation = Orientation,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = _joinPanel
                };
                _rootContainer = _daisyJoin;
            }
            else
            {
                _rootPanel = new StackPanel
                {
                    Orientation = Orientation,
                    Spacing = Spacing,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _rootContainer = _rootPanel;
            }

            UpdateFlowDirection();

            if (isFlipStyle)
            {
                // Flip clock style: TextBlocks in DaisyJoin (like bedside clocks)
                BuildFlipClockVisuals(showSeparators);
            }
            else
            {
                // Animated style: DaisyCountdown controls
                BuildAnimatedClockVisuals(showLabels, showSeparators, useJoinLayout);
            }

            // AM/PM
            if (ShowAmPm && Format == ClockFormat.TwelveHour)
            {
                _amPmLabel = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };
                AddToContainer(_amPmLabel);
            }

            Content = _rootContainer;

            // Refresh DaisyJoin after adding children
            _daisyJoin?.RefreshItems();

            ApplyAll();
            UpdateDisplay();
        }

        private void BuildFlipClockVisuals(bool showSeparators)
        {
            // Hours
            _hoursText = CreateFlipDigit();
            AddToContainer(WrapInFlipBorder(_hoursText));

            // Separator 1 (inside join for flip style)
            if (showSeparators)
            {
                // Flip separators are self-contained, use narrower padding
                var sep1 = CreateFlipSeparator();
                AddToContainer(WrapFlipSeparator(sep1));
            }

            // Minutes
            _minutesText = CreateFlipDigit();
            AddToContainer(WrapInFlipBorder(_minutesText));

            // Seconds
            if (ShowSeconds)
            {
                if (showSeparators)
                {
                    // Flip separators are self-contained, use narrower padding
                    var sep2 = CreateFlipSeparator();
                    AddToContainer(WrapFlipSeparator(sep2));
                }

                _secondsText = CreateFlipDigit();
                AddToContainer(WrapInFlipBorder(_secondsText));
            }
        }

        private void BuildAnimatedClockVisuals(bool showLabels, bool showSeparators, bool useJoinLayout)
        {
            // Hours
            if (showLabels)
            {
                var hoursPanel = CreateUnitPanel(out _hoursCountdown, out _hoursLabel, TimeUnit.Hours);
                AddToContainer(hoursPanel);
            }
            else
            {
                _hoursCountdown = CreateCountdown();
                if (useJoinLayout)
                {
                    AddToContainer(WrapForJoin(_hoursCountdown));
                }
                else
                {
                    AddToContainer(_hoursCountdown);
                }
            }

            // Separator 1 (only in non-join mode)
            if (showSeparators && !useJoinLayout)
            {
                _separator1 = CreateSeparator();
                AddToContainer(_separator1);
            }

            // Minutes
            if (showLabels)
            {
                var minutesPanel = CreateUnitPanel(out _minutesCountdown, out _minutesLabel, TimeUnit.Minutes);
                AddToContainer(minutesPanel);
            }
            else
            {
                _minutesCountdown = CreateCountdown();
                if (useJoinLayout)
                {
                    AddToContainer(WrapForJoin(_minutesCountdown));
                }
                else
                {
                    AddToContainer(_minutesCountdown);
                }
            }

            // Seconds
            if (ShowSeconds)
            {
                if (showSeparators && !useJoinLayout)
                {
                    _separator2 = CreateSeparator();
                    AddToContainer(_separator2);
                }

                if (showLabels)
                {
                    var secondsPanel = CreateUnitPanel(out _secondsCountdown, out _secondsLabel, TimeUnit.Seconds);
                    AddToContainer(secondsPanel);
                }
                else
                {
                    _secondsCountdown = CreateCountdown();
                    if (useJoinLayout)
                    {
                        AddToContainer(WrapForJoin(_secondsCountdown));
                    }
                    else
                    {
                        AddToContainer(_secondsCountdown);
                    }
                }
            }
        }

        /// <summary>
        /// Wraps a control with padding for DaisyJoin. Apply matching background for consistent theming.
        /// </summary>
        private static Border WrapForJoin(DaisyCountdown countdown)
        {
            // Set foreground to match base background for proper contrast
            // Use lightweight styling: inject a resource the control will pick up in ApplyColors
            countdown.Resources["DaisyCountdownForeground"] = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");

            return new Border
            {
                Child = countdown,
                Padding = new Thickness(8, 4, 8, 4),
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };
        }

        private void AddToContainer(UIElement element)
        {
            if (_joinPanel != null)
            {
                _joinPanel.Children.Add(element);
            }
            else
            {
                _rootPanel?.Children.Add(element);
            }
        }



        private TextBlock CreateFlipDigit()
        {
            var fontSize = DaisyResourceLookup.GetDefaultHeaderFontSize(Size);
            return new TextBlock
            {
                Text = "00",
                FontSize = fontSize,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
        }

        private TextBlock CreateFlipSeparator()
        {
            var fontSize = DaisyResourceLookup.GetDefaultHeaderFontSize(Size);
            return new TextBlock
            {
                Text = Separator,
                FontSize = fontSize,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
        }

        private static Border WrapInFlipBorder(TextBlock textBlock)
        {
            // Classic flip-clock styling with dark background
            return new Border
            {
                Child = textBlock,
                Padding = new Thickness(12, 8, 12, 8),
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };
        }

        private static Border WrapFlipSeparator(TextBlock textBlock)
        {
            // Narrower padding for colon separators in flip style
            return new Border
            {
                Child = textBlock,
                Padding = new Thickness(4, 8, 4, 8),
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };
        }

        private TextBlock CreatePlainTextBlock()
        {
            var fontSize = DaisyResourceLookup.GetDefaultHeaderFontSize(Size);
            return new TextBlock
            {
                Text = "00:00:00",
                FontSize = fontSize,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
        }

        private DaisyCountdown CreateCountdown()
        {
            var countdown = new DaisyCountdown
            {
                Digits = 2,
                Size = Size,
                ClockUnit = CountdownClockUnit.None, // We manage time ourselves
                VerticalAlignment = VerticalAlignment.Center
            };

            if (LabelFormat != ClockLabelFormat.None)
            {
                DaisyNeumorphic.SetIsEnabled(countdown, false);
            }

            return countdown;
        }

        private Grid CreateSeparator()
        {
            var fontSize = DaisyResourceLookup.GetDefaultHeaderFontSize(Size);

            // Wrap in Grid like DaisyCountdown for consistent vertical alignment
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // The colon character sits higher than digits due to baseline differences.
            // Add top padding to push it down to visually align with digit centers.
            var topPadding = fontSize * 0.1; // 10% of font size to offset baseline

            var textBlock = new TextBlock
            {
                Text = Separator,
                FontSize = fontSize,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(0, topPadding, 0, 0),
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };

            grid.Children.Add(textBlock);

            // Store reference to TextBlock for color updates
            return grid;
        }

        private StackPanel CreateUnitPanel(out DaisyCountdown countdown, out TextBlock label, TimeUnit unit)
        {
            // Panel orientation matches the overall orientation
            // Horizontal: [06] h   Vertical: [06]
            //                               hours
            var panelOrientation = Orientation == Orientation.Horizontal
                ? Orientation.Horizontal
                : Orientation.Vertical;

            var panel = new StackPanel
            {
                Orientation = panelOrientation,
                Spacing = panelOrientation == Orientation.Horizontal ? 4 : 2,
                // Stretch horizontally so children can center properly within
                HorizontalAlignment = panelOrientation == Orientation.Vertical
                    ? HorizontalAlignment.Stretch
                    : HorizontalAlignment.Center
            };

            countdown = CreateCountdown();
            // Explicit center alignment for vertical mode
            countdown.HorizontalAlignment = HorizontalAlignment.Center;
            panel.Children.Add(countdown);

            label = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                Text = GetUnitLabel(unit, 0)
            };
            panel.Children.Add(label);

            return panel;
        }

        private void UpdateFlowDirection()
        {
            var flowDirection = FloweryTimeHelpers.IsRtlCulture()
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            if (_rootPanel != null)
            {
                _rootPanel.FlowDirection = flowDirection;
            }

            if (_daisyJoin != null)
            {
                _daisyJoin.FlowDirection = flowDirection;
            }
        }

        #endregion

        #region Timer Management

        private void UpdateTimerForMode()
        {
            StopAllTimers();

            switch (Mode)
            {
                case ClockMode.Clock:
                    StartClockTimer();
                    break;
                case ClockMode.Timer:
                    // Timer doesn't auto-start
                    _timerRemaining = TimerDuration;
                    TimerRemaining = _timerRemaining;
                    break;
                case ClockMode.Stopwatch:
                    // Stopwatch doesn't auto-start
                    _stopwatchPausedElapsed = TimeSpan.Zero;
                    StopwatchElapsed = TimeSpan.Zero;
                    break;
            }

            UpdateDisplay();
        }

        private void StartClockTimer()
        {
            _clockTimer = new ManagedTimer(TimeSpan.FromSeconds(1));
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();
        }

        private void OnClockTick(object? sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void StopAllTimers()
        {
            _clockTimer?.Dispose();
            _clockTimer = null;

            _timerTimer?.Dispose();
            _timerTimer = null;

            _stopwatchTimer?.Dispose();
            _stopwatchTimer = null;

            IsRunning = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the timer or stopwatch (depending on mode).
        /// </summary>
        public void Start()
        {
            if (Mode == ClockMode.Clock) return;

            if (Mode == ClockMode.Timer)
            {
                StartTimer();
            }
            else if (Mode == ClockMode.Stopwatch)
            {
                StartStopwatch();
            }
        }

        /// <summary>
        /// Pauses the timer or stopwatch.
        /// </summary>
        public void Pause()
        {
            if (Mode == ClockMode.Timer)
            {
                _timerTimer?.Pause();
            }
            else if (Mode == ClockMode.Stopwatch)
            {
                PauseStopwatch();
            }

            IsRunning = false;
        }

        /// <summary>
        /// Stops and resets the timer or stopwatch.
        /// </summary>
        public void Reset()
        {
            if (Mode == ClockMode.Timer)
            {
                ResetTimer();
            }
            else if (Mode == ClockMode.Stopwatch)
            {
                ResetStopwatch();
            }
        }

        private void StartTimer()
        {
            if (_timerTimer == null)
            {
                _timerTimer = new ManagedTimer(TimeSpan.FromSeconds(1));
                _timerTimer.Tick += OnTimerTick;
            }

            _timerTimer.Start();
            IsRunning = true;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _timerRemaining = _timerRemaining.Subtract(TimeSpan.FromSeconds(1));

            if (_timerRemaining <= TimeSpan.Zero)
            {
                _timerRemaining = TimeSpan.Zero;
                _timerTimer?.Stop();
                IsRunning = false;
                TimerFinished?.Invoke(this, EventArgs.Empty);
            }

            TimerRemaining = _timerRemaining;
            TimerTick?.Invoke(this, _timerRemaining);
            UpdateDisplay();
        }

        private void ResetTimer()
        {
            _timerTimer?.Stop();
            _timerRemaining = TimerDuration;
            TimerRemaining = _timerRemaining;
            IsRunning = false;
            UpdateDisplay();
        }

        private void StartStopwatch()
        {
            _stopwatchStartTime = DateTime.UtcNow.Subtract(_stopwatchPausedElapsed);

            if (_stopwatchTimer == null)
            {
                _stopwatchTimer = new ManagedTimer(TimeSpan.FromMilliseconds(100));
                _stopwatchTimer.Tick += OnStopwatchTick;
            }

            _stopwatchTimer.Start();
            IsRunning = true;
        }

        private void OnStopwatchTick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.UtcNow - _stopwatchStartTime;
            StopwatchElapsed = elapsed;
            StopwatchTick?.Invoke(this, elapsed);
            UpdateDisplay();
        }

        private void PauseStopwatch()
        {
            _stopwatchPausedElapsed = StopwatchElapsed;
            _stopwatchTimer?.Pause();
        }

        private void ResetStopwatch()
        {
            _stopwatchTimer?.Stop();
            _stopwatchPausedElapsed = TimeSpan.Zero;
            StopwatchElapsed = TimeSpan.Zero;
            IsRunning = false;
            ClearLaps();
            UpdateDisplay();
        }

        #endregion

        #region Display Update

        private void UpdateDisplay()
        {
            // Check if we have any display elements
            var hasAnimatedDisplay = _hoursCountdown != null && _minutesCountdown != null;
            var hasFlipDisplay = _hoursText != null && _minutesText != null;
            var hasTextDisplay = _timeText != null;

            if (!hasAnimatedDisplay && !hasFlipDisplay && !hasTextDisplay)
                return;

            int hours, minutes, seconds;
            bool isPm = false;

            switch (Mode)
            {
                case ClockMode.Clock:
                    var now = DateTime.Now;
                    hours = now.Hour;
                    minutes = now.Minute;
                    seconds = now.Second;

                    if (Format == ClockFormat.TwelveHour)
                    {
                        isPm = hours >= 12;
                        hours %= 12;
                        if (hours == 0) hours = 12;

                        if (_amPmLabel is { } label)
                        {
                            label.Text = isPm
                                ? FloweryTimeHelpers.GetPmLabel()
                                : FloweryTimeHelpers.GetAmLabel();
                        }
                    }
                    break;

                case ClockMode.Timer:
                    hours = (int)_timerRemaining.TotalHours;
                    minutes = _timerRemaining.Minutes;
                    seconds = _timerRemaining.Seconds;
                    break;

                case ClockMode.Stopwatch:
                    var elapsed = StopwatchElapsed;
                    hours = (int)elapsed.TotalHours;
                    minutes = elapsed.Minutes;
                    seconds = elapsed.Seconds;
                    break;

                default:
                    hours = minutes = seconds = 0;
                    break;
            }

            if (hasTextDisplay)
            {
                // Text style: single formatted string
                var timeString = ShowSeconds
                    ? $"{hours:D2}{Separator}{minutes:D2}{Separator}{seconds:D2}"
                    : $"{hours:D2}{Separator}{minutes:D2}";

                if (ShowAmPm && Format == ClockFormat.TwelveHour)
                {
                    var amPm = isPm ? FloweryTimeHelpers.GetPmLabel() : FloweryTimeHelpers.GetAmLabel();
                    timeString = $"{timeString} {amPm}";
                }

                _timeText!.Text = timeString;
            }
            else if (hasFlipDisplay)
            {
                // Flip style: update TextBlocks
                _hoursText!.Text = hours.ToString("D2");
                _minutesText!.Text = minutes.ToString("D2");

                if (_secondsText != null && ShowSeconds)
                {
                    _secondsText.Text = seconds.ToString("D2");
                }
            }
            else if (hasAnimatedDisplay)
            {
                // Animated style: update DaisyCountdown controls
                _hoursCountdown!.Value = hours;
                _minutesCountdown!.Value = minutes;

                if (_secondsCountdown != null && ShowSeconds)
                {
                    _secondsCountdown.Value = seconds;
                }
            }

            UpdateLabels(hours, minutes, seconds);
        }

        private void UpdateLabels(int hours = 0, int minutes = 0, int seconds = 0)
        {
            if (LabelFormat == ClockLabelFormat.None)
                return;

            if (_hoursLabel != null)
                _hoursLabel.Text = GetUnitLabel(TimeUnit.Hours, hours);

            if (_minutesLabel != null)
                _minutesLabel.Text = GetUnitLabel(TimeUnit.Minutes, minutes);

            if (_secondsLabel != null)
                _secondsLabel.Text = GetUnitLabel(TimeUnit.Seconds, seconds);
        }

        private string GetUnitLabel(TimeUnit unit, int value)
        {
            var format = LabelFormat == ClockLabelFormat.Short
                ? TimeLabelFormat.Short
                : TimeLabelFormat.Long;

            return FloweryTimeHelpers.GetTimeUnitLabel(unit, value, format);
        }

        #endregion

        #region Styling

        private void ApplyAll()
        {
            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            // Update countdown controls
            if (_hoursCountdown != null) _hoursCountdown.Size = Size;
            if (_minutesCountdown != null) _minutesCountdown.Size = Size;
            if (_secondsCountdown != null) _secondsCountdown.Size = Size;

            // Update separator font size
            var fontSize = DaisyResourceLookup.GetDefaultHeaderFontSize(Size);

            // Separators are wrapped in Grid - get the TextBlock inside
            if (GetSeparatorTextBlock(_separator1) is TextBlock sep1) sep1.FontSize = fontSize;
            if (GetSeparatorTextBlock(_separator2) is TextBlock sep2) sep2.FontSize = fontSize;
            if (_hoursLabel != null) _hoursLabel.FontSize = fontSize * 0.6;
            if (_minutesLabel != null) _minutesLabel.FontSize = fontSize * 0.6;
            if (_secondsLabel != null) _secondsLabel.FontSize = fontSize * 0.6;
            if (_amPmLabel != null) _amPmLabel.FontSize = fontSize * 0.6;

            // Flip style text sizing
            if (_hoursText != null) _hoursText.FontSize = fontSize;
            if (_minutesText != null) _minutesText.FontSize = fontSize;
            if (_secondsText != null) _secondsText.FontSize = fontSize;

            // Text style sizing
            if (_timeText != null) _timeText.FontSize = fontSize;

            if (_rootPanel != null)
            {
                _rootPanel.Spacing = Spacing;
            }
        }

        private void ApplyColors()
        {
            var foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            var neutralForeground = DaisyResourceLookup.GetBrush("DaisyNeutralContentBrush");

            // Separators are wrapped in Grid - get the TextBlock inside
            if (GetSeparatorTextBlock(_separator1) is TextBlock sep1) sep1.Foreground = foreground;
            if (GetSeparatorTextBlock(_separator2) is TextBlock sep2) sep2.Foreground = foreground;
            if (_hoursLabel != null) _hoursLabel.Foreground = foreground;
            if (_minutesLabel != null) _minutesLabel.Foreground = foreground;
            if (_secondsLabel != null) _secondsLabel.Foreground = foreground;
            if (_amPmLabel != null) _amPmLabel.Foreground = foreground;

            // Text style uses base content color (plain text)
            if (_timeText != null) _timeText.Foreground = foreground;

            // Flip style uses base palette backgrounds, so use base content for contrast
            if (_hoursText != null) _hoursText.Foreground = foreground;
            if (_minutesText != null) _minutesText.Foreground = foreground;
            if (_secondsText != null) _secondsText.Foreground = foreground;
        }

        private static TextBlock? GetSeparatorTextBlock(UIElement? separator)
        {
            // Separator is a Grid containing a TextBlock
            if (separator is Grid grid && grid.Children.Count > 0)
            {
                return grid.Children[0] as TextBlock;
            }
            return null;
        }

        #endregion
    }
}
