using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;
using Flowery.Helpers;

#nullable enable

namespace Flowery.Controls
{
    /// <summary>
    /// Clock unit for countdown display when used as a digit cell.
    /// </summary>
    public enum CountdownClockUnit
    {
        /// <summary>No clock unit - uses Value or Duration directly</summary>
        None,
        /// <summary>Display current hour</summary>
        Hours,
        /// <summary>Display current minute</summary>
        Minutes,
        /// <summary>Display current second</summary>
        Seconds
    }

    /// <summary>
    /// Mode for the countdown control.
    /// </summary>
    public enum CountdownMode
    {
        /// <summary>Display only - shows Value or Duration statically</summary>
        Display,
        /// <summary>Countdown timer - counts down from Duration to zero</summary>
        Countdown,
        /// <summary>Stopwatch - counts up from zero</summary>
        Stopwatch
    }

    /// <summary>
    /// A versatile time display control that can:
    /// 1. Display a single time segment value (0-99) for use as a digit cell in DaisyClock
    /// 2. Display and manage a countdown timer with TimeSpan precision
    /// 3. Work as a stopwatch with lap recording
    /// </summary>
    public partial class DaisyCountdown : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private TextBlock? _valueTextBlock;
        private ManagedTimer? _timer;

        // Stopwatch state
        private DateTime _startTime;
        private TimeSpan _pausedElapsed;
        private readonly ObservableCollection<LapTime> _laps = new();
        private TimeSpan _lastLapTime;

        public DaisyCountdown()
        {
            DefaultStyleKey = typeof(DaisyCountdown);
            IsTabStop = false;

            Laps = new ReadOnlyObservableCollection<LapTime>(_laps);
        }

        #region Dependency Properties - Digit Cell Mode

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(int),
                typeof(DaisyCountdown),
                new PropertyMetadata(0, OnValueChanged));

        /// <summary>
        /// Gets or sets the displayed value (0-999).
        /// Used when control is a digit cell or in simple countdown mode.
        /// </summary>
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Max(0, Math.Min(999, value)));
        }

        public static readonly DependencyProperty DigitsProperty =
            DependencyProperty.Register(
                nameof(Digits),
                typeof(int),
                typeof(DaisyCountdown),
                new PropertyMetadata(2, OnDisplayChanged));

        /// <summary>
        /// Gets or sets the number of digits to display (1-3).
        /// </summary>
        public int Digits
        {
            get => (int)GetValue(DigitsProperty);
            set => SetValue(DigitsProperty, Math.Max(1, Math.Min(3, value)));
        }

        public static readonly DependencyProperty ClockUnitProperty =
            DependencyProperty.Register(
                nameof(ClockUnit),
                typeof(CountdownClockUnit),
                typeof(DaisyCountdown),
                new PropertyMetadata(CountdownClockUnit.None, OnClockUnitChanged));

        /// <summary>
        /// When set to Hours/Minutes/Seconds, displays that component of the current time.
        /// Used by DaisyClock for Segmented display style.
        /// </summary>
        public CountdownClockUnit ClockUnit
        {
            get => (CountdownClockUnit)GetValue(ClockUnitProperty);
            set => SetValue(ClockUnitProperty, value);
        }

        #endregion

        #region Dependency Properties - Timer Mode

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(CountdownMode),
                typeof(DaisyCountdown),
                new PropertyMetadata(CountdownMode.Display, OnModeChanged));

        /// <summary>
        /// Gets or sets the operating mode: Display, Countdown, or Stopwatch.
        /// </summary>
        public CountdownMode Mode
        {
            get => (CountdownMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(TimeSpan),
                typeof(DaisyCountdown),
                new PropertyMetadata(TimeSpan.FromMinutes(5), OnDurationChanged));

        /// <summary>
        /// Gets or sets the duration for Countdown mode.
        /// </summary>
        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty RemainingProperty =
            DependencyProperty.Register(
                nameof(Remaining),
                typeof(TimeSpan),
                typeof(DaisyCountdown),
                new PropertyMetadata(TimeSpan.Zero));

        /// <summary>
        /// Gets the remaining time in Countdown mode (read-only externally).
        /// </summary>
        public TimeSpan Remaining
        {
            get => (TimeSpan)GetValue(RemainingProperty);
            private set => SetValue(RemainingProperty, value);
        }

        public static readonly DependencyProperty ElapsedProperty =
            DependencyProperty.Register(
                nameof(Elapsed),
                typeof(TimeSpan),
                typeof(DaisyCountdown),
                new PropertyMetadata(TimeSpan.Zero));

        /// <summary>
        /// Gets the elapsed time in Stopwatch mode (read-only externally).
        /// </summary>
        public TimeSpan Elapsed
        {
            get => (TimeSpan)GetValue(ElapsedProperty);
            private set => SetValue(ElapsedProperty, value);
        }

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register(
                nameof(IsRunning),
                typeof(bool),
                typeof(DaisyCountdown),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets whether the countdown/stopwatch is currently running.
        /// </summary>
        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            private set => SetValue(IsRunningProperty, value);
        }

        public static readonly DependencyProperty IsCountingDownProperty =
            DependencyProperty.Register(
                nameof(IsCountingDown),
                typeof(bool),
                typeof(DaisyCountdown),
                new PropertyMetadata(false, OnIsCountingDownChanged));

        /// <summary>
        /// Gets or sets whether the simple countdown is active (decrements Value).
        /// For backwards compatibility with simple countdown mode.
        /// </summary>
        public bool IsCountingDown
        {
            get => (bool)GetValue(IsCountingDownProperty);
            set => SetValue(IsCountingDownProperty, value);
        }

        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register(
                nameof(Interval),
                typeof(TimeSpan),
                typeof(DaisyCountdown),
                new PropertyMetadata(TimeSpan.FromSeconds(1)));

        /// <summary>
        /// Gets or sets the timer tick interval.
        /// </summary>
        public TimeSpan Interval
        {
            get => (TimeSpan)GetValue(IntervalProperty);
            set => SetValue(IntervalProperty, value);
        }

        public static readonly DependencyProperty LoopProperty =
            DependencyProperty.Register(
                nameof(Loop),
                typeof(bool),
                typeof(DaisyCountdown),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, Value loops back to LoopFrom when it reaches 0.
        /// </summary>
        public bool Loop
        {
            get => (bool)GetValue(LoopProperty);
            set => SetValue(LoopProperty, value);
        }

        public static readonly DependencyProperty LoopFromProperty =
            DependencyProperty.Register(
                nameof(LoopFrom),
                typeof(int),
                typeof(DaisyCountdown),
                new PropertyMetadata(59));

        /// <summary>
        /// Gets or sets the value to reset to when looping.
        /// </summary>
        public int LoopFrom
        {
            get => (int)GetValue(LoopFromProperty);
            set => SetValue(LoopFromProperty, value);
        }

        #endregion

        #region Dependency Properties - Appearance

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyCountdown),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when simple countdown (Value-based) reaches zero.
        /// </summary>
        public event EventHandler? CountdownCompleted;

        /// <summary>
        /// Raised when TimeSpan-based countdown reaches zero.
        /// </summary>
        public event EventHandler? TimerCompleted;

        /// <summary>
        /// Raised on each timer tick.
        /// </summary>
        public event EventHandler<TimeSpan>? Tick;

        /// <summary>
        /// Raised when a lap is recorded in Stopwatch mode.
        /// </summary>
        public event EventHandler<LapTime>? LapRecorded;

        #endregion

        #region Lap Recording

        /// <summary>
        /// Gets the collection of recorded lap times (read-only).
        /// </summary>
        public ReadOnlyObservableCollection<LapTime> Laps { get; }

        /// <summary>
        /// Records a lap at the current elapsed time.
        /// Only works in Stopwatch mode when running.
        /// </summary>
        public void RecordLap()
        {
            if (Mode != CountdownMode.Stopwatch || !IsRunning)
                return;

            var currentElapsed = Elapsed;
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

        #region Callbacks

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCountdown countdown)
            {
                countdown.UpdateDisplay();
            }
        }

        private static void OnDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCountdown countdown)
            {
                countdown.UpdateDisplay();
            }
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCountdown countdown)
            {
                countdown.StopTimer();
                countdown.Reset();
            }
        }

        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCountdown countdown)
            {
                if (!countdown.IsRunning && countdown.Mode == CountdownMode.Countdown)
                {
                    countdown.Remaining = countdown.Duration;
                }
            }
        }

        private static void OnClockUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCountdown countdown)
            {
                countdown.UpdateTimerState();
                if (countdown.ClockUnit != CountdownClockUnit.None)
                {
                    countdown.UpdateClockValue();
                }
            }
        }

        private static void OnIsCountingDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCountdown countdown)
            {
                countdown.UpdateTimerState();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCountdown countdown)
            {
                countdown.ApplyAll();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                ApplyAll();
                UpdateTimerState();
                return;
            }

            BuildVisualTree();
            ApplyAll();
            UpdateTimerState();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            StopTimer();
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

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _valueTextBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            _rootGrid.Children.Add(_valueTextBlock);
            Content = _rootGrid;

            ApplyAll();
        }

        #endregion

        #region Timer

        private void UpdateTimerState()
        {
            bool needsTimer = IsCountingDown || ClockUnit != CountdownClockUnit.None;

            if (needsTimer && _timer == null)
            {
                StartInternalTimer();
            }
            else if (!needsTimer && _timer != null)
            {
                StopTimer();
            }
        }

        private void StartInternalTimer()
        {
            StopTimer();
            _timer = new ManagedTimer(Interval);
            _timer.Tick += OnInternalTimerTick;
            _timer.Start();
        }

        private void StartTimer()
        {
            StopTimer();
            var interval = TimeSpan.FromMilliseconds(100); // For smooth updates
            _timer = new ManagedTimer(interval);
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void OnInternalTimerTick(object? sender, EventArgs e)
        {
            if (ClockUnit != CountdownClockUnit.None)
            {
                UpdateClockValue();
            }
            else if (IsCountingDown)
            {
                if (Value > 0)
                {
                    Value--;
                }
                else
                {
                    if (Loop)
                    {
                        Value = LoopFrom;
                    }
                    else
                    {
                        IsCountingDown = false;
                        CountdownCompleted?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (Mode == CountdownMode.Countdown)
            {
                var elapsed = DateTime.UtcNow - _startTime;
                Remaining = Duration - elapsed;

                if (Remaining <= TimeSpan.Zero)
                {
                    Remaining = TimeSpan.Zero;
                    Stop();
                    TimerCompleted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Tick?.Invoke(this, Remaining);
                }
            }
            else if (Mode == CountdownMode.Stopwatch)
            {
                Elapsed = DateTime.UtcNow - _startTime;
                Tick?.Invoke(this, Elapsed);
            }
        }

        private void UpdateClockValue()
        {
            var now = DateTime.Now;
            Value = ClockUnit switch
            {
                CountdownClockUnit.Hours => now.Hour,
                CountdownClockUnit.Minutes => now.Minute,
                CountdownClockUnit.Seconds => now.Second,
                _ => Value
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the countdown or stopwatch.
        /// </summary>
        public void Start()
        {
            if (Mode == CountdownMode.Display)
            {
                // For simple countdown mode
                IsCountingDown = true;
                return;
            }

            if (Mode == CountdownMode.Countdown)
            {
                var startFrom = Remaining > TimeSpan.Zero ? Duration - Remaining : TimeSpan.Zero;
                _startTime = DateTime.UtcNow.Subtract(startFrom);
            }
            else if (Mode == CountdownMode.Stopwatch)
            {
                _startTime = DateTime.UtcNow.Subtract(_pausedElapsed);
            }

            StartTimer();
            IsRunning = true;
        }

        /// <summary>
        /// Pauses the countdown or stopwatch.
        /// </summary>
        public void Pause()
        {
            if (Mode == CountdownMode.Stopwatch)
            {
                _pausedElapsed = Elapsed;
            }

            StopTimer();
            IsRunning = false;
            IsCountingDown = false;
        }

        /// <summary>
        /// Stops the countdown or stopwatch (same as Pause).
        /// </summary>
        public void Stop() => Pause();

        /// <summary>
        /// Resets the countdown or stopwatch to initial state.
        /// </summary>
        public void Reset()
        {
            StopTimer();
            IsRunning = false;
            IsCountingDown = false;

            if (Mode == CountdownMode.Countdown)
            {
                Remaining = Duration;
            }
            else if (Mode == CountdownMode.Stopwatch)
            {
                Elapsed = TimeSpan.Zero;
                _pausedElapsed = TimeSpan.Zero;
                ClearLaps();
            }

            UpdateDisplay();
        }

        /// <summary>
        /// Sets the duration from a string (e.g., "5:30", "PT5M30S", "330").
        /// </summary>
        public bool TrySetDuration(string input)
        {
            if (FloweryTimeHelpers.TryParseDurationPrecise(input, out var result) ||
                FloweryTimeHelpers.TryParseIso8601Duration(input, out result) ||
                FloweryTimeHelpers.TryParseDuration(input, out result))
            {
                Duration = result;
                return true;
            }
            return false;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_valueTextBlock == null)
                return;

            ApplySizing();
            ApplyColors();
            UpdateDisplay();
        }

        private void ApplySizing()
        {
            if (_valueTextBlock == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);

            double fontSize = resources == null
                ? DaisyResourceLookup.GetDefaultHeaderFontSize(Size)
                : DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}HeaderFontSize",
                    DaisyResourceLookup.GetDefaultHeaderFontSize(Size));
            _valueTextBlock.FontSize = fontSize;
        }

        private void ApplyColors()
        {
            if (_valueTextBlock == null)
                return;

            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyCountdown", "Foreground");
            _valueTextBlock.Foreground = fgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
        }

        private void UpdateDisplay()
        {
            if (_valueTextBlock == null)
                return;

            // For digit cell mode (used by DaisyClock), just show Value
            var format = Digits switch
            {
                3 => "D3",
                2 => "D2",
                _ => "D1"
            };

            _valueTextBlock.Text = Value.ToString(format);
        }

        #endregion
    }

    /// <summary>
    /// Represents a single lap time recorded by a stopwatch.
    /// </summary>
    public class LapTime
    {
        /// <summary>The lap number (1-based).</summary>
        public int Number { get; set; }

        /// <summary>When the lap was recorded (absolute time for data exchange).</summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>Total elapsed time from stopwatch start.</summary>
        public TimeSpan TotalElapsed { get; set; }

        /// <summary>Duration of this lap (time since last lap or start).</summary>
        public TimeSpan LapDuration { get; set; }

        /// <summary>
        /// Creates a new LapTime instance.
        /// </summary>
        public LapTime(int number, DateTimeOffset timestamp, TimeSpan totalElapsed, TimeSpan lapDuration)
        {
            Number = number;
            Timestamp = timestamp;
            TotalElapsed = totalElapsed;
            LapDuration = lapDuration;
        }

        /// <summary>
        /// Parameterless constructor for XAML serialization.
        /// </summary>
        public LapTime() { }
    }
}
