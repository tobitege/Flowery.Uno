using System;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Flowery.Controls
{
    /// <summary>
    /// A RadioButton that applies a DaisyUI theme when selected.
    /// Use within a group to allow multi-theme selection.
    /// </summary>
    public partial class DaisyThemeRadio : RadioButton
    {
        private bool _isSyncing;
        private bool _isPointerOver;
        private readonly DaisyControlLifecycle _lifecycle;

        private Border? _radioIndicatorBorder;

        private Ellipse? _radioCheckMark;
        private Border? _buttonModeBorder;

        #region ThemeName
        public static readonly DependencyProperty ThemeNameProperty =
            DependencyProperty.Register(
                nameof(ThemeName),
                typeof(string),
                typeof(DaisyThemeRadio),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// The theme name to apply when this radio is checked.
        /// </summary>
        public string ThemeName
        {
            get => (string)GetValue(ThemeNameProperty);
            set => SetValue(ThemeNameProperty, value);
        }
        #endregion

        #region Mode
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(ThemeRadioMode),
                typeof(DaisyThemeRadio),
                new PropertyMetadata(ThemeRadioMode.Radio, OnModeChanged));

        /// <summary>
        /// Display mode: Radio (standard radio button) or Button (styled as a button).
        /// </summary>
        public ThemeRadioMode Mode
        {
            get => (ThemeRadioMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
        #endregion

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyThemeRadio),
                new PropertyMetadata(DaisySize.Medium, OnSizeChanged));

        /// <summary>
        /// The size of the radio button.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        #endregion

        public DaisyThemeRadio()
        {
            _lifecycle = new DaisyControlLifecycle(
                this,
                OnThemeUpdated,
                () => Size,
                s => Size = s,
                handleLifecycleEvents: false,
                subscribeSizeChanges: false);

            Checked += OnCheckedChanged;
            Unchecked += OnCheckedChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            PointerEntered += OnPointerEntered;
            PointerExited += OnPointerExited;
        }

        // Wrapper for lifecycle - combines theme sync and brush refresh
        private void OnThemeUpdated()
        {
            SyncWithCurrentTheme();
            RefreshThemeBrushes();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureTemplate();
            _lifecycle.HandleLoaded();
            ApplyAll();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleUnloaded();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            EnsureTemplate();
            CacheTemplateParts();
            ApplyAll();
            RefreshThemeBrushes();
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyThemeRadio radio)
            {
                radio.EnsureTemplate();
                radio.CacheTemplateParts();
                radio.ApplyAll();
            }
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyThemeRadio radio)
            {
                radio.ApplyAll();
            }
        }

        private void EnsureTemplate()
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
                return;

            DaisyTokenDefaults.EnsureDefaults(resources);

            var desiredKey = Mode == ThemeRadioMode.Button
                ? "DaisyThemeRadioButtonTemplate"
                : "DaisyThemeRadioRadioTemplate";

            if (resources.TryGetValue(desiredKey, out var templateObj) && templateObj is ControlTemplate template)
            {
                if (!ReferenceEquals(Template, template))
                {
                    Template = template;
                }
            }

            if (Mode == ThemeRadioMode.Button)
            {
                BorderThickness = new Thickness(1);
                CornerRadius = new CornerRadius(8);
                HorizontalAlignment = HorizontalAlignment.Stretch;
                HorizontalContentAlignment = HorizontalAlignment.Center;
            }
            else
            {
                Padding = new Thickness(8, 0, 0, 0);
                HorizontalAlignment = HorizontalAlignment.Left;
                HorizontalContentAlignment = HorizontalAlignment.Left;
            }
        }

        private void CacheTemplateParts()
        {
            _radioIndicatorBorder = GetTemplateChild("PART_Border") as Border;
            _radioCheckMark = GetTemplateChild("CheckMark") as Ellipse;
            _buttonModeBorder = GetTemplateChild("PART_ButtonBorder") as Border;
        }

        private void ApplyAll()
        {
            ApplySizing();
            ApplyVisualState();
        }

        private void ApplySizing()
        {
            // Match Avalonia theme semantics:
            // - Radio mode: only Small alters indicator sizes.
            // - Button mode: Small/Large alter padding & font size.
            if (Mode == ThemeRadioMode.Button)
            {
                switch (Size)
                {
                    case DaisySize.Small:
                        Padding = new Thickness(12, 6, 12, 6);
                        FontSize = 12;
                        break;
                    case DaisySize.Large:
                        Padding = new Thickness(20, 14, 20, 14);
                        FontSize = 16;
                        break;
                    default:
                        FontSize = 14;
                        Padding = new Thickness(16, 10, 16, 10);
                        break;
                }
            }
            else
            {
                FontSize = 14;
                // Indicator sizing is controlled by template parts; for Small we keep parity by shrinking padding.
                if (Size == DaisySize.Small)
                {
                    Padding = new Thickness(6, 0, 0, 0);
                }
                else
                {
                    Padding = new Thickness(8, 0, 0, 0);
                }

                if (_radioIndicatorBorder != null && _radioCheckMark != null)
                {
                    if (Size == DaisySize.Small)
                    {
                        _radioIndicatorBorder.Width = 16;
                        _radioIndicatorBorder.Height = 16;
                        _radioIndicatorBorder.CornerRadius = new CornerRadius(8);
                        _radioCheckMark.Width = 10;
                        _radioCheckMark.Height = 10;
                    }
                    else
                    {
                        _radioIndicatorBorder.Width = 20;
                        _radioIndicatorBorder.Height = 20;
                        _radioIndicatorBorder.CornerRadius = new CornerRadius(10);
                        _radioCheckMark.Width = 12;
                        _radioCheckMark.Height = 12;
                    }
                }
            }
        }

        private void ApplyVisualState()
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
                return;

            DaisyTokenDefaults.EnsureDefaults(resources);

            if (Mode == ThemeRadioMode.Button)
            {
                // Default
                Background = resources.TryGetValue("DaisyBase200Brush", out var bg) && bg is Brush bgBrush ? bgBrush : Background;
                Foreground = resources.TryGetValue("DaisyBaseContentBrush", out var fg) && fg is Brush fgBrush ? fgBrush : Foreground;
                BorderBrush = resources.TryGetValue("DaisyBase300Brush", out var bb) && bb is Brush borderBrush ? borderBrush : BorderBrush;

                // Hover
                if (_isPointerOver && IsChecked != true)
                {
                    if (resources.TryGetValue("DaisyBase300Brush", out var hov) && hov is Brush hovBrush)
                        Background = hovBrush;
                }

                // Checked wins over hover (Avalonia selector specificity)
                if (IsChecked == true)
                {
                    if (resources.TryGetValue("DaisyPrimaryBrush", out var primary) && primary is Brush primaryBrush)
                    {
                        Background = primaryBrush;
                        BorderBrush = primaryBrush;
                    }

                    if (resources.TryGetValue("DaisyPrimaryContentBrush", out var primaryContent) && primaryContent is Brush primaryContentBrush)
                        Foreground = primaryContentBrush;
                }
            }
        }

        private void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _isPointerOver = true;
            ApplyAll();
        }

        private void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _isPointerOver = false;
            ApplyAll();
        }

        private void OnCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;

            if (IsChecked == true && !string.IsNullOrEmpty(ThemeName))
            {
                DaisyThemeManager.ApplyTheme(ThemeName);
            }

            ApplyAll();
        }


        private void RefreshThemeBrushes()
        {
            // Clone brushes to ensure visual update (assigning same brush instance is a no-op)
            var baseContentBrush = CloneBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"));
            var base100Brush = CloneBrush(DaisyResourceLookup.GetBrush("DaisyBase100Brush"));

            if (_radioIndicatorBorder != null && baseContentBrush != null)
            {
                if (IsChecked == true)
                {
                    _radioIndicatorBorder.Background = baseContentBrush;
                    _radioIndicatorBorder.BorderBrush = baseContentBrush;
                }
                else
                {
                    _radioIndicatorBorder.BorderBrush = baseContentBrush;
                    if (base100Brush != null)
                        _radioIndicatorBorder.Background = base100Brush;
                }
            }

            if (_radioCheckMark != null && base100Brush != null)
            {
                _radioCheckMark.Fill = base100Brush;
            }

            // Update foreground for text visibility
            if (Mode == ThemeRadioMode.Radio && baseContentBrush != null)
            {
                Foreground = baseContentBrush;
            }
        }

        private static SolidColorBrush? CloneBrush(Brush? brush)
        {
            if (brush is SolidColorBrush scb)
                return new SolidColorBrush(scb.Color);
            return null;
        }

        private void SyncWithCurrentTheme()
        {
            _isSyncing = true;
            try
            {
                var currentTheme = DaisyThemeManager.CurrentThemeName;
                var isThisTheme = string.Equals(currentTheme, ThemeName, StringComparison.OrdinalIgnoreCase);
                IsChecked = isThisTheme;
            }
            finally
            {
                _isSyncing = false;
            }

            // We suppress OnCheckedChanged while syncing to avoid re-applying the theme.
            // Still need to refresh visuals for button-mode backgrounds, hover, etc.
            ApplyAll();
        }
    }
}
