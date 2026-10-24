namespace Flowery.Controls
{
    /// <summary>
    /// A toggle control for switching between light/dark themes.
    /// </summary>
    public partial class DaisyThemeController : ToggleButton
    {
        private bool _isSyncing;
        private readonly DaisyControlLifecycle _lifecycle;

        #region Size
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyThemeController),
                new PropertyMetadata(DaisySize.Auto, OnSizeChanged));

        /// <summary>
        /// Gets or sets the checkbox size tier. Auto follows the current size scope or global size.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        /// <summary>
        /// Gets the effective checkbox size without replacing the authored Size value.
        /// </summary>
        public DaisySize ResolvedSize => FlowerySizeManager.ResolveSize(this, Size);
        #endregion

        #region Mode
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                nameof(Mode),
                typeof(ThemeControllerMode),
                typeof(DaisyThemeController),
                new PropertyMetadata(ThemeControllerMode.Toggle, OnModeChanged));

        /// <summary>
        /// The display mode of the controller.
        /// </summary>
        public ThemeControllerMode Mode
        {
            get => (ThemeControllerMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
        #endregion

        #region UncheckedLabel
        public static readonly DependencyProperty UncheckedLabelProperty =
            DependencyProperty.Register(
                nameof(UncheckedLabel),
                typeof(string),
                typeof(DaisyThemeController),
                new PropertyMetadata("Light"));

        /// <summary>
        /// Label displayed when unchecked (light theme).
        /// </summary>
        public string UncheckedLabel
        {
            get => (string)GetValue(UncheckedLabelProperty);
            set => SetValue(UncheckedLabelProperty, value);
        }
        #endregion

        #region CheckedLabel
        public static readonly DependencyProperty CheckedLabelProperty =
            DependencyProperty.Register(
                nameof(CheckedLabel),
                typeof(string),
                typeof(DaisyThemeController),
                new PropertyMetadata("Dark"));

        /// <summary>
        /// Label displayed when checked (dark theme).
        /// </summary>
        public string CheckedLabel
        {
            get => (string)GetValue(CheckedLabelProperty);
            set => SetValue(CheckedLabelProperty, value);
        }
        #endregion

        #region UncheckedTheme
        public static readonly DependencyProperty UncheckedThemeProperty =
            DependencyProperty.Register(
                nameof(UncheckedTheme),
                typeof(string),
                typeof(DaisyThemeController),
                new PropertyMetadata("Light"));

        /// <summary>
        /// Theme name to apply when unchecked.
        /// </summary>
        public string UncheckedTheme
        {
            get => (string)GetValue(UncheckedThemeProperty);
            set => SetValue(UncheckedThemeProperty, value);
        }
        #endregion

        #region CheckedTheme
        public static readonly DependencyProperty CheckedThemeProperty =
            DependencyProperty.Register(
                nameof(CheckedTheme),
                typeof(string),
                typeof(DaisyThemeController),
                new PropertyMetadata("Dark"));

        /// <summary>
        /// Theme name to apply when checked.
        /// </summary>
        public string CheckedTheme
        {
            get => (string)GetValue(CheckedThemeProperty);
            set => SetValue(CheckedThemeProperty, value);
        }
        #endregion

        public DaisyThemeController()
        {
            _lifecycle = new DaisyControlLifecycle(
                this,
                OnThemeUpdated,
                handleLifecycleEvents: false);

            FlowerySizeManager.Register(this, ApplySizing, () => ResolvedSize);

            Checked += OnCheckedChanged;
            Unchecked += OnCheckedChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // Wrapper for lifecycle - combines all theme-related updates
        private void OnThemeUpdated()
        {
            SyncWithCurrentTheme();
            UpdateAlternateTheme(DaisyThemeManager.CurrentThemeName ?? "");
            RefreshThemeBrushes();
            ApplySizing();
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyThemeController controller)
                controller.ApplySizing();
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyThemeController controller)
            {
                controller.EnsureTemplate();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureTemplate();
            _lifecycle.HandleLoaded();
            UpdateVisualState(false);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleUnloaded();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            EnsureTemplate();
            RefreshThemeBrushes();
            ApplySizing();
            UpdateVisualState(false);
        }

        private void ApplySizing()
        {
            var size = ResolvedSize;
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(size);
            if (GetTemplateChild("CheckboxRoot") is Grid root)
            {
                var hitSize = DaisyResourceLookup.GetDouble($"DaisySize{sizeKey}Height", FlowerySizeManager.GetControlHeight(size));
                root.MinWidth = hitSize;
                root.MinHeight = hitSize;
            }
            if (GetTemplateChild("CheckboxMode") is Border indicator)
            {
                var indicatorSize = DaisyResourceLookup.GetDouble($"DaisyCheckbox{sizeKey}Size", 20);
                indicator.Width = indicatorSize;
                indicator.Height = indicatorSize;
            }
            if (GetTemplateChild("CheckMark") is Path checkMark)
            {
                var checkmarkSize = DaisyResourceLookup.GetDouble($"DaisyCheckmark{sizeKey}Size", 12);
                checkMark.Width = checkmarkSize;
                checkMark.Height = checkmarkSize;
            }
        }

        private void EnsureTemplate()
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
                return;

            DaisyTokenDefaults.EnsureDefaults(resources);

            var desiredKey = Mode switch
            {
                ThemeControllerMode.Toggle => "DaisyThemeControllerToggleTemplate",
                ThemeControllerMode.Checkbox => "DaisyThemeControllerCheckboxTemplate",
                ThemeControllerMode.Swap => "DaisyThemeControllerSwapTemplate",
                ThemeControllerMode.ToggleWithText => "DaisyThemeControllerToggleWithTextTemplate",
                ThemeControllerMode.ToggleWithIcons => "DaisyThemeControllerToggleWithIconsTemplate",
                _ => "DaisyThemeControllerToggleTemplate"
            };

            if (resources.TryGetValue(desiredKey, out var templateObj) && templateObj is ControlTemplate template)
            {
                if (!ReferenceEquals(Template, template))
                {
                    Template = template;
                }
            }
        }

        private void OnCheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateVisualState(true);

            if (_isSyncing) return;

            var targetTheme = IsChecked == true ? CheckedTheme : UncheckedTheme;
            DaisyThemeManager.ApplyTheme(targetTheme);
        }


        private void SyncWithCurrentTheme()
        {
            _isSyncing = true;
            try
            {
                var currentTheme = DaisyThemeManager.CurrentThemeName;
                var isBaseTheme = string.Equals(currentTheme, UncheckedTheme, StringComparison.OrdinalIgnoreCase);
                IsChecked = !isBaseTheme;
                UpdateVisualState(true);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void UpdateVisualState(bool useTransitions)
        {
            var stateName = IsChecked == true ? "Checked" : "Unchecked";
            VisualStateManager.GoToState(this, stateName, useTransitions);
        }

        private void UpdateAlternateTheme(string themeName)
        {
            // If the new theme is not the base/unchecked theme, update the checked theme
            if (!string.Equals(themeName, UncheckedTheme, StringComparison.OrdinalIgnoreCase))
            {
                CheckedTheme = themeName;
                CheckedLabel = themeName;
            }
        }

        private void RefreshThemeBrushes()
        {
            // Clone brushes to ensure visual update (assigning same brush instance is a no-op)
            var primaryBrush = CloneBrush(DaisyResourceLookup.GetBrush("DaisyPrimaryBrush"));
            var warningBrush = CloneBrush(DaisyResourceLookup.GetBrush("DaisyWarningBrush"));
            var infoBrush = CloneBrush(DaisyResourceLookup.GetBrush("DaisyInfoBrush"));
            var baseContentBrush = CloneBrush(DaisyResourceLookup.GetBrush("DaisyBaseContentBrush"));

            // Update toggle knobs
            if (GetTemplateChild("ToggleKnob") is Ellipse toggleKnob && primaryBrush != null)
                toggleKnob.Fill = primaryBrush;
            if (GetTemplateChild("TextToggleKnob") is Ellipse textKnob && primaryBrush != null)
                textKnob.Fill = primaryBrush;
            if (GetTemplateChild("IconToggleKnob") is Ellipse iconKnob && primaryBrush != null)
                iconKnob.Fill = primaryBrush;

            // Update sun/moon icons
            if (GetTemplateChild("SunIcon") is Path sunIcon && warningBrush != null)
                sunIcon.Fill = warningBrush;
            if (GetTemplateChild("MoonIcon") is Path moonIcon && infoBrush != null)
                moonIcon.Fill = infoBrush;

            // Update checkmark
            if (GetTemplateChild("CheckMark") is Path checkMark && baseContentBrush != null)
                checkMark.Fill = baseContentBrush;
        }

        private static SolidColorBrush? CloneBrush(Brush? brush)
        {
            if (brush is SolidColorBrush scb)
                return new SolidColorBrush(scb.Color);
            return null;
        }
    }
}
