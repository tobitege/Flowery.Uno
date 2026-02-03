using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Foundation;

namespace Flowery.Controls
{
    /// <summary>
    /// Shape for DaisyButtonGroup.
    /// </summary>
    public enum DaisyButtonGroupShape
    {
        Default,
        Square,
        Rounded,
        Pill
    }

    /// <summary>
    /// Selection mode for DaisyButtonGroup.
    /// </summary>
    public enum DaisyButtonGroupSelectionMode
    {
        /// <summary>
        /// Only one button can be selected at a time.
        /// </summary>
        Single,

        /// <summary>
        /// Multiple buttons can be selected simultaneously (like Bold+Italic+Underline toggles).
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Event args for button group selection changes.
    /// </summary>
    public class ButtonGroupItemSelectedEventArgs(UIElement item) : EventArgs
    {
        public UIElement Item { get; } = item;
    }

    /// <summary>
    /// A segmented button container styled after the "Button Group" pattern.
    /// Supports optional auto-selection and consistent styling for mixed segments.
    /// </summary>
    public partial class DaisyButtonGroup : DaisyBaseContentControl
    {
        private StackPanel? _itemsPanel;
        private readonly List<Button> _buttonItems = [];
        private readonly List<FrameworkElement> _segmentItems = [];
        private readonly Dictionary<FrameworkElement, double> _originalMinWidths = [];
        private readonly Dictionary<FrameworkElement, HorizontalAlignment> _originalHorizontalAlignments = [];

        private readonly HashSet<Button> _hoveredButtons = [];
        private readonly HashSet<Button> _pressedButtons = [];

        private Brush? _segmentNormalBackground;
        private Brush? _segmentHoverBackground;
        private Brush? _segmentForeground;
        private Brush? _segmentBorderBrush;

        public DaisyButtonGroup()
        {
            IsTabStop = false;
            _selectedIndices.CollectionChanged += OnSelectedIndicesChanged;
        }

        private void OnSelectedIndicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateSelection();
            SelectedIndicesChanged?.Invoke(this, e);
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            DaisyBaseContentControl.GlobalNeumorphicChanged += OnGlobalNeumorphicChanged;

            if (_itemsPanel != null)
            {
                ApplyTheme();
                return;
            }

            CollectAndDisplay();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            DaisyBaseContentControl.GlobalNeumorphicChanged -= OnGlobalNeumorphicChanged;
        }

        private void OnGlobalNeumorphicChanged(object? sender, EventArgs e)
        {
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                ApplyTheme();
            });
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyTheme();
        }

        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyButtonVariant),
                typeof(DaisyButtonGroup),
                new PropertyMetadata(DaisyButtonVariant.Default, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the visual variant applied to all segments.
        /// </summary>
        public DaisyButtonVariant Variant
        {
            get => (DaisyButtonVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyButtonGroup),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size applied to all segments.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region ButtonStyle
        public static readonly DependencyProperty ButtonStyleProperty =
            DependencyProperty.Register(
                nameof(ButtonStyle),
                typeof(DaisyButtonStyle),
                typeof(DaisyButtonGroup),
                new PropertyMetadata(DaisyButtonStyle.Default, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the segment style (Default, Outline, Dash, Soft).
        /// </summary>
        public DaisyButtonStyle ButtonStyle
        {
            get => (DaisyButtonStyle)GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }
        #endregion

        #region Shape
        public static readonly DependencyProperty ShapeProperty =
            DependencyProperty.Register(
                nameof(Shape),
                typeof(DaisyButtonGroupShape),
                typeof(DaisyButtonGroup),
                new PropertyMetadata(DaisyButtonGroupShape.Default, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the overall group shape.
        /// </summary>
        public DaisyButtonGroupShape Shape
        {
            get => (DaisyButtonGroupShape)GetValue(ShapeProperty);
            set => SetValue(ShapeProperty, value);
        }
        #endregion

        #region Orientation
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyButtonGroup),
                new PropertyMetadata(Orientation.Horizontal, OnLayoutChanged));

        /// <summary>
        /// Gets or sets whether segments are laid out horizontally or vertically.
        /// </summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        #endregion

        #region AutoSelect
        public static readonly DependencyProperty AutoSelectProperty =
            DependencyProperty.Register(
                nameof(AutoSelect),
                typeof(bool),
                typeof(DaisyButtonGroup),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether clicking a segment automatically selects it.
        /// </summary>
        public bool AutoSelect
        {
            get => (bool)GetValue(AutoSelectProperty);
            set => SetValue(AutoSelectProperty, value);
        }
        #endregion

        #region SelectedIndex
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedIndex),
                typeof(int),
                typeof(DaisyButtonGroup),
                new PropertyMetadata(-1, OnSelectedIndexChanged));

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButtonGroup group)
                group.UpdateSelection();
        }
        #endregion

        #region SelectionMode
        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(
                nameof(SelectionMode),
                typeof(DaisyButtonGroupSelectionMode),
                typeof(DaisyButtonGroup),
                new PropertyMetadata(DaisyButtonGroupSelectionMode.Single, OnSelectionModeChanged));

        /// <summary>
        /// Gets or sets the selection mode (Single or Multiple).
        /// </summary>
        public DaisyButtonGroupSelectionMode SelectionMode
        {
            get => (DaisyButtonGroupSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButtonGroup group)
            {
                // Clear all selections when switching modes
                group._selectedIndices.Clear();
                group.SelectedIndex = -1;
                group.UpdateSelection();
            }
        }
        #endregion

        #region SelectedIndices
        private readonly ObservableCollection<int> _selectedIndices = [];

        /// <summary>
        /// Gets the collection of selected indices when SelectionMode is Multiple.
        /// Modifying this collection will update the visual selection state.
        /// </summary>
        public ObservableCollection<int> SelectedIndices => _selectedIndices;

        /// <summary>
        /// Raised when the selected indices change (in Multiple selection mode).
        /// </summary>
        public event EventHandler<NotifyCollectionChangedEventArgs>? SelectedIndicesChanged;
        #endregion

        /// <summary>
        /// Raised when a button segment is clicked.
        /// </summary>
        public event EventHandler<ButtonGroupItemSelectedEventArgs>? ItemSelected;

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButtonGroup group && group._itemsPanel != null)
            {
                group._itemsPanel.Orientation = group.Orientation;
                group.ApplyJoinCorners();
                group.ApplyUniformSegmentWidth();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButtonGroup group)
                group.ApplyTheme();
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

        private void CollectAndDisplay()
        {
            RestoreUniformSegmentWidths();
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            _itemsPanel = new StackPanel
            {
                Orientation = Orientation,
                Spacing = 0 // Joined
            };

            // Detach handlers from previous content to avoid duplicates on re-collect.
            foreach (var oldButton in _buttonItems)
            {
                oldButton.Click -= OnButtonClick;
                oldButton.PointerEntered -= OnButtonPointerEntered;
                oldButton.PointerExited -= OnButtonPointerExited;
                oldButton.PointerPressed -= OnButtonPointerPressed;
                oldButton.PointerReleased -= OnButtonPointerReleased;
                oldButton.PointerCaptureLost -= OnButtonPointerCaptureLost;
            }

            _buttonItems.Clear();
            _segmentItems.Clear();
            _hoveredButtons.Clear();
            _pressedButtons.Clear();

            // Collect children from Content
            if (Content is Panel panel)
            {
                // Detect orientation from child StackPanel if provided
                if (panel is StackPanel sourceStack)
                {
                    Orientation = sourceStack.Orientation;
                }

                var children = new List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                panel.Children.Clear();

                foreach (var child in children)
                {
                    if (child is FrameworkElement fe)
                    {
                        _segmentItems.Add(fe);
                        _itemsPanel.Children.Add(fe);

                        if (fe is Button button)
                        {
                            button.Click += OnButtonClick;
                            button.PointerEntered += OnButtonPointerEntered;
                            button.PointerExited += OnButtonPointerExited;
                            button.PointerPressed += OnButtonPointerPressed;
                            button.PointerReleased += OnButtonPointerReleased;
                            button.PointerCaptureLost += OnButtonPointerCaptureLost;
                            _buttonItems.Add(button);

                            // Tell DaisyButton not to manage its own visuals; the group will do it.
                            if (button is DaisyButton db)
                            {
                                db.IsExternallyControlled = true;
                            }
                        }

                        // IMPORTANT: Disable neumorphic on ALL child controls.
                        // The GROUP itself is the single neumorphic surface - it draws ONE unified
                        // shadow/border around all children. If we let each child have its own
                        // neumorphic effect, we'd get overlapping shadows and borders between
                        // adjacent buttons, which looks wrong. The children are flat visual
                        // segments inside the container's neumorphic shell.
                        if (fe is DaisyBaseContentControl dbcc)
                        {
                            DaisyNeumorphic.SetIsEnabled(dbcc, false);
                        }
                        else if (fe is DaisyButton db2)
                        {
                            DaisyNeumorphic.SetIsEnabled(db2, false);
                        }
                    }
                }
            }
            else if (Content is FrameworkElement singleElement && !ReferenceEquals(Content, _itemsPanel))
            {
                _segmentItems.Add(singleElement);
                _itemsPanel.Children.Add(singleElement);
                if (singleElement is Button button)
                {
                    button.Click += OnButtonClick;
                    button.PointerEntered += OnButtonPointerEntered;
                    button.PointerExited += OnButtonPointerExited;
                    button.PointerPressed += OnButtonPointerPressed;
                    button.PointerReleased += OnButtonPointerReleased;
                    button.PointerCaptureLost += OnButtonPointerCaptureLost;
                    _buttonItems.Add(button);

                    // Tell DaisyButton not to manage its own visuals; the group will do it.
                    if (button is DaisyButton db)
                    {
                        db.IsExternallyControlled = true;
                    }
                }

                // IMPORTANT: Disable neumorphic on ALL child controls.
                // The GROUP itself is the single neumorphic surface - it draws ONE unified
                // shadow/border around all children. If we let each child have its own
                // neumorphic effect, we'd get overlapping shadows and borders between
                // adjacent buttons, which looks wrong. The children are flat visual
                // segments inside the container's neumorphic shell.
                if (singleElement is DaisyBaseContentControl dbcc)
                {
                    DaisyNeumorphic.SetIsEnabled(dbcc, false);
                }
                else if (singleElement is DaisyButton db2)
                {
                    DaisyNeumorphic.SetIsEnabled(db2, false);
                }
            }

            Content = _itemsPanel;
            ApplyJoinCorners();
            ApplyTheme();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                int index = _buttonItems.IndexOf(button);
                if (AutoSelect && index >= 0)
                {
                    if (SelectionMode == DaisyButtonGroupSelectionMode.Multiple)
                    {
                        // Toggle selection in multi-select mode
                        if (!_selectedIndices.Remove(index))
                            _selectedIndices.Add(index);
                    }
                    else
                    {
                        // Single-select mode: replace selection
                        SelectedIndex = index;
                    }
                }

                ItemSelected?.Invoke(this, new ButtonGroupItemSelectedEventArgs(button));
            }
        }

        private void OnButtonPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                _hoveredButtons.Add(button);
                UpdateSegmentVisuals();
            }
        }

        private void OnButtonPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                _hoveredButtons.Remove(button);
                _pressedButtons.Remove(button);
                UpdateSegmentVisuals();
            }
        }

        private void OnButtonPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                _pressedButtons.Add(button);
                UpdateSegmentVisuals();
            }
        }

        private void OnButtonPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                _pressedButtons.Remove(button);
                UpdateSegmentVisuals();
            }
        }

        private void OnButtonPointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Button button)
            {
                _pressedButtons.Remove(button);
                UpdateSegmentVisuals();
            }
        }

        private void UpdateSelection()
        {
            UpdateSegmentVisuals();
        }

        private void UpdateSegmentVisuals()
        {
            if (_segmentNormalBackground == null || _segmentForeground == null || _segmentBorderBrush == null)
                return;

            foreach (var button in _buttonItems)
            {
                int index = _buttonItems.IndexOf(button);
                bool isSelected;
                if (SelectionMode == DaisyButtonGroupSelectionMode.Multiple)
                {
                    isSelected = AutoSelect && _selectedIndices.Contains(index);
                }
                else
                {
                    isSelected = AutoSelect && SelectedIndex >= 0 && index == SelectedIndex;
                }
                var isHovered = _hoveredButtons.Contains(button);
                var isPressed = _pressedButtons.Contains(button);

                var bg = isPressed
                    ? (_segmentHoverBackground ?? _segmentNormalBackground)
                    : (isSelected || isHovered)
                        ? (_segmentHoverBackground ?? _segmentNormalBackground)
                        : _segmentNormalBackground;

                // Force group-controlled visuals via local values.
                button.Background = bg;
                button.Foreground = _segmentForeground;
                button.BorderBrush = _segmentBorderBrush;
                button.BorderThickness = new Thickness(1);

                // IMPORTANT: Keep neumorphic disabled on child buttons during visual updates.
                // See collection phase comments for full explanation: the GROUP is the single
                // neumorphic surface, children must remain flat segments inside it.
                if (button is DaisyButton db)
                {
                    DaisyNeumorphic.SetIsEnabled(db, false);
                }

                button.RenderTransformOrigin = new Point(0.5, 0.5);
                button.RenderTransform = isPressed ? new ScaleTransform { ScaleX = 0.98, ScaleY = 0.98 } : null;
            }

            // Non-button segments (e.g. count badges) follow the group's static visuals.
            foreach (var seg in _segmentItems)
            {
                if (seg is Border border)
                {
                    border.Background = _segmentNormalBackground;
                    border.BorderBrush = _segmentBorderBrush;
                    border.BorderThickness = new Thickness(1);
                }

                // IMPORTANT: Keep neumorphic disabled on all children during visual updates.
                // See collection phase comments for full explanation: the GROUP is the single
                // neumorphic surface, children must remain flat segments inside it.
                if (seg is DaisyBaseContentControl dbcc)
                {
                    DaisyNeumorphic.SetIsEnabled(dbcc, false);
                }
            }
        }

        private void ApplyJoinCorners()
        {
            if (_segmentItems.Count == 0)
                return;

            bool isHorizontal = Orientation == Orientation.Horizontal;
            double radius = Shape switch
            {
                DaisyButtonGroupShape.Square => 0,
                DaisyButtonGroupShape.Pill => 999,
                DaisyButtonGroupShape.Rounded => 12,
                _ => 8
            };

            // Set the group's own CornerRadius so the neumorphic helper can use it for rounded shadows
            CornerRadius = new CornerRadius(radius);

            for (int i = 0; i < _segmentItems.Count; i++)
            {
                bool isFirst = i == 0;
                bool isLast = i == _segmentItems.Count - 1;

                CornerRadius cornerRadius;
                if (isFirst && isLast)
                {
                    cornerRadius = new CornerRadius(radius);
                }
                else if (isHorizontal)
                {
                    if (isFirst)
                        cornerRadius = new CornerRadius(radius, 0, 0, radius);
                    else if (isLast)
                        cornerRadius = new CornerRadius(0, radius, radius, 0);
                    else
                        cornerRadius = new CornerRadius(0);
                }
                else
                {
                    if (isFirst)
                        cornerRadius = new CornerRadius(radius, radius, 0, 0);
                    else if (isLast)
                        cornerRadius = new CornerRadius(0, 0, radius, radius);
                    else
                        cornerRadius = new CornerRadius(0);
                }

                var seg = _segmentItems[i];
                if (seg is Button button)
                {
                    button.CornerRadius = cornerRadius;
                }
                else if (seg is Border border)
                {
                    border.CornerRadius = cornerRadius;
                }

                // Overlap borders so dividers don't become "double thickness".
                if (seg is FrameworkElement fe)
                {
                    if (isFirst)
                    {
                        fe.Margin = new Thickness(0);
                    }
                    else
                    {
                        fe.Margin = isHorizontal
                            ? new Thickness(-1, 0, 0, 0)
                            : new Thickness(0, -1, 0, 0);
                    }
                }
            }
        }

        private void ApplyTheme()
        {
            if (_segmentItems.Count == 0)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            var effectiveSize = FlowerySizeManager.ShouldIgnoreGlobalSize(this)
                ? Size
                : FlowerySizeManager.CurrentSize;

            // ---- Sizing (token-first) ----
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(effectiveSize);
            var height = resources == null
                ? DaisyResourceLookup.GetDefaultHeight(effectiveSize)
                : DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}Height", DaisyResourceLookup.GetDefaultHeight(effectiveSize));
            var fontSize = resources == null
                ? DaisyResourceLookup.GetDefaultFontSize(effectiveSize)
                : DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}FontSize", DaisyResourceLookup.GetDefaultFontSize(effectiveSize));
            var padding = resources == null
                ? DaisyResourceLookup.GetDefaultPadding(effectiveSize)
                : DaisyResourceLookup.GetThickness(resources, $"DaisyButton{sizeKey}Padding", DaisyResourceLookup.GetDefaultPadding(effectiveSize));

            foreach (var seg in _segmentItems)
            {
                if (seg is Button button)
                {
                    button.Height = height;
                    button.Padding = padding;
                    button.FontSize = fontSize;
                    button.HorizontalContentAlignment = HorizontalAlignment.Center;
                    button.VerticalContentAlignment = VerticalAlignment.Center;
                }
                else if (seg is Border border)
                {
                    border.Height = height;
                    border.Padding = padding;
                }
            }

            // ---- Colors (match DaisyUI theme semantics) ----
            var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

            var base100 = DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush", transparent);
            var base200 = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush", transparent);
            var base300 = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", transparent);
            var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", transparent);

            Brush normalBg;
            Brush hoverBg;
            Brush fg;
            Brush dividerBorder;

            switch (Variant)
            {
                case DaisyButtonVariant.Neutral:
                    normalBg = base200;
                    hoverBg = base300;
                    fg = baseContent;
                    dividerBorder = base300;
                    break;

                case DaisyButtonVariant.Primary:
                    normalBg = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", base200);
                    fg = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryContentBrush", baseContent);
                    hoverBg = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryFocusBrush", normalBg);
                    dividerBorder = DaisyResourceLookup.WithOpacity(fg, 0.3) ?? base300;
                    break;

                case DaisyButtonVariant.Secondary:
                    normalBg = DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", base200);
                    fg = DaisyResourceLookup.GetBrush(resources, "DaisySecondaryContentBrush", baseContent);
                    hoverBg = DaisyResourceLookup.GetBrush(resources, "DaisySecondaryFocusBrush", normalBg);
                    dividerBorder = DaisyResourceLookup.WithOpacity(fg, 0.3) ?? base300;
                    break;

                case DaisyButtonVariant.Accent:
                    normalBg = DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", base200);
                    fg = DaisyResourceLookup.GetBrush(resources, "DaisyAccentContentBrush", baseContent);
                    hoverBg = DaisyResourceLookup.GetBrush(resources, "DaisyAccentFocusBrush", normalBg);
                    dividerBorder = DaisyResourceLookup.WithOpacity(fg, 0.3) ?? base300;
                    break;

                case DaisyButtonVariant.Info:
                    normalBg = DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", base200);
                    fg = DaisyResourceLookup.GetBrush(resources, "DaisyInfoContentBrush", baseContent);
                    hoverBg = DaisyResourceLookup.WithOpacity(normalBg, 0.9) ?? normalBg;
                    dividerBorder = DaisyResourceLookup.WithOpacity(fg, 0.3) ?? base300;
                    break;

                case DaisyButtonVariant.Success:
                    normalBg = DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", base200);
                    fg = DaisyResourceLookup.GetBrush(resources, "DaisySuccessContentBrush", baseContent);
                    hoverBg = DaisyResourceLookup.WithOpacity(normalBg, 0.9) ?? normalBg;
                    dividerBorder = DaisyResourceLookup.WithOpacity(fg, 0.3) ?? base300;
                    break;

                case DaisyButtonVariant.Warning:
                    normalBg = DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", base200);
                    fg = DaisyResourceLookup.GetBrush(resources, "DaisyWarningContentBrush", baseContent);
                    hoverBg = DaisyResourceLookup.WithOpacity(normalBg, 0.9) ?? normalBg;
                    dividerBorder = DaisyResourceLookup.WithOpacity(fg, 0.3) ?? base300;
                    break;

                case DaisyButtonVariant.Error:
                    normalBg = DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", base200);
                    fg = DaisyResourceLookup.GetBrush(resources, "DaisyErrorContentBrush", baseContent);
                    hoverBg = DaisyResourceLookup.WithOpacity(normalBg, 0.9) ?? normalBg;
                    dividerBorder = DaisyResourceLookup.WithOpacity(fg, 0.3) ?? base300;
                    break;

                case DaisyButtonVariant.Ghost:
                case DaisyButtonVariant.Link:
                default:
                    // For groups, treat Default as base100 with base200 hover (standard DaisyUI behavior).
                    normalBg = base100;
                    hoverBg = base200;
                    fg = baseContent;
                    dividerBorder = base300;
                    break;
            }

            if (ButtonStyle == DaisyButtonStyle.Outline || ButtonStyle == DaisyButtonStyle.Dash)
            {
                normalBg = transparent;
            }
            else if (ButtonStyle == DaisyButtonStyle.Soft)
            {
                normalBg = base200;
                hoverBg = base300;
                fg = baseContent;
                dividerBorder = base300;
            }

            // Check for lightweight styling overrides (instance-level takes precedence)
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyButtonGroup", "SegmentBackground");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyButtonGroup", "SegmentForeground");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyButtonGroup", "SegmentBorderBrush");

            _segmentNormalBackground = bgOverride ?? normalBg;
            _segmentHoverBackground = hoverBg;
            _segmentForeground = fgOverride ?? fg;
            _segmentBorderBrush = borderOverride ?? dividerBorder;

            ApplyJoinCorners();
            ApplyUniformSegmentWidth();
            UpdateSegmentVisuals();
        }

        private void ApplyUniformSegmentWidth()
        {
            if (_segmentItems.Count == 0)
                return;

            if (Orientation != Orientation.Vertical)
            {
                RestoreUniformSegmentWidths();
                // In horizontal mode, buttons should not stretch horizontally
                foreach (var seg in _segmentItems)
                {
                    if (seg is FrameworkElement fe)
                        fe.HorizontalAlignment = HorizontalAlignment.Left;
                }
                return;
            }

            // In vertical mode, make all buttons stretch to fill the group width.
            // This ensures all buttons have the same width automatically.
            foreach (var seg in _segmentItems)
            {
                if (seg is FrameworkElement fe)
                {
                    if (!_originalMinWidths.ContainsKey(fe))
                        _originalMinWidths[fe] = fe.MinWidth;
                    if (!_originalHorizontalAlignments.ContainsKey(fe))
                        _originalHorizontalAlignments[fe] = fe.HorizontalAlignment;

                    fe.HorizontalAlignment = HorizontalAlignment.Stretch;
                }
            }

            // Schedule a layout pass to measure actual widths and set MinWidth to the largest
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_segmentItems.Count == 0 || Orientation != Orientation.Vertical)
                    return;

                double maxWidth = 0;
                foreach (var seg in _segmentItems)
                {
                    if (seg is FrameworkElement fe && fe.ActualWidth > maxWidth)
                        maxWidth = fe.ActualWidth;
                }

                // If we couldn't get actual widths yet, measure desired sizes
                if (maxWidth <= 0)
                {
                    foreach (var seg in _segmentItems)
                    {
                        if (seg is FrameworkElement fe)
                        {
                            fe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                            if (fe.DesiredSize.Width > maxWidth)
                                maxWidth = fe.DesiredSize.Width;
                        }
                    }
                }

                if (maxWidth > 0)
                {
                    foreach (var seg in _segmentItems)
                    {
                        if (seg is FrameworkElement fe && fe.MinWidth < maxWidth)
                            fe.MinWidth = maxWidth;
                    }
                }
            });
        }

        private void RestoreUniformSegmentWidths()
        {
            foreach (var entry in _originalMinWidths)
                entry.Key.MinWidth = entry.Value;
            _originalMinWidths.Clear();

            foreach (var entry in _originalHorizontalAlignments)
                entry.Key.HorizontalAlignment = entry.Value;
            _originalHorizontalAlignments.Clear();
        }

    }
}
