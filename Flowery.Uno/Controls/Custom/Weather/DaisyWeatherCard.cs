using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Controls.Weather.Models;
using Flowery.Controls.Weather.Services;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Flowery.Controls.Weather
{
    /// <summary>
    /// A composite weather card that can display current weather, forecast, and metrics.
    /// Supports both manual property binding and automatic data fetching via IWeatherService.
    /// </summary>
    public partial class DaisyWeatherCard : DaisyBaseContentControl
    {
        private CancellationTokenSource? _loadingCts;
        private CancellationTokenSource? _autoRefreshCts;
        private bool _pendingSizeUpdate;

        #region Size

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(DaisySize.Medium, OnSizeChanged));

        /// <summary>
        /// The size of the weather card.
        /// When <see cref="FlowerySizeManager.UseGlobalSizeByDefault"/> is enabled,
        /// this will follow <see cref="FlowerySizeManager.CurrentSize"/> unless IgnoreGlobalSize is set.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherCard control)
                control.UpdateSizeVisualState();
        }

        #endregion

        #region Current Weather Properties

        public static readonly DependencyProperty TemperatureProperty =
            DependencyProperty.Register(
                nameof(Temperature),
                typeof(double),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(0d));

        public double Temperature
        {
            get => (double)GetValue(TemperatureProperty);
            set => SetValue(TemperatureProperty, value);
        }

        public static readonly DependencyProperty FeelsLikeProperty =
            DependencyProperty.Register(
                nameof(FeelsLike),
                typeof(double),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(0d));

        public double FeelsLike
        {
            get => (double)GetValue(FeelsLikeProperty);
            set => SetValue(FeelsLikeProperty, value);
        }

        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register(
                nameof(Condition),
                typeof(WeatherCondition),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(WeatherCondition.Unknown));

        public WeatherCondition Condition
        {
            get => (WeatherCondition)GetValue(ConditionProperty);
            set => SetValue(ConditionProperty, value);
        }

        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(
                nameof(Date),
                typeof(DateTime),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(DateTime.Now));

        public DateTime Date
        {
            get => (DateTime)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        public static readonly DependencyProperty SunriseProperty =
            DependencyProperty.Register(
                nameof(Sunrise),
                typeof(TimeSpan),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Sunrise
        {
            get => (TimeSpan)GetValue(SunriseProperty);
            set => SetValue(SunriseProperty, value);
        }

        public static readonly DependencyProperty SunsetProperty =
            DependencyProperty.Register(
                nameof(Sunset),
                typeof(TimeSpan),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Sunset
        {
            get => (TimeSpan)GetValue(SunsetProperty);
            set => SetValue(SunsetProperty, value);
        }

        #endregion

        #region Forecast Properties

        public static readonly DependencyProperty ForecastProperty =
            DependencyProperty.Register(
                nameof(Forecast),
                typeof(IEnumerable<ForecastDay>),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(null));

        public IEnumerable<ForecastDay>? Forecast
        {
            get => (IEnumerable<ForecastDay>?)GetValue(ForecastProperty);
            set => SetValue(ForecastProperty, value);
        }

        #endregion

        #region Metrics Properties

        public static readonly DependencyProperty UvIndexProperty =
            DependencyProperty.Register(
                nameof(UvIndex),
                typeof(double),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(0d));

        public double UvIndex
        {
            get => (double)GetValue(UvIndexProperty);
            set => SetValue(UvIndexProperty, value);
        }

        public static readonly DependencyProperty UvMaxProperty =
            DependencyProperty.Register(
                nameof(UvMax),
                typeof(double),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(0d));

        public double UvMax
        {
            get => (double)GetValue(UvMaxProperty);
            set => SetValue(UvMaxProperty, value);
        }

        public static readonly DependencyProperty WindSpeedProperty =
            DependencyProperty.Register(
                nameof(WindSpeed),
                typeof(double),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(0d));

        public double WindSpeed
        {
            get => (double)GetValue(WindSpeedProperty);
            set => SetValue(WindSpeedProperty, value);
        }

        public static readonly DependencyProperty WindMaxProperty =
            DependencyProperty.Register(
                nameof(WindMax),
                typeof(double),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(0d));

        public double WindMax
        {
            get => (double)GetValue(WindMaxProperty);
            set => SetValue(WindMaxProperty, value);
        }

        public static readonly DependencyProperty WindUnitProperty =
            DependencyProperty.Register(
                nameof(WindUnit),
                typeof(string),
                typeof(DaisyWeatherCard),
                new PropertyMetadata("km/h"));

        public string WindUnit
        {
            get => (string)GetValue(WindUnitProperty);
            set => SetValue(WindUnitProperty, value);
        }

        public static readonly DependencyProperty HumidityProperty =
            DependencyProperty.Register(
                nameof(Humidity),
                typeof(int),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(0));

        public int Humidity
        {
            get => (int)GetValue(HumidityProperty);
            set => SetValue(HumidityProperty, value);
        }

        public static readonly DependencyProperty HumidityMaxProperty =
            DependencyProperty.Register(
                nameof(HumidityMax),
                typeof(int),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(0));

        public int HumidityMax
        {
            get => (int)GetValue(HumidityMaxProperty);
            set => SetValue(HumidityMaxProperty, value);
        }

        #endregion

        #region Configuration Properties

        public static readonly DependencyProperty TemperatureUnitProperty =
            DependencyProperty.Register(
                nameof(TemperatureUnit),
                typeof(string),
                typeof(DaisyWeatherCard),
                new PropertyMetadata("C"));

        public string TemperatureUnit
        {
            get => (string)GetValue(TemperatureUnitProperty);
            set => SetValue(TemperatureUnitProperty, value);
        }

        public static readonly DependencyProperty ShowCurrentProperty =
            DependencyProperty.Register(
                nameof(ShowCurrent),
                typeof(bool),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(true));

        public bool ShowCurrent
        {
            get => (bool)GetValue(ShowCurrentProperty);
            set => SetValue(ShowCurrentProperty, value);
        }

        public static readonly DependencyProperty ShowForecastProperty =
            DependencyProperty.Register(
                nameof(ShowForecast),
                typeof(bool),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(true));

        public bool ShowForecast
        {
            get => (bool)GetValue(ShowForecastProperty);
            set => SetValue(ShowForecastProperty, value);
        }

        public static readonly DependencyProperty ShowMetricsProperty =
            DependencyProperty.Register(
                nameof(ShowMetrics),
                typeof(bool),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(true));

        public bool ShowMetrics
        {
            get => (bool)GetValue(ShowMetricsProperty);
            set => SetValue(ShowMetricsProperty, value);
        }

        public static readonly DependencyProperty ShowSunTimesProperty =
            DependencyProperty.Register(
                nameof(ShowSunTimes),
                typeof(bool),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(true));

        public bool ShowSunTimes
        {
            get => (bool)GetValue(ShowSunTimesProperty);
            set => SetValue(ShowSunTimesProperty, value);
        }

        public static readonly DependencyProperty DateFormatProperty =
            DependencyProperty.Register(
                nameof(DateFormat),
                typeof(string),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Date format string for the header display.
        /// Default is null, which automatically selects a compact locale-aware format (e.g., "ddd, dd.MM." or "ddd, MM/dd").
        /// Use shorter formats like "ddd, d MMM" for constrained layouts.
        /// </summary>
        public string DateFormat
        {
            get => (string)GetValue(DateFormatProperty);
            set => SetValue(DateFormatProperty, value);
        }

        #endregion

        #region Service Properties

        public static readonly DependencyProperty WeatherServiceProperty =
            DependencyProperty.Register(
                nameof(WeatherService),
                typeof(IWeatherService),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(null, OnServiceOrLocationChanged));

        public IWeatherService? WeatherService
        {
            get => (IWeatherService?)GetValue(WeatherServiceProperty);
            set => SetValue(WeatherServiceProperty, value);
        }

        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.Register(
                nameof(Location),
                typeof(string),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(string.Empty, OnServiceOrLocationChanged));

        public string Location
        {
            get => (string)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        public static readonly DependencyProperty ForecastDaysProperty =
            DependencyProperty.Register(
                nameof(ForecastDays),
                typeof(int),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(5));

        public int ForecastDays
        {
            get => (int)GetValue(ForecastDaysProperty);
            set => SetValue(ForecastDaysProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(false));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage),
                typeof(string),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(null));

        public string? ErrorMessage
        {
            get => (string?)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public static readonly DependencyProperty AutoRefreshProperty =
            DependencyProperty.Register(
                nameof(AutoRefresh),
                typeof(bool),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(false, OnAutoRefreshChanged));

        /// <summary>
        /// Whether to automatically refresh weather data at the specified interval.
        /// </summary>
        public bool AutoRefresh
        {
            get => (bool)GetValue(AutoRefreshProperty);
            set => SetValue(AutoRefreshProperty, value);
        }

        public static readonly DependencyProperty RefreshIntervalProperty =
            DependencyProperty.Register(
                nameof(RefreshInterval),
                typeof(TimeSpan),
                typeof(DaisyWeatherCard),
                new PropertyMetadata(TimeSpan.FromMinutes(30), OnAutoRefreshChanged));

        /// <summary>
        /// Interval between automatic refreshes (default: 30 minutes).
        /// </summary>
        public TimeSpan RefreshInterval
        {
            get => (TimeSpan)GetValue(RefreshIntervalProperty);
            set => SetValue(RefreshIntervalProperty, value);
        }

        #endregion

        public DaisyWeatherCard()
        {
            DefaultStyleKey = typeof(DaisyWeatherCard);
            LayoutUpdated += OnLayoutUpdated;
            IsTabStop = false;
        }

        // Cache detected palette name (background color doesn't change, only theme brushes do)
        private string? _detectedBackgroundPalette;

        private void ApplyAll()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Refresh brushes from current theme
            ApplyColors();
            UpdateSizeVisualState();
        }

        private void ApplyColors()
        {
            // Detect which palette the Background belongs to (once, then cached)
            if (_detectedBackgroundPalette == null)
            {
                var bgColor = (Background as SolidColorBrush)?.Color;
                if (bgColor != null)
                {
                    _detectedBackgroundPalette = DaisyResourceLookup.GetPaletteNameForColor(bgColor.Value);
                }
            }

            // Get fresh brushes from current theme
            if (_detectedBackgroundPalette != null)
            {
                var (freshBackground, freshForeground) = DaisyResourceLookup.GetPaletteBrushes(_detectedBackgroundPalette);
                if (freshBackground != null)
                    Background = freshBackground;
                if (freshForeground != null)
                    Foreground = freshForeground;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            ApplyAll();
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            if (_pendingSizeUpdate && ActualWidth > 0)
            {
                _pendingSizeUpdate = false;
                UpdateSizeVisualState();
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            _loadingCts?.Cancel();
            _loadingCts = null;

            _autoRefreshCts?.Cancel();
            _autoRefreshCts = null;
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

        private void UpdateSizeVisualState()
        {
            var stateName = Size switch
            {
                DaisySize.ExtraSmall => "ExtraSmall",
                DaisySize.Small => "Small",
                DaisySize.Medium => "Medium",
                DaisySize.Large => "Large",
                DaisySize.ExtraLarge => "ExtraLarge",
                _ => "Medium"
            };

            VisualStateManager.GoToState(this, stateName, true);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _pendingSizeUpdate = true;
        }

        private static void OnServiceOrLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherCard card)
            {
                card.HandleServiceOrLocationChanged();
            }
        }

        private void HandleServiceOrLocationChanged()
        {
            if (WeatherService != null && !string.IsNullOrWhiteSpace(Location))
            {
                _ = LoadWeatherDataAsync();
                HandleAutoRefreshChanged();
            }
        }

        private static void OnAutoRefreshChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherCard card)
            {
                card.HandleAutoRefreshChanged();
            }
        }

        private void HandleAutoRefreshChanged()
        {
            _autoRefreshCts?.Cancel();
            _autoRefreshCts = null;

            if (AutoRefresh && WeatherService != null && !string.IsNullOrWhiteSpace(Location))
            {
                _autoRefreshCts = new CancellationTokenSource();
                _ = RunAutoRefreshLoop(_autoRefreshCts.Token);
            }
        }

        private async Task RunAutoRefreshLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(RefreshInterval, token);
                    if (!token.IsCancellationRequested)
                    {
                        await LoadWeatherDataAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Manually triggers a refresh of weather data from the configured service.
        /// </summary>
        public async Task RefreshAsync()
        {
            await LoadWeatherDataAsync();
        }

        private async Task LoadWeatherDataAsync()
        {
            if (WeatherService == null || string.IsNullOrWhiteSpace(Location))
                return;

            _loadingCts?.Cancel();
            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            try
            {
                await EnqueueAsync(() =>
                {
                    IsLoading = true;
                    ErrorMessage = null;
                });

                var current = await WeatherService.GetCurrentWeatherAsync(Location, token);
                IEnumerable<ForecastDay>? forecast = ShowForecast
                    ? await WeatherService.GetForecastAsync(Location, ForecastDays, token)
                    : null;
                WeatherMetrics? metrics = ShowMetrics
                    ? await WeatherService.GetMetricsAsync(Location, token)
                    : null;

                if (token.IsCancellationRequested)
                    return;

                await EnqueueAsync(() =>
                {
                    Temperature = current.Temperature;
                    FeelsLike = current.FeelsLike;
                    Condition = current.Condition;
                    Date = current.Date;
                    Sunrise = current.Sunrise;
                    Sunset = current.Sunset;

                    if (forecast != null)
                    {
                        Forecast = [.. forecast];
                    }

                    if (metrics != null)
                    {
                        UvIndex = metrics.UvIndex;
                        UvMax = metrics.UvMax;
                        WindSpeed = metrics.WindSpeed;
                        WindMax = metrics.WindMax;
                        WindUnit = metrics.WindUnit;
                        Humidity = metrics.Humidity;
                        HumidityMax = metrics.HumidityMax;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Cancelled - ignore
            }
            catch (Exception ex)
            {
                await EnqueueAsync(() =>
                {
                    ErrorMessage = ex.Message;
                });
            }
            finally
            {
                await EnqueueAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private Task EnqueueAsync(Action action)
        {
            var dispatcherQueue = DispatcherQueue;
            if (dispatcherQueue == null || dispatcherQueue.HasThreadAccess)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}
