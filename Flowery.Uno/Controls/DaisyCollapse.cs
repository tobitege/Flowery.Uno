using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
// Note: Do not add "using Microsoft.UI.Xaml.Shapes;" - it causes ambiguity with System.IO.Path

namespace Flowery.Controls
{
    public enum DaisyCollapseVariant
    {
        Arrow,
        Plus,
        None
    }

    /// <summary>
    /// A Collapse/Expander control styled after DaisyUI's Collapse component.
    /// Unlike the native Expander which has known issues on WASM/Skia, this control
    /// builds its visual tree programmatically for reliable cross-platform rendering.
    /// </summary>
    public partial class DaisyCollapse : DaisyBaseContentControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(DaisyCollapse),
                new PropertyMetadata(null, OnHeaderChanged));

        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
                nameof(IsExpanded),
                typeof(bool),
                typeof(DaisyCollapse),
                new PropertyMetadata(false, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyCollapse),
                new PropertyMetadata(DaisySize.Medium, OnStylePropertyChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyCollapseVariant),
                typeof(DaisyCollapse),
                new PropertyMetadata(DaisyCollapseVariant.Arrow, OnVariantChanged));

        public DaisyCollapseVariant Variant
        {
            get => (DaisyCollapseVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty ExpanderContentProperty =
            DependencyProperty.Register(
                nameof(ExpanderContent),
                typeof(object),
                typeof(DaisyCollapse),
                new PropertyMetadata(null, OnExpanderContentChanged));

        /// <summary>
        /// The content that expands/collapses. Use this instead of Content.
        /// </summary>
        public object? ExpanderContent
        {
            get => GetValue(ExpanderContentProperty);
            set => SetValue(ExpanderContentProperty, value);
        }

        #endregion

        #region Private Fields

        private Border? _outerBorder;
        private DaisyButton? _headerButton;
        private ContentPresenter? _headerPresenter;
        private Border? _contentBorder;
        private ContentPresenter? _contentPresenter;
        private Viewbox? _iconViewbox;
        private Microsoft.UI.Xaml.Shapes.Path? _iconPath;
        private StackPanel? _rootPanel;
        private bool _layoutBuilt;
        private bool _isFocused;
        #endregion

        public DaisyCollapse()
        {
            DefaultStyleKey = typeof(DaisyCollapse);
            IsTabStop = true;
            GotFocus += OnControlGotFocus;
            LostFocus += OnControlLostFocus;

            BuildLayout();
        }

        private void ApplyAll()
        {
            ApplySizing();
            UpdateBorderColors();
            UpdateIconColor();
        }

        private void BuildLayout()
        {
            // Outer border wrapper for proper styling
            _outerBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8)
            };

            _rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Build header button for reliable click handling on all platforms
            // Using DaisyButton with Ghost variant - it automatically respects global sizing
            _headerButton = new DaisyButton
            {
                Variant = DaisyButtonVariant.Ghost,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(0),
                IsTabStop = false,
                Size = Size // Sync with our Size property
            };

            var headerGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _headerPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_headerPresenter, 0);
            headerGrid.Children.Add(_headerPresenter);

            // Icon container (chevron/plus/minus based on Variant)
            _iconViewbox = new Viewbox
            {
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

            _iconPath = new Microsoft.UI.Xaml.Shapes.Path
            {
                Stretch = Stretch.Uniform
            };
            _iconViewbox.Child = _iconPath;
            Grid.SetColumn(_iconViewbox, 1);
            headerGrid.Children.Add(_iconViewbox);

            _headerButton.Content = headerGrid;
            _headerButton.Click += OnHeaderClick;

            // Content area
            _contentBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(0, 8, 0, 0)
            };

            _contentPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _contentBorder.Child = _contentPresenter;

            _rootPanel.Children.Add(_headerButton);
            _rootPanel.Children.Add(_contentBorder);

            _outerBorder.Child = _rootPanel;
            Content = _outerBorder;

            _layoutBuilt = true;

            UpdateIcon();
            UpdateContentVisibility();
            ApplySizing();
            UpdateBorderColors();
        }

        #region Event Handlers

        protected override void OnLoaded()
        {
            base.OnLoaded();

            // Sync properties set before visual tree was connected
            if (_headerPresenter != null && Header != null)
            {
                _headerPresenter.Content = Header;
            }

            if (_contentPresenter != null && ExpanderContent != null)
            {
                _contentPresenter.Content = ExpanderContent;
            }

            UpdateIcon();
            UpdateContentVisibility();
            ApplyAll();
            UpdateAutomationProperties();
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

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _outerBorder ?? base.GetNeumorphicHostElement();
        }

        private void OnHeaderClick(object sender, RoutedEventArgs e)
        {
            if (!IsSelfFocused())
            {
                Focus(FocusState.Pointer);
            }
            ToggleExpanded();
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            if (e.Key != VirtualKey.Enter && e.Key != VirtualKey.Space)
                return;

            if (!IsSelfFocused())
                return;

            ToggleExpanded();
            e.Handled = true;
        }

        private void OnControlGotFocus(object sender, RoutedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, this))
                return;

            _isFocused = true;
            UpdateBorderColors();
        }

        private void OnControlLostFocus(object sender, RoutedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, this))
                return;

            _isFocused = false;
            UpdateBorderColors();
        }

        private void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        private bool IsSelfFocused()
        {
            return XamlRoot != null && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), this);
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCollapse collapse && collapse._headerPresenter != null)
            {
                collapse._headerPresenter.Content = e.NewValue;
            }
            if (d is DaisyCollapse collapseWithAutomation)
            {
                collapseWithAutomation.UpdateAutomationProperties();
            }
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCollapse collapse)
            {
                collapse.UpdateContentVisibility();
                collapse.UpdateIcon();
            }
        }

        private static void OnStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCollapse collapse)
            {
                collapse.ApplySizing();
            }
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCollapse collapse)
            {
                collapse.UpdateIcon();
            }
        }

        private static void OnExpanderContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCollapse collapse && collapse._contentPresenter != null)
            {
                collapse._contentPresenter.Content = e.NewValue;
            }
        }


        #endregion

        #region Update Methods

        private void UpdateContentVisibility()
        {
            if (_contentBorder != null)
            {
                _contentBorder.Visibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateIcon()
        {
            if (_iconPath == null || _iconViewbox == null)
                return;

            // Hide icon if Variant is None
            if (Variant == DaisyCollapseVariant.None)
            {
                _iconViewbox.Visibility = Visibility.Collapsed;
                return;
            }

            _iconViewbox.Visibility = Visibility.Visible;

            try
            {
                string pathData;

                pathData = Variant switch
                {
                    DaisyCollapseVariant.Plus => IsExpanded
                        ? "M19,13H5V11H19V13Z"  // Minus path
                        : "M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z", // Plus path
                    // Arrow: Chevron down when collapsed, Chevron up when expanded
                    _ => IsExpanded
                        ? FloweryPathHelpers.ChevronUpPath
                        : FloweryPathHelpers.ChevronDownPath
                };

                _iconPath.Data = FloweryPathHelpers.ParseGeometry(pathData);
            }
            catch
            {
                // Ignore path parsing failures
            }

            UpdateIconColor();
        }

        private void UpdateIconColor()
        {
            if (_iconPath == null)
                return;

            _iconPath.Fill = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
        }

        private void ApplySizing()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
            {
                DaisyTokenDefaults.EnsureDefaults(resources);
            }

            var fontSize = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "FontSize",
                DaisyResourceLookup.GetDefaultFontSize(Size));
            
            FontSize = fontSize;
            
            // Sync header button size (DaisyButton handles its own sizing)
            if (_headerButton != null)
            {
                _headerButton.Size = Size;
            }

            // Adjust icon size based on control size
            if (_iconViewbox != null)
            {
                var iconSize = Size switch
                {
                    DaisySize.ExtraSmall => 10.0,
                    DaisySize.Small => 12.0,
                    DaisySize.Medium => 14.0,
                    DaisySize.Large => 16.0,
                    DaisySize.ExtraLarge => 18.0,
                    _ => 14.0
                };
                _iconViewbox.Width = iconSize;
                _iconViewbox.Height = iconSize;
            }

            // Adjust padding based on size
            if (_outerBorder != null)
            {
                var (hPadding, vPadding) = Size switch
                {
                    DaisySize.ExtraSmall => (8.0, 4.0),
                    DaisySize.Small => (10.0, 6.0),
                    DaisySize.Medium => (12.0, 8.0),
                    DaisySize.Large => (14.0, 10.0),
                    DaisySize.ExtraLarge => (16.0, 12.0),
                    _ => (12.0, 8.0)
                };
                _outerBorder.Padding = new Thickness(hPadding, vPadding, hPadding, vPadding);
            }
        }


        private void UpdateBorderColors()
        {
            if (_outerBorder == null)
                return;

            _outerBorder.Background = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _outerBorder.BorderBrush = _isFocused
                ? DaisyResourceLookup.GetBrush("DaisyAccentBrush")
                : DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            _outerBorder.BorderThickness = new Thickness(1);
        }

        private void UpdateAutomationProperties()
        {
            var name = DaisyAccessibility.GetAccessibleNameFromContent(Header);
            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

        #endregion

        #region Content Routing

        /// <summary>
        /// Routes any content set via XAML Content property to ExpanderContent.
        /// This ensures DaisyCollapse can be used with direct child content in XAML.
        /// </summary>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // If layout hasn't been built yet, ignore (constructor will handle it)
            if (!_layoutBuilt || _outerBorder == null)
                return;

            // If new content is our outer border, do nothing (this is our internal layout)
            if (ReferenceEquals(newContent, _outerBorder))
                return;

            // If someone sets Content directly (e.g., via XAML child), route it to ExpanderContent
            if (newContent != null && !ReferenceEquals(newContent, _outerBorder))
            {
                ExpanderContent = newContent;
                // Restore our outer border as the actual Content
                Content = _outerBorder;
            }
        }

        #endregion
    }
}
