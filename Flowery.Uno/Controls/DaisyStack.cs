using System;
using System.Collections.Generic;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// Navigation direction for DaisyStack.
    /// </summary>
    public enum DaisyStackNavigation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Placement options for counters and indicators.
    /// </summary>
    public enum DaisyPlacement
    {
        Top,
        Bottom,
        Start,
        End
    }

    /// <summary>
    /// DaisyStack displays children in a stacked arrangement with optional navigation.
    /// When ShowNavigation is false (default), all children are visible with layered offsets.
    /// When ShowNavigation is true, only one item is visible at a time with prev/next arrows.
    /// </summary>
    public partial class DaisyStack : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Grid? _stackContainer;
        private DaisyButton? _previousButton;
        private DaisyButton? _nextButton;
        private TextBlock? _counterTextBlock;
        private readonly List<UIElement> _items = [];
        private int _selectedIndex;
        public DaisyStack()
        {
            DefaultStyleKey = typeof(DaisyStack);
            IsTabStop = true;
            TabFocusNavigation = KeyboardNavigationMode.Local;
            UseSystemFocusVisuals = true;
            KeyDown += OnKeyDown;
        }

        #region Dependency Properties
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(
                nameof(AccessibleText),
                typeof(string),
                typeof(DaisyStack),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyStack stack)
            {
                stack.UpdateAutomationProperties();
            }
        }

        public static readonly DependencyProperty ShowNavigationProperty =
            DependencyProperty.Register(
                nameof(ShowNavigation),
                typeof(bool),
                typeof(DaisyStack),
                new PropertyMetadata(false, OnLayoutChanged));

        /// <summary>
        /// When true, navigation arrows are shown and only one item is visible at a time.
        /// </summary>
        public bool ShowNavigation
        {
            get => (bool)GetValue(ShowNavigationProperty);
            set => SetValue(ShowNavigationProperty, value);
        }

        public static readonly DependencyProperty NavigationPlacementProperty =
            DependencyProperty.Register(
                nameof(NavigationPlacement),
                typeof(DaisyStackNavigation),
                typeof(DaisyStack),
                new PropertyMetadata(DaisyStackNavigation.Horizontal, OnLayoutChanged));

        public DaisyStackNavigation NavigationPlacement
        {
            get => (DaisyStackNavigation)GetValue(NavigationPlacementProperty);
            set => SetValue(NavigationPlacementProperty, value);
        }

        public static readonly DependencyProperty NavigationColorProperty =
            DependencyProperty.Register(
                nameof(NavigationColor),
                typeof(DaisyColor),
                typeof(DaisyStack),
                new PropertyMetadata(DaisyColor.Default, OnAppearanceChanged));

        public DaisyColor NavigationColor
        {
            get => (DaisyColor)GetValue(NavigationColorProperty);
            set => SetValue(NavigationColorProperty, value);
        }

        public static readonly DependencyProperty ShowCounterProperty =
            DependencyProperty.Register(
                nameof(ShowCounter),
                typeof(bool),
                typeof(DaisyStack),
                new PropertyMetadata(false, OnLayoutChanged));

        /// <summary>
        /// When true, shows a counter label like "1 / 5".
        /// </summary>
        public bool ShowCounter
        {
            get => (bool)GetValue(ShowCounterProperty);
            set => SetValue(ShowCounterProperty, value);
        }

        public static readonly DependencyProperty CounterPlacementProperty =
            DependencyProperty.Register(
                nameof(CounterPlacement),
                typeof(DaisyPlacement),
                typeof(DaisyStack),
                new PropertyMetadata(DaisyPlacement.Bottom, OnLayoutChanged));

        public DaisyPlacement CounterPlacement
        {
            get => (DaisyPlacement)GetValue(CounterPlacementProperty);
            set => SetValue(CounterPlacementProperty, value);
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedIndex),
                typeof(int),
                typeof(DaisyStack),
                new PropertyMetadata(0, OnSelectedIndexChanged));

        /// <summary>
        /// Gets or sets the currently selected item index (0-based).
        /// </summary>
        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, CoerceIndex(value));
        }

        public static readonly DependencyProperty StackOpacityProperty =
            DependencyProperty.Register(
                nameof(StackOpacity),
                typeof(double),
                typeof(DaisyStack),
                new PropertyMetadata(0.6, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the opacity of non-active items in static (non-navigation) mode.
        /// </summary>
        public double StackOpacity
        {
            get => (double)GetValue(StackOpacityProperty);
            set => SetValue(StackOpacityProperty, value);
        }

        /// <summary>
        /// Gets the total number of items in the stack.
        /// </summary>
        public int ItemCount => _items.Count;

        #endregion

        #region Callbacks

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyStack stack)
            {
                stack.RebuildLayout();
            }
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyStack stack)
            {
                stack.UpdateItemVisibility();
                stack.UpdateCounter();
                stack.UpdateAutomationProperties();
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyStack stack)
            {
                stack.ApplyAll();
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
            UpdateAutomationProperties();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Capture user content
            var userContent = Content;
            Content = null;

            _rootGrid = new Grid();

            // Stack container
            _stackContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Collect items from user content
            _items.Clear();
            if (userContent is Panel panel)
            {
                while (panel.Children.Count > 0)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);
                    _items.Add(child);
                }
            }
            else if (userContent is UIElement element)
            {
                _items.Add(element);
            }

            // Add items to stack container (last item goes first for z-order)
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                _stackContainer.Children.Add(_items[i]);
            }

            RebuildLayout();

            Content = _rootGrid;

            UpdateItemVisibility();
        }

        private void RebuildLayout()
        {
            if (_rootGrid == null || _stackContainer == null)
                return;

            _rootGrid.Children.Clear();
            _rootGrid.RowDefinitions.Clear();
            _rootGrid.ColumnDefinitions.Clear();

            if (ShowNavigation)
            {
                BuildNavigationLayout();
            }
            else
            {
                // Simple stack layout
                _rootGrid.Children.Add(_stackContainer);
            }

            UpdateItemVisibility();
            UpdateCounter();
        }

        private void BuildNavigationLayout()
        {
            if (_rootGrid == null || _stackContainer == null)
                return;

            bool isHorizontal = NavigationPlacement == DaisyStackNavigation.Horizontal;

            // Create buttons
            var previousArrow = !isHorizontal ? "\u2191" : "\u2190";
            var nextArrow = !isHorizontal ? "\u2193" : "\u2192";

            _previousButton = new DaisyButton
            {
                Shape = DaisyButtonShape.Circle,
                Size = DaisySize.Small,
                Opacity = 0.8,
                Content = CreateArrowIcon(previousArrow),
                IsTabStop = false
            };
            _previousButton.Click += OnPreviousClick;

            _nextButton = new DaisyButton
            {
                Shape = DaisyButtonShape.Circle,
                Size = DaisySize.Small,
                Opacity = 0.8,
                Content = CreateArrowIcon(nextArrow),
                IsTabStop = false
            };
            _nextButton.Click += OnNextClick;

            // Counter
            _counterTextBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8)
            };

            if (isHorizontal)
            {
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                _previousButton.VerticalAlignment = VerticalAlignment.Center;
                _nextButton.VerticalAlignment = VerticalAlignment.Center;

                Grid.SetColumn(_previousButton, 0);
                Grid.SetColumn(_stackContainer, 1);
                Grid.SetColumn(_nextButton, 2);

                _rootGrid.Children.Add(_previousButton);
                _rootGrid.Children.Add(_stackContainer);
                _rootGrid.Children.Add(_nextButton);
            }
            else
            {
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                _previousButton.HorizontalAlignment = HorizontalAlignment.Center;
                _nextButton.HorizontalAlignment = HorizontalAlignment.Center;

                Grid.SetRow(_previousButton, 0);
                Grid.SetRow(_stackContainer, 1);
                Grid.SetRow(_nextButton, 2);

                _rootGrid.Children.Add(_previousButton);
                _rootGrid.Children.Add(_stackContainer);
                _rootGrid.Children.Add(_nextButton);
            }

            // Add counter based on placement
            if (ShowCounter)
            {
                AddCounterToLayout();
            }

            ApplyButtonColors();
        }

        private void AddCounterToLayout()
        {
            if (_rootGrid == null || _counterTextBlock == null)
                return;

            // Simplified: add counter overlaid or below content
            // For now, add to the main grid
            _counterTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
            _counterTextBlock.VerticalAlignment = CounterPlacement == DaisyPlacement.Top
                ? VerticalAlignment.Top
                : VerticalAlignment.Bottom;
            _counterTextBlock.Margin = new Thickness(0, 8, 0, 8);

            // Add on top of stack container column/row
            if (_rootGrid.ColumnDefinitions.Count > 0)
            {
                Grid.SetColumn(_counterTextBlock, 1);
            }
            if (_rootGrid.RowDefinitions.Count > 0)
            {
                Grid.SetRow(_counterTextBlock, 1);
            }

            _rootGrid.Children.Add(_counterTextBlock);
        }

        private static TextBlock CreateArrowIcon(string arrow)
        {
            return new TextBlock
            {
                Text = arrow,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        #endregion

        #region Navigation

        private void OnPreviousClick(object sender, RoutedEventArgs e)
        {
            DaisyAccessibility.FocusOnPointer(this);
            Previous();
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            DaisyAccessibility.FocusOnPointer(this);
            Next();
        }

        /// <summary>
        /// Navigate to the previous item.
        /// </summary>
        public void Previous()
        {
            if (_items.Count == 0) return;
            SelectedIndex = CoerceIndex(SelectedIndex - 1);
        }

        /// <summary>
        /// Navigate to the next item.
        /// </summary>
        public void Next()
        {
            if (_items.Count == 0) return;
            SelectedIndex = CoerceIndex(SelectedIndex + 1);
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Handled)
                return;

            switch (e.Key)
            {
                case VirtualKey.Left when NavigationPlacement == DaisyStackNavigation.Horizontal:
                case VirtualKey.Up when NavigationPlacement == DaisyStackNavigation.Vertical:
                    Previous();
                    e.Handled = true;
                    break;
                case VirtualKey.Right when NavigationPlacement == DaisyStackNavigation.Horizontal:
                case VirtualKey.Down when NavigationPlacement == DaisyStackNavigation.Vertical:
                    Next();
                    e.Handled = true;
                    break;
                case VirtualKey.Home:
                    SelectedIndex = 0;
                    e.Handled = true;
                    break;
                case VirtualKey.End:
                    if (ItemCount > 0)
                    {
                        SelectedIndex = ItemCount - 1;
                    }
                    e.Handled = true;
                    break;
            }
        }

        private int CoerceIndex(int value)
        {
            if (_items.Count == 0) return 0;
            if (value < 0) return _items.Count - 1; // Wrap around
            if (value >= _items.Count) return 0;    // Wrap around
            return value;
        }

        private void UpdateAutomationProperties()
        {
            var name = string.IsNullOrWhiteSpace(AccessibleText) ? "Stack" : AccessibleText;
            AutomationProperties.SetName(this, name);
        }

        private void UpdateItemVisibility()
        {
            if (_stackContainer == null || _items.Count == 0)
                return;

            _selectedIndex = Math.Max(0, Math.Min(_items.Count - 1, SelectedIndex));

            if (ShowNavigation)
            {
                // Only show selected item
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i] is FrameworkElement fe)
                    {
                        fe.Visibility = i == _selectedIndex ? Visibility.Visible : Visibility.Collapsed;
                        fe.Opacity = 1;
                        fe.RenderTransform = null;
                    }
                }
            }
            else
            {
                // Show all items with stacked effect
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i] is FrameworkElement fe)
                    {
                        fe.Visibility = Visibility.Visible;

                        if (i == 0)
                        {
                            Canvas.SetZIndex(fe, 10);
                            fe.Opacity = 1;
                            fe.RenderTransform = null;
                        }
                        else
                        {
                            Canvas.SetZIndex(fe, 10 - i);

                            var offset = i * 4;
                            var scale = Math.Max(0.8, 1.0 - 0.05 * i);
                            var opacity = Math.Max(0.3, StackOpacity - 0.1 * (i - 1));

                            fe.Opacity = opacity;
                            var transformGroup = new TransformGroup();
                            transformGroup.Children.Add(new TranslateTransform { X = offset, Y = offset });
                            transformGroup.Children.Add(new ScaleTransform { ScaleX = scale, ScaleY = scale });
                            fe.RenderTransform = transformGroup;
                        }
                    }
                }
            }
        }

        private void UpdateCounter()
        {
            if (_counterTextBlock == null)
                return;

            _counterTextBlock.Text = $"{_selectedIndex + 1} / {_items.Count}";
            _counterTextBlock.Visibility = ShowCounter ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            ApplyButtonColors();
            UpdateCounter();
        }

        private void ApplyButtonColors()
        {
            if (_previousButton == null || _nextButton == null)
                return;

            var variant = NavigationColor switch
            {
                DaisyColor.Primary => DaisyButtonVariant.Primary,
                DaisyColor.Secondary => DaisyButtonVariant.Secondary,
                DaisyColor.Accent => DaisyButtonVariant.Accent,
                DaisyColor.Neutral => DaisyButtonVariant.Neutral,
                DaisyColor.Info => DaisyButtonVariant.Info,
                DaisyColor.Success => DaisyButtonVariant.Success,
                DaisyColor.Warning => DaisyButtonVariant.Warning,
                DaisyColor.Error => DaisyButtonVariant.Error,
                _ => DaisyButtonVariant.Neutral
            };

            _previousButton.Variant = variant;
            _nextButton.Variant = variant;

            if (_counterTextBlock != null)
            {
                _counterTextBlock.Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            }
        }

        #endregion
    }
}
