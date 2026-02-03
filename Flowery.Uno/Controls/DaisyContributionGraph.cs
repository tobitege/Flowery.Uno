using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Flowery.Localization;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// A GitHub-style contribution heatmap graph.
    /// </summary>
    public partial class DaisyContributionGraph : DaisyBaseContentControl
    {
        private const int WeeksInYear = 53;
        private const int DaysInWeek = 7;

        private Grid? _rootGrid;
        private ScrollViewer? _scrollViewer;
        private Grid? _layoutGrid;
        private Grid? _monthHeadersGrid;
        private StackPanel? _dayLabelsPanel;
        private Grid? _cellsGrid;
        private StackPanel? _legendPanel;

        private double _cellStepX;
        private double _cellStepY;
        private double _gridWidth;
        private double _gridHeight;

        // Flag to suppress rebuilds during ApplySizing to avoid intermediate inconsistent states
        private bool _suppressRebuild;
        public DaisyContributionGraph()
        {
            // Use the current culture's first day of week (respects regional settings)
            StartDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplySizing();
            Rebuild();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            Rebuild();
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

        #region Computed Sizes

        /// <summary>
        /// Gets the horizontal distance between week columns (cell size + horizontal margins).
        /// </summary>
        public double CellStepX => _cellStepX;

        /// <summary>
        /// Gets the vertical distance between day rows (cell size + vertical margins).
        /// </summary>
        public double CellStepY => _cellStepY;

        /// <summary>
        /// Gets the total width of the 53-week grid in pixels.
        /// </summary>
        public double GridWidth => _gridWidth;

        /// <summary>
        /// Gets the total height of the 7-day grid in pixels.
        /// </summary>
        public double GridHeight => _gridHeight;

        #endregion

        #region Contributions
        public static readonly DependencyProperty ContributionsProperty =
            DependencyProperty.Register(
                nameof(Contributions),
                typeof(IEnumerable<DaisyContributionDay>),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(null, OnRebuildRequested));

        public IEnumerable<DaisyContributionDay>? Contributions
        {
            get => (IEnumerable<DaisyContributionDay>?)GetValue(ContributionsProperty);
            set => SetValue(ContributionsProperty, value);
        }
        #endregion

        #region Year
        public static readonly DependencyProperty YearProperty =
            DependencyProperty.Register(
                nameof(Year),
                typeof(int),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(DateTime.Now.Year, OnRebuildRequested));

        public int Year
        {
            get => (int)GetValue(YearProperty);
            set => SetValue(YearProperty, value);
        }
        #endregion

        #region StartDayOfWeek
        public static readonly DependencyProperty StartDayOfWeekProperty =
            DependencyProperty.Register(
                nameof(StartDayOfWeek),
                typeof(DayOfWeek),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(DayOfWeek.Monday, OnRebuildRequested));

        public DayOfWeek StartDayOfWeek
        {
            get => (DayOfWeek)GetValue(StartDayOfWeekProperty);
            set => SetValue(StartDayOfWeekProperty, value);
        }
        #endregion

        #region ShowLegend
        public static readonly DependencyProperty ShowLegendProperty =
            DependencyProperty.Register(
                nameof(ShowLegend),
                typeof(bool),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(true, OnRebuildRequested));

        public bool ShowLegend
        {
            get => (bool)GetValue(ShowLegendProperty);
            set => SetValue(ShowLegendProperty, value);
        }
        #endregion

        #region ShowToolTips
        public static readonly DependencyProperty ShowToolTipsProperty =
            DependencyProperty.Register(
                nameof(ShowToolTips),
                typeof(bool),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(true, OnRebuildRequested));

        public bool ShowToolTips
        {
            get => (bool)GetValue(ShowToolTipsProperty);
            set => SetValue(ShowToolTipsProperty, value);
        }
        #endregion

        #region ShowMonthLabels
        public static readonly DependencyProperty ShowMonthLabelsProperty =
            DependencyProperty.Register(
                nameof(ShowMonthLabels),
                typeof(bool),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(true, OnRebuildRequested));

        public bool ShowMonthLabels
        {
            get => (bool)GetValue(ShowMonthLabelsProperty);
            set => SetValue(ShowMonthLabelsProperty, value);
        }
        #endregion

        #region ShowDayLabels
        public static readonly DependencyProperty ShowDayLabelsProperty =
            DependencyProperty.Register(
                nameof(ShowDayLabels),
                typeof(bool),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(true, OnRebuildRequested));

        public bool ShowDayLabels
        {
            get => (bool)GetValue(ShowDayLabelsProperty);
            set => SetValue(ShowDayLabelsProperty, value);
        }
        #endregion

        #region HighlightMonthStartBorders
        public static readonly DependencyProperty HighlightMonthStartBordersProperty =
            DependencyProperty.Register(
                nameof(HighlightMonthStartBorders),
                typeof(bool),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(false, OnRebuildRequested));

        public bool HighlightMonthStartBorders
        {
            get => (bool)GetValue(HighlightMonthStartBordersProperty);
            set => SetValue(HighlightMonthStartBordersProperty, value);
        }
        #endregion

        #region CellSize
        public static readonly DependencyProperty CellSizeProperty =
            DependencyProperty.Register(
                nameof(CellSize),
                typeof(double),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(12.0, OnRebuildRequested));

        public double CellSize
        {
            get => (double)GetValue(CellSizeProperty);
            set => SetValue(CellSizeProperty, ValueCoercion.PixelSize(value, min: 4.0, max: 32.0));
        }
        #endregion

        #region CellMargin
        public static readonly DependencyProperty CellMarginProperty =
            DependencyProperty.Register(
                nameof(CellMargin),
                typeof(Thickness),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(new Thickness(1), OnRebuildRequested));

        public Thickness CellMargin
        {
            get => (Thickness)GetValue(CellMarginProperty);
            set => SetValue(CellMarginProperty, ValueCoercion.Thickness(value, min: 0.0, max: 8.0));
        }
        #endregion

        #region CellCornerRadius
        public static readonly DependencyProperty CellCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CellCornerRadius),
                typeof(CornerRadius),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(new CornerRadius(2), OnRebuildRequested));

        public CornerRadius CellCornerRadius
        {
            get => (CornerRadius)GetValue(CellCornerRadiusProperty);
            set => SetValue(CellCornerRadiusProperty, ValueCoercion.CornerRadius(value, min: 0.0, max: CellSize / 2));
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyContributionGraph),
                new PropertyMetadata(DaisySize.Medium, OnSizeChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyContributionGraph graph)
            {
                graph.ApplySizing();
                graph.Rebuild();
            }
        }

        private void ApplySizing()
        {
            // Suppress intermediate rebuilds - we'll rebuild once at the end with consistent values
            _suppressRebuild = true;

            try
            {
                // Map DaisySize to CellSize and related properties
                var newCellSize = Size switch
                {
                    DaisySize.ExtraSmall => 8.0,
                    DaisySize.Small => 10.0,
                    DaisySize.Medium => 12.0,
                    DaisySize.Large => 14.0,
                    DaisySize.ExtraLarge => 16.0,
                    _ => 12.0
                };

                var cellMargin = Size switch
                {
                    DaisySize.ExtraSmall => 1.0,  // Use whole pixels to avoid cumulative rounding drift
                    DaisySize.Small => 1.0,
                    DaisySize.Medium => 1.0,
                    DaisySize.Large => 2.0,       // Larger sizes can have 2px gap
                    DaisySize.ExtraLarge => 2.0,
                    _ => 1.0
                };

                var cornerRadius = Size switch
                {
                    DaisySize.ExtraSmall => 1.0,
                    DaisySize.Small => 2.0,
                    DaisySize.Medium => 2.0,
                    DaisySize.Large => 3.0,
                    DaisySize.ExtraLarge => 3.0,
                    _ => 2.0
                };

                // Apply the computed values (order doesn't matter now since rebuilds are suppressed)
                SetValue(CellSizeProperty, newCellSize);
                SetValue(CellMarginProperty, new Thickness(cellMargin));
                SetValue(CellCornerRadiusProperty, new CornerRadius(cornerRadius));
            }
            finally
            {
                _suppressRebuild = false;
            }
        }
        #endregion

        public ObservableCollection<DaisyContributionGraphCell> Cells { get; } = [];
        public ObservableCollection<DaisyContributionMonthHeader> MonthHeaders { get; } = [];
        public ObservableCollection<DaisyContributionDayLabel> DayLabels { get; } = [];

        private static void OnRebuildRequested(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyContributionGraph graph && !graph._suppressRebuild)
                graph.Rebuild();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid();

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _layoutGrid = new Grid
            {
                // Add padding for scrollbar room (bottom) and last column visibility (right)
                Padding = new Thickness(0, 0, 8, 16)
            };
            _layoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _layoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _layoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _layoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Use Grid instead of Canvas for month headers to guarantee column alignment with cells
            _monthHeadersGrid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
            for (int c = 0; c < WeeksInYear; c++)
                _monthHeadersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(_monthHeadersGrid, 0);
            Grid.SetColumn(_monthHeadersGrid, 1);
            _layoutGrid.Children.Add(_monthHeadersGrid);

            _dayLabelsPanel = new StackPanel { Orientation = Orientation.Vertical };
            Grid.SetRow(_dayLabelsPanel, 1);
            Grid.SetColumn(_dayLabelsPanel, 0);
            _layoutGrid.Children.Add(_dayLabelsPanel);

            _cellsGrid = new Grid();
            for (int r = 0; r < DaysInWeek; r++)
                _cellsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < WeeksInYear; c++)
                _cellsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(_cellsGrid, 1);
            Grid.SetColumn(_cellsGrid, 1);
            _layoutGrid.Children.Add(_cellsGrid);

            _scrollViewer.Content = _layoutGrid;

            _legendPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var outer = new StackPanel { Spacing = 8 };
            outer.Children.Add(_scrollViewer);
            outer.Children.Add(_legendPanel);

            _rootGrid.Children.Add(outer);
            Content = _rootGrid;
        }

        private void Rebuild()
        {
            if (_monthHeadersGrid == null || _dayLabelsPanel == null || _cellsGrid == null || _legendPanel == null)
                return;

            UpdateCellSteps();
            RebuildDayLabels();
            RebuildMonthHeaders();
            RebuildCells();
            RebuildVisuals();
        }

        private void UpdateCellSteps()
        {
            var margin = CellMargin;
            _cellStepX = CellSize + margin.Left + margin.Right;
            _cellStepY = CellSize + margin.Top + margin.Bottom;
            _gridWidth = WeeksInYear * _cellStepX;
            _gridHeight = DaysInWeek * _cellStepY;
        }

        private void RebuildDayLabels()
        {
            DayLabels.Clear();

            var format = CultureInfo.CurrentCulture.DateTimeFormat;
            for (int i = 0; i < DaysInWeek; i++)
            {
                var day = (DayOfWeek)(((int)StartDayOfWeek + i) % DaysInWeek);
                var label = i % 2 == 0 ? format.AbbreviatedDayNames[(int)day] : string.Empty;
                DayLabels.Add(new DaisyContributionDayLabel { Text = label });
            }
        }

        private void RebuildMonthHeaders()
        {
            MonthHeaders.Clear();

            var year = Year;
            var format = CultureInfo.CurrentCulture.DateTimeFormat;
            var startDate = new DateTime(year, 1, 1);
            var first = GetFirstGridDate(startDate);
            var lastWeekIndex = -1;

            for (int month = 1; month <= 12; month++)
            {
                var firstOfMonth = new DateTime(year, month, 1);
                var weekIndex = (int)((firstOfMonth.Date - first.Date).TotalDays / DaysInWeek);
                if (weekIndex < 0 || weekIndex >= WeeksInYear)
                    continue;
                if (weekIndex == lastWeekIndex)
                    continue;

                var text = format.AbbreviatedMonthNames[month - 1];
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                MonthHeaders.Add(new DaisyContributionMonthHeader
                {
                    Text = text,
                    WeekIndex = weekIndex,
                    Left = weekIndex * CellStepX
                });

                lastWeekIndex = weekIndex;
            }
        }

        private void RebuildCells()
        {
            Cells.Clear();

            var year = Year;
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31);
            var first = GetFirstGridDate(startDate);

            var map = new Dictionary<DateTime, DaisyContributionDay>();
            if (Contributions != null)
            {
                foreach (var item in Contributions)
                    map[item.Date.Date] = item;
            }

            for (int day = 0; day < DaysInWeek; day++)
            {
                for (int week = 0; week < WeeksInYear; week++)
                {
                    var currentDate = first.AddDays(week * DaysInWeek + day);

                    if (!IsDateInGraphRange(currentDate, startDate, endDate, year))
                    {
                        Cells.Add(new DaisyContributionGraphCell { Date = null, Count = 0, Level = -1, ToolTipText = null });
                        continue;
                    }

                    map.TryGetValue(currentDate.Date, out var data);
                    var count = data?.Count ?? 0;
                    var level = ValueCoercion.Clamp(data?.Level ?? 0, 0, 4);

                    Cells.Add(new DaisyContributionGraphCell
                    {
                        Date = currentDate,
                        Count = count,
                        Level = level,
                        ToolTipText = ShowToolTips ? FormatToolTip(currentDate, count) : null,
                        IsMonthStart = HighlightMonthStartBorders && currentDate.Day == 1
                    });
                }
            }
        }

        private void RebuildVisuals()
        {
            if (_monthHeadersGrid == null || _dayLabelsPanel == null || _cellsGrid == null || _legendPanel == null)
                return;

            var fontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
            var monthHeaderHeight = fontSize + 8; // Extra padding to prevent descender clipping

            _monthHeadersGrid.Children.Clear();
            _monthHeadersGrid.Height = monthHeaderHeight;
            _monthHeadersGrid.Visibility = ShowMonthLabels ? Visibility.Visible : Visibility.Collapsed;

            // Synchronize month header column widths with cells grid columns
            for (int c = 0; c < _monthHeadersGrid.ColumnDefinitions.Count; c++)
            {
                _monthHeadersGrid.ColumnDefinitions[c].Width = new GridLength(CellStepX, GridUnitType.Pixel);
            }

            if (ShowMonthLabels)
            {
                // Place month headers in Grid columns - guarantees alignment with cells
                foreach (var mh in MonthHeaders)
                {
                    var tb = new TextBlock
                    {
                        Text = mh.Text,
                        FontSize = fontSize,
                        Opacity = 0.7,
                        Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                        // Left padding to match cell margin
                        Margin = new Thickness(CellMargin.Left, 0, 0, 0)
                    };
                    Grid.SetColumn(tb, mh.WeekIndex);
                    // Span multiple columns to prevent text clipping (months are typically 4+ weeks apart)
                    Grid.SetColumnSpan(tb, Math.Min(4, WeeksInYear - mh.WeekIndex));
                    _monthHeadersGrid.Children.Add(tb);
                }
            }

            _dayLabelsPanel.Children.Clear();
            _dayLabelsPanel.Visibility = ShowDayLabels ? Visibility.Visible : Visibility.Collapsed;

            // Calculate row height - must be at least enough for the font
            var fontLineHeight = Math.Ceiling(fontSize * 1.5);
            var effectiveRowHeight = Math.Max(CellStepY, fontLineHeight);

            // Calculate extra margin needed per cell to match the effective row height
            var extraVerticalMargin = (effectiveRowHeight - CellStepY) / 2;

            if (ShowDayLabels)
            {
                foreach (var dl in DayLabels)
                {
                    // Wrap TextBlock in a Border to properly center text within row height
                    var container = new Border
                    {
                        Height = effectiveRowHeight,
                        Margin = new Thickness(0, 0, 4, 0),
                        Child = new TextBlock
                        {
                            Text = dl.Text,
                            FontSize = fontSize,
                            Opacity = 0.7,
                            Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    _dayLabelsPanel.Children.Add(container);
                }
            }

            _cellsGrid.Children.Clear();

            // CRITICAL: Set explicit column widths to match CellStepX exactly.
            // Grid.Auto sizing doesn't guarantee exact widths, causing month header drift.
            // This ensures columns match the Canvas-based month header positioning.
            for (int c = 0; c < _cellsGrid.ColumnDefinitions.Count; c++)
            {
                _cellsGrid.ColumnDefinitions[c].Width = new GridLength(CellStepX, GridUnitType.Pixel);
            }

            // Set explicit row heights for consistency
            for (int r = 0; r < _cellsGrid.RowDefinitions.Count; r++)
            {
                _cellsGrid.RowDefinitions[r].Height = new GridLength(effectiveRowHeight, GridUnitType.Pixel);
            }

            // Width is now determined by column definitions (53 * CellStepX)
            _cellsGrid.ClearValue(FrameworkElement.MinWidthProperty);
            _cellsGrid.ClearValue(FrameworkElement.HeightProperty);

            for (int i = 0; i < Cells.Count; i++)
            {
                var cell = Cells[i];
                var border = CreateCellBorder(cell);
                // Add extra margin to center cells within the larger row height
                if (extraVerticalMargin > 0)
                {
                    var currentMargin = border.Margin;
                    border.Margin = new Thickness(
                        currentMargin.Left,
                        currentMargin.Top + extraVerticalMargin,
                        currentMargin.Right,
                        currentMargin.Bottom + extraVerticalMargin);
                }
                Grid.SetRow(border, i / WeeksInYear);
                Grid.SetColumn(border, i % WeeksInYear);
                _cellsGrid.Children.Add(border);
            }

            _legendPanel.Children.Clear();
            _legendPanel.Visibility = ShowLegend ? Visibility.Visible : Visibility.Collapsed;
            if (ShowLegend)
            {
                _legendPanel.Children.Add(new TextBlock
                {
                    Text = FloweryLocalization.GetStringInternal("ContributionGraph_Less", "Less"),
                    FontSize = fontSize,
                    Opacity = 0.7,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var swatches = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
                for (int i = 0; i <= 4; i++)
                {
                    swatches.Children.Add(CreateLegendSwatch(i));
                }
                _legendPanel.Children.Add(swatches);

                _legendPanel.Children.Add(new TextBlock
                {
                    Text = FloweryLocalization.GetStringInternal("ContributionGraph_More", "More"),
                    FontSize = fontSize,
                    Opacity = 0.7,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
        }

        private Border CreateLegendSwatch(int level)
        {
            var cell = new DaisyContributionGraphCell { Date = DateTime.Today, Level = level, IsMonthStart = false };
            var border = CreateCellBorder(cell);
            border.Width = 10;
            border.Height = 10;
            border.Margin = new Thickness(0);
            border.CornerRadius = new CornerRadius(2);
            return border;
        }

        private Border CreateCellBorder(DaisyContributionGraphCell cell)
        {
            var border = new Border
            {
                Width = CellSize,
                Height = CellSize,
                Margin = CellMargin,
                CornerRadius = CellCornerRadius,
                BorderThickness = new Thickness(0)
            };

            if (cell.Level < 0)
            {
                border.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                border.Opacity = 0;
                return border;
            }

            ApplyCellColors(border, cell.Level);

            if (cell.IsMonthStart)
            {
                border.BorderThickness = new Thickness(1);
                border.BorderBrush = DaisyResourceLookup.GetBrush("DaisySecondaryBrush");
            }

            if (ShowToolTips && !string.IsNullOrWhiteSpace(cell.ToolTipText))
            {
                ToolTipService.SetToolTip(border, cell.ToolTipText);
            }

            border.PointerEntered += (_, _) =>
            {
                border.BorderThickness = new Thickness(1);
                border.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            };
            border.PointerExited += (_, _) =>
            {
                border.BorderThickness = cell.IsMonthStart ? new Thickness(1) : new Thickness(0);
                border.BorderBrush = cell.IsMonthStart ? DaisyResourceLookup.GetBrush("DaisySecondaryBrush") : null;
            };

            return border;
        }

        private static void ApplyCellColors(Border border, int level)
        {
            var primary = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
            var base300 = DaisyResourceLookup.GetBrush("DaisyBase300Brush");

            // Apply opacity to the background brush only, NOT the entire element.
            // This keeps the border stroke (for IsMonthStart cells) fully visible.
            var opacity = level switch
            {
                0 => 1.0,
                1 => 0.25,
                2 => 0.45,
                3 => 0.65,
                _ => 1.0
            };

            if (level == 0)
            {
                border.Background = base300;
            }
            else if (primary is SolidColorBrush scb)
            {
                // Create a new brush with the desired opacity
                var color = scb.Color;
                color.A = (byte)(255 * opacity);
                border.Background = new SolidColorBrush(color);
            }
            else
            {
                // Fallback: apply opacity to element (shouldn't happen with SolidColorBrush)
                border.Background = primary;
                border.Opacity = opacity;
            }
        }

        private DateTime GetFirstGridDate(DateTime startDate)
        {
            var diff = ((int)startDate.DayOfWeek - (int)StartDayOfWeek + DaysInWeek) % DaysInWeek;
            return startDate.AddDays(-diff);
        }

        private static bool IsDateInGraphRange(DateTime date, DateTime startDate, DateTime endDate, int targetYear)
        {
            if (date >= startDate && date <= endDate) return true;
            if (date.Year == targetYear - 1 && date.Month == 12) return true;
            if (date.Year == targetYear + 1 && date.Month == 1) return true;
            return false;
        }

        private static string FormatToolTip(DateTime date, int count)
        {
            var countText = count switch
            {
                0 => FloweryLocalization.GetStringInternal("ContributionGraph_NoContributions", "No contributions"),
                1 => FloweryLocalization.GetStringInternal("ContributionGraph_OneContribution", "1 contribution"),
                _ => string.Format(FloweryLocalization.GetStringInternal("ContributionGraph_Contributions", "{0} contributions"), count)
            };

            return $"{countText} on {date:D}";
        }
    }

    /// <summary>
    /// Contribution data point used by <see cref="DaisyContributionGraph"/>.
    /// </summary>
    public partial class DaisyContributionDay
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public int Level { get; set; }
    }

    public partial class DaisyContributionGraphCell
    {
        public DateTime? Date { get; set; }
        public int Count { get; set; }
        public int Level { get; set; }
        public string? ToolTipText { get; set; }
        public bool IsMonthStart { get; set; }
    }

    public partial class DaisyContributionMonthHeader
    {
        public string Text { get; set; } = string.Empty;
        public int WeekIndex { get; set; }
        public double Left { get; set; } // Legacy - for Canvas positioning
    }

    public partial class DaisyContributionDayLabel
    {
        public string Text { get; set; } = string.Empty;
    }
}
