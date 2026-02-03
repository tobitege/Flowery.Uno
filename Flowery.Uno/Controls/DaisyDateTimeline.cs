using System;
using System.Collections.Generic;
using System.Globalization;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// Defines the strategy for disabling dates in the timeline.
    /// </summary>
    public enum DateDisableStrategy
    {
        None,
        BeforeToday,
        AfterToday,
        BeforeDate,
        AfterDate,
        All
    }

    /// <summary>
    /// Defines which elements to show in each date item.
    /// </summary>
    [Flags]
    public enum DateElementDisplay
    {
        DayName = 1,
        DayNumber = 2,
        MonthName = 4,
        Default = DayName | DayNumber | MonthName,
        Compact = DayName | DayNumber,
        NumberOnly = DayNumber
    }

    /// <summary>
    /// Defines how the selected date is positioned after selection or initial load.
    /// </summary>
    public enum DateSelectionMode
    {
        None,
        AlwaysFirst,
        AutoCenter
    }

    /// <summary>
    /// Defines the visual style of the date timeline header.
    /// </summary>
    public enum DateTimelineHeaderType
    {
        None,
        MonthYear,
        Switcher
    }

    /// <summary>
    /// Defines the layout orientation of date items.
    /// </summary>
    public enum DateItemLayout
    {
        Vertical,
        Horizontal
    }

    /// <summary>
    /// Represents a marked date with an optional tooltip text.
    /// </summary>
    public class DateMarker
    {
        public DateTime Date { get; set; }
        public string Text { get; set; } = string.Empty;

        public DateMarker(DateTime date, string text = "")
        {
            Date = date.Date;
            Text = text;
        }

        public DateMarker() { }
    }

    /// <summary>
    /// A horizontal scrollable date timeline picker inspired by easy_date_timeline.
    /// Displays dates in a horizontal strip with customizable appearance.
    /// </summary>
    public partial class DaisyDateTimeline : DaisyBaseContentControl
    {
        private const double DragThreshold = 5.0;

        private StackPanel? _rootPanel;
        private Grid? _headerGrid;
        private DaisyButton? _previousButton;
        private DaisyButton? _nextButton;
        private TextBlock? _headerTextBlock;
        private ScrollViewer? _scrollViewer;
        private StackPanel? _itemsPanel;

        private bool _isDragging;
        private bool _hasDragged;
        private Windows.Foundation.Point _dragStartPoint;
        private double _dragStartOffsetX;
        private double _effectiveItemWidth;
        private bool _isItemWidthInternalUpdate;
        private bool _hasExplicitItemWidth;
        private double _effectiveItemSpacing;
        private bool _isItemSpacingInternalUpdate;
        private bool _hasExplicitItemSpacing;
        public DaisyDateTimeline()
        {
            KeyDown += OnKeyDown;
            PointerWheelChanged += OnPointerWheelChanged;
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += OnPointerCaptureLost;

            IsTabStop = true;
            TabFocusNavigation = KeyboardNavigationMode.Local;

            UpdateHeaderText();
            UpdateAutomationProperties();
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplyAll();
            ScrollToSelectedDate(defer: true);
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

        #region Dependency Properties

        public static readonly DependencyProperty FirstDateProperty =
            DependencyProperty.Register(
                nameof(FirstDate),
                typeof(DateTime),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DateTime.Today.AddMonths(-1), OnRebuildRequested));

        public DateTime FirstDate
        {
            get => (DateTime)GetValue(FirstDateProperty);
            set => SetValue(FirstDateProperty, value);
        }

        public static readonly DependencyProperty LastDateProperty =
            DependencyProperty.Register(
                nameof(LastDate),
                typeof(DateTime),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DateTime.Today.AddMonths(3), OnRebuildRequested));

        public DateTime LastDate
        {
            get => (DateTime)GetValue(LastDateProperty);
            set => SetValue(LastDateProperty, value);
        }

        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(
                nameof(SelectedDate),
                typeof(DateTime?),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DateTime.Today, OnSelectedDateChanged));

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(
                nameof(ItemWidth),
                typeof(double),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(64.0, OnItemWidthChanged));

        public double ItemWidth
        {
            get => (double)GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        public static readonly DependencyProperty ItemSpacingProperty =
            DependencyProperty.Register(
                nameof(ItemSpacing),
                typeof(double),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(8.0, OnItemSpacingChanged));

        public double ItemSpacing
        {
            get => (double)GetValue(ItemSpacingProperty);
            set => SetValue(ItemSpacingProperty, value);
        }

        public static readonly DependencyProperty DisplayElementsProperty =
            DependencyProperty.Register(
                nameof(DisplayElements),
                typeof(DateElementDisplay),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DateElementDisplay.Default, OnRebuildRequested));

        public DateElementDisplay DisplayElements
        {
            get => (DateElementDisplay)GetValue(DisplayElementsProperty);
            set => SetValue(DisplayElementsProperty, value);
        }

        public static readonly DependencyProperty DisableStrategyProperty =
            DependencyProperty.Register(
                nameof(DisableStrategy),
                typeof(DateDisableStrategy),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DateDisableStrategy.None, OnRebuildRequested));

        public DateDisableStrategy DisableStrategy
        {
            get => (DateDisableStrategy)GetValue(DisableStrategyProperty);
            set => SetValue(DisableStrategyProperty, value);
        }

        public static readonly DependencyProperty DisableBeforeDateProperty =
            DependencyProperty.Register(
                nameof(DisableBeforeDate),
                typeof(DateTime?),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(null, OnRebuildRequested));

        public DateTime? DisableBeforeDate
        {
            get => (DateTime?)GetValue(DisableBeforeDateProperty);
            set => SetValue(DisableBeforeDateProperty, value);
        }

        public static readonly DependencyProperty DisableAfterDateProperty =
            DependencyProperty.Register(
                nameof(DisableAfterDate),
                typeof(DateTime?),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(null, OnRebuildRequested));

        public DateTime? DisableAfterDate
        {
            get => (DateTime?)GetValue(DisableAfterDateProperty);
            set => SetValue(DisableAfterDateProperty, value);
        }

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(
                nameof(SelectionMode),
                typeof(DateSelectionMode),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DateSelectionMode.AutoCenter));

        public DateSelectionMode SelectionMode
        {
            get => (DateSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public static readonly DependencyProperty HeaderTypeProperty =
            DependencyProperty.Register(
                nameof(HeaderType),
                typeof(DateTimelineHeaderType),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DateTimelineHeaderType.MonthYear, OnHeaderChanged));

        public DateTimelineHeaderType HeaderType
        {
            get => (DateTimelineHeaderType)GetValue(HeaderTypeProperty);
            set => SetValue(HeaderTypeProperty, value);
        }

        public static readonly DependencyProperty LocaleProperty =
            DependencyProperty.Register(
                nameof(Locale),
                typeof(CultureInfo),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(CultureInfo.CurrentCulture, OnRebuildRequested));

        public CultureInfo Locale
        {
            get => (CultureInfo)GetValue(LocaleProperty);
            set => SetValue(LocaleProperty, value);
        }

        public static readonly DependencyProperty ShowTodayHighlightProperty =
            DependencyProperty.Register(
                nameof(ShowTodayHighlight),
                typeof(bool),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(true, OnRebuildRequested));

        public bool ShowTodayHighlight
        {
            get => (bool)GetValue(ShowTodayHighlightProperty);
            set => SetValue(ShowTodayHighlightProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DaisySize.Medium, OnRebuildRequested));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ItemLayoutProperty =
            DependencyProperty.Register(
                nameof(ItemLayout),
                typeof(DateItemLayout),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(DateItemLayout.Vertical, OnRebuildRequested));

        public DateItemLayout ItemLayout
        {
            get => (DateItemLayout)GetValue(ItemLayoutProperty);
            set => SetValue(ItemLayoutProperty, value);
        }

        public static readonly DependencyProperty MarkedDatesProperty =
            DependencyProperty.Register(
                nameof(MarkedDates),
                typeof(IList<DateMarker>),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(null, OnRebuildRequested));

        public IList<DateMarker>? MarkedDates
        {
            get => (IList<DateMarker>?)GetValue(MarkedDatesProperty);
            set => SetValue(MarkedDatesProperty, value);
        }

        public static readonly DependencyProperty ScrollBarVisibilityProperty =
            DependencyProperty.Register(
                nameof(ScrollBarVisibility),
                typeof(ScrollBarVisibility),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(ScrollBarVisibility.Hidden, OnHeaderChanged));

        public ScrollBarVisibility ScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(ScrollBarVisibilityProperty);
            set => SetValue(ScrollBarVisibilityProperty, value);
        }

        public static readonly DependencyProperty AutoWidthProperty =
            DependencyProperty.Register(
                nameof(AutoWidth),
                typeof(bool),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(false, OnHeaderChanged));

        public bool AutoWidth
        {
            get => (bool)GetValue(AutoWidthProperty);
            set => SetValue(AutoWidthProperty, value);
        }

        public static readonly DependencyProperty VisibleDaysCountProperty =
            DependencyProperty.Register(
                nameof(VisibleDaysCount),
                typeof(int),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(7, OnHeaderChanged));

        public int VisibleDaysCount
        {
            get => (int)GetValue(VisibleDaysCountProperty);
            set => SetValue(VisibleDaysCountProperty, value);
        }

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(
                nameof(HeaderText),
                typeof(string),
                typeof(DaisyDateTimeline),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets the header text showing the current month and year.
        /// </summary>
        public string HeaderText
        {
            get => (string)GetValue(HeaderTextProperty);
            private set => SetValue(HeaderTextProperty, value);
        }

        #endregion

        #region Date Formatting Helpers

        public string SelectedDateLong => SelectedDate?.ToString("D", Locale) ?? string.Empty;
        public string SelectedDateShort => SelectedDate?.ToString("d", Locale) ?? string.Empty;
        public string SelectedDateIso => SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        public string SelectedDateMonthDay => SelectedDate?.ToString("MMMM d", Locale) ?? string.Empty;

        #endregion

        #region Events

        public event EventHandler<DateTime>? DateChanged;
        public event EventHandler<DateTime>? DateClicked;
        public event EventHandler<DateTime>? DateConfirmed;
        public event EventHandler? EscapePressed;

        #endregion

        private static void OnRebuildRequested(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDateTimeline timeline)
            {
                timeline.ApplyAll();
                timeline.ScrollToSelectedDate(defer: true);
            }
        }

        private static void OnItemWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDateTimeline timeline)
            {
                if (!timeline._isItemWidthInternalUpdate)
                    timeline._hasExplicitItemWidth = true;

                timeline.ApplyAll();
                timeline.ScrollToSelectedDate(defer: true);
            }
        }

        private static void OnItemSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDateTimeline timeline)
            {
                if (!timeline._isItemSpacingInternalUpdate)
                    timeline._hasExplicitItemSpacing = true;

                timeline.ApplyAll();
                timeline.ScrollToSelectedDate(defer: true);
            }
        }

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDateTimeline timeline)
            {
                timeline.ApplyAll();
            }
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDateTimeline timeline)
            {
                timeline.UpdateHeaderText();
                timeline.UpdateSelectedStates();
                timeline.ScrollToSelectedDate(defer: false);

                if (e.NewValue is DateTime dt)
                    timeline.DateChanged?.Invoke(timeline, dt);
            }
        }

        private void BuildVisualTree()
        {
            if (_rootPanel != null)
                return;

            _rootPanel = new StackPanel
            {
                Spacing = 12
            };

            _headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            _previousButton = new DaisyButton
            {
                Variant = DaisyButtonVariant.Ghost,
                Shape = DaisyButtonShape.Square,
                Size = DaisySize.Small,
                Visibility = Visibility.Collapsed,
                Content = FloweryPathHelpers.CreateChevron(true),
                IsTabStop = false
            };
            _previousButton.Click += (_, _) =>
            {
                DaisyAccessibility.FocusOnPointer(this);
                OnPreviousMonthClick();
            };
            _headerGrid.Children.Add(_previousButton);
            Grid.SetColumn(_previousButton, 0);

            _headerTextBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            };
            _headerGrid.Children.Add(_headerTextBlock);
            Grid.SetColumn(_headerTextBlock, 1);

            _nextButton = new DaisyButton
            {
                Variant = DaisyButtonVariant.Ghost,
                Shape = DaisyButtonShape.Square,
                Size = DaisySize.Small,
                Visibility = Visibility.Collapsed,
                Content = FloweryPathHelpers.CreateChevron(false),
                IsTabStop = false
            };
            _nextButton.Click += (_, _) =>
            {
                DaisyAccessibility.FocusOnPointer(this);
                OnNextMonthClick();
            };
            _headerGrid.Children.Add(_nextButton);
            Grid.SetColumn(_nextButton, 2);

            _rootPanel.Children.Add(_headerGrid);

            _itemsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility,
                HorizontalScrollMode = ScrollMode.Enabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollMode = ScrollMode.Disabled,
                Content = _itemsPanel
            };
            _scrollViewer.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);
            _rootPanel.Children.Add(_scrollViewer);

            Content = _rootPanel;
        }

        private void ApplyAll()
        {
            BuildVisualTree();

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            ApplyHeader();
            ApplySizing();
            GenerateDateItems();
        }

        private void ApplyHeader()
        {
            if (_headerGrid == null || _headerTextBlock == null || _previousButton == null || _nextButton == null)
                return;

            _headerTextBlock.Text = HeaderText;

            // Header visibility
            _headerGrid.Visibility = HeaderType == DateTimelineHeaderType.None ? Visibility.Collapsed : Visibility.Visible;

            // Nav buttons only on Switcher
            var showNav = HeaderType == DateTimelineHeaderType.Switcher;
            _previousButton.Visibility = showNav ? Visibility.Visible : Visibility.Collapsed;
            _nextButton.Visibility = showNav ? Visibility.Visible : Visibility.Collapsed;

            if (_scrollViewer != null)
                _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility;
        }

        private void ApplySizing()
        {
            if (_headerTextBlock == null)
                return;

            var effectiveSize = GetEffectiveTimelineSize();
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(effectiveSize);
            _effectiveItemWidth = ResolveItemWidth(effectiveSize, sizeKey);
            _effectiveItemSpacing = ResolveItemSpacing();

            // Apply header font size token.
            var headerFont = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}HeaderFontSize", 16);
            _headerTextBlock.FontSize = headerFont;

            // AutoWidth: set Width to exact visible days count
            if (AutoWidth && VisibleDaysCount > 0)
            {
                var itemWidth = _effectiveItemWidth > 0 ? _effectiveItemWidth : ItemWidth;
                var itemSpacing = _effectiveItemSpacing > 0 ? _effectiveItemSpacing : ItemSpacing;
                var calculatedWidth = (VisibleDaysCount * itemWidth) + ((VisibleDaysCount - 1) * itemSpacing);
                if (MinWidth > 0) calculatedWidth = Math.Max(MinWidth, calculatedWidth);
                if (MaxWidth > 0 && MaxWidth < double.MaxValue) calculatedWidth = Math.Min(MaxWidth, calculatedWidth);
                Width = calculatedWidth;
            }
        }

        private DaisySize GetEffectiveTimelineSize()
        {
            return FlowerySizeManager.ShouldIgnoreGlobalSize(this) ? Size : FlowerySizeManager.CurrentSize;
        }

        private double ResolveItemWidth(DaisySize size, string sizeKey)
        {
            if (_hasExplicitItemWidth)
                return ItemWidth;

            var baseWidth = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}ItemWidth", 64);
            var autoWidth = GetAutoItemWidth(size, sizeKey);
            var resolvedWidth = Math.Max(baseWidth, autoWidth);

            if (Math.Abs(ItemWidth - resolvedWidth) > 0.1)
            {
                _isItemWidthInternalUpdate = true;
                try
                {
                    ItemWidth = resolvedWidth;
                }
                finally
                {
                    _isItemWidthInternalUpdate = false;
                }
            }

            return resolvedWidth;
        }

        private double ResolveItemSpacing()
        {
            if (_hasExplicitItemSpacing)
                return ItemSpacing;

            var spacing = DaisyResourceLookup.GetDouble("DaisyDateTimelineItemSpacing", 6);
            if (Math.Abs(ItemSpacing - spacing) > 0.1)
            {
                _isItemSpacingInternalUpdate = true;
                try
                {
                    ItemSpacing = spacing;
                }
                finally
                {
                    _isItemSpacingInternalUpdate = false;
                }
            }

            return spacing;
        }

        private double GetAutoItemWidth(DaisySize size, string sizeKey)
        {
            var padding = DaisyResourceLookup.GetThickness($"DaisyDateTimeline{sizeKey}Padding", new Thickness(8, 12, 8, 12));
            var paddingWidth = padding.Left + padding.Right;
            var borderWidth = 2.0;

            if (ItemLayout == DateItemLayout.Horizontal)
            {
                var dayNameWidth = DisplayElements.HasFlag(DateElementDisplay.DayName) ? GetMaxDayNameWidth(sizeKey) : 0;
                var dayNumberWidth = DisplayElements.HasFlag(DateElementDisplay.DayNumber) ? GetMaxDayNumberWidth(sizeKey) : 0;
                var spacing = DisplayElements.HasFlag(DateElementDisplay.DayName) && DisplayElements.HasFlag(DateElementDisplay.DayNumber)
                    ? DaisyDateTimelineItem.GetHorizontalSpacingForSize(size)
                    : 0;

                return paddingWidth + borderWidth + dayNameWidth + dayNumberWidth + spacing;
            }

            var maxTextWidth = 0.0;
            if (DisplayElements.HasFlag(DateElementDisplay.MonthName))
                maxTextWidth = Math.Max(maxTextWidth, GetMaxMonthNameWidth(sizeKey));
            if (DisplayElements.HasFlag(DateElementDisplay.DayName))
                maxTextWidth = Math.Max(maxTextWidth, GetMaxDayNameWidth(sizeKey));
            if (DisplayElements.HasFlag(DateElementDisplay.DayNumber))
                maxTextWidth = Math.Max(maxTextWidth, GetMaxDayNumberWidth(sizeKey));

            return paddingWidth + borderWidth + maxTextWidth;
        }

        private double GetMaxMonthNameWidth(string sizeKey)
        {
            var fontSize = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}MonthNameFontSize", 10);
            return MeasureMaxTextWidth(GetMonthNameSamples(Locale), fontSize, Microsoft.UI.Text.FontWeights.Medium);
        }

        private double GetMaxDayNameWidth(string sizeKey)
        {
            var fontSize = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}DayNameFontSize", 10);
            return MeasureMaxTextWidth(GetDayNameSamples(Locale), fontSize, Microsoft.UI.Text.FontWeights.Medium);
        }

        private double GetMaxDayNumberWidth(string sizeKey)
        {
            var fontSize = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}DayNumberFontSize", 20);
            return MeasureMaxDayNumberWidth(fontSize);
        }

        private double MeasureMaxDayNumberWidth(double fontSize)
        {
            var maxWidth = 0.0;
            var culture = CultureInfo.CurrentCulture;
            for (var day = 1; day <= 31; day++)
            {
                var width = MeasureTextWidth(day.ToString(culture), fontSize, Microsoft.UI.Text.FontWeights.Bold);
                if (width > maxWidth)
                    maxWidth = width;
            }

            return maxWidth;
        }

        private double MeasureMaxTextWidth(IEnumerable<string> samples, double fontSize, Windows.UI.Text.FontWeight fontWeight)
        {
            var maxWidth = 0.0;
            foreach (var sample in samples)
            {
                var width = MeasureTextWidth(sample, fontSize, fontWeight);
                if (width > maxWidth)
                    maxWidth = width;
            }

            return maxWidth;
        }

        private double MeasureTextWidth(string text, double fontSize, Windows.UI.Text.FontWeight fontWeight)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = fontWeight,
                TextWrapping = TextWrapping.NoWrap
            };

            if (FontFamily != null)
                textBlock.FontFamily = FontFamily;

            textBlock.FontStyle = FontStyle;
            textBlock.FontStretch = FontStretch;
            textBlock.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.DesiredSize.Width;
        }

        private static IEnumerable<string> GetMonthNameSamples(CultureInfo culture)
        {
            for (var month = 1; month <= 12; month++)
                yield return new DateTime(2024, month, 1).ToString("MMM", culture).ToUpperInvariant();
        }

        private static IEnumerable<string> GetDayNameSamples(CultureInfo culture)
        {
            var start = new DateTime(2024, 1, 1);
            for (var day = 0; day < 7; day++)
                yield return start.AddDays(day).ToString("ddd", culture).ToUpperInvariant();
        }

        private void UpdateHeaderText()
        {
            var date = SelectedDate ?? DateTime.Today;
            HeaderText = date.ToString("MMMM yyyy", Locale);
            UpdateAutomationProperties();
        }

        private void UpdateAutomationProperties()
        {
            var name = string.IsNullOrWhiteSpace(HeaderText) ? "Date timeline" : HeaderText;
            AutomationProperties.SetName(this, name);
        }

        private void GenerateDateItems()
        {
            if (_itemsPanel == null)
                return;

            _itemsPanel.Children.Clear();

            var first = FirstDate.Date;
            var last = LastDate.Date;
            var today = DateTime.Today;

            var markerLookup = new Dictionary<DateTime, string>();
            if (MarkedDates != null)
            {
                foreach (var marker in MarkedDates)
                    markerLookup[marker.Date.Date] = marker.Text;
            }

            var effectiveSize = GetEffectiveTimelineSize();
            var itemWidth = _effectiveItemWidth > 0 ? _effectiveItemWidth : ItemWidth;
            var itemSpacing = _effectiveItemSpacing > 0 ? _effectiveItemSpacing : ItemSpacing;

            var current = first;
            while (current <= last)
            {
                markerLookup.TryGetValue(current, out var markerText);

                var item = new DaisyDateTimelineItem
                {
                    Date = current,
                    IsToday = current == today,
                    IsDisabled = IsDateDisabled(current),
                    IsSelected = SelectedDate.HasValue && current == SelectedDate.Value.Date,
                    DayName = current.ToString("ddd", Locale).ToUpperInvariant(),
                    DayNumber = current.Day.ToString(),
                    MonthName = current.ToString("MMM", Locale).ToUpperInvariant(),
                    ShowDayName = DisplayElements.HasFlag(DateElementDisplay.DayName),
                    ShowDayNumber = DisplayElements.HasFlag(DateElementDisplay.DayNumber),
                    ShowMonthName = DisplayElements.HasFlag(DateElementDisplay.MonthName),
                    ShowTodayHighlight = ShowTodayHighlight,
                    Layout = ItemLayout,
                    IsMarked = markerText != null,
                    MarkerText = markerText ?? string.Empty,
                    Size = effectiveSize,
                    Width = itemWidth,
                    Margin = new Thickness(0, 0, itemSpacing, 0)
                };

                item.Tapped += OnDateItemTapped;
                _itemsPanel.Children.Add(item);
                current = current.AddDays(1);
            }

            if (_itemsPanel.Children.Count > 0 && _itemsPanel.Children[^1] is FrameworkElement lastItem)
            {
                lastItem.Margin = new Thickness(0);
            }
        }

        private bool IsDateDisabled(DateTime date)
        {
            return DisableStrategy switch
            {
                DateDisableStrategy.None => false,
                DateDisableStrategy.BeforeToday => date < DateTime.Today,
                DateDisableStrategy.AfterToday => date > DateTime.Today,
                DateDisableStrategy.BeforeDate => DisableBeforeDate.HasValue && date < DisableBeforeDate.Value.Date,
                DateDisableStrategy.AfterDate => DisableAfterDate.HasValue && date > DisableAfterDate.Value.Date,
                DateDisableStrategy.All => true,
                _ => false
            };
        }

        private void OnDateItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is DaisyDateTimelineItem item && !item.IsDisabled)
            {
                SelectedDate = item.Date;
                DateClicked?.Invoke(this, item.Date);
            }
        }

        private void UpdateSelectedStates()
        {
            if (_itemsPanel == null)
                return;

            foreach (var child in _itemsPanel.Children)
            {
                if (child is DaisyDateTimelineItem item)
                {
                    item.IsSelected = SelectedDate.HasValue && item.Date == SelectedDate.Value.Date;
                }
            }
        }

        private void ScrollToSelectedDate(bool defer)
        {
            if (_scrollViewer == null || !SelectedDate.HasValue || SelectionMode == DateSelectionMode.None)
                return;

            void DoScroll()
            {
                var selectedDate = SelectedDate.Value.Date;
                var dayOffset = (selectedDate - FirstDate.Date).Days;
                if (dayOffset < 0)
                    return;

                var itemWidth = _effectiveItemWidth > 0 ? _effectiveItemWidth : ItemWidth;
                var itemSpacing = _effectiveItemSpacing > 0 ? _effectiveItemSpacing : ItemSpacing;
                var itemTotalWidth = itemWidth + itemSpacing;
                var targetOffset = dayOffset * itemTotalWidth;

                double finalOffset = targetOffset;
                if (SelectionMode == DateSelectionMode.AutoCenter)
                {
                    var viewportCenter = _scrollViewer.ViewportWidth / 2;
                    finalOffset = targetOffset - viewportCenter + (itemWidth / 2);
                    finalOffset = Math.Max(0, finalOffset);
                }

                _scrollViewer.ChangeView(finalOffset, null, null, disableAnimation: true);
            }

            if (defer)
            {
                _ = DispatcherQueue.TryEnqueue(DoScroll);
            }
            else
            {
                DoScroll();
            }
        }

        private void OnPreviousMonthClick()
        {
            var current = SelectedDate ?? DateTime.Today;
            var newDate = current.AddMonths(-1);

            if (newDate >= FirstDate)
                SelectedDate = newDate;
            else
                SelectedDate = FirstDate;
        }

        private void OnNextMonthClick()
        {
            var current = SelectedDate ?? DateTime.Today;
            var newDate = current.AddMonths(1);

            if (newDate <= LastDate)
                SelectedDate = newDate;
            else
                SelectedDate = LastDate;
        }

        /// <summary>
        /// Scrolls to and selects the specified date if it's within the valid range.
        /// </summary>
        public void GoToDate(DateTime date)
        {
            if (date >= FirstDate && date <= LastDate)
                SelectedDate = date;
        }

        /// <summary>
        /// Scrolls to and selects today's date if it's within the valid range.
        /// </summary>
        public void GoToToday()
        {
            GoToDate(DateTime.Today);
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Handled)
                return;

            switch (e.Key)
            {
                case VirtualKey.Left:
                    NavigateByDays(-1);
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    NavigateByDays(1);
                    e.Handled = true;
                    break;
                case VirtualKey.Up:
                    NavigateByDays(-7);
                    e.Handled = true;
                    break;
                case VirtualKey.Down:
                    NavigateByDays(7);
                    e.Handled = true;
                    break;
                case VirtualKey.Home:
                    SelectFirstAvailableDate();
                    e.Handled = true;
                    break;
                case VirtualKey.End:
                    SelectLastAvailableDate();
                    e.Handled = true;
                    break;
                case VirtualKey.PageUp:
                    OnPreviousMonthClick();
                    e.Handled = true;
                    break;
                case VirtualKey.PageDown:
                    OnNextMonthClick();
                    e.Handled = true;
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    if (SelectedDate.HasValue)
                        DateConfirmed?.Invoke(this, SelectedDate.Value);
                    e.Handled = true;
                    break;
                case VirtualKey.Escape:
                    var today = DateTime.Today;
                    if (today >= FirstDate && today <= LastDate)
                        GoToToday();
                    EscapePressed?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    break;
            }
        }

        private void NavigateByDays(int days)
        {
            var current = SelectedDate ?? DateTime.Today;
            var newDate = current.AddDays(days);

            while (newDate >= FirstDate && newDate <= LastDate && IsDateDisabled(newDate))
            {
                newDate = newDate.AddDays(days > 0 ? 1 : -1);
            }

            if (newDate >= FirstDate && newDate <= LastDate && !IsDateDisabled(newDate))
                SelectedDate = newDate;
        }

        private void SelectFirstAvailableDate()
        {
            var date = FirstDate;
            while (date <= LastDate && IsDateDisabled(date))
                date = date.AddDays(1);
            if (date <= LastDate)
                SelectedDate = date;
        }

        private void SelectLastAvailableDate()
        {
            var date = LastDate;
            while (date >= FirstDate && IsDateDisabled(date))
                date = date.AddDays(-1);
            if (date >= FirstDate)
                SelectedDate = date;
        }

        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (_scrollViewer == null)
                return;

            var point = e.GetCurrentPoint(this);
            if (point.Properties.MouseWheelDelta == 0)
                return;

            var delta = -point.Properties.MouseWheelDelta / 120.0 * 50.0;
            var newOffset = _scrollViewer.HorizontalOffset + delta;
            if (newOffset < 0) newOffset = 0;
            _scrollViewer.ChangeView(newOffset, null, null, disableAnimation: true);
            e.Handled = true;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_scrollViewer == null)
                return;

            var point = e.GetCurrentPoint(this);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            _isDragging = true;
            _hasDragged = false;
            _dragStartPoint = point.Position;
            _dragStartOffsetX = _scrollViewer.HorizontalOffset;
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || _scrollViewer == null)
                return;

            var currentPoint = e.GetCurrentPoint(this).Position;
            var deltaX = Math.Abs(currentPoint.X - _dragStartPoint.X);

            if (!_hasDragged && deltaX > DragThreshold)
            {
                _hasDragged = true;
                CapturePointer(e.Pointer);
            }

            if (_hasDragged)
            {
                var delta = _dragStartPoint.X - currentPoint.X;
                var newOffset = _dragStartOffsetX + delta;
                if (newOffset < 0) newOffset = 0;
                _scrollViewer.ChangeView(newOffset, null, null, disableAnimation: true);
                e.Handled = true;
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging)
                return;

            var wasDragging = _hasDragged;
            _isDragging = false;
            _hasDragged = false;

            if (wasDragging)
            {
                ReleasePointerCapture(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
            _hasDragged = false;
        }
    }

    /// <summary>
    /// Represents a single date item in the DaisyDateTimeline.
    /// </summary>
    public partial class DaisyDateTimelineItem : DaisyBaseContentControl
    {
        private Border? _background;
        private StackPanel? _stack;
        private TextBlock? _monthName;
        private TextBlock? _dayNumber;
        private TextBlock? _dayName;

        private bool _isPointerOver;
        public DaisyDateTimelineItem()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            IsTabStop = false;

            PointerEntered += (_, _) => { _isPointerOver = true; ApplyAll(); };
            PointerExited += (_, _) => { _isPointerOver = false; ApplyAll(); };
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
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

        #region Dependency Properties

        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(nameof(Date), typeof(DateTime), typeof(DaisyDateTimelineItem), new PropertyMetadata(default(DateTime)));

        public DateTime Date
        {
            get => (DateTime)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(DaisyDateTimelineItem), new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public static readonly DependencyProperty IsTodayProperty =
            DependencyProperty.Register(nameof(IsToday), typeof(bool), typeof(DaisyDateTimelineItem), new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsToday
        {
            get => (bool)GetValue(IsTodayProperty);
            set => SetValue(IsTodayProperty, value);
        }

        public static readonly DependencyProperty IsDisabledProperty =
            DependencyProperty.Register(nameof(IsDisabled), typeof(bool), typeof(DaisyDateTimelineItem), new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsDisabled
        {
            get => (bool)GetValue(IsDisabledProperty);
            set => SetValue(IsDisabledProperty, value);
        }

        public static readonly DependencyProperty DayNameProperty =
            DependencyProperty.Register(nameof(DayName), typeof(string), typeof(DaisyDateTimelineItem), new PropertyMetadata(string.Empty, OnAppearanceChanged));

        public string DayName
        {
            get => (string)GetValue(DayNameProperty);
            set => SetValue(DayNameProperty, value);
        }

        public static readonly DependencyProperty DayNumberProperty =
            DependencyProperty.Register(nameof(DayNumber), typeof(string), typeof(DaisyDateTimelineItem), new PropertyMetadata(string.Empty, OnAppearanceChanged));

        public string DayNumber
        {
            get => (string)GetValue(DayNumberProperty);
            set => SetValue(DayNumberProperty, value);
        }

        public static readonly DependencyProperty MonthNameProperty =
            DependencyProperty.Register(nameof(MonthName), typeof(string), typeof(DaisyDateTimelineItem), new PropertyMetadata(string.Empty, OnAppearanceChanged));

        public string MonthName
        {
            get => (string)GetValue(MonthNameProperty);
            set => SetValue(MonthNameProperty, value);
        }

        public static readonly DependencyProperty ShowDayNameProperty =
            DependencyProperty.Register(nameof(ShowDayName), typeof(bool), typeof(DaisyDateTimelineItem), new PropertyMetadata(true, OnAppearanceChanged));

        public bool ShowDayName
        {
            get => (bool)GetValue(ShowDayNameProperty);
            set => SetValue(ShowDayNameProperty, value);
        }

        public static readonly DependencyProperty ShowDayNumberProperty =
            DependencyProperty.Register(nameof(ShowDayNumber), typeof(bool), typeof(DaisyDateTimelineItem), new PropertyMetadata(true, OnAppearanceChanged));

        public bool ShowDayNumber
        {
            get => (bool)GetValue(ShowDayNumberProperty);
            set => SetValue(ShowDayNumberProperty, value);
        }

        public static readonly DependencyProperty ShowMonthNameProperty =
            DependencyProperty.Register(nameof(ShowMonthName), typeof(bool), typeof(DaisyDateTimelineItem), new PropertyMetadata(true, OnAppearanceChanged));

        public bool ShowMonthName
        {
            get => (bool)GetValue(ShowMonthNameProperty);
            set => SetValue(ShowMonthNameProperty, value);
        }

        public static readonly DependencyProperty ShowTodayHighlightProperty =
            DependencyProperty.Register(nameof(ShowTodayHighlight), typeof(bool), typeof(DaisyDateTimelineItem), new PropertyMetadata(true, OnAppearanceChanged));

        public bool ShowTodayHighlight
        {
            get => (bool)GetValue(ShowTodayHighlightProperty);
            set => SetValue(ShowTodayHighlightProperty, value);
        }

        public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register(nameof(Layout), typeof(DateItemLayout), typeof(DaisyDateTimelineItem), new PropertyMetadata(DateItemLayout.Vertical, OnAppearanceChanged));

#if __ANDROID__
        public new DateItemLayout Layout
#else
        public DateItemLayout Layout
#endif
        {
            get => (DateItemLayout)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        public static readonly DependencyProperty IsMarkedProperty =
            DependencyProperty.Register(nameof(IsMarked), typeof(bool), typeof(DaisyDateTimelineItem), new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsMarked
        {
            get => (bool)GetValue(IsMarkedProperty);
            set => SetValue(IsMarkedProperty, value);
        }

        public static readonly DependencyProperty MarkerTextProperty =
            DependencyProperty.Register(nameof(MarkerText), typeof(string), typeof(DaisyDateTimelineItem), new PropertyMetadata(string.Empty, OnAppearanceChanged));

        public string MarkerText
        {
            get => (string)GetValue(MarkerTextProperty);
            set => SetValue(MarkerTextProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(DaisySize), typeof(DaisyDateTimelineItem), new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDateTimelineItem item)
                item.ApplyAll();
        }

        #endregion

        private void BuildVisualTree()
        {
            if (_background != null)
                return;

            _monthName = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Medium,
                Opacity = 0.7
            };

            _dayNumber = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            _dayName = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Medium,
                Opacity = 0.7
            };

            _stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 2
            };
            _stack.Children.Add(_monthName);
            _stack.Children.Add(_dayNumber);
            _stack.Children.Add(_dayName);

            _background = new Border
            {
                Child = _stack,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            Content = _background;
        }

        internal static double GetHorizontalSpacingForSize(DaisySize size)
        {
            return size switch
            {
                DaisySize.ExtraSmall => 3,
                DaisySize.Small => 4,
                DaisySize.Medium => 5,
                DaisySize.Large => 6,
                DaisySize.ExtraLarge => 6,
                _ => 5
            };
        }

        private void ApplyAll()
        {
            if (_background == null || _monthName == null || _dayNumber == null || _dayName == null || _stack == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            _monthName.Text = MonthName;
            _dayNumber.Text = DayNumber;
            _dayName.Text = DayName;

            _monthName.Visibility = ShowMonthName ? Visibility.Visible : Visibility.Collapsed;
            _dayNumber.Visibility = ShowDayNumber ? Visibility.Visible : Visibility.Collapsed;
            _dayName.Visibility = ShowDayName ? Visibility.Visible : Visibility.Collapsed;

            // Base style
            _background.Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _background.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            _background.BorderThickness = new Thickness(1);
            _background.Opacity = IsDisabled ? 0.4 : 1.0;

            var baseContent = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            _monthName.Foreground = baseContent;
            _dayName.Foreground = baseContent;
            _dayNumber.Foreground = baseContent;

            // Layout tweaks
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            if (Layout == DateItemLayout.Horizontal)
            {
                _stack.Orientation = Orientation.Horizontal;
                _stack.Spacing = GetHorizontalSpacingForSize(Size);
                _monthName.Visibility = Visibility.Collapsed;

                _dayName.FontSize = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}DayNameFontSize", 10);
                _dayName.FontWeight = Microsoft.UI.Text.FontWeights.Medium;
                _dayName.VerticalAlignment = VerticalAlignment.Center;
                _dayName.Opacity = 0.7;

                _dayNumber.FontSize = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}DayNumberFontSize", 20);
                _dayNumber.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                _dayNumber.VerticalAlignment = VerticalAlignment.Center;

                _background.MinWidth = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}ItemWidth", 64);
                _background.MinHeight = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}Height", 80);
                _background.Padding = DaisyResourceLookup.GetThickness($"DaisyDateTimeline{sizeKey}Padding", new Thickness(8, 12, 8, 12));
                _background.CornerRadius = DaisyResourceLookup.GetCornerRadius($"DaisyDateTimeline{sizeKey}CornerRadius", new CornerRadius(12));
            }
            else
            {
                _stack.Orientation = Orientation.Vertical;
                _stack.Spacing = 2;

                _monthName.FontSize = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}MonthNameFontSize", 10);
                _dayName.FontSize = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}DayNameFontSize", 10);
                _dayNumber.FontSize = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}DayNumberFontSize", 20);

                _background.MinHeight = DaisyResourceLookup.GetDouble($"DaisyDateTimeline{sizeKey}Height", 80);
                _background.Padding = DaisyResourceLookup.GetThickness($"DaisyDateTimeline{sizeKey}Padding", new Thickness(8, 12, 8, 12));
                _background.CornerRadius = DaisyResourceLookup.GetCornerRadius($"DaisyDateTimeline{sizeKey}CornerRadius", new CornerRadius(12));
            }

            // Hover
            if (_isPointerOver && !IsDisabled)
            {
                _background.Background = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
                _background.RenderTransform = new ScaleTransform { ScaleX = 1.02, ScaleY = 1.02 };
                _background.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
            }
            else
            {
                _background.RenderTransform = null;
            }

            // Today highlight (when not selected)
            if (IsToday && ShowTodayHighlight && !IsSelected)
            {
                _background.BorderBrush = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                _background.BorderThickness = new Thickness(2);
            }

            // Marked (when not selected)
            if (IsMarked && !IsSelected)
            {
                _background.Background = DaisyResourceLookup.GetBrush("DaisySecondaryBrush");
                _dayNumber.Foreground = DaisyResourceLookup.GetBrush("DaisySecondaryContentBrush");
                _dayName.Foreground = DaisyResourceLookup.GetBrush("DaisySecondaryContentBrush");
                _monthName.Foreground = DaisyResourceLookup.GetBrush("DaisySecondaryContentBrush");
            }

            // Selected
            if (IsSelected)
            {
                _background.Background = DaisyResourceLookup.GetBrush(_isPointerOver ? "DaisyPrimaryFocusBrush" : "DaisyPrimaryBrush");
                _background.BorderBrush = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                _dayNumber.Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush");
                _dayName.Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush");
                _monthName.Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush");
                _dayName.Opacity = 0.8;
                _monthName.Opacity = 0.8;
            }

            // ToolTip for marked dates with text
            if (IsMarked && !string.IsNullOrWhiteSpace(MarkerText))
            {
                ToolTipService.SetToolTip(this, MarkerText);
            }
            else
            {
                ToolTipService.SetToolTip(this, null);
            }

            IsHitTestVisible = !IsDisabled;
        }
    }
}
