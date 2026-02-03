using System;
using System.Collections.Generic;
using Flowery.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Flowery.Helpers;
using Flowery.Theming;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// Event args for dock item selection.
    /// </summary>
    public class DockItemSelectedEventArgs(UIElement item, int index) : EventArgs
    {
        public UIElement Item { get; } = item;
        public int Index { get; } = index;
    }

    /// <summary>
    /// A dock/taskbar control styled after macOS dock.
    /// Displays items horizontally with optional magnification on hover.
    /// </summary>
    /// <remarks>
    /// For proper icon scaling, use <see cref="DaisyIconText"/> as dock items.
    /// DaisyIconText uses Viewbox internally which ensures icons scale correctly
    /// when the dock size changes.
    /// <code>
    /// &lt;daisy:DaisyDock Size="Medium"&gt;
    ///     &lt;StackPanel Orientation="Horizontal"&gt;
    ///         &lt;daisy:DaisyIconText IconData="{StaticResource DaisyIconHome}" /&gt;
    ///         &lt;daisy:DaisyIconText IconData="{StaticResource DaisyIconSettings}" /&gt;
    ///     &lt;/StackPanel&gt;
    /// &lt;/daisy:DaisyDock&gt;
    /// </code>
    /// </remarks>
    public partial class DaisyDock : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _dockBorder;
        private Grid? _itemsHost;
        private StackPanel? _itemsPanel;
        private readonly List<UIElement> _dockItems = [];
        private int _selectedIndex = -1;
        private Border? _selectionIndicator;
        private CompositeTransform? _selectionTransform;
        private Storyboard? _selectionStoryboard;
        private bool _selectionUpdateQueued;
        private bool _pendingSelectionAnimate;

        public DaisyDock()
        {
            DefaultStyleKey = typeof(DaisyDock);
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyDock),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty AutoSelectProperty =
            DependencyProperty.Register(
                nameof(AutoSelect),
                typeof(bool),
                typeof(DaisyDock),
                new PropertyMetadata(true));

        /// <summary>
        /// When true, clicking an item automatically selects it and applies the active style.
        /// </summary>
        public bool AutoSelect
        {
            get => (bool)GetValue(AutoSelectProperty);
            set => SetValue(AutoSelectProperty, value);
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedIndex),
                typeof(int),
                typeof(DaisyDock),
                new PropertyMetadata(-1, OnSelectedIndexChanged));

        /// <summary>
        /// Gets or sets the currently selected item index.
        /// </summary>
        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        /// <summary>
        /// Event raised when a dock item is selected.
        /// </summary>
        public event EventHandler<DockItemSelectedEventArgs>? ItemSelected;

        #endregion

        #region Selection Animation

        public static readonly DependencyProperty IsSelectionAnimationEnabledProperty =
            DependencyProperty.Register(
                nameof(IsSelectionAnimationEnabled),
                typeof(bool),
                typeof(DaisyDock),
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
            if (d is DaisyDock dock)
            {
                dock.UpdateSelection();
            }
        }

        #endregion

        #region Accessibility
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyDock),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDock dock)
            {
                dock.UpdateAutomationProperties();
            }
        }
        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDock dock)
            {
                dock.ApplyAll();
            }
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyDock dock)
            {
                dock.UpdateSelection();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                ApplyAll();
                return;
            }

            BuildVisualTree();
            ApplyAll();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || _dockItems.Count == 0)
                return;

            var handled = true;
            switch (e.Key)
            {
                case VirtualKey.Left:
                case VirtualKey.Up:
                    MoveSelection(-1);
                    break;
                case VirtualKey.Right:
                case VirtualKey.Down:
                    MoveSelection(1);
                    break;
                case VirtualKey.Home:
                    SetSelection(0);
                    break;
                case VirtualKey.End:
                    SetSelection(_dockItems.Count - 1);
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    ActivateSelection();
                    break;
                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
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
            // Capture user content (should be a Panel with dock items)
            var userContent = Content;
            Content = null;

            _rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            _dockBorder = new Border
            {
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(8)
            };

            _itemsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };

            _selectionIndicator = new Border
            {
                Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"),
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

            // Collect items from user content
            _dockItems.Clear();
            if (userContent is Panel panel)
            {
                // Move children to dock items panel
                while (panel.Children.Count > 0)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);
                    AddDockItem(child);
                }
            }
            else if (userContent is UIElement element)
            {
                AddDockItem(element);
            }

            _dockBorder.Child = _itemsHost;
            _rootGrid.Children.Add(_dockBorder);
            Content = _rootGrid;
        }

        private void AddDockItem(UIElement element)
        {
            _dockItems.Add(element);

            // Wrap in a container for styling
            var container = new Border
            {
                Padding = new Thickness(4),
                CornerRadius = new CornerRadius(8),
                Tag = _dockItems.Count - 1
            };

            // Subscribe to pointer events
            container.PointerEntered += OnItemPointerEntered;
            container.PointerExited += OnItemPointerExited;
            container.Tapped += OnItemTapped;

            container.Child = element;
            _itemsPanel!.Children.Add(container);
        }

        private void OnItemPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border container)
            {
                var index = container.Tag is int i ? i : -1;
                if (IsSelectionAnimationEnabled && index == _selectedIndex)
                    return;

                container.Background = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            }
        }

        private void OnItemPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border container)
            {
                var index = container.Tag is int i ? i : -1;
                if (IsSelectionAnimationEnabled)
                {
                    container.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                }
                else
                {
                    container.Background = index == _selectedIndex
                        ? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush")
                        : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                }
            }
        }

        private void OnItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Border container && container.Tag is int index)
            {
                if (AutoSelect)
                {
                    SelectedIndex = index;
                }

                if (index >= 0 && index < _dockItems.Count)
                {
                    ItemSelected?.Invoke(this, new DockItemSelectedEventArgs(_dockItems[index], index));
                }

                DaisyAccessibility.FocusOnPointer(this);
                UpdateAutomationProperties();
            }
        }

        private void UpdateSelection()
        {
            if (_itemsPanel == null) return;

            _selectedIndex = SelectedIndex;
            var animateSelection = IsLoaded && IsSelectionAnimationEnabled;

            for (int i = 0; i < _itemsPanel.Children.Count; i++)
            {
                if (_itemsPanel.Children[i] is Border container)
                {
                    if (IsSelectionAnimationEnabled)
                    {
                        container.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                    }
                    else
                    {
                        container.Background = i == _selectedIndex
                            ? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush")
                            : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                    }
                }
            }

            if (IsSelectionAnimationEnabled)
            {
                UpdateSelectionIndicator(animateSelection);
            }
            else
            {
                HideSelectionIndicator();
            }

            UpdateAutomationProperties();
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_dockBorder == null || _itemsPanel == null)
                return;

            ApplySizing();
            ApplyColors();
            UpdateAutomationProperties();
        }

        private void ApplySizing()
        {
            if (_dockBorder == null || _itemsPanel == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Get size key for token lookup
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);

            // Look up item size from design tokens
            double itemSize = DaisyResourceLookup.GetDouble($"DaisyDock{sizeKey}ItemSize", 32);

            // Look up spacing from design tokens
            double spacing = DaisyResourceLookup.GetDouble($"DaisyDock{sizeKey}Spacing", 4);

            _itemsPanel.Spacing = spacing;

            // Icon size ratio from design tokens
            double iconSizeRatio = DaisyResourceLookup.GetDouble("DaisyDockIconSizeRatio", 0.6);
            double iconSize = itemSize * iconSizeRatio;

            // Apply size to all dock items
            for (int i = 0; i < _itemsPanel.Children.Count; i++)
            {
                if (_itemsPanel.Children[i] is Border container && container.Child != null)
                {
                    if (container.Child is FrameworkElement fe)
                    {
                        fe.Width = itemSize;
                        fe.Height = itemSize;
                    }

                    // If the child is DaisyIconText, set its IconSize to scale properly
                    if (container.Child is DaisyIconText iconText)
                    {
                        iconText.IconSize = iconSize;
                        // Prevent global size from overriding our explicit sizing
                        FlowerySizeManager.SetIgnoreGlobalSize(iconText, true);
                    }
                }
            }
        }

        private void ApplyColors()
        {
            if (_dockBorder == null)
                return;

            // Check for lightweight styling overrides
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyDock", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyDock", "BorderBrush");

            _dockBorder.Background = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _dockBorder.BorderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            _dockBorder.BorderThickness = new Thickness(1);

            UpdateSelection();
        }

        #endregion

        private void MoveSelection(int delta)
        {
            if (_dockItems.Count == 0)
                return;

            var currentIndex = SelectedIndex < 0 ? 0 : SelectedIndex;
            var nextIndex = Math.Clamp(currentIndex + delta, 0, _dockItems.Count - 1);
            SetSelection(nextIndex);
        }

        private void SetSelection(int index)
        {
            if (index < 0 || index >= _dockItems.Count)
                return;

            SelectedIndex = index;
        }

        private void ActivateSelection()
        {
            if (_dockItems.Count == 0)
                return;

            if (SelectedIndex < 0)
            {
                SelectedIndex = 0;
            }

            if (SelectedIndex >= 0 && SelectedIndex < _dockItems.Count)
            {
                ItemSelected?.Invoke(this, new DockItemSelectedEventArgs(_dockItems[SelectedIndex], SelectedIndex));
            }
        }

        private void UpdateAutomationProperties()
        {
            var name = !string.IsNullOrWhiteSpace(AccessibleText) ? AccessibleText : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                var itemName = GetItemAccessibleName(SelectedIndex);
                name = string.IsNullOrWhiteSpace(itemName) ? "Dock" : $"Dock: {itemName}";
            }

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

        private void HideSelectionIndicator()
        {
            if (_selectionIndicator == null)
                return;

            StopSelectionStoryboard();
            _selectionIndicator.Opacity = 0;
        }

        private void UpdateSelectionIndicator(bool animate)
        {
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
            if (!IsSelectionAnimationEnabled || _itemsPanel == null || _itemsHost == null || _selectionIndicator == null || _selectionTransform == null)
                return false;

            if (_selectedIndex < 0 || _selectedIndex >= _itemsPanel.Children.Count)
            {
                HideSelectionIndicator();
                return true;
            }

            if (_itemsPanel.Children[_selectedIndex] is not Border container)
            {
                HideSelectionIndicator();
                return true;
            }

            if (container.ActualWidth <= 0 || container.ActualHeight <= 0 || _itemsHost.ActualWidth <= 0 || _itemsHost.ActualHeight <= 0)
                return false;

            var origin = container.TransformToVisual(_itemsHost);
            var point = origin.TransformPoint(new Windows.Foundation.Point(0, 0));

            var targetX = point.X;
            var targetY = point.Y;
            var targetWidth = container.ActualWidth;
            var targetHeight = container.ActualHeight;

            _selectionIndicator.Background = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
            _selectionIndicator.CornerRadius = container.CornerRadius;

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

        private string? GetItemAccessibleName(int index)
        {
            if (index < 0 || index >= _dockItems.Count)
                return null;

            var item = _dockItems[index];
            switch (item)
            {
                case DaisyIconText iconText when !string.IsNullOrWhiteSpace(iconText.Text):
                    return iconText.Text;
                case TextBlock textBlock when !string.IsNullOrWhiteSpace(textBlock.Text):
                    return textBlock.Text;
                case ContentControl control:
                    if (control.Content is string contentText && !string.IsNullOrWhiteSpace(contentText))
                        return contentText;
                    if (control.Content is TextBlock contentTextBlock && !string.IsNullOrWhiteSpace(contentTextBlock.Text))
                        return contentTextBlock.Text;
                    break;
            }

            var automationName = AutomationProperties.GetName(item);
            return string.IsNullOrWhiteSpace(automationName) ? null : automationName;
        }
    }
}
