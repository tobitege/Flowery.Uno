using Flowery.Controls.Weather.Models;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Flowery.Controls.Weather
{
    /// <summary>
    /// Displays a horizontal strip of daily weather forecasts.
    /// Uses DaisyWeatherForecastItem as item containers to support size-responsive styling.
    /// </summary>
    public partial class DaisyWeatherForecast : ItemsControl
    {
        private const double MinItemSpacing = 2;
        private readonly DaisyControlLifecycle _lifecycle;

        private bool _pendingSizeUpdate;
        private ScrollViewer? _scrollViewer;
        private ButtonBase? _leftScrollButton;
        private ButtonBase? _rightScrollButton;

        public DaisyWeatherForecast()
        {
            DefaultStyleKey = typeof(DaisyWeatherForecast);
            SizeChanged += OnSizeChanged;
            LayoutUpdated += OnLayoutUpdated;
            IsTabStop = false;

            _lifecycle = new DaisyControlLifecycle(
                this,
                applyAll: ApplyAll,
                getSize: () => Size,
                setSize: s => Size = s);
        }

        private void ApplyAll()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Refresh Foreground from current theme (Background is transparent)
            var freshForeground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            if (freshForeground != null)
                Foreground = freshForeground;

            ApplySizing();
        }

        #region Size

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyWeatherForecast),
                new PropertyMetadata(DaisySize.Medium, OnSizePropertyChanged));

        /// <summary>
        /// The size of the forecast display.
        /// When <see cref="FlowerySizeManager.UseGlobalSizeByDefault"/> is enabled,
        /// this will follow <see cref="FlowerySizeManager.CurrentSize"/> unless IgnoreGlobalSize is set.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherForecast control)
                control.ApplySizing();
        }

        #endregion

        #region AutoFitItems

        public static readonly DependencyProperty AutoFitItemsProperty =
            DependencyProperty.Register(
                nameof(AutoFitItems),
                typeof(bool),
                typeof(DaisyWeatherForecast),
                new PropertyMetadata(true, OnAutoFitItemsChanged));

        /// <summary>
        /// When true (default), automatically calculates item widths to fit all items
        /// within the available container width. Items scale down to ExtraSmall styling
        /// as needed. When items exceed the minimum readable size, the control becomes
        /// horizontally scrollable to prevent content clipping.
        /// </summary>
        public bool AutoFitItems
        {
            get => (bool)GetValue(AutoFitItemsProperty);
            set => SetValue(AutoFitItemsProperty, value);
        }

        private static void OnAutoFitItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherForecast control)
                control.ApplySizing();
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty TemperatureUnitProperty =
            DependencyProperty.Register(
                nameof(TemperatureUnit),
                typeof(string),
                typeof(DaisyWeatherForecast),
                new PropertyMetadata("C"));

        /// <summary>
        /// Temperature unit (C or F).
        /// </summary>
        public string TemperatureUnit
        {
            get => (string)GetValue(TemperatureUnitProperty);
            set => SetValue(TemperatureUnitProperty, value);
        }

        public static readonly DependencyProperty ShowPrecipitationProperty =
            DependencyProperty.Register(
                nameof(ShowPrecipitation),
                typeof(bool),
                typeof(DaisyWeatherForecast),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether to show precipitation chance.
        /// </summary>
        public bool ShowPrecipitation
        {
            get => (bool)GetValue(ShowPrecipitationProperty);
            set => SetValue(ShowPrecipitationProperty, value);
        }

        public static readonly DependencyProperty ItemSpacingProperty =
            DependencyProperty.Register(
                nameof(ItemSpacing),
                typeof(double),
                typeof(DaisyWeatherForecast),
                new PropertyMetadata(MinItemSpacing, OnItemSpacingChanged));

        /// <summary>
        /// The spacing between forecast items.
        /// When AutoFitItems is true, this is automatically calculated to distribute
        /// excess space between items.
        /// </summary>
        public double ItemSpacing
        {
            get => (double)GetValue(ItemSpacingProperty);
            set => SetValue(ItemSpacingProperty, value);
        }

        private static void OnItemSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyWeatherForecast control)
                control.ApplySizing();
        }

        #endregion

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Recalculate item widths when container size changes
            if (AutoFitItems && e.NewSize.Width > 0)
            {
                _pendingSizeUpdate = true;
            }
            UpdateScrollIndicators();
        }

        private void OnLayoutUpdated(object? sender, object e)
        {
            // Apply sizing after layout when we have valid dimensions and containers
            if (_pendingSizeUpdate && ActualWidth > 0)
            {
                _pendingSizeUpdate = false;
                ApplySizingDeferred();
            }
            UpdateScrollIndicators();
        }

        private void ApplySizing()
        {
            UpdateSizeVisualState();
            _pendingSizeUpdate = true;
        }

        private void ApplySizingDeferred()
        {
            var availableWidth = GetAvailableWidth();
            if (AutoFitItems && availableWidth > 0 && Items.Count > 0)
            {
                // Calculate optimal item width and spacing to fit all items
                var (itemWidth, itemSpacing, effectiveSize) = CalculateItemSizing(availableWidth);
                ItemSpacing = itemSpacing;
                PropagrateSizeToItems(itemWidth, effectiveSize);
            }
            else
            {
                // Use visual state-based sizing
                PropagrateSizeToItems(null, Size);
            }
            UpdateScrollIndicators();
        }

        // Absolute minimums for readability - these can NEVER be violated
        private const double AbsoluteMinIconSize = 12;
        private const double AbsoluteMinFontSize = 8;
        private const double AbsoluteMinPadding = 4;
        // Minimum item width for 3-digit Fahrenheit temps ("102°|95°" ≈ 46px) + 2*padding
        private const double AbsoluteMinItemWidth = 48;

        /// <summary>
        /// Calculates the minimum width required to display all items at absolute minimum sizing.
        /// Developers should ensure their container is at least this wide.
        /// </summary>
        public double MinimumRequiredWidth
        {
            get
            {
                int count = Items.Count;
                if (count == 0) return 0;
                return (AbsoluteMinItemWidth * count) + (MinItemSpacing * (count - 1));
            }
        }

        private double GetAvailableWidth()
        {
            if (_scrollViewer != null && _scrollViewer.ViewportWidth > 0)
                return _scrollViewer.ViewportWidth;

            double paddingWidth = Padding.Left + Padding.Right;
            double borderWidth = BorderThickness.Left + BorderThickness.Right;
            double availableWidth = ActualWidth - paddingWidth - borderWidth;

            if (availableWidth <= 0 && ActualWidth > 0)
                availableWidth = ActualWidth;

            return availableWidth;
        }

        private (double ItemWidth, double ItemSpacing, DaisySize EffectiveSize) CalculateItemSizing(double availableWidth)
        {
            int itemCount = Items.Count;
            if (itemCount <= 0 || availableWidth <= 0)
                return (double.NaN, MinItemSpacing, Size);

            // Favor internal width (padding) over large gaps. 
            // Keep spacing fixed at minimum unless manually overridden.
            double finalSpacing = MinItemSpacing;

            // Calculate total width if we use target widths and minimum spacing
            double totalSpacing = finalSpacing * (itemCount - 1);

            // Distribute all remaining space to the items to create a solid dashboard look
            double finalItemWidth = (availableWidth - totalSpacing) / itemCount;
            
            DaisySize effectiveSize = Size;

            // If items would be too small, hit the floor and enable scrolling
            if (finalItemWidth < AbsoluteMinItemWidth)
            {
                finalItemWidth = AbsoluteMinItemWidth;
            }

            // Cap the effective size at the requested Size.
            // Items can scale DOWN to fit width, but should not scale UP beyond requested global size.
            if ((int)effectiveSize > (int)Size)
            {
                effectiveSize = Size;
            }

            // Update effective size based on resulting width for styling (downscaling only)
            if (finalItemWidth < 60 && effectiveSize > DaisySize.ExtraSmall) effectiveSize = DaisySize.ExtraSmall;
            else if (finalItemWidth < 70 && effectiveSize > DaisySize.Small) effectiveSize = DaisySize.Small;
            else if (finalItemWidth < 80 && effectiveSize > DaisySize.Medium) effectiveSize = DaisySize.Medium;

            return (finalItemWidth, finalSpacing, effectiveSize);
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

        private void PropagrateSizeToItems(double? calculatedWidth, DaisySize effectiveSize)
        {
            // Update properties on all existing item containers
            for (int i = 0; i < Items.Count; i++)
            {
                if (ContainerFromIndex(i) is DaisyWeatherForecastItem container)
                {
                    // Use effective size (may be scaled down by AutoFitItems)
                    container.Size = effectiveSize;
                    container.TemperatureUnit = TemperatureUnit;
                    container.ShowPrecipitation = ShowPrecipitation;

                    // Apply calculated width if AutoFitItems is enabled
                    if (calculatedWidth.HasValue && !double.IsNaN(calculatedWidth.Value))
                    {
                        container.ItemWidth = calculatedWidth.Value;
                    }
                    else
                    {
                        container.ItemWidth = double.NaN; // Use visual state sizing
                    }
                }
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateSizeVisualState();
            _pendingSizeUpdate = true;

            _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
            _leftScrollButton = GetTemplateChild("PART_LeftScrollButton") as ButtonBase;
            _rightScrollButton = GetTemplateChild("PART_RightScrollButton") as ButtonBase;

            if (_scrollViewer != null)
            {
                _scrollViewer.ViewChanged += (s, e) => UpdateScrollIndicators();
            }

            if (_leftScrollButton != null)
                _leftScrollButton.Click += (s, e) => Scroll(-1);
            
            if (_rightScrollButton != null)
                _rightScrollButton.Click += (s, e) => Scroll(1);

            UpdateScrollIndicators();
        }

        private void Scroll(int direction)
        {
            if (_scrollViewer == null) return;
            
            double offset = _scrollViewer.HorizontalOffset;
            double amount = _scrollViewer.ViewportWidth * 0.75;
            
            _scrollViewer.ChangeView(offset + (amount * direction), null, null, false); // Added 'false' for animation
        }

        private void UpdateScrollIndicators()
        {
            if (_scrollViewer == null) return;

            // Left indicator: show if scrolled past start
            if (_leftScrollButton != null)
            {
                _leftScrollButton.Visibility = _scrollViewer.HorizontalOffset > 0.5 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            // Right indicator: show if scrollable content remains
            if (_rightScrollButton != null)
            {
                // Tolerance of 0.5px for float inaccuracies
                bool canScrollRight = _scrollViewer.ExtentWidth > (_scrollViewer.ViewportWidth + _scrollViewer.HorizontalOffset + 0.5);
                _rightScrollButton.Visibility = canScrollRight 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is DaisyWeatherForecastItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DaisyWeatherForecastItem();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is DaisyWeatherForecastItem container && item is ForecastDay forecast)
            {
                container.DayName = forecast.DayName;
                container.Condition = forecast.Condition;
                container.HighTemperature = forecast.HighTemperature;
                container.LowTemperature = forecast.LowTemperature;
                container.PrecipitationChance = forecast.ChanceOfPrecipitation;
                container.Size = Size;
                container.TemperatureUnit = TemperatureUnit;
                container.ShowPrecipitation = ShowPrecipitation;

                // Apply calculated sizing if AutoFitItems is enabled and we have valid dimensions
                var availableWidth = GetAvailableWidth();
                if (AutoFitItems && availableWidth > 0 && Items.Count > 0)
                {
                    var (itemWidth, _, effectiveSize) = CalculateItemSizing(availableWidth);
                    container.Size = effectiveSize;
                    if (!double.IsNaN(itemWidth))
                    {
                        container.ItemWidth = itemWidth;
                    }
                }
            }
        }
    }
}
