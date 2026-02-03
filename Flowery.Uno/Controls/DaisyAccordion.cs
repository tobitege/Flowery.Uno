using Flowery.Helpers;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using Windows.System;

namespace Flowery.Controls
{

    /// <summary>
    /// An accordion control that displays multiple collapsible sections.
    /// Only one section can be expanded at a time.
    /// </summary>
    public partial class DaisyAccordion : DaisyBaseContentControl
    {
        private StackPanel? _itemsPanel;
        private readonly List<DaisyAccordionItem> _accordionItems = [];

        public DaisyAccordion()
        {
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

        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyCollapseVariant),
                typeof(DaisyAccordion),
                new PropertyMetadata(DaisyCollapseVariant.Arrow, OnVariantChanged));

        public DaisyCollapseVariant Variant
        {
            get => (DaisyCollapseVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAccordion accordion)
                accordion.SyncItemVariants();
        }
        #endregion

        #region ExpandedIndex
        public static readonly DependencyProperty ExpandedIndexProperty =
            DependencyProperty.Register(
                nameof(ExpandedIndex),
                typeof(int),
                typeof(DaisyAccordion),
                new PropertyMetadata(-1, OnExpandedIndexChanged));

        public int ExpandedIndex
        {
            get => (int)GetValue(ExpandedIndexProperty);
            set => SetValue(ExpandedIndexProperty, value);
        }

        private static void OnExpandedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAccordion accordion)
                accordion.UpdateExpandedStates();
        }
        #endregion

        private void CollectAndDisplay()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            _itemsPanel = new StackPanel { Spacing = 4 };
            _accordionItems.Clear();

            // Collect DaisyAccordionItem children from Content
            if (Content is Panel panel)
            {
                var children = new List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                panel.Children.Clear();

                int index = 0;
                foreach (var child in children)
                {
                    if (child is DaisyAccordionItem item)
                    {
                        item.Index = index;
                        item.Variant = Variant;
                        item.ParentAccordion = this;
                        _accordionItems.Add(item);
                        _itemsPanel.Children.Add(item);
                        index++;
                    }
                }
            }

            Content = _itemsPanel;
            UpdateExpandedStates();
            ApplyTheme();
        }

        internal void OnItemExpanded(DaisyAccordionItem expandedItem)
        {
            int index = _accordionItems.IndexOf(expandedItem);
            if (index >= 0)
            {
                ExpandedIndex = index;
            }

            // Collapse other items
            foreach (var item in _accordionItems)
            {
                if (item != expandedItem && item.IsExpanded)
                {
                    item.IsExpanded = false;
                }
            }
        }

        private void UpdateExpandedStates()
        {
            for (int i = 0; i < _accordionItems.Count; i++)
            {
                _accordionItems[i].IsExpanded = i == ExpandedIndex;
            }
        }

        private void SyncItemVariants()
        {
            foreach (var item in _accordionItems)
            {
                item.Variant = Variant;
                item.RebuildVisual();
            }
        }

        private void ApplyTheme()
        {
            foreach (var item in _accordionItems)
            {
                item.RebuildVisual();
            }
        }
    }

    /// <summary>
    /// Individual accordion item with a header and collapsible content.
    /// </summary>
    public partial class DaisyAccordionItem : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private Border? _headerBorder;
        private TextBlock? _headerText;
        private Microsoft.UI.Xaml.Shapes.Path? _indicator;
        private ContentPresenter? _contentPresenter;
        private object? _userContent;
        private bool _isFocused;

        internal DaisyAccordion? ParentAccordion { get; set; }
        internal int Index { get; set; }

        public DaisyAccordionItem()
        {
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            // Capture user content before replacing
            if (Content != null && !ReferenceEquals(Content, _rootGrid))
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

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _headerBorder ?? base.GetNeumorphicHostElement();
        }

        #region Header
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(DaisyAccordionItem),
                new PropertyMetadata(string.Empty, OnAppearanceChanged));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
        #endregion

        #region IsExpanded
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
                nameof(IsExpanded),
                typeof(bool),
                typeof(DaisyAccordionItem),
                new PropertyMetadata(false, OnExpandedChanged));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        private static void OnExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAccordionItem item)
            {
                item.RebuildVisual();
                if (item.IsExpanded)
                {
                    item.ParentAccordion?.OnItemExpanded(item);
                }
            }
        }
        #endregion

        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyCollapseVariant),
                typeof(DaisyAccordionItem),
                new PropertyMetadata(DaisyCollapseVariant.Arrow, OnAppearanceChanged));

        public DaisyCollapseVariant Variant
        {
            get => (DaisyCollapseVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAccordionItem item)
                item.RebuildVisual();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // Header row
            _headerBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12)
            };
            
            var headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            _headerText = new TextBlock
            {
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_headerText, 0);
            headerGrid.Children.Add(_headerText);

            _indicator = new Microsoft.UI.Xaml.Shapes.Path
            {
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_indicator, 1);
            headerGrid.Children.Add(_indicator);

            _headerBorder.Child = headerGrid;
            _headerBorder.PointerPressed += OnHeaderPressed;
            Grid.SetRow(_headerBorder, 0);
            _rootGrid.Children.Add(_headerBorder);

            // Content row
            _contentPresenter = new ContentPresenter
            {
                Margin = new Thickness(16, 8, 16, 16)
            };
            Grid.SetRow(_contentPresenter, 1);
            _rootGrid.Children.Add(_contentPresenter);

            Content = _rootGrid;
        }

        private void OnHeaderPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            DaisyAccessibility.FocusOnPointer(this);
            ToggleExpanded();
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || !IsEnabled)
                return;

            DaisyAccessibility.TryHandleEnterOrSpace(e, ToggleExpanded);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            _isFocused = ReferenceEquals(e.OriginalSource, this);
            RebuildVisual();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            _isFocused = false;
            RebuildVisual();
        }

        private void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        internal void RebuildVisual()
        {
            if (_rootGrid == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Check for lightweight styling overrides
            var headerBgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyAccordionItem", "HeaderBackground");
            var headerBorderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyAccordionItem", "HeaderBorderBrush");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyAccordionItem", "Foreground");

            var base200Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush");
            var base300Brush = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush");
            var baseContentBrush = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush");

            // Header styling
            if (_headerBorder != null)
            {
                _headerBorder.Background = headerBgOverride ?? base200Brush;
                _headerBorder.BorderBrush = _isFocused
                    ? DaisyResourceLookup.GetBrush("DaisyAccentBrush")
                    : headerBorderOverride ?? base300Brush;
                _headerBorder.BorderThickness = new Thickness(1);
            }

            if (_headerText != null)
            {
                _headerText.Text = Header;
                _headerText.Foreground = fgOverride ?? baseContentBrush;
            }

            // Indicator
            if (_indicator != null)
            {
                _indicator.Fill = fgOverride ?? baseContentBrush;
                _indicator.Visibility = Variant == DaisyCollapseVariant.None 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;

                var iconKey = Variant switch
                {
                    DaisyCollapseVariant.Arrow => IsExpanded ? "DaisyIconChevronUp" : "DaisyIconChevronDown",
                    DaisyCollapseVariant.Plus => IsExpanded ? "DaisyIconMinus" : "DaisyIconPlus",
                    _ => null
                };
                if (!string.IsNullOrEmpty(iconKey))
                {
                    var pathData = FloweryPathHelpers.GetIconPathData(iconKey);
                    if (!string.IsNullOrEmpty(pathData))
                    {
                        FloweryPathHelpers.TrySetPathData(_indicator, () => FloweryPathHelpers.ParseGeometry(pathData));
                    }
                }
            }

            // Content visibility
            if (_contentPresenter != null)
            {
                _contentPresenter.Visibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
                _contentPresenter.Content = _userContent;
            }

            UpdateAutomationProperties();
        }

        private void UpdateAutomationProperties()
        {
            DaisyAccessibility.SetAutomationNameOrClear(this, Header);
        }

    }
}

