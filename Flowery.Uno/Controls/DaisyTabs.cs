using System;
using System.Collections.Generic;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// Defines the visual variant of the tabs.
    /// </summary>
    public enum DaisyTabVariant
    {
        None,
        Bordered,
        Lifted,
        Boxed
    }

    public enum DaisyTabWidthMode
    {
        Auto,
        Equal,
        Fixed
    }

    public enum DaisyTabPaletteColor
    {
        Default,
        Purple,
        Indigo,
        Pink,
        SkyBlue,
        Blue,
        Lime,
        Green,
        Yellow,
        Orange,
        Red,
        Gray
    }

    /// <summary>
    /// A tab control styled after DaisyUI's Tabs component.
    /// </summary>
    public partial class DaisyTabs : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Panel? _tabStrip;
        private ContentPresenter? _contentPresenter;
        private Border? _tabStripBorder;
        private int _selectedIndex = 0;
        private bool _isUpdatingAppearance;
        private static readonly IReadOnlyList<DaisyTabPaletteSwatch> TabPaletteSwatches =
            DaisyTabPaletteDefinitions.GetSwatches();
        private readonly List<DaisyTabItem> _items = [];
        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(DaisyTabVariant), typeof(DaisyTabs),
                new PropertyMetadata(DaisyTabVariant.Boxed, OnAppearanceChanged));

        public DaisyTabVariant Variant
        {
            get => (DaisyTabVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(DaisySize), typeof(DaisyTabs),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(DaisyTabs),
                new PropertyMetadata(0, OnSelectedIndexChanged));

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public static readonly DependencyProperty TabWidthModeProperty =
            DependencyProperty.Register(nameof(TabWidthMode), typeof(DaisyTabWidthMode), typeof(DaisyTabs),
                new PropertyMetadata(DaisyTabWidthMode.Auto, OnAppearanceChanged));

        public DaisyTabWidthMode TabWidthMode
        {
            get => (DaisyTabWidthMode)GetValue(TabWidthModeProperty);
            set => SetValue(TabWidthModeProperty, value);
        }

        public static readonly DependencyProperty TabMaxWidthProperty =
            DependencyProperty.Register(nameof(TabMaxWidth), typeof(double), typeof(DaisyTabs),
                new PropertyMetadata(double.PositiveInfinity, OnAppearanceChanged));

        public double TabMaxWidth
        {
            get => (double)GetValue(TabMaxWidthProperty);
            set => SetValue(TabMaxWidthProperty, value);
        }

        public static readonly DependencyProperty TabMinWidthProperty =
            DependencyProperty.Register(nameof(TabMinWidth), typeof(double), typeof(DaisyTabs),
                new PropertyMetadata(0.0, OnAppearanceChanged));

        public double TabMinWidth
        {
            get => (double)GetValue(TabMinWidthProperty);
            set => SetValue(TabMinWidthProperty, value);
        }

        public static readonly DependencyProperty EnableTabContextMenuProperty =
            DependencyProperty.Register(nameof(EnableTabContextMenu), typeof(bool), typeof(DaisyTabs),
                new PropertyMetadata(false));

        public bool EnableTabContextMenu
        {
            get => (bool)GetValue(EnableTabContextMenuProperty);
            set => SetValue(EnableTabContextMenuProperty, value);
        }

        #region Accessibility
        public static readonly DependencyProperty AccessibleTextProperty =
            DependencyProperty.Register(nameof(AccessibleText), typeof(string), typeof(DaisyTabs),
                new PropertyMetadata(null, OnAccessibilityChanged));

        public string? AccessibleText
        {
            get => (string?)GetValue(AccessibleTextProperty);
            set => SetValue(AccessibleTextProperty, value);
        }

        private static void OnAccessibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabs tabs)
            {
                tabs.UpdateAutomationProperties();
            }
        }
        #endregion

        public event EventHandler<DaisyTabItem>? CloseTabRequested;
        public event EventHandler<DaisyTabItem>? CloseOtherTabsRequested;
        public event EventHandler<DaisyTabItem>? CloseTabsToRightRequested;
        public event EventHandler<DaisyTabPaletteColorEventArgs>? TabPaletteColorChangeRequested;

        #region Attached Property: TabColor
        public static readonly DependencyProperty TabColorProperty =
            DependencyProperty.RegisterAttached("TabColor", typeof(DaisyColor), typeof(DaisyTabs),
                new PropertyMetadata(DaisyColor.Default, OnTabPaletteColorChanged));

        public static DaisyColor GetTabColor(DependencyObject element) => (DaisyColor)element.GetValue(TabColorProperty);
        public static void SetTabColor(DependencyObject element, DaisyColor value) => element.SetValue(TabColorProperty, value);
        #endregion

        #region Attached Property: TabPaletteColor
        public static readonly DependencyProperty TabPaletteColorProperty =
            DependencyProperty.RegisterAttached("TabPaletteColor", typeof(DaisyTabPaletteColor), typeof(DaisyTabs),
                new PropertyMetadata(DaisyTabPaletteColor.Default, OnTabPaletteColorChanged));

        public static DaisyTabPaletteColor GetTabPaletteColor(DependencyObject element) => (DaisyTabPaletteColor)element.GetValue(TabPaletteColorProperty);
        public static void SetTabPaletteColor(DependencyObject element, DaisyTabPaletteColor value) => element.SetValue(TabPaletteColorProperty, value);

        private static void OnTabPaletteColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabItem item)
            {
                item.Owner?.UpdateAppearance();
            }
            else if (d is FrameworkElement fe)
            {
                var tabs = FindParentTabs(fe);
                tabs?.UpdateAppearance();
            }
        }

        private static DaisyTabs? FindParentTabs(FrameworkElement element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is DaisyTabs tabs) return tabs;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
        #endregion

        #endregion

        public DaisyTabItem[] Items => [.. _items];

        public DaisyTabs()
        {
            DefaultStyleKey = typeof(DaisyTabs);
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootGrid != null)
            {
                UpdateAppearance();
                return;
            }

            CollectItemsFromContent();
            BuildVisualTree();
            UpdateAppearance();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            UpdateAppearance();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || _items.Count == 0)
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
                    SetSelection(_items.Count - 1);
                    break;
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    SetSelection(SelectedIndex);
                    break;
                default:
                    handled = false;
                    break;
            }

            e.Handled = handled;
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        private void CollectItemsFromContent()
        {
            if (Content is Panel panel)
            {
                var children = panel.Children.ToList();
                panel.Children.Clear();

                foreach (var child in children)
                {
                    if (child is TabViewItem tabItem)
                    {
                        var item = new DaisyTabItem { Header = tabItem.Header?.ToString() ?? "Tab", Content = tabItem.Content };
                        // Transfer attached properties
                        item.Owner = this;
                        SetTabColor(item, GetTabColor(tabItem));
                        SetTabPaletteColor(item, GetTabPaletteColor(tabItem));
                        _items.Add(item);
                    }
                    else if (child is DaisyTabItem daisyItem)
                    {
                        daisyItem.Owner = this;
                        _items.Add(daisyItem);
                    }
                }
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabs tabs)
            {
                tabs.UpdateAppearance();
            }
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyTabs tabs)
            {
                tabs._selectedIndex = (int)e.NewValue;
                tabs.UpdateSelection();
            }
        }

        /// <summary>
        /// Adds a tab to the control.
        /// </summary>
        public void AddTab(string header, object? content)
        {
            var item = new DaisyTabItem { Header = header, Content = content };
            item.Owner = this;
            _items.Add(item);

            if (_tabStrip != null)
            {
                AddTabButton(item, _items.Count - 1);
            }
        }

        /// <summary>
        /// Clears all tabs.
        /// </summary>
        public void ClearTabs()
        {
            _items.Clear();
            _tabStrip?.Children.Clear();
            if (_contentPresenter != null)
                _contentPresenter.Content = null;
        }

        private void BuildVisualTree()
        {
            _rootGrid = new Grid();
            _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Tab strip container
            _tabStripBorder = new Border
            {
                Padding = new Thickness(4),
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            if (TabWidthMode == DaisyTabWidthMode.Equal)
            {
                _tabStrip = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            }
            else
            {
                _tabStrip = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4
                };
            }

            _tabStripBorder.Child = _tabStrip;
            Grid.SetRow(_tabStripBorder, 0);
            _rootGrid.Children.Add(_tabStripBorder);

            // Content area
            _contentPresenter = new ContentPresenter
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            Grid.SetRow(_contentPresenter, 1);
            _rootGrid.Children.Add(_contentPresenter);

            Content = _rootGrid;

            // Add existing items
            for (int i = 0; i < _items.Count; i++)
            {
                AddTabButton(_items[i], i);
            }
        }

        private void UpdateTabWidths()
        {
            if (_tabStripBorder == null || _tabStrip == null) return;

            var mode = TabWidthMode;
            var maxWidth = TabMaxWidth;
            var minWidth = TabMinWidth;

            if (mode == DaisyTabWidthMode.Equal && _tabStrip is not Grid)
            {
                var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
                MoveTabButtons(_tabStrip, grid);
                _tabStrip = grid;
                _tabStripBorder.Child = grid;
            }
            else if (mode != DaisyTabWidthMode.Equal && _tabStrip is Grid)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4
                };
                MoveTabButtons(_tabStrip, panel);
                _tabStrip = panel;
                _tabStripBorder.Child = panel;
            }

            if (mode == DaisyTabWidthMode.Equal && _tabStrip is Grid equalGrid)
            {
                equalGrid.ColumnDefinitions.Clear();
                for (int i = 0; i < equalGrid.Children.Count; i++)
                {
                    equalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    if (equalGrid.Children[i] is FrameworkElement element)
                    {
                        Grid.SetColumn(element, i);
                    }
                }
            }

            foreach (var child in _tabStrip.Children)
            {
                if (child is Button button)
                {
                    switch (mode)
                    {
                        case DaisyTabWidthMode.Auto:
                            button.Width = double.NaN;
                            button.HorizontalAlignment = HorizontalAlignment.Left;
                            break;
                        case DaisyTabWidthMode.Equal:
                            // In Equal mode, the _tabStrip is a Grid with * columns
                            button.Width = double.NaN;
                            button.HorizontalAlignment = HorizontalAlignment.Stretch;
                            break;
                        case DaisyTabWidthMode.Fixed:
                            // Use Min/Max as proxies for Fixed if a specific TabWidth property is added later
                            button.Width = double.NaN;
                            button.HorizontalAlignment = HorizontalAlignment.Stretch;
                            break;
                    }

                    button.MaxWidth = double.IsNaN(maxWidth) ? double.PositiveInfinity : maxWidth;
                    button.MinWidth = minWidth;
                }
            }
        }

        private static void MoveTabButtons(Panel from, Panel to)
        {
            List<UIElement> children = [];
            foreach (var child in from.Children)
            {
                children.Add(child);
            }

            from.Children.Clear();
            foreach (var child in children)
            {
                to.Children.Add(child);
            }
        }

        private void AddTabButton(DaisyTabItem item, int index)
        {
            if (_tabStrip == null) return;

            var button = new Button
            {
                Content = new TextBlock { Text = item.Header },
                Tag = index,
                Padding = DaisyResourceLookup.GetDefaultPadding(Size),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                IsTabStop = false
            };

            if (!double.IsNaN(TabMaxWidth)) button.MaxWidth = TabMaxWidth;
            if (TabMinWidth > 0) button.MinWidth = TabMinWidth;

            button.Click += OnTabButtonClick;
            button.RightTapped += OnTabButtonRightTapped;

            // Apply grid sizing if in Equal mode
            if (TabWidthMode == DaisyTabWidthMode.Equal && _tabStrip is Grid grid)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(button, index);
            }

            // Sync properties to button for visual update
            SetTabColor(button, GetTabColor(item));
            SetTabPaletteColor(button, GetTabPaletteColor(item));

            if (EnableTabContextMenu)
            {
                button.ContextFlyout = CreateTabContextMenu(item, index);
            }

            _tabStrip.Children.Add(button);
        }

        private MenuFlyout CreateTabContextMenu(DaisyTabItem item, int index)
        {
            var flyout = new MenuFlyout();

            var closeItem = new MenuFlyoutItem { Text = "Close", Icon = new FontIcon { Glyph = "\uE711" } };
            closeItem.Click += (s, e) => CloseTabRequested?.Invoke(this, item);
            flyout.Items.Add(closeItem);

            var closeOthersItem = new MenuFlyoutItem { Text = "Close Others" };
            closeOthersItem.Click += (s, e) => CloseOtherTabsRequested?.Invoke(this, item);
            flyout.Items.Add(closeOthersItem);

            var closeToRightItem = new MenuFlyoutItem { Text = "Close to the Right" };
            closeToRightItem.Click += (s, e) => CloseTabsToRightRequested?.Invoke(this, item);
            flyout.Items.Add(closeToRightItem);

            flyout.Items.Add(new MenuFlyoutSeparator());

            var colorHeader = new MenuFlyoutItem { Text = "Tab Color", IsEnabled = false };
            flyout.Items.Add(colorHeader);

            var colorGridItem = new DaisyTabColorGridMenuItem
            {
                OwnerFlyout = flyout,
                TargetItem = item,
                Swatches = TabPaletteSwatches
            };
            colorGridItem.ColorSelected += OnTabPaletteSwatchSelected;
            flyout.Items.Add(colorGridItem);

            return flyout;
        }

        private void OnTabButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index)
            {
                DaisyAccessibility.FocusOnPointer(this);
                SelectedIndex = index;
            }
        }

        private void OnTabButtonRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is Button button && button.ContextFlyout is FlyoutBase flyout)
            {
                flyout.ShowAt(button);
                e.Handled = true;
            }
        }

        private void OnTabPaletteSwatchSelected(object? sender, DaisyTabPaletteColorEventArgs e)
        {
            TabPaletteColorChangeRequested?.Invoke(this, e);
        }

        private void UpdateSelection()
        {
            if (_tabStrip == null || _contentPresenter == null) return;
            if (_items.Count == 0) return;

            // Ensure index is valid
            if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
            {
                _selectedIndex = 0;
            }

            // Check for lightweight styling overrides
            var activeBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTabs", "ActiveBackground");
            var activeFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTabs", "ActiveForeground");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTabs", "Foreground");

            // Update button appearances
            for (int i = 0; i < _tabStrip.Children.Count; i++)
            {
                if (_tabStrip.Children[i] is Button button)
                {
                    bool isSelected = i == _selectedIndex;
                    // Sync properties from item to button for visual update
                    SetTabColor(button, GetTabColor(_items[i]));
                    SetTabPaletteColor(button, GetTabPaletteColor(_items[i]));
                    UpdateTabButtonAppearance(button, isSelected, activeBgOverride, activeFgOverride, fgOverride);
                }
            }

            // Update content
            _contentPresenter.Content = _items[_selectedIndex].Content;
            UpdateAutomationProperties();
        }

        private void UpdateTabButtonAppearance(Button button, bool isSelected, Brush? activeBgOverride, Brush? activeFgOverride, Brush? fgOverride)
        {
            if (button.Tag is not int index || index < 0 || index >= _items.Count) return;

            var tabColor = GetTabColor(button);
            var paletteColor = GetTabPaletteColor(button);
            GetPaletteBrushes(paletteColor, out var paletteBackground, out var paletteForeground);
            var accentBrush = paletteBackground ?? GetTabBrush(tabColor);
            var contentBrush = paletteForeground ?? GetTabContentBrush(tabColor);
            var baseForeground = fgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
            var baseBorder = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            var baselineThickness = DaisyResourceLookup.GetDouble("DaisyBorderThicknessMedium", 2);
            var thickThickness = DaisyResourceLookup.GetDouble("DaisyBorderThicknessThick", 3);
            var hasAccent = paletteColor != DaisyTabPaletteColor.Default || tabColor != DaisyColor.Default;

            if (Variant == DaisyTabVariant.Bordered)
            {
                if (isSelected && hasAccent)
                {
                    button.Background = accentBrush ?? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                    ApplyForeground(button, contentBrush ?? DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush"));
                    button.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                    button.BorderThickness = new Thickness(0);
                    button.BorderBrush = null;
                    button.Margin = new Thickness(0);
                }
                else
                {
                    button.Background = new SolidColorBrush(Colors.Transparent);
                    ApplyForeground(button, isSelected ? activeFgOverride ?? baseForeground : baseForeground);
                    button.FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
                    button.BorderThickness = new Thickness(0, 0, 0, isSelected ? thickThickness : baselineThickness);
                    button.BorderBrush = isSelected
                        ? activeBgOverride ?? accentBrush ?? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush")
                        : accentBrush ?? baseBorder;
                    button.Margin = isSelected ? new Thickness(0, 0, 0, -baselineThickness) : new Thickness(0);
                }
                SyncButtonStateResources(button);
                return;
            }

            if (Variant == DaisyTabVariant.Lifted)
            {
                if (isSelected && hasAccent)
                {
                    button.Background = accentBrush ?? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                    ApplyForeground(button, contentBrush ?? DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush"));
                    button.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                    button.BorderThickness = new Thickness(1, 1, 1, 0);
                    button.BorderBrush = accentBrush ?? baseBorder;
                }
                else
                {
                    button.Background = isSelected ? DaisyResourceLookup.GetBrush("DaisyBase100Brush") : new SolidColorBrush(Colors.Transparent);
                    ApplyForeground(button, isSelected ? activeFgOverride ?? baseForeground : baseForeground);
                    button.FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
                    button.BorderThickness = new Thickness(1, 1, 1, 0);
                    button.BorderBrush = accentBrush ?? baseBorder;
                }
                button.CornerRadius = DaisyResourceLookup.GetDefaultCornerRadius(Size);
                button.Margin = new Thickness(0, 0, 0, -baselineThickness);
                SyncButtonStateResources(button);
                return;
            }

            if (Variant == DaisyTabVariant.Boxed)
            {
                button.CornerRadius = DaisyResourceLookup.GetDefaultCornerRadius(Size);
                button.Margin = new Thickness(0);
                if (isSelected)
                {
                    button.Background = (hasAccent ? accentBrush : activeBgOverride) ?? DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                    ApplyForeground(button, (hasAccent ? contentBrush : activeFgOverride) ?? DaisyResourceLookup.GetBrush("DaisyPrimaryContentBrush"));
                    button.BorderThickness = new Thickness(0);
                    button.FontWeight = Microsoft.UI.Text.FontWeights.Medium;
                }
                else
                {
                    button.Background = new SolidColorBrush(Colors.Transparent);
                    ApplyForeground(button, baseForeground);
                    button.BorderThickness = new Thickness(1);
                    button.BorderBrush = accentBrush ?? baseBorder;
                    button.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                }
                SyncButtonStateResources(button);
                return;
            }

            // None/default variant
            button.Background = new SolidColorBrush(Colors.Transparent);
            ApplyForeground(button, accentBrush ?? (isSelected ? activeFgOverride ?? baseForeground : baseForeground));
            button.BorderThickness = new Thickness(0);
            button.Margin = new Thickness(0);
            button.FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
            SyncButtonStateResources(button);
        }

        private static Brush? GetTabBrush(DaisyColor color)
        {
            if (color != DaisyColor.Default)
            {
                string key = color switch
                {
                    DaisyColor.Primary => "DaisyPrimaryBrush",
                    DaisyColor.Secondary => "DaisySecondaryBrush",
                    DaisyColor.Accent => "DaisyAccentBrush",
                    DaisyColor.Info => "DaisyInfoBrush",
                    DaisyColor.Success => "DaisySuccessBrush",
                    DaisyColor.Warning => "DaisyWarningBrush",
                    DaisyColor.Error => "DaisyErrorBrush",
                    _ => "DaisyPrimaryBrush"
                };
                return DaisyResourceLookup.GetBrush(key);
            }

            return null;
        }

        private static Brush? GetTabContentBrush(DaisyColor color)
        {
            if (color != DaisyColor.Default)
            {
                string key = color switch
                {
                    DaisyColor.Primary => "DaisyPrimaryContentBrush",
                    DaisyColor.Secondary => "DaisySecondaryContentBrush",
                    DaisyColor.Accent => "DaisyAccentContentBrush",
                    DaisyColor.Info => "DaisyInfoContentBrush",
                    DaisyColor.Success => "DaisySuccessContentBrush",
                    DaisyColor.Warning => "DaisyWarningContentBrush",
                    DaisyColor.Error => "DaisyErrorContentBrush",
                    _ => "DaisyPrimaryContentBrush"
                };
                return DaisyResourceLookup.GetBrush(key);
            }

            return null;
        }
        
        private void GetPaletteBrushes(DaisyTabPaletteColor palette, out Brush? background, out Brush? foreground)
        {
            background = null;
            foreground = null;

            if (palette == DaisyTabPaletteColor.Default)
                return;

            background = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTabs", $"Palette{palette}Background");
            foreground = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTabs", $"Palette{palette}Foreground");

            if (!DaisyTabPaletteDefinitions.TryGet(palette, out var def))
                return;

            background ??= new SolidColorBrush(Flowery.Helpers.FloweryColorHelpers.ColorFromHex(def.BackgroundHex));
            foreground ??= new SolidColorBrush(Flowery.Helpers.FloweryColorHelpers.ColorFromHex(def.ForegroundHex));
        }

        private static void ApplyForeground(Button button, Brush foreground)
        {
            button.Foreground = foreground;
            if (button.Content is TextBlock textBlock)
            {
                textBlock.Foreground = foreground;
            }
        }

        private static void SyncButtonStateResources(Button button)
        {
            var background = button.Background ?? new SolidColorBrush(Colors.Transparent);
            var foreground = button.Foreground ?? new SolidColorBrush(Colors.Transparent);
            var borderBrush = button.BorderBrush ?? new SolidColorBrush(Colors.Transparent);

            button.Resources["ButtonBackgroundPointerOver"] = background;
            button.Resources["ButtonBackgroundPressed"] = background;
            button.Resources["ButtonForegroundPointerOver"] = foreground;
            button.Resources["ButtonForegroundPressed"] = foreground;
            button.Resources["ButtonBorderBrushPointerOver"] = borderBrush;
            button.Resources["ButtonBorderBrushPressed"] = borderBrush;
            button.Resources["ButtonBackgroundDisabled"] = background;
            button.Resources["ButtonForegroundDisabled"] = foreground;
            button.Resources["ButtonBorderBrushDisabled"] = borderBrush;

            if (button.IsPointerOver || button.IsPressed)
            {
                RefreshButtonVisualState(button);
            }
        }

        private static void RefreshButtonVisualState(Button button)
        {
            var targetState = button.IsEnabled
                ? (button.IsPressed ? "Pressed" : (button.IsPointerOver ? "PointerOver" : "Normal"))
                : "Disabled";

            VisualStateManager.GoToState(button, "Normal", false);
            if (targetState != "Normal")
            {
                VisualStateManager.GoToState(button, targetState, false);
            }
        }

        private void UpdateAppearance()
        {
            if (_isUpdatingAppearance) return;
            if (_tabStripBorder == null || _tabStrip == null) return;

            _isUpdatingAppearance = true;
            try
            {
                // Check for lightweight styling overrides
                var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTabs", "Background");
                var borderBrushOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyTabs", "BorderBrush");

                // Sync FontSize to the control for propagation
                FontSize = DaisyResourceLookup.GetDefaultFontSize(Size);

                // Update tab strip background based on variant
                switch (Variant)
                {
                    case DaisyTabVariant.Boxed:
                        _tabStripBorder.Background = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
                        _tabStripBorder.BorderBrush = borderBrushOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");
                        _tabStripBorder.BorderThickness = new Thickness(1); // Give Boxed some definition
                        _tabStripBorder.Padding = new Thickness(2); // Keep small padding for the pill look
                        _tabStripBorder.CornerRadius = new CornerRadius(12);
                        if (_contentPresenter != null) _contentPresenter.Margin = new Thickness(2, 0, 2, 0); // Align content with tabs
                        break;

                    case DaisyTabVariant.Bordered:
                        _tabStripBorder.Background = new SolidColorBrush(Colors.Transparent);
                        var baselineThickness = DaisyResourceLookup.GetDouble("DaisyBorderThicknessMedium", 2);
                        _tabStripBorder.BorderThickness = new Thickness(0, 0, 0, baselineThickness);
                        _tabStripBorder.BorderBrush = borderBrushOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");
                        _tabStripBorder.Padding = new Thickness(0);
                        _tabStripBorder.CornerRadius = new CornerRadius(0);
                        if (_contentPresenter != null) _contentPresenter.Margin = new Thickness(0);
                        break;

                    case DaisyTabVariant.Lifted:
                        _tabStripBorder.Background = new SolidColorBrush(Colors.Transparent);
                        var baselineThicknessLifted = DaisyResourceLookup.GetDouble("DaisyBorderThicknessMedium", 2);
                        _tabStripBorder.BorderThickness = new Thickness(0, 0, 0, baselineThicknessLifted);
                        _tabStripBorder.BorderBrush = borderBrushOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");
                        _tabStripBorder.Padding = new Thickness(0);
                        _tabStripBorder.CornerRadius = new CornerRadius(0);
                        if (_contentPresenter != null) _contentPresenter.Margin = new Thickness(0);
                        break;

                    default:
                        _tabStripBorder.Background = new SolidColorBrush(Colors.Transparent);
                        _tabStripBorder.BorderThickness = new Thickness(0);
                        _tabStripBorder.Padding = new Thickness(0);
                        _tabStripBorder.CornerRadius = new CornerRadius(0);
                        if (_contentPresenter != null) _contentPresenter.Margin = new Thickness(0);
                        break;
                }

                // Update tab button visual sizing based on Size property
                var padding = DaisyResourceLookup.GetDefaultPadding(Size);
                var fontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
                // Proportional height increase using spacing token
                var height = DaisyResourceLookup.GetDefaultHeight(Size) + DaisyResourceLookup.GetSpacing(Size) * 0.8;

                foreach (var child in _tabStrip.Children)
                {
                    if (child is Button button)
                    {
                        button.Padding = padding;
                        button.FontSize = fontSize;
                        button.Height = height;
                        button.VerticalAlignment = VerticalAlignment.Center;
                    }
                }

                UpdateTabWidths();
                UpdateSelection();
            }
            finally
            {
                _isUpdatingAppearance = false;
            }
        }

        private void MoveSelection(int delta)
        {
            if (_items.Count == 0)
                return;

            var current = SelectedIndex;
            if (current < 0)
            {
                current = 0;
            }

            var nextIndex = Math.Clamp(current + delta, 0, _items.Count - 1);
            SetSelection(nextIndex);
        }

        private void SetSelection(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            SelectedIndex = index;
        }

        private void UpdateAutomationProperties()
        {
            var name = AccessibleText;
            if (string.IsNullOrWhiteSpace(name))
            {
                var header = _items.Count > 0 && _selectedIndex >= 0 && _selectedIndex < _items.Count
                    ? _items[_selectedIndex].Header
                    : null;
                name = string.IsNullOrWhiteSpace(header) ? "Tabs" : $"Tabs: {header}";
            }

            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }
    }

    /// <summary>
    /// Represents a single tab item.
    /// </summary>
    public partial class DaisyTabItem : FrameworkElement
    {
        public string Header { get; set; } = string.Empty;
        public object? Content { get; set; }
        internal DaisyTabs? Owner { get; set; }
    }
    public class DaisyTabPaletteColorEventArgs(DaisyTabItem item, DaisyTabPaletteColor color) : EventArgs
    {
        public DaisyTabItem Item { get; } = item;
        public DaisyTabPaletteColor Color { get; } = color;
    }
}
