using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Generic;

namespace Flowery.Controls
{
    /// <summary>
    /// Position of timeline item content.
    /// </summary>
    public enum TimelineItemPosition
    {
        Start,
        End
    }

    /// <summary>
    /// A timeline control styled after DaisyUI's Timeline component.
    /// Displays chronological events with connecting lines.
    /// </summary>
    public partial class DaisyTimeline : DaisyBaseContentControl
    {
        private StackPanel? _itemsPanel;
        private readonly List<DaisyTimelineItem> _timelineItems = [];

        public DaisyTimeline()
        {
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_itemsPanel != null)
            {
                ApplyTheme();
                return;
            }

            CollectAndDisplay();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyTheme();
        }

        #region Orientation
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyTimeline),
                new PropertyMetadata(Orientation.Vertical, OnLayoutChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        #endregion

        #region IsCompact
        public static readonly DependencyProperty IsCompactProperty =
            DependencyProperty.Register(
                nameof(IsCompact),
                typeof(bool),
                typeof(DaisyTimeline),
                new PropertyMetadata(false, OnLayoutChanged));

        /// <summary>
        /// When true, displays a more compact timeline layout (hides start content).
        /// </summary>
        public bool IsCompact
        {
            get => (bool)GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }
        #endregion

        #region SnapIcon
        public static readonly DependencyProperty SnapIconProperty =
            DependencyProperty.Register(
                nameof(SnapIcon),
                typeof(bool),
                typeof(DaisyTimeline),
                new PropertyMetadata(false, OnLayoutChanged));

        /// <summary>
        /// When true, snaps the middle icon to the top of the timeline item.
        /// </summary>
        public bool SnapIcon
        {
            get => (bool)GetValue(SnapIconProperty);
            set => SetValue(SnapIconProperty, value);
        }
        #endregion

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTimeline timeline)
                timeline.ApplyLayoutChanges();
        }

        private void CollectAndDisplay()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            _itemsPanel = new StackPanel
            {
                Orientation = Orientation
            };

            _timelineItems.Clear();

            // Collect DaisyTimelineItem children from Content
            if (Content is Panel panel)
            {
                var children = new List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                panel.Children.Clear();

                int index = 0;
                foreach (var child in children)
                {
                    if (child is DaisyTimelineItem item)
                    {
                        item.Index = index;
                        item.Orientation = Orientation;
                        item.IsCompact = IsCompact;
                        item.SnapIcon = SnapIcon;
                        _timelineItems.Add(item);
                        _itemsPanel.Children.Add(item);
                        index++;
                    }
                }
            }

            // Update first/last flags
            for (int i = 0; i < _timelineItems.Count; i++)
            {
                _timelineItems[i].IsFirst = i == 0;
                _timelineItems[i].IsLast = i == _timelineItems.Count - 1;
            }

            Content = _itemsPanel;

            ApplyTheme();
        }

        private void ApplyLayoutChanges()
        {
            if (_itemsPanel == null)
                return;

            _itemsPanel.Orientation = Orientation;

            foreach (var item in _timelineItems)
            {
                item.Orientation = Orientation;
                item.IsCompact = IsCompact;
                item.SnapIcon = SnapIcon;
                item.RebuildVisual();
            }
        }

        private void ApplyTheme()
        {
            foreach (var item in _timelineItems)
            {
                item.RebuildVisual();
            }
        }
    }

    /// <summary>
    /// A single item within a DaisyTimeline container.
    /// </summary>
    public partial class DaisyTimelineItem : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _connectorStart;
        private Ellipse? _indicator;
        private ContentPresenter? _iconPresenter;
        private Border? _connectorEnd;
        private ContentPresenter? _startPresenter;
        private ContentPresenter? _endPresenter;
        private Border? _startBox;
        private Border? _endBox;
        private long _foregroundCallbackToken;
        private bool _hasForegroundOverride;
        private bool _foregroundOverrideInitialized;
        private bool _isApplyingForeground;
        public DaisyTimelineItem()
        {
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            _foregroundCallbackToken = RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundChanged);
            EnsureForegroundOverrideInitialized();
            BuildVisualTree();
            RebuildVisual();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            if (_foregroundCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(ForegroundProperty, _foregroundCallbackToken);
                _foregroundCallbackToken = 0;
            }
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            RebuildVisual();
        }

        #region StartContent
        public static readonly DependencyProperty StartContentProperty =
            DependencyProperty.Register(
                nameof(StartContent),
                typeof(object),
                typeof(DaisyTimelineItem),
                new PropertyMetadata(null, OnAppearanceChanged));

        public object? StartContent
        {
            get => GetValue(StartContentProperty);
            set => SetValue(StartContentProperty, value);
        }
        #endregion

        #region MiddleContent
        public static readonly DependencyProperty MiddleContentProperty =
            DependencyProperty.Register(
                nameof(MiddleContent),
                typeof(object),
                typeof(DaisyTimelineItem),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Content shown in the middle indicator (typically an icon).
        /// </summary>
        public object? MiddleContent
        {
            get => GetValue(MiddleContentProperty);
            set => SetValue(MiddleContentProperty, value);
        }
        #endregion

        #region EndContent
        public static readonly DependencyProperty EndContentProperty =
            DependencyProperty.Register(
                nameof(EndContent),
                typeof(object),
                typeof(DaisyTimelineItem),
                new PropertyMetadata(null, OnAppearanceChanged));

        public object? EndContent
        {
            get => GetValue(EndContentProperty);
            set => SetValue(EndContentProperty, value);
        }
        #endregion

        #region IsActive
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(DaisyTimelineItem),
                new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }
        #endregion

        #region IsBoxed
        public static readonly DependencyProperty IsBoxedProperty =
            DependencyProperty.Register(
                nameof(IsBoxed),
                typeof(bool),
                typeof(DaisyTimelineItem),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// When true, displays content in a boxed container.
        /// </summary>
        public bool IsBoxed
        {
            get => (bool)GetValue(IsBoxedProperty);
            set => SetValue(IsBoxedProperty, value);
        }
        #endregion

        #region HasStartLine
        public static readonly DependencyProperty HasStartLineProperty =
            DependencyProperty.Register(
                nameof(HasStartLine),
                typeof(bool),
                typeof(DaisyTimelineItem),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// When true, shows the connector line before this item.
        /// </summary>
        public bool HasStartLine
        {
            get => (bool)GetValue(HasStartLineProperty);
            set => SetValue(HasStartLineProperty, value);
        }
        #endregion

        #region HasEndLine
        public static readonly DependencyProperty HasEndLineProperty =
            DependencyProperty.Register(
                nameof(HasEndLine),
                typeof(bool),
                typeof(DaisyTimelineItem),
                new PropertyMetadata(false, OnAppearanceChanged));

        /// <summary>
        /// When true, shows the connector line after this item.
        /// </summary>
        public bool HasEndLine
        {
            get => (bool)GetValue(HasEndLineProperty);
            set => SetValue(HasEndLineProperty, value);
        }
        #endregion

        #region Position
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(
                nameof(Position),
                typeof(TimelineItemPosition),
                typeof(DaisyTimelineItem),
                new PropertyMetadata(TimelineItemPosition.End, OnAppearanceChanged));

        /// <summary>
        /// Determines which side the boxed content appears on (Start or End).
        /// </summary>
        public TimelineItemPosition Position
        {
            get => (TimelineItemPosition)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }
        #endregion

        #region Internal Properties
        internal bool IsFirst { get; set; }
        internal bool IsLast { get; set; }
        internal int Index { get; set; }
        internal Orientation Orientation { get; set; } = Orientation.Vertical;
        internal bool IsCompact { get; set; }
        internal bool SnapIcon { get; set; }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTimelineItem item)
                item.RebuildVisual();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid();
            RebuildGridLayout();
            Content = _rootGrid;
        }

        private void RebuildGridLayout()
        {
            if (_rootGrid == null)
                return;

            _rootGrid.Children.Clear();
            _rootGrid.RowDefinitions.Clear();
            _rootGrid.ColumnDefinitions.Clear();

            bool isVertical = Orientation == Orientation.Vertical;

            if (isVertical)
            {
                // Vertical: [start content] | [connector-indicator-connector] | [end content]
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Start content column (plain presenter)
                _startPresenter = new ContentPresenter
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = SnapIcon ? VerticalAlignment.Top : VerticalAlignment.Center,
                    Margin = new Thickness(0, SnapIcon ? 4 : 0, 12, 0)
                };
                _rootGrid.Children.Add(_startPresenter);
                Grid.SetColumn(_startPresenter, 0);

                // Start content box (for IsBoxed)
                _startBox = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = SnapIcon ? VerticalAlignment.Top : VerticalAlignment.Center,
                    Margin = new Thickness(0, SnapIcon ? 4 : 0, 12, 0),
                    Visibility = Visibility.Collapsed
                };
                var startBoxContent = new ContentPresenter();
                _startBox.Child = startBoxContent;
                _rootGrid.Children.Add(_startBox);
                Grid.SetColumn(_startBox, 0);

                // Middle column with connectors and indicator
                var middlePanel = new Grid
                {
                    RowDefinitions =
                    {
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                    }
                };

                _connectorStart = new Border
                {
                    Width = 4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                middlePanel.Children.Add(_connectorStart);
                Grid.SetRow(_connectorStart, 0);

                var indicatorContainer = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                _indicator = new Ellipse { Width = 20, Height = 20 };
                _iconPresenter = new ContentPresenter
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                indicatorContainer.Children.Add(_indicator);
                indicatorContainer.Children.Add(_iconPresenter);
                middlePanel.Children.Add(indicatorContainer);
                Grid.SetRow(indicatorContainer, 1);

                _connectorEnd = new Border
                {
                    Width = 4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                middlePanel.Children.Add(_connectorEnd);
                Grid.SetRow(_connectorEnd, 2);

                _rootGrid.Children.Add(middlePanel);
                Grid.SetColumn(middlePanel, 1);

                // End content column (plain presenter)
                _endPresenter = new ContentPresenter
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = SnapIcon ? VerticalAlignment.Top : VerticalAlignment.Center,
                    Margin = new Thickness(12, SnapIcon ? 4 : 0, 0, 0)
                };
                _rootGrid.Children.Add(_endPresenter);
                Grid.SetColumn(_endPresenter, 2);

                // End content box (for IsBoxed)
                _endBox = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = SnapIcon ? VerticalAlignment.Top : VerticalAlignment.Center,
                    Margin = new Thickness(12, SnapIcon ? 4 : 0, 0, 0),
                    Visibility = Visibility.Collapsed
                };
                var endBoxContent = new ContentPresenter();
                _endBox.Child = endBoxContent;
                _rootGrid.Children.Add(_endBox);
                Grid.SetColumn(_endBox, 2);

                _rootGrid.MinHeight = 80;
            }
            else
            {
                // Horizontal: Start on top, indicator+lines in middle, End on bottom
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // Start content row (plain presenter)
                _startPresenter = new ContentPresenter
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                _rootGrid.Children.Add(_startPresenter);
                Grid.SetRow(_startPresenter, 0);

                // Start content box (for IsBoxed and Position=Start)
                _startBox = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 8),
                    Visibility = Visibility.Collapsed
                };
                var startBoxContent = new ContentPresenter();
                _startBox.Child = startBoxContent;
                _rootGrid.Children.Add(_startBox);
                Grid.SetRow(_startBox, 0);

                // Middle row with connectors and indicator
                var middlePanel = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    }
                };

                _connectorStart = new Border
                {
                    Height = 4,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };
                middlePanel.Children.Add(_connectorStart);
                Grid.SetColumn(_connectorStart, 0);

                var indicatorContainer = new Grid { Margin = new Thickness(4, 0, 4, 0) };
                _indicator = new Ellipse { Width = 20, Height = 20 };
                _iconPresenter = new ContentPresenter
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                indicatorContainer.Children.Add(_indicator);
                indicatorContainer.Children.Add(_iconPresenter);
                middlePanel.Children.Add(indicatorContainer);
                Grid.SetColumn(indicatorContainer, 1);

                _connectorEnd = new Border
                {
                    Height = 4,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };
                middlePanel.Children.Add(_connectorEnd);
                Grid.SetColumn(_connectorEnd, 2);

                _rootGrid.Children.Add(middlePanel);
                Grid.SetRow(middlePanel, 1);

                // End content row (plain presenter)
                _endPresenter = new ContentPresenter
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                _rootGrid.Children.Add(_endPresenter);
                Grid.SetRow(_endPresenter, 2);

                // End content box (for IsBoxed)
                _endBox = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 8, 0, 0),
                    Visibility = Visibility.Collapsed
                };
                var endBoxContent = new ContentPresenter();
                _endBox.Child = endBoxContent;
                _rootGrid.Children.Add(_endBox);
                Grid.SetRow(_endBox, 2);

                _rootGrid.MinWidth = 100;
            }
        }

        internal void RebuildVisual()
        {
            if (_rootGrid == null)
                return;

            RebuildGridLayout();

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Check for lightweight styling overrides
            var indicatorOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTimelineItem", "IndicatorBrush");
            var activeOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTimelineItem", "ActiveBrush");
            var connectorOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTimelineItem", "ConnectorBrush");
            var boxBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTimelineItem", "BoxBackground");
            var boxBorderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTimelineItem", "BoxBorderBrush");

            var primaryBrush = activeOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush");
            var base200Brush = boxBgOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush");
            var base300Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush");
            var labelBrush = _hasForegroundOverride
                ? Foreground
                : GetAppBrush("DaisyBaseContentBrush");

            if (!_hasForegroundOverride)
            {
                SetForeground(labelBrush);
            }

            // Indicator styling
            var indicatorBrush = IsActive ? primaryBrush : (indicatorOverride ?? connectorOverride ?? base300Brush);
            if (_indicator != null)
            {
                _indicator.Fill = indicatorBrush;
            }

            // Icon
            if (_iconPresenter != null)
            {
                _iconPresenter.Content = MiddleContent;
                _iconPresenter.Visibility = MiddleContent != null ? Visibility.Visible : Visibility.Collapsed;
            }

            // Determine line visibility:
            // - If HasStartLine/HasEndLine are explicitly set, use those
            // - Otherwise, use IsFirst/IsLast for automatic visibility
            bool showStartLine = HasStartLine || !IsFirst;
            bool showEndLine = HasEndLine || !IsLast;

            // Connector visibility and coloring
            var connectorBrush = connectorOverride ?? base300Brush;
            if (_connectorStart != null)
            {
                // Use active color if the item is active
                _connectorStart.Background = IsActive ? primaryBrush : connectorBrush;
                _connectorStart.Visibility = showStartLine ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_connectorEnd != null)
            {
                _connectorEnd.Background = IsActive ? primaryBrush : connectorBrush;
                _connectorEnd.Visibility = showEndLine ? Visibility.Visible : Visibility.Collapsed;
            }

            // Determine boxed content visibility based on Position
            bool showStartAsBox = IsBoxed && Position == TimelineItemPosition.Start;
            bool showEndAsBox = IsBoxed && Position == TimelineItemPosition.End;

            // Start content
            if (_startPresenter != null && _startBox != null)
            {
                if (IsCompact)
                {
                    // Compact mode hides start content
                    _startPresenter.Visibility = Visibility.Collapsed;
                    _startBox.Visibility = Visibility.Collapsed;
                }
                else if (showStartAsBox)
                {
                    _startPresenter.Visibility = Visibility.Collapsed;
                    _startBox.Visibility = StartContent != null ? Visibility.Visible : Visibility.Collapsed;
                    _startBox.Background = base200Brush;
                    _startBox.BorderBrush = boxBorderOverride ?? base300Brush;
                    _startBox.BorderThickness = new Thickness(1);
                    if (_startBox.Child is ContentPresenter cp)
                        cp.Content = StartContent;
                }
                else
                {
                    _startPresenter.Content = StartContent;
                    _startPresenter.Visibility = StartContent != null ? Visibility.Visible : Visibility.Collapsed;
                    _startBox.Visibility = Visibility.Collapsed;
                }
            }

            // End content
            if (_endPresenter != null && _endBox != null)
            {
                // Get effective end content (EndContent property or base Content)
                var endContent = EndContent ?? base.Content;
                if (endContent is Grid) endContent = null; // Don't show the root grid as content

                if (showEndAsBox)
                {
                    _endPresenter.Visibility = Visibility.Collapsed;
                    _endBox.Visibility = endContent != null ? Visibility.Visible : Visibility.Collapsed;
                    _endBox.Background = base200Brush;
                    _endBox.BorderBrush = boxBorderOverride ?? base300Brush;
                    _endBox.BorderThickness = new Thickness(1);
                    if (_endBox.Child is ContentPresenter cp)
                        cp.Content = endContent;
                }
                else
                {
                    _endPresenter.Content = endContent;
                    _endPresenter.Visibility = endContent != null ? Visibility.Visible : Visibility.Collapsed;
                    _endBox.Visibility = Visibility.Collapsed;
                }
            }

            ApplyContentForeground(StartContent, labelBrush);
            ApplyContentForeground(EndContent ?? base.Content, labelBrush);
        }


        private void OnForegroundChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_isApplyingForeground)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            RebuildVisual();
        }

        private void EnsureForegroundOverrideInitialized()
        {
            if (_foregroundOverrideInitialized)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            _foregroundOverrideInitialized = true;
        }

        private void SetForeground(Brush brush)
        {
            if (ReferenceEquals(Foreground, brush))
                return;

            _isApplyingForeground = true;
            try
            {
                Foreground = brush;
            }
            finally
            {
                _isApplyingForeground = false;
            }
        }

        private static Brush GetAppBrush(string key)
        {
            var resources = Application.Current?.Resources;
            if (resources != null && resources.TryGetValue(key, out var value) && value is Brush brush)
                return brush;

            return DaisyResourceLookup.GetBrush(key);
        }

        private static void ApplyContentForeground(object? content, Brush brush)
        {
            if (content is TextBlock textBlock)
            {
                if (textBlock.ReadLocalValue(TextBlock.ForegroundProperty) == DependencyProperty.UnsetValue)
                {
                    textBlock.Foreground = brush;
                }
            }
            else if (content is Control control)
            {
                if (control.ReadLocalValue(Control.ForegroundProperty) == DependencyProperty.UnsetValue)
                {
                    control.Foreground = brush;
                }
            }
        }
    }
}
