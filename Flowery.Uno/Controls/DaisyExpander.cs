using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
// Note: Do not add "using Microsoft.UI.Xaml.Shapes;" - it causes ambiguity with System.IO.Path
using Flowery.Helpers;
using Flowery.Theming;
using Flowery.Services;
using Microsoft.UI;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// A custom expander control that works reliably on all platforms including Skia/WASM.
    /// Unlike the built-in Expander which has known issues on non-Windows platforms,
    /// this control uses simple visibility toggling with a clickable header.
    /// </summary>
    public partial class DaisyExpander : DaisyBaseContentControl
    {
        private Button? _headerButton;
        private Border? _headerBorder;
        private ContentPresenter? _headerContent;
        private Microsoft.UI.Xaml.Shapes.Path? _chevron;
        private Border? _contentBorder;
        private ContentPresenter? _contentPresenter;

        #region Dependency Properties

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(DaisyExpander),
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
                typeof(DaisyExpander),
                new PropertyMetadata(true, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public static readonly DependencyProperty HeaderBackgroundProperty =
            DependencyProperty.Register(
                nameof(HeaderBackground),
                typeof(Brush),
                typeof(DaisyExpander),
                new PropertyMetadata(null, OnHeaderBackgroundChanged));

        public Brush? HeaderBackground
        {
            get => (Brush?)GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        public static readonly DependencyProperty HeaderForegroundProperty =
            DependencyProperty.Register(
                nameof(HeaderForeground),
                typeof(Brush),
                typeof(DaisyExpander),
                new PropertyMetadata(null, OnHeaderForegroundChanged));

        public Brush? HeaderForeground
        {
            get => (Brush?)GetValue(HeaderForegroundProperty);
            set => SetValue(HeaderForegroundProperty, value);
        }

        public static readonly DependencyProperty HeaderPaddingProperty =
            DependencyProperty.Register(
                nameof(HeaderPadding),
                typeof(Thickness),
                typeof(DaisyExpander),
                new PropertyMetadata(new Thickness(8, 6, 8, 6), OnHeaderPaddingChanged));

        public Thickness HeaderPadding
        {
            get => (Thickness)GetValue(HeaderPaddingProperty);
            set => SetValue(HeaderPaddingProperty, value);
        }

        public static readonly DependencyProperty ContentPaddingProperty =
            DependencyProperty.Register(
                nameof(ContentPadding),
                typeof(Thickness),
                typeof(DaisyExpander),
                new PropertyMetadata(new Thickness(0), OnContentPaddingChanged));

        public Thickness ContentPadding
        {
            get => (Thickness)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
        }

        public static readonly DependencyProperty ChevronSizeProperty =
            DependencyProperty.Register(
                nameof(ChevronSize),
                typeof(double),
                typeof(DaisyExpander),
                new PropertyMetadata(12d, OnChevronSizeChanged));

        public double ChevronSize
        {
            get => (double)GetValue(ChevronSizeProperty);
            set => SetValue(ChevronSizeProperty, value);
        }

        public static readonly DependencyProperty ShowChevronProperty =
            DependencyProperty.Register(
                nameof(ShowChevron),
                typeof(bool),
                typeof(DaisyExpander),
                new PropertyMetadata(true, OnShowChevronChanged));

        public bool ShowChevron
        {
            get => (bool)GetValue(ShowChevronProperty);
            set => SetValue(ShowChevronProperty, value);
        }

        #endregion

        public DaisyExpander()
        {
            DefaultStyleKey = typeof(DaisyExpander);
            IsTabStop = false;

            BuildLayout();

            // Register for BorderBrush/BorderThickness changes (inherited from Control)
            RegisterPropertyChangedCallback(BorderBrushProperty, (_, _) => UpdateBorder());
            RegisterPropertyChangedCallback(BorderThicknessProperty, (_, _) => UpdateBorder());
        }

        private void BuildLayout()
        {
            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Use a Button for the header to ensure click events work on all platforms
            // Button.Click is more reliably supported than Border.Tapped on Skia/WASM
            _headerButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

            var headerGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _headerContent = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_headerContent, 0);
            headerGrid.Children.Add(_headerContent);

            // Chevron icon
            var chevronViewbox = new Viewbox
            {
                Width = ChevronSize,
                Height = ChevronSize,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 4, 0)
            };

            _chevron = new Microsoft.UI.Xaml.Shapes.Path
            {
                Stretch = Stretch.Uniform
            };
            chevronViewbox.Child = _chevron;
            Grid.SetColumn(chevronViewbox, 1);
            headerGrid.Children.Add(chevronViewbox);

            _headerButton.Content = headerGrid;
            _headerButton.Click += OnHeaderClick;

            // Wrap header in a border to support BorderBrush/BorderThickness on header row
            _headerBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = _headerButton
            };

            // Content area
            _contentBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _contentPresenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _contentBorder.Child = _contentPresenter;

            rootPanel.Children.Add(_headerBorder);
            rootPanel.Children.Add(_contentBorder);

            Content = rootPanel;

            UpdateChevron();
            UpdateContentVisibility();
            UpdateHeaderPadding();
            UpdateContentPadding();
            UpdateBorder();
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            // Ensure header content is synced after control is fully loaded
            // This is needed because the property might be set before _headerContent is ready
            if (_headerContent != null && Header != null)
            {
                _headerContent.Content = Header;
            }
            
            // Ensure content is synced
            if (_contentPresenter != null && ExpanderContent != null)
            {
                _contentPresenter.Content = ExpanderContent;
            }
            
            UpdateChevron();
            UpdateContentVisibility();
            UpdateColors();
            // LogContentState("Loaded");
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            UpdateColors();
            UpdateBorder();
        }

        private void OnHeaderClick(object sender, RoutedEventArgs e)
        {
            ToggleExpanded();
        }

        private void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpander expander && expander._headerContent != null)
            {
                expander._headerContent.Content = e.NewValue;
            }
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpander expander)
            {
                expander.UpdateChevron();
                expander.UpdateContentVisibility();
            }
        }

        private static void OnHeaderBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpander expander && expander._headerButton != null)
            {
                expander._headerButton.Background = (Brush?)e.NewValue ?? new SolidColorBrush(Colors.Transparent);
            }
        }

        private static void OnHeaderForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpander expander)
            {
                expander.UpdateChevronColor();
            }
        }

        private static void OnHeaderPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpander expander)
            {
                expander.UpdateHeaderPadding();
            }
        }

        private static void OnContentPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpander expander)
            {
                expander.UpdateContentPadding();
            }
        }

        private static void OnChevronSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpander expander && expander._chevron?.Parent is Viewbox viewbox)
            {
                var size = (double)e.NewValue;
                viewbox.Width = size;
                viewbox.Height = size;
            }
        }

        private static void OnShowChevronChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyExpander expander && expander._chevron?.Parent is Viewbox viewbox)
            {
                viewbox.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateChevron()
        {
            if (_chevron == null)
                return;

            try
            {
                var pathData = IsExpanded ? FloweryPathHelpers.ChevronUpPath : FloweryPathHelpers.ChevronDownPath;
                _chevron.Data = FloweryPathHelpers.ParseGeometry(pathData);
            }
            catch
            {
                // Fallback if path parsing fails
            }

            UpdateChevronColor();
        }

        private void UpdateChevronColor()
        {
            if (_chevron == null)
                return;

            var foreground = HeaderForeground ?? DaisyResourceLookup.GetBrush(Application.Current?.Resources, "DaisyBaseContentBrush", new SolidColorBrush(Colors.White));
            _chevron.Fill = foreground;
        }

        private void UpdateContentVisibility()
        {
            if (_contentBorder != null)
            {
                _contentBorder.Visibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateHeaderPadding()
        {
            if (_headerButton != null)
            {
                _headerButton.Padding = HeaderPadding;
            }
        }

        private void UpdateContentPadding()
        {
            if (_contentBorder != null)
            {
                _contentBorder.Padding = ContentPadding;
            }
        }

        private void UpdateColors()
        {
            UpdateChevronColor();

            if (_headerButton != null)
            {
                _headerButton.Background = HeaderBackground ?? new SolidColorBrush(Colors.Transparent);
            }
        }

        private void UpdateBorder()
        {
            if (_headerBorder != null)
            {
                _headerBorder.BorderBrush = BorderBrush;
                _headerBorder.BorderThickness = BorderThickness;
            }
        }

        // private void LogContentState(string context)
        // {
        //     if (_loggedContent)
        //     {
        //         return;
        //     }

        //     _loggedContent = true;
        //     var content = ExpanderContent;
        //     var typeName = content?.GetType().FullName ?? "<null>";
        //     var countInfo = "<n/a>";

        //     if (content is Panel panel)
        //     {
        //         countInfo = panel.Children.Count.ToString();
        //     }
        //     else if (content is ItemsControl itemsControl)
        //     {
        //         countInfo = itemsControl.Items?.Count.ToString() ?? "0";
        //     }
        //     else if (content is System.Collections.ICollection collection)
        //     {
        //         countInfo = collection.Count.ToString();
        //     }

        //     FloweryDiagnostics.Log($"{DateTimeOffset.Now:O} [DaisyExpander] {context}: contentType={typeName}, count={countInfo}");
        // }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            // Don't call base - we manage content ourselves
            // The base.Content is our layout panel, actual content goes to _contentPresenter
            if (_contentPresenter != null && newContent != Content)
            {
                _contentPresenter.Content = newContent;
                // FloweryDiagnostics.Log($"[DaisyExpander] Content changed: type={newContent?.GetType().FullName ?? "<null>"}");
            }
        }

        /// <summary>
        /// Sets the actual expandable content (not the layout panel).
        /// Use this instead of Content property.
        /// </summary>
        public object? ExpanderContent
        {
            get => _contentPresenter?.Content;
            set
            {
                if (_contentPresenter != null)
                {
                    _contentPresenter.Content = value;
                }
            }
        }

    }
}
