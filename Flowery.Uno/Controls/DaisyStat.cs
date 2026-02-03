using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Controls
{
    /// <summary>
    /// Variant for DaisyStat value and description coloring.
    /// </summary>
    public enum DaisyStatVariant
    {
        Default,
        Primary,
        Secondary,
        Accent,
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// A Stat display control styled after DaisyUI's Stat component.
    /// Displays a value with optional title, description, figure, and actions.
    /// </summary>
    public partial class DaisyStat : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private StackPanel? _contentPanel;
        private TextBlock? _titleBlock;
        private TextBlock? _valueBlock;
        private TextBlock? _descriptionBlock;
        private ContentPresenter? _figurePresenter;
        private ContentPresenter? _actionsPresenter;
        public DaisyStat()
        {
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            BuildVisualTree();
            ApplyAll();
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

        #region Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(DaisyStat),
                new PropertyMetadata(null, OnAppearanceChanged));

        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region Value
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(DaisyStat),
                new PropertyMetadata(null, OnAppearanceChanged));

        public string? Value
        {
            get => (string?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        #endregion

        #region Description
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(DaisyStat),
                new PropertyMetadata(null, OnAppearanceChanged));

#if __IOS__ || __MACOS__
        public new string? Description
#else
        public string? Description
#endif
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
        #endregion

        #region Figure
        public static readonly DependencyProperty FigureProperty =
            DependencyProperty.Register(
                nameof(Figure),
                typeof(object),
                typeof(DaisyStat),
                new PropertyMetadata(null, OnAppearanceChanged));

        public object? Figure
        {
            get => GetValue(FigureProperty);
            set => SetValue(FigureProperty, value);
        }
        #endregion

        #region Actions
        public static readonly DependencyProperty ActionsProperty =
            DependencyProperty.Register(
                nameof(Actions),
                typeof(object),
                typeof(DaisyStat),
                new PropertyMetadata(null, OnAppearanceChanged));

        public object? Actions
        {
            get => GetValue(ActionsProperty);
            set => SetValue(ActionsProperty, value);
        }
        #endregion

        #region IsCentered
        public static readonly DependencyProperty IsCenteredProperty =
            DependencyProperty.Register(
                nameof(IsCentered),
                typeof(bool),
                typeof(DaisyStat),
                new PropertyMetadata(false, OnAppearanceChanged));

        public bool IsCentered
        {
            get => (bool)GetValue(IsCenteredProperty);
            set => SetValue(IsCenteredProperty, value);
        }
        #endregion

        #region Variant
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyStatVariant),
                typeof(DaisyStat),
                new PropertyMetadata(DaisyStatVariant.Default, OnAppearanceChanged));

        public DaisyStatVariant Variant
        {
            get => (DaisyStatVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }
        #endregion

        #region DescriptionVariant
        public static readonly DependencyProperty DescriptionVariantProperty =
            DependencyProperty.Register(
                nameof(DescriptionVariant),
                typeof(DaisyStatVariant),
                typeof(DaisyStat),
                new PropertyMetadata(DaisyStatVariant.Default, OnAppearanceChanged));

        public DaisyStatVariant DescriptionVariant
        {
            get => (DaisyStatVariant)GetValue(DescriptionVariantProperty);
            set => SetValue(DescriptionVariantProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyStat),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// The size of the stat display.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyStat stat)
                stat.ApplyAll();
        }

        private void BuildVisualTree()
        {
            if (_rootGrid != null)
                return;

            // Main grid with content and figure columns
            _rootGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            // Content StackPanel
            _contentPanel = new StackPanel { Spacing = 0 };

            // Title
            _titleBlock = new TextBlock
            {
                Opacity = 0.6,
                Margin = new Thickness(0, 0, 0, 4)
            };
            _contentPanel.Children.Add(_titleBlock);

            // Value
            _valueBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            _contentPanel.Children.Add(_valueBlock);

            // Description
            _descriptionBlock = new TextBlock
            {
                Opacity = 0.6,
                Margin = new Thickness(0, 4, 0, 0)
            };
            _contentPanel.Children.Add(_descriptionBlock);

            // Actions
            _actionsPresenter = new ContentPresenter
            {
                Margin = new Thickness(0, 8, 0, 0)
            };
            _contentPanel.Children.Add(_actionsPresenter);

            _rootGrid.Children.Add(_contentPanel);

            // Figure
            _figurePresenter = new ContentPresenter
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };
            Grid.SetColumn(_figurePresenter, 1);
            _rootGrid.Children.Add(_figurePresenter);

            Content = _rootGrid;
        }

        private void ApplyAll()
        {
            if (_rootGrid == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            ApplyLayout();
            ApplyTheme(resources);
        }

        private void ApplyLayout()
        {
            var alignment = IsCentered ? HorizontalAlignment.Center : HorizontalAlignment.Left;

            if (_contentPanel != null)
                _contentPanel.HorizontalAlignment = alignment;
            if (_titleBlock != null)
                _titleBlock.HorizontalAlignment = alignment;
            if (_valueBlock != null)
                _valueBlock.HorizontalAlignment = alignment;
            if (_descriptionBlock != null)
                _descriptionBlock.HorizontalAlignment = alignment;
            if (_actionsPresenter != null)
                _actionsPresenter.HorizontalAlignment = alignment;

            // Update text content
            if (_titleBlock != null)
            {
                _titleBlock.Text = Title ?? "";
                _titleBlock.Visibility = string.IsNullOrEmpty(Title) ? Visibility.Collapsed : Visibility.Visible;
            }
            if (_valueBlock != null)
            {
                _valueBlock.Text = Value ?? "";
            }
            if (_descriptionBlock != null)
            {
                _descriptionBlock.Text = Description ?? "";
                _descriptionBlock.Visibility = string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;
            }

            // Update presenters
            if (_actionsPresenter != null)
            {
                _actionsPresenter.Content = Actions;
                _actionsPresenter.Visibility = Actions == null ? Visibility.Collapsed : Visibility.Visible;
            }
            if (_figurePresenter != null)
            {
                _figurePresenter.Content = Figure;
                _figurePresenter.Visibility = Figure == null ? Visibility.Collapsed : Visibility.Visible;
            }

            // Stat padding
            Padding = new Thickness(16, 12, 16, 12);
        }

        private void ApplyTheme(ResourceDictionary? resources)
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

            // Check for lightweight styling overrides
            var titleFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyStat", "TitleForeground");
            var valueFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyStat", "ValueForeground");
            var descFgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyStat", "DescriptionForeground");

            var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush");
            var secondaryFontSize = GetDouble(resources, "DaisySizeMediumSecondaryFontSize", 12);

            // Title styling
            if (_titleBlock != null)
            {
                _titleBlock.Foreground = titleFgOverride ?? baseContent;
                _titleBlock.FontSize = secondaryFontSize;
            }

            // Value styling with variant
            if (_valueBlock != null)
            {
                _valueBlock.Foreground = valueFgOverride ?? GetVariantBrush(resources, Variant, baseContent);
            }

            // Description styling with variant
            if (_descriptionBlock != null)
            {
                var descBrush = descFgOverride ?? GetVariantBrush(resources, DescriptionVariant, baseContent);
                _descriptionBlock.Foreground = descBrush;
                _descriptionBlock.FontSize = secondaryFontSize;

                // Full opacity for non-default description variant
                _descriptionBlock.Opacity = DescriptionVariant == DaisyStatVariant.Default ? 0.6 : 1.0;
            }
        }

        private static Brush GetVariantBrush(ResourceDictionary? resources, DaisyStatVariant variant, Brush fallback)
        {
            return variant switch
            {
                DaisyStatVariant.Primary => DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush"),
                DaisyStatVariant.Secondary => DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush"),
                DaisyStatVariant.Accent => DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush"),
                DaisyStatVariant.Info => DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush"),
                DaisyStatVariant.Success => DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush"),
                DaisyStatVariant.Warning => DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush"),
                DaisyStatVariant.Error => DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush"),
                _ => fallback
            };
        }

        #region Resource Helpers
        private static double GetDouble(ResourceDictionary? resources, string key, double fallback)
        {
            if (resources == null)
                return fallback;
            return DaisyResourceLookup.GetDouble(resources, key, fallback);
        }
        #endregion
    }

    /// <summary>
    /// A container for DaisyStat controls that displays them in a row or column with dividers.
    /// </summary>
    public partial class DaisyStats : DaisyBaseContentControl
    {
        private Border? _rootBorder;
        private StackPanel? _itemsPanel;

        public DaisyStats()
        {
            IsTabStop = false;
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_rootBorder != null)
            {
                ApplyTheme();
                UpdateChildBorders();
                return;
            }

            CollectAndDisplay();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyTheme();
            UpdateChildBorders();
        }

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _rootBorder ?? base.GetNeumorphicHostElement();
        }

        #region Orientation
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyStats),
                new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyStats stats)
            {
                if (stats._itemsPanel != null)
                    stats._itemsPanel.Orientation = stats.Orientation;
                stats.UpdateChildBorders();
            }
        }
        #endregion

        private void CollectAndDisplay()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            // Create root border
            _rootBorder = new Border
            {
                CornerRadius = new CornerRadius(8)
            };

            // Create items panel
            _itemsPanel = new StackPanel
            {
                Orientation = Orientation
            };

            // Collect DaisyStat children from Content
            if (Content is Panel panel)
            {
                var children = new System.Collections.Generic.List<UIElement>();
                foreach (var child in panel.Children)
                    children.Add(child);

                panel.Children.Clear();

                foreach (var child in children)
                {
                    if (child is DaisyStat stat)
                        _itemsPanel.Children.Add(stat);
                }
            }
            else if (Content is DaisyStat singleStat)
            {
                _itemsPanel.Children.Add(singleStat);
            }

            _rootBorder.Child = _itemsPanel;
            Content = _rootBorder;

            ApplyTheme();
            UpdateChildBorders();
        }

        private void ApplyTheme()
        {
            var resources = Application.Current?.Resources;
            if (_rootBorder != null)
            {
                _rootBorder.Background = DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush");
            }
        }

        private void UpdateChildBorders()
        {
            if (_itemsPanel == null)
                return;

            var resources = Application.Current?.Resources;
            var dividerBrush = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush");
            var isHorizontal = Orientation == Orientation.Horizontal;

            for (int i = 0; i < _itemsPanel.Children.Count; i++)
            {
                if (_itemsPanel.Children[i] is DaisyStat stat)
                {
                    if (i == 0)
                    {
                        stat.BorderThickness = new Thickness(0);
                    }
                    else
                    {
                        stat.BorderThickness = isHorizontal
                            ? new Thickness(1, 0, 0, 0)
                            : new Thickness(0, 1, 0, 0);
                        stat.BorderBrush = dividerBrush;
                    }
                }
            }
        }

    }
}
