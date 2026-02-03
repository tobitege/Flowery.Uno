using Flowery.Services;
using Flowery.Theming;
using Flowery.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.Generic;

namespace Flowery.Controls
{
    /// <summary>
    /// A Menu control styled after DaisyUI's Menu component.
    /// </summary>
    public partial class DaisyMenu : DaisyBaseContentControl
    {
        private Border? _containerBorder;
        private Grid? _itemsHost;
        private Border? _selectionIndicator;
        private CompositeTransform? _selectionTransform;
        private Storyboard? _selectionStoryboard;
        private FrameworkElement? _selectionTarget;
        private StackPanel? _itemsPanel;
        private readonly List<DaisyMenuItem> _menuItems = [];
        private bool _selectionUpdateQueued;
        private bool _pendingSelectionAnimate;

        public event EventHandler<DaisyMenuItem>? ItemInvoking;
        public event EventHandler<DaisyMenuItem>? ItemInvoked;

        public DaisyMenu()
        {
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_containerBorder != null)
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

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _containerBorder ?? base.GetNeumorphicHostElement();
        }

        #region Orientation
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyMenu),
                new PropertyMetadata(Orientation.Vertical, OnLayoutChanged));

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
                typeof(DaisyMenu),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        #region ActiveForeground
        public static readonly DependencyProperty ActiveForegroundProperty =
            DependencyProperty.Register(
                nameof(ActiveForeground),
                typeof(Brush),
                typeof(DaisyMenu),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the foreground color of active/selected menu items.
        /// </summary>
        public Brush? ActiveForeground
        {
            get => (Brush?)GetValue(ActiveForegroundProperty);
            set => SetValue(ActiveForegroundProperty, value);
        }
        #endregion

        #region ActiveBackground
        public static readonly DependencyProperty ActiveBackgroundProperty =
            DependencyProperty.Register(
                nameof(ActiveBackground),
                typeof(Brush),
                typeof(DaisyMenu),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the background color of active/selected menu items.
        /// </summary>
        public Brush? ActiveBackground
        {
            get => (Brush?)GetValue(ActiveBackgroundProperty);
            set => SetValue(ActiveBackgroundProperty, value);
        }
        #endregion

        #region Selection Animation

        public static readonly DependencyProperty IsSelectionAnimationEnabledProperty =
            DependencyProperty.Register(
                nameof(IsSelectionAnimationEnabled),
                typeof(bool),
                typeof(DaisyMenu),
                new PropertyMetadata(true, OnSelectionAnimationChanged));

        /// <summary>
        /// When true (default), selection highlighting animates between items.
        /// </summary>
        public bool IsSelectionAnimationEnabled
        {
            get => (bool)GetValue(IsSelectionAnimationEnabledProperty);
            set => SetValue(IsSelectionAnimationEnabledProperty, value);
        }

        private static void OnSelectionAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMenu menu)
            {
                menu.ApplyTheme();
            }
        }

        #endregion

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMenu menu)
            {
                if (menu._itemsPanel != null)
                    menu._itemsPanel.Orientation = menu.Orientation;
                menu.UpdateItemAlignment();
                menu.UpdateSelectionIndicator(animate: false);
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMenu menu)
                menu.ApplyTheme();
        }

        private void CollectAndDisplay()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            _containerBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush"),
                BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush"),
                BorderThickness = new Thickness(1)
            };

            _itemsPanel = new StackPanel
            {
                Orientation = Orientation,
                Spacing = 4
            };

            _selectionIndicator = new Border
            {
                CornerRadius = new CornerRadius(8),
                Opacity = 0,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            _selectionTransform = FloweryAnimationHelpers.EnsureCompositeTransform(_selectionIndicator);

            _itemsHost = new Grid();
            _itemsHost.Children.Add(_selectionIndicator);
            _itemsHost.Children.Add(_itemsPanel);

            _menuItems.Clear();

            // Collect DaisyMenuItem children from Content
            if (Content is Panel panel)
            {
                var children = new List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                panel.Children.Clear();

                foreach (var child in children)
                {
                    if (child is DaisyMenuItem item)
                    {
                        item.ParentMenu = this;
                        item.Size = Size;
                        item.HorizontalAlignment = Orientation == Orientation.Vertical
                            ? HorizontalAlignment.Stretch
                            : HorizontalAlignment.Left;
                        item.VerticalAlignment = VerticalAlignment.Center;
                        _menuItems.Add(item);
                        _itemsPanel.Children.Add(item);
                    }
                }
            }

            _containerBorder.Child = _itemsHost;
            Content = _containerBorder;
            ApplyTheme();
        }

        internal void OnItemSelected(DaisyMenuItem selectedItem)
        {
            foreach (var item in _menuItems)
            {
                item.IsActive = item == selectedItem;
            }

            UpdateSelectionIndicator(animate: IsLoaded && IsSelectionAnimationEnabled);
        }

        internal void RaiseItemInvoking(DaisyMenuItem item)
        {
            ItemInvoking?.Invoke(this, item);
        }

        internal void RaiseItemInvoked(DaisyMenuItem item)
        {
            ItemInvoked?.Invoke(this, item);
        }

        private void ApplyTheme()
        {
            // Update container colors for theme
            if (_containerBorder != null)
            {
                _containerBorder.Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
                _containerBorder.BorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            }

            foreach (var item in _menuItems)
            {
                item.Size = Size;
                item.ActiveBackground = ActiveBackground;
                item.ActiveForeground = ActiveForeground;
                item.RebuildVisual();
            }

            UpdateSelectionIndicator(animate: false);
        }

        private void UpdateItemAlignment()
        {
            if (_menuItems.Count == 0)
                return;

            var align = Orientation == Orientation.Vertical
                ? HorizontalAlignment.Stretch
                : HorizontalAlignment.Left;

            foreach (var item in _menuItems)
            {
                item.HorizontalAlignment = align;
            }
        }

        private void HideSelectionIndicator()
        {
            if (_selectionIndicator == null)
                return;

            StopSelectionStoryboard();
            _selectionIndicator.Opacity = 0;
            SetSelectionTarget(null);
        }

        private void UpdateSelectionIndicator(bool animate)
        {
            if (!IsSelectionAnimationEnabled)
            {
                HideSelectionIndicator();
                if (_selectionUpdateQueued)
                {
                    LayoutUpdated -= OnSelectionLayoutUpdated;
                    _selectionUpdateQueued = false;
                }
                return;
            }

            if (TryUpdateSelectionIndicator(animate))
            {
                if (_selectionUpdateQueued)
                {
                    LayoutUpdated -= OnSelectionLayoutUpdated;
                    _selectionUpdateQueued = false;
                }
            }
            else
            {
                QueueSelectionIndicatorUpdate(animate);
            }
        }

        private void QueueSelectionIndicatorUpdate(bool animate)
        {
            _pendingSelectionAnimate = animate;
            if (_selectionUpdateQueued)
                return;

            _selectionUpdateQueued = true;
            LayoutUpdated += OnSelectionLayoutUpdated;
        }

        private void OnSelectionLayoutUpdated(object? sender, object e)
        {
            if (TryUpdateSelectionIndicator(_pendingSelectionAnimate))
            {
                LayoutUpdated -= OnSelectionLayoutUpdated;
                _selectionUpdateQueued = false;
            }
        }

        private bool TryUpdateSelectionIndicator(bool animate)
        {
            if (!IsSelectionAnimationEnabled || _itemsHost == null || _selectionIndicator == null || _selectionTransform == null)
                return false;

            DaisyMenuItem? selectedItem = null;
            foreach (var item in _menuItems)
            {
                if (item.IsActive)
                {
                    selectedItem = item;
                    break;
                }
            }

            if (selectedItem == null)
            {
                HideSelectionIndicator();
                return true;
            }

            FrameworkElement? container = selectedItem.RootBorder;
            if (container != null)
            {
                if (container.ActualWidth <= 0 || container.ActualHeight <= 0)
                    return false;
            }
            else
            {
                container = selectedItem;
                if (container.ActualWidth <= 0 || container.ActualHeight <= 0)
                    return false;
            }

            if (container.ActualWidth <= 0 || container.ActualHeight <= 0 || _itemsHost.ActualWidth <= 0 || _itemsHost.ActualHeight <= 0)
                return false;

            SetSelectionTarget(container);

            var origin = container.TransformToVisual(_itemsHost);
            var rect = origin.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));

            var targetX = rect.X;
            var targetY = rect.Y;
            var targetWidth = rect.Width;
            var targetHeight = rect.Height;

            var resolvedBackground = selectedItem.ResolvedActiveBackground ?? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
            _selectionIndicator.Background = resolvedBackground;
            if (selectedItem.RootBorder != null)
            {
                _selectionIndicator.CornerRadius = selectedItem.RootBorder.CornerRadius;
            }

            var shouldAnimate = animate && _selectionIndicator.Opacity > 0;
            if (!shouldAnimate)
            {
                StopSelectionStoryboard();
                _selectionIndicator.Width = targetWidth;
                _selectionIndicator.Height = targetHeight;
                _selectionTransform.TranslateX = targetX;
                _selectionTransform.TranslateY = targetY;
                _selectionIndicator.Opacity = 1;
                return true;
            }

            var currentWidth = ResolveCurrentLength(_selectionIndicator.Width, _selectionIndicator.ActualWidth, targetWidth);
            var currentHeight = ResolveCurrentLength(_selectionIndicator.Height, _selectionIndicator.ActualHeight, targetHeight);
            var currentTranslateX = _selectionTransform.TranslateX;
            var currentTranslateY = _selectionTransform.TranslateY;

            if (_selectionIndicator.Opacity <= 0)
            {
                currentTranslateX = targetX;
                currentTranslateY = targetY;
            }

            StopSelectionStoryboard();
            var storyboard = FloweryAnimationHelpers.CreateStoryboard();
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(200);

            FloweryAnimationHelpers.AddDoubleAnimation(storyboard, _selectionIndicator, "Width", currentWidth, targetWidth, duration, easing);
            FloweryAnimationHelpers.AddDoubleAnimation(storyboard, _selectionIndicator, "Height", currentHeight, targetHeight, duration, easing);
            FloweryAnimationHelpers.AddPanAnimation(storyboard, _selectionTransform, "TranslateX", currentTranslateX, targetX, duration, easing);
            FloweryAnimationHelpers.AddPanAnimation(storyboard, _selectionTransform, "TranslateY", currentTranslateY, targetY, duration, easing);
            FloweryAnimationHelpers.AddFadeAnimation(storyboard, _selectionIndicator, _selectionIndicator.Opacity, 1, duration, easing);

            _selectionStoryboard = storyboard;
            storyboard.Begin();
            return true;
        }

        private static double ResolveCurrentLength(double length, double actual, double fallback)
        {
            if (!double.IsNaN(length) && length > 0)
                return length;

            if (actual > 0)
                return actual;

            return fallback;
        }

        private void StopSelectionStoryboard()
        {
            if (_selectionStoryboard == null)
                return;

            _selectionStoryboard.Stop();
            _selectionStoryboard = null;
        }

        private void SetSelectionTarget(FrameworkElement? target)
        {
            if (ReferenceEquals(_selectionTarget, target))
                return;

            if (_selectionTarget != null)
            {
                _selectionTarget.SizeChanged -= OnSelectionTargetSizeChanged;
            }

            _selectionTarget = target;
            if (_selectionTarget != null)
            {
                _selectionTarget.SizeChanged += OnSelectionTargetSizeChanged;
            }
        }

        private void OnSelectionTargetSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsSelectionAnimationEnabled)
                return;

            UpdateSelectionIndicator(animate: false);
        }
    }

    /// <summary>
    /// Individual menu item within a DaisyMenu.
    /// </summary>
    public partial class DaisyMenuItem : DaisyBaseContentControl
    {
        private Border? _rootBorder;
        private ContentPresenter? _contentPresenter;
        private object? _userContent;
        private bool _isPointerOver;

        internal DaisyMenu? ParentMenu { get; set; }
        internal DaisySize Size { get; set; } = DaisySize.Medium;
        internal Brush? ActiveBackground { get; set; }
        internal Brush? ActiveForeground { get; set; }
        internal Brush? ResolvedActiveBackground { get; private set; }
        internal Border? RootBorder => _rootBorder;

        public DaisyMenuItem()
        {
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            // Capture user content before replacing
            if (Content != null && !ReferenceEquals(Content, _rootBorder))
            {
                _userContent = Content;
            }

            BuildVisualTree();
            RebuildVisual();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            RebuildVisual();
        }

        #region IsActive
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(DaisyMenuItem),
                new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }
        #endregion

        #region IsDisabled
        public static readonly DependencyProperty IsDisabledProperty =
            DependencyProperty.Register(
                nameof(IsDisabled),
                typeof(bool),
                typeof(DaisyMenuItem),
                new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsDisabled
        {
            get => (bool)GetValue(IsDisabledProperty);
            set => SetValue(IsDisabledProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyMenuItem item && item.IsLoaded)
                item.RebuildVisual();
        }

        private void BuildVisualTree()
        {
            if (_rootBorder != null)
                return;

            _rootBorder = new Border
            {
                CornerRadius = new CornerRadius(8)
            };

            _contentPresenter = new ContentPresenter
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _rootBorder.Child = _contentPresenter;
            _rootBorder.PointerEntered += OnPointerEntered;
            _rootBorder.PointerExited += OnPointerExited;
            Content = _rootBorder;
        }

        private void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (IsDisabled)
                return;

            _isPointerOver = true;
            RebuildVisual();
        }

        private void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_isPointerOver)
            {
                _isPointerOver = false;
                RebuildVisual();
            }
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || IsDisabled)
                return;

            DaisyAccessibility.TryHandleEnterOrSpace(e, OnBaseClick);
        }

        protected override void OnBaseClick()
        {
            if (IsDisabled)
                return;

            // If the menu closes while the pointer is over this item,
            // PointerExited won't fire. Clear hover to avoid stale hover visuals on next open.
            _isPointerOver = false;

            ParentMenu?.RaiseItemInvoking(this);
            ParentMenu?.OnItemSelected(this);
            base.OnBaseClick();
            ParentMenu?.RaiseItemInvoked(this);
        }

        internal void RebuildVisual()
        {
            if (_rootBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Check for lightweight styling overrides
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyMenuItem", "Foreground");
            var activeFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyMenuItem", "ActiveForeground");
            var activeBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyMenuItem", "ActiveBackground");
            var hoverBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyMenuItem", "HoverBackground");

            var baseContentBrush = fgOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush");
            var primaryBrush = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush");
            var primaryContentBrush = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryContentBrush");
            var hoverBrush = hoverBgOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush");
            var useAnimatedSelection = ParentMenu?.IsSelectionAnimationEnabled == true;

            // Get sizing from tokens (guaranteed by EnsureDefaults)
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            var padding = DaisyResourceLookup.GetThickness($"DaisyMenu{sizeKey}Padding", new Thickness(8, 5, 8, 5));
            double fontSize = DaisyResourceLookup.GetDouble($"DaisyMenu{sizeKey}FontSize", 12);

            _rootBorder.Padding = padding;

            ResolvedActiveBackground = ActiveBackground ?? activeBgOverride ?? primaryBrush;

            // Apply colors based on state
            if (IsDisabled)
            {
                _rootBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                _rootBorder.Opacity = 0.5;
                IsTabStop = false;
            }
            else if (IsActive)
            {
                _rootBorder.Background = useAnimatedSelection
                    ? new SolidColorBrush(Microsoft.UI.Colors.Transparent)
                    : ResolvedActiveBackground;
                _rootBorder.Opacity = 1.0;
                IsTabStop = true;
            }
            else if (_isPointerOver)
            {
                _rootBorder.Background = hoverBrush ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                _rootBorder.Opacity = 1.0;
                IsTabStop = true;
            }
            else
            {
                _rootBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                _rootBorder.Opacity = 1.0;
                IsTabStop = true;
            }

            // Content
            if (_contentPresenter != null)
            {
                _contentPresenter.Content = _userContent;
                FontSize = fontSize;

                if (IsActive && ActiveForeground != null)
                {
                    Foreground = ActiveForeground;
                }
                else if (IsActive)
                {
                    Foreground = activeFgOverride ?? primaryContentBrush;
                }
                else
                {
                    Foreground = baseContentBrush;
                }
            }

            UpdateAutomationProperties();
        }

        private void UpdateAutomationProperties()
        {
            var name = GetAccessibleName();
            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

        private string? GetAccessibleName()
        {
            return DaisyAccessibility.GetAccessibleNameFromContent(_userContent);
        }

    }
}
