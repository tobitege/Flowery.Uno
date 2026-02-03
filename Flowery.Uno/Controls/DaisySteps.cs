using System.Text.Json.Serialization;
using Flowery.Theming;
using Microsoft.UI.Xaml.Shapes;

namespace Flowery.Controls
{
    /// <summary>
    /// Color options for DaisyStepItem.
    /// </summary>
    public enum DaisyStepColor
    {
        Default,
        Neutral,
        Primary,
        Secondary,
        Accent,
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// A Steps control styled after DaisyUI's Steps component.
    /// Displays a series of steps with optional active state.
    /// </summary>
    public partial class DaisySteps : DaisyBaseContentControl
    {
        private StackPanel? _itemsPanel;
        private readonly List<DaisyStepItem> _stepItems = [];
        private bool _isUpdatingFromJson;

        public DaisySteps()
        {
            IsTabStop = false;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // Only collect and display if we aren't currently loading from JSON
            // and the new content isn't our own internal items panel.
            if (!_isUpdatingFromJson && newContent != null && !ReferenceEquals(newContent, _itemsPanel))
            {
                CollectAndDisplay();
            }
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

        #region Orientation
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisySteps),
                new PropertyMetadata(Orientation.Horizontal, OnLayoutChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisySteps),
                new PropertyMetadata(DaisySize.Medium, OnLayoutChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region SelectedIndex
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedIndex),
                typeof(int),
                typeof(DaisySteps),
                new PropertyMetadata(-1, OnLayoutChanged));

        /// <summary>
        /// Steps with index less than or equal to this value will be marked as active.
        /// Set to -1 to disable automatic active state management.
        /// </summary>
        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }
        #endregion

        #region JsonSteps
        public static readonly DependencyProperty JsonStepsProperty =
            DependencyProperty.Register(
                nameof(JsonSteps),
                typeof(string),
                typeof(DaisySteps),
                new PropertyMetadata(null, OnJsonStepsChanged));

        /// <summary>
        /// Gets or sets a JSON string representing the steps.
        /// Format: [{"content": "Step 1", "color": "Primary", "isActive": true}, ...]
        /// </summary>
        public string? JsonSteps
        {
            get => (string?)GetValue(JsonStepsProperty);
            set => SetValue(JsonStepsProperty, value);
        }

        private static void OnJsonStepsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySteps steps && e.NewValue is string json)
            {
                steps.LoadFromJson(json);
            }
        }

        private void LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize(
                    json,
                    DaisyStepsJsonContext.Default.ListDaisyStepModel);
                if (data == null) return;

                _isUpdatingFromJson = true;

                var panel = new StackPanel { Orientation = Orientation };
                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    var stepItem = new DaisyStepItem
                    {
                        Content = item.Content,
                        IsActive = item.IsActive
                    };

                    if (!string.IsNullOrEmpty(item.Color) && Enum.TryParse<DaisyStepColor>(item.Color, true, out var color))
                    {
                        stepItem.Color = color;
                    }

                    panel.Children.Add(stepItem);
                }

                Content = panel;
                CollectAndDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading DaisySteps from JSON: {ex.Message}");
            }
            finally
            {
                _isUpdatingFromJson = false;
            }
        }
        #endregion

        /// <summary>
        /// Gets the current number of steps.
        /// </summary>
        public int ItemCount => _stepItems.Count;

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySteps steps)
                steps.ApplyLayoutChanges();
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

            _stepItems.Clear();

            // Collect DaisyStepItem children from Content
            if (Content is Panel panel)
            {
                var children = new List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                panel.Children.Clear();

                int index = 0;
                foreach (var child in children)
                {
                    if (child is DaisyStepItem stepItem)
                    {
                        stepItem.Index = index;
                        stepItem.Orientation = Orientation;
                        stepItem.Size = Size;
                        _stepItems.Add(stepItem);
                        _itemsPanel.Children.Add(stepItem);
                        index++;
                    }
                }
            }

            // Update first/last flags
            for (int i = 0; i < _stepItems.Count; i++)
            {
                _stepItems[i].IsFirst = i == 0;
                _stepItems[i].IsLast = i == _stepItems.Count - 1;
            }

            Content = _itemsPanel;

            ApplyTheme();
            UpdateActiveStates();
        }

        private void ApplyLayoutChanges()
        {
            if (_itemsPanel == null)
                return;

            _itemsPanel.Orientation = Orientation;

            foreach (var step in _stepItems)
            {
                step.Orientation = Orientation;
                step.Size = Size;
                step.RebuildVisual();
            }

            UpdateActiveStates();
        }

        private void UpdateActiveStates()
        {
            for (int i = 0; i < _stepItems.Count; i++)
            {
                // If SelectedIndex is set, use it; otherwise respect each item's own IsActive
                if (SelectedIndex >= 0)
                {
                    _stepItems[i].IsActive = i <= SelectedIndex;
                }
                _stepItems[i].RebuildVisual();
            }
        }

        private void ApplyTheme()
        {
            foreach (var step in _stepItems)
            {
                step.RebuildVisual();
            }
        }
    }

    /// <summary>
    /// A single step item within a DaisySteps container.
    /// </summary>
    public partial class DaisyStepItem : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private object? _userContent;
        private Border? _connectorBefore;
        private Ellipse? _indicator;
        private TextBlock? _indicatorText;
        private ContentPresenter? _iconPresenter;
        private Border? _connectorAfter;
        private TextBlock? _labelBlock;

        public DaisyStepItem()
        {
            IsTabStop = false;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // Capture the user's content before we replace it with our own visual tree
            if (newContent != null && !ReferenceEquals(newContent, _rootGrid))
            {
                _userContent = newContent;
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            RebuildVisual();
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

        #region Color
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(DaisyStepColor),
                typeof(DaisyStepItem),
                new PropertyMetadata(DaisyStepColor.Default, OnAppearanceChanged));

        public DaisyStepColor Color
        {
            get => (DaisyStepColor)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyStepItem),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region Icon
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(DaisyStepItem),
                new PropertyMetadata(null, OnAppearanceChanged));

        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        #endregion

        #region DataContent
        public static readonly DependencyProperty DataContentProperty =
            DependencyProperty.Register(
                nameof(DataContent),
                typeof(string),
                typeof(DaisyStepItem),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// The text shown inside the step indicator circle.
        /// </summary>
        public string? DataContent
        {
            get => (string?)GetValue(DataContentProperty);
            set => SetValue(DataContentProperty, value);
        }
        #endregion

        #region IsActive
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(DaisyStepItem),
                new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }
        #endregion

        #region Internal Properties
        internal bool IsFirst { get; set; }
        internal bool IsLast { get; set; }
        internal int Index { get; set; }
        internal Orientation Orientation { get; set; } = Orientation.Horizontal;
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyStepItem item)
                item.RebuildVisual();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid();

            // For horizontal: [connector][indicator][connector][label below]
            // For vertical: grid with indicator + connectors, label on side
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

            bool isHorizontal = Orientation == Orientation.Horizontal;

            if (isHorizontal)
            {
                // Horizontal layout: connector - indicator - connector in a row, label below
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Connector before
                _connectorBefore = new Border { Height = 2, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(_connectorBefore, 0);
                Grid.SetColumn(_connectorBefore, 0);
                _rootGrid.Children.Add(_connectorBefore);

                // Indicator container
                var indicatorContainer = new Grid();
                var indSize = GetIndicatorSize(Size);
                var fontSize = GetFontSize(Size);

                _indicator = new Ellipse 
                { 
                    Width = indSize, 
                    Height = indSize,
                    StrokeThickness = DaisyResourceLookup.GetDouble("DaisyBorderThicknessMedium", 2)
                };
                _indicatorText = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = fontSize
                };
                _iconPresenter = new ContentPresenter
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                indicatorContainer.Children.Add(_indicator);
                indicatorContainer.Children.Add(_indicatorText);
                indicatorContainer.Children.Add(_iconPresenter);
                Grid.SetRow(indicatorContainer, 0);
                Grid.SetColumn(indicatorContainer, 1);
                _rootGrid.Children.Add(indicatorContainer);

                // Connector after
                _connectorAfter = new Border { Height = 2, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(_connectorAfter, 0);
                Grid.SetColumn(_connectorAfter, 2);
                _rootGrid.Children.Add(_connectorAfter);

                // Label
                _labelBlock = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                    Margin = new Thickness(DaisyResourceLookup.GetSpacing(Size) / 2, DaisyResourceLookup.GetSpacing(Size) * 0.6, DaisyResourceLookup.GetSpacing(Size) / 2, 0),
                    FontSize = GetFontSize(Size),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(_labelBlock, 1);
                Grid.SetColumn(_labelBlock, 0);
                Grid.SetColumnSpan(_labelBlock, 3);
                _rootGrid.Children.Add(_labelBlock);

                _rootGrid.MinWidth = GetMinStepSize(Size);
            }
            else
            {
                // Vertical layout: connector above, indicator + label, connector below
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Connector before (top)
                _connectorBefore = new Border { Width = 2, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetRow(_connectorBefore, 0);
                Grid.SetColumn(_connectorBefore, 0);
                _rootGrid.Children.Add(_connectorBefore);

                // Indicator container
                var indicatorContainer = new Grid();
                var indSize = GetIndicatorSize(Size);
                var fontSize = GetFontSize(Size);

                _indicator = new Ellipse 
                { 
                    Width = indSize, 
                    Height = indSize,
                    StrokeThickness = DaisyResourceLookup.GetDouble("DaisyBorderThicknessMedium", 2)
                };
                _indicatorText = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = fontSize
                };
                _iconPresenter = new ContentPresenter
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                indicatorContainer.Children.Add(_indicator);
                indicatorContainer.Children.Add(_indicatorText);
                indicatorContainer.Children.Add(_iconPresenter);
                Grid.SetRow(indicatorContainer, 1);
                Grid.SetColumn(indicatorContainer, 0);
                _rootGrid.Children.Add(indicatorContainer);

                // Label
                _labelBlock = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(DaisyResourceLookup.GetSpacing(Size), 0, 0, 0),
                    FontSize = GetFontSize(Size)
                };
                Grid.SetRow(_labelBlock, 1);
                Grid.SetColumn(_labelBlock, 1);
                _rootGrid.Children.Add(_labelBlock);

                // Connector after (bottom)
                _connectorAfter = new Border { Width = 2, HorizontalAlignment = HorizontalAlignment.Center };
                Grid.SetRow(_connectorAfter, 2);
                Grid.SetColumn(_connectorAfter, 0);
                _rootGrid.Children.Add(_connectorAfter);

                _rootGrid.MinHeight = GetMinStepSize(Size);
            }
        }

        private static double GetMinStepSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 35,
            DaisySize.Small => 45,
            DaisySize.Medium => 60,
            DaisySize.Large => 80,
            DaisySize.ExtraLarge => 100,
            _ => 60
        };

        private static double GetIndicatorSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 14,
            DaisySize.Small => 18,
            DaisySize.Medium => 24,
            DaisySize.Large => 32,
            DaisySize.ExtraLarge => 40,
            _ => 24
        };

        private static double GetFontSize(DaisySize size) => size switch
        {
            DaisySize.ExtraSmall => 9,
            DaisySize.Small => 10,
            DaisySize.Medium => 12,
            DaisySize.Large => 14,
            DaisySize.ExtraLarge => 16,
            _ => 12
        };

        internal void RebuildVisual()
        {
            if (_rootGrid == null)
                return;

            RebuildGridLayout();

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Check for lightweight styling overrides
            var indicatorOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyStepItem", "IndicatorBrush");
            var connectorOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyStepItem", "ConnectorBrush");
            var labelFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyStepItem", "LabelForeground");

            // Get theme colors
            var baseContentBrush = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush");
            var base300Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush");
            var colorBrush = GetColorBrush(resources);
            var colorContentBrush = GetColorContentBrush(resources);

            // Active state determines styling
            var base100Brush = DaisyResourceLookup.GetBrush("DaisyBase100Brush");
            var borderThickness = DaisyResourceLookup.GetDouble("DaisyBorderThicknessMedium", 2);

            var indicatorFill = IsActive ? colorBrush : base100Brush;
            var indicatorStroke = IsActive ? colorBrush : (indicatorOverride ?? base300Brush);
            var indicatorTextBrush = IsActive ? colorContentBrush : baseContentBrush;
            var connectorBrush = IsActive ? colorBrush : (connectorOverride ?? base300Brush);

            // Apply indicator styling
            if (_indicator != null)
            {
                _indicator.Fill = indicatorFill;
                _indicator.Stroke = indicatorStroke;
                _indicator.StrokeThickness = borderThickness;
            }

            // Apply text/icon
            if (_indicatorText != null)
            {
                _indicatorText.Foreground = indicatorTextBrush;
                if (Icon != null)
                {
                    _indicatorText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _indicatorText.Visibility = Visibility.Visible;
                    _indicatorText.Text = DataContent ?? (Index + 1).ToString();
                }
            }

            if (_iconPresenter != null)
            {
                _iconPresenter.Content = Icon;
                _iconPresenter.Visibility = Icon != null ? Visibility.Visible : Visibility.Collapsed;
            }

            // Connector visibility
            if (_connectorBefore != null)
            {
                _connectorBefore.Background = connectorBrush;
                _connectorBefore.Visibility = IsFirst ? Visibility.Collapsed : Visibility.Visible;
            }

            if (_connectorAfter != null)
            {
                _connectorAfter.Background = connectorBrush;
                _connectorAfter.Visibility = IsLast ? Visibility.Collapsed : Visibility.Visible;
            }

            // Label
            if (_labelBlock != null)
            {
                _labelBlock.Foreground = labelFgOverride ?? baseContentBrush;
                _labelBlock.Text = _userContent?.ToString() ?? "";
            }
        }

        private Brush GetColorBrush(ResourceDictionary? resources)
        {
            string key = Color switch
            {
                DaisyStepColor.Neutral => "DaisyNeutralBrush",
                DaisyStepColor.Primary => "DaisyPrimaryBrush",
                DaisyStepColor.Secondary => "DaisySecondaryBrush",
                DaisyStepColor.Accent => "DaisyAccentBrush",
                DaisyStepColor.Info => "DaisyInfoBrush",
                DaisyStepColor.Success => "DaisySuccessBrush",
                DaisyStepColor.Warning => "DaisyWarningBrush",
                DaisyStepColor.Error => "DaisyErrorBrush",
                _ => "DaisyPrimaryBrush"
            };
            return DaisyResourceLookup.GetBrush(resources, key);
        }

        private Brush GetColorContentBrush(ResourceDictionary? resources)
        {
            string key = Color switch
            {
                DaisyStepColor.Neutral => "DaisyNeutralContentBrush",
                DaisyStepColor.Primary => "DaisyPrimaryContentBrush",
                DaisyStepColor.Secondary => "DaisySecondaryContentBrush",
                DaisyStepColor.Accent => "DaisyAccentContentBrush",
                DaisyStepColor.Info => "DaisyInfoContentBrush",
                DaisyStepColor.Success => "DaisySuccessContentBrush",
                DaisyStepColor.Warning => "DaisyWarningContentBrush",
                DaisyStepColor.Error => "DaisyErrorContentBrush",
                _ => "DaisyPrimaryContentBrush"
            };
            return DaisyResourceLookup.GetBrush(resources, key);
        }

    }

    /// <summary>
    /// Model for JSON deserialization of steps.
    /// </summary>
    public class DaisyStepModel
    {
        public string? Content { get; set; }
        public string? Color { get; set; }
        public bool IsActive { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(List<DaisyStepModel>))]
    internal partial class DaisyStepsJsonContext : JsonSerializerContext
    {
    }
}
