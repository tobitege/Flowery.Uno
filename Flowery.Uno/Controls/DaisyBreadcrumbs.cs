using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// Breadcrumbs component that helps users navigate through the website.
    /// </summary>
    public partial class DaisyBreadcrumbs : DaisyBaseContentControl
    {
        private StackPanel? _itemsPanel;
        private readonly List<DaisyBreadcrumbItem> _breadcrumbItems = [];

        public DaisyBreadcrumbs()
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

        #region Separator
        public static readonly DependencyProperty SeparatorProperty =
            DependencyProperty.Register(
                nameof(Separator),
                typeof(string),
                typeof(DaisyBreadcrumbs),
                new PropertyMetadata("/", OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the separator character/content between breadcrumb items.
        /// Default is "/" but can be changed to ">" or custom content.
        /// </summary>
        public string Separator
        {
            get => (string)GetValue(SeparatorProperty);
            set => SetValue(SeparatorProperty, value);
        }
        #endregion

        #region SeparatorOpacity
        public static readonly DependencyProperty SeparatorOpacityProperty =
            DependencyProperty.Register(
                nameof(SeparatorOpacity),
                typeof(double),
                typeof(DaisyBreadcrumbs),
                new PropertyMetadata(0.5, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the opacity of the separator.
        /// </summary>
        public double SeparatorOpacity
        {
            get => (double)GetValue(SeparatorOpacityProperty);
            set => SetValue(SeparatorOpacityProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyBreadcrumbs),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the size of the breadcrumbs.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyBreadcrumbs breadcrumbs)
                breadcrumbs.SyncItemProperties();
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
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            _itemsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0
            };

            _breadcrumbItems.Clear();

            // Collect DaisyBreadcrumbItem children from Content
            if (Content is Panel panel)
            {
                var children = new List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                panel.Children.Clear();

                int index = 0;
                int count = 0;
                // First count the items
                foreach (var child in children)
                {
                    if (child is DaisyBreadcrumbItem)
                        count++;
                }

                foreach (var child in children)
                {
                    if (child is DaisyBreadcrumbItem item)
                    {
                        item.Index = index;
                        item.IsFirst = index == 0;
                        item.IsLast = index == count - 1;
                        item.Separator = Separator;
                        item.SeparatorOpacity = SeparatorOpacity;
                        item.Size = Size;
                        _breadcrumbItems.Add(item);
                        _itemsPanel.Children.Add(item);
                        index++;
                    }
                }
            }

            Content = _itemsPanel;
            ApplyTheme();
        }

        private void SyncItemProperties()
        {
            foreach (var item in _breadcrumbItems)
            {
                item.Separator = Separator;
                item.SeparatorOpacity = SeparatorOpacity;
                item.Size = Size;
                item.RebuildVisual();
            }
        }

        private void ApplyTheme()
        {
            foreach (var item in _breadcrumbItems)
            {
                item.RebuildVisual();
            }
        }
    }

    /// <summary>
    /// Individual breadcrumb item that can display content with an optional icon.
    /// </summary>
    public partial class DaisyBreadcrumbItem : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private TextBlock? _separatorText;
        private Microsoft.UI.Xaml.Shapes.Path? _iconPath;
        private TextBlock? _contentText;
        private ContentPresenter? _contentPresenter;
        private object? _userContent;

        internal int Index { get; set; }
        internal bool IsFirst { get; set; }
        internal bool IsLast { get; set; }
        internal double SeparatorOpacity { get; set; } = 0.5;
        internal DaisySize Size { get; set; } = DaisySize.Medium;

        public DaisyBreadcrumbItem()
        {
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

        #region Separator
        public static readonly DependencyProperty SeparatorProperty =
            DependencyProperty.Register(
                nameof(Separator),
                typeof(string),
                typeof(DaisyBreadcrumbItem),
                new PropertyMetadata("/", OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the separator text displayed before this item.
        /// </summary>
        public string Separator
        {
            get => (string)GetValue(SeparatorProperty);
            set => SetValue(SeparatorProperty, value);
        }
        #endregion

        #region Icon
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(Geometry),
                typeof(DaisyBreadcrumbItem),
                new PropertyMetadata(null, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets the icon geometry to display before the content.
        /// </summary>
        public Geometry? Icon
        {
            get => (Geometry?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        #endregion

        #region IsClickable
        public static readonly DependencyProperty IsClickableProperty =
            DependencyProperty.Register(
                nameof(IsClickable),
                typeof(bool),
                typeof(DaisyBreadcrumbItem),
                new PropertyMetadata(true, OnAppearanceChanged));

        /// <summary>
        /// Gets or sets whether the item is clickable/interactive.
        /// Default is true for all items except the last one.
        /// </summary>
        public bool IsClickable
        {
            get => (bool)GetValue(IsClickableProperty);
            set => SetValue(IsClickableProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyBreadcrumbItem item)
                item.RebuildVisual();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            _rootGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // Separator
            _separatorText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };
            Grid.SetColumn(_separatorText, 0);
            _rootGrid.Children.Add(_separatorText);

            // Icon
            _iconPath = new Microsoft.UI.Xaml.Shapes.Path
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                Stretch = Stretch.Uniform
            };
            Grid.SetColumn(_iconPath, 1);
            _rootGrid.Children.Add(_iconPath);

            // Content (text or presenter)
            _contentText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_contentText, 2);
            _rootGrid.Children.Add(_contentText);

            _contentPresenter = new ContentPresenter
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_contentPresenter, 2);
            _rootGrid.Children.Add(_contentPresenter);

            Content = _rootGrid;
        }

        protected override void OnBaseClick()
        {
            if (IsClickable && !IsLast)
            {
                base.OnBaseClick();
            }
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            if (!(IsClickable && !IsLast))
                return;

            DaisyAccessibility.TryHandleEnterOrSpace(e, OnBaseClick);
        }

        internal void RebuildVisual()
        {
            if (_rootGrid == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Check for lightweight styling overrides
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyBreadcrumbItem", "Foreground");
            var linkFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyBreadcrumbItem", "LinkForeground");
            var separatorFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyBreadcrumbItem", "SeparatorForeground");

            var baseContentBrush = fgOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush");
            var primaryBrush = linkFgOverride ?? DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush");

            // Get font size based on size
            double fontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
            // Breadcrumb icons/separators are slightly larger than standard icons
            double iconSize = DaisyResourceLookup.GetDefaultIconSize(Size) + 2;

            // Separator
            if (_separatorText != null)
            {
                _separatorText.Text = Separator;
                _separatorText.Foreground = separatorFgOverride ?? baseContentBrush;
                _separatorText.Opacity = SeparatorOpacity;
                _separatorText.FontSize = fontSize;
                _separatorText.Visibility = IsFirst ? Visibility.Collapsed : Visibility.Visible;
            }

            // Icon
            if (_iconPath != null)
            {
                _iconPath.Data = Icon;
                _iconPath.Fill = IsLast ? baseContentBrush : primaryBrush;
                _iconPath.Width = iconSize;
                _iconPath.Height = iconSize;
                _iconPath.Visibility = Icon != null ? Visibility.Visible : Visibility.Collapsed;
            }

            // Content
            bool isClickable = IsClickable && !IsLast;
            var contentBrush = isClickable ? primaryBrush : baseContentBrush;
            double contentOpacity = IsLast ? 0.7 : 1.0;
            IsTabStop = isClickable;

            if (_userContent is string text)
            {
                if (_contentText != null)
                {
                    _contentText.Text = text;
                    _contentText.FontSize = fontSize;
                    _contentText.Foreground = contentBrush;
                    _contentText.Opacity = contentOpacity;
                    _contentText.Visibility = Visibility.Visible;
                }
                if (_contentPresenter != null)
                {
                    _contentPresenter.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (_contentText != null)
                {
                    _contentText.Visibility = Visibility.Collapsed;
                }
                if (_contentPresenter != null)
                {
                    _contentPresenter.Content = _userContent;
                    _contentPresenter.Opacity = contentOpacity;
                    _contentPresenter.Visibility = Visibility.Visible;
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
            return DaisyAccessibility.GetAccessibleNameFromContent(_userContent, _contentText?.Text);
        }

    }
}
