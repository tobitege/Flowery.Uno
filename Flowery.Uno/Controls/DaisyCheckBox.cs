using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A CheckBox control styled after DaisyUI's Checkbox component.
    /// Supports variant colors and multiple sizes.
    /// </summary>
    public partial class DaisyCheckBox : CheckBox
    {
        private Border? _checkboxBorder;
        private Microsoft.UI.Xaml.Shapes.Path? _checkmark;
        private TextBlock? _labelTextBlock;
        private object? _userContent;
        private long _foregroundCallbackToken;
        private bool _hasForegroundOverride;
        private bool _foregroundOverrideInitialized;
        private bool _isApplyingForeground;
        private readonly DaisyControlLifecycle _lifecycle;

        public DaisyCheckBox()
        {
            DefaultStyleKey = typeof(DaisyCheckBox);
            // Override base CheckBox template to prevent conflicts with our code-driven visual tree
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Center;
            MinWidth = 0;
            MinHeight = 0;

            _lifecycle = new DaisyControlLifecycle(
                this,
                ApplyAll,
                () => Size,
                s => Size = s,
                handleLifecycleEvents: false,
                subscribeSizeChanges: false);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Checked += (s, e) => ApplyAll();
            Unchecked += (s, e) => ApplyAll();
        }

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyCheckBoxVariant),
                typeof(DaisyCheckBox),
                new PropertyMetadata(DaisyCheckBoxVariant.Default, OnAppearanceChanged));

        public DaisyCheckBoxVariant Variant
        {
            get => (DaisyCheckBoxVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyCheckBox),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyCheckBox cb)
            {
                cb.ApplyAll();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _foregroundCallbackToken = RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundChanged);
            EnsureForegroundOverrideInitialized();

            // Capture user content before we replace Content with our visual tree.
            if (Content != null && Content is not StackPanel) // Added check to avoid repeated capture if re-loaded
            {
                _userContent = Content;
                // Detach before re-parenting into our ContentPresenter (Uno throws if an element has 2 parents).
                Content = null;
            }

            BuildVisualTree();
            _lifecycle.HandleLoaded();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleUnloaded();

            if (_foregroundCallbackToken != 0)
            {
                UnregisterPropertyChangedCallback(ForegroundProperty, _foregroundCallbackToken);
                _foregroundCallbackToken = 0;
            }
        }

        #endregion

        #region Visual Tree

        private void BuildVisualTree()
        {
            // Build once; on subsequent loads just ensure the label reflects the captured content.
            if (Content is StackPanel existingRoot)
            {
                if (existingRoot.Children.Count > 1 && existingRoot.Children[1] is ContentPresenter existingPresenter)
                {
                    existingPresenter.Content = _userContent;
                    _labelTextBlock = null;
                }
                else if (existingRoot.Children.Count > 1 && existingRoot.Children[1] is TextBlock existingText)
                {
                    existingText.Text = _userContent as string ?? _userContent?.ToString() ?? string.Empty;
                    _labelTextBlock = existingText;
                }

                if (_checkboxBorder == null && existingRoot.Children.Count > 0 && existingRoot.Children[0] is Border existingBorder)
                {
                    _checkboxBorder = existingBorder;
                    _checkmark = existingBorder.Child as Microsoft.UI.Xaml.Shapes.Path;
                }
                return;
            }

            var root = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            _checkboxBorder = new Border
            {
                BorderThickness = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Checkmark path (hidden when unchecked)
            _checkmark = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = CreateCheckmarkGeometry(),
                StrokeThickness = 2,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            _checkboxBorder.Child = _checkmark;
            root.Children.Add(_checkboxBorder);

            if (_userContent is string labelText)
            {
                _labelTextBlock = new TextBlock
                {
                    Text = labelText,
                    VerticalAlignment = VerticalAlignment.Center
                };
                root.Children.Add(_labelTextBlock);
            }
            else
            {
                // Content presenter for the label
                var contentPresenter = new ContentPresenter
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = _userContent
                };
                _labelTextBlock = null;
                root.Children.Add(contentPresenter);
            }

            Content = root;
        }

        private static PathGeometry CreateCheckmarkGeometry()
        {
            // Simple checkmark path: L-shaped tick
            var pathGeometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(2, 6),
                IsClosed = false
            };
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(5, 9) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(10, 2) });
            pathGeometry.Figures.Add(figure);
            return pathGeometry;
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_checkboxBorder == null || _checkmark == null)
                return;

            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            if (_checkboxBorder == null || _checkmark == null)
                return;

            // Get sizing from tokens (guaranteed by EnsureDefaults)
            var sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);
            double boxSize = DaisyResourceLookup.GetDouble($"DaisyCheckbox{sizeKey}Size", 18);
            double checkmarkSize = DaisyResourceLookup.GetDouble($"DaisyCheckmark{sizeKey}Size", 12);

            _checkboxBorder.Width = boxSize;
            _checkboxBorder.Height = boxSize;
            _checkboxBorder.CornerRadius = DaisyResourceLookup.GetDefaultCornerRadius(Size);

            _checkmark.Width = checkmarkSize;
            _checkmark.Height = checkmarkSize;

            FontSize = DaisyResourceLookup.GetDefaultFontSize(Size);
        }

        private void ApplyColors()
        {
            if (_checkboxBorder == null || _checkmark == null)
                return;

            bool isChecked = IsChecked == true;

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyCheckBoxVariant.Primary => "Primary",
                DaisyCheckBoxVariant.Secondary => "Secondary",
                DaisyCheckBoxVariant.Accent => "Accent",
                DaisyCheckBoxVariant.Neutral => "Neutral",
                DaisyCheckBoxVariant.Success => "Success",
                DaisyCheckBoxVariant.Warning => "Warning",
                DaisyCheckBoxVariant.Info => "Info",
                DaisyCheckBoxVariant.Error => "Error",
                _ => ""
            };

            // Get variant color from palette
            var (bgBrushKey, fgBrushKey) = Variant switch
            {
                DaisyCheckBoxVariant.Primary => ("DaisyPrimaryBrush", "DaisyPrimaryContentBrush"),
                DaisyCheckBoxVariant.Secondary => ("DaisySecondaryBrush", "DaisySecondaryContentBrush"),
                DaisyCheckBoxVariant.Accent => ("DaisyAccentBrush", "DaisyAccentContentBrush"),
                DaisyCheckBoxVariant.Neutral => ("DaisyNeutralBrush", "DaisyNeutralContentBrush"),
                DaisyCheckBoxVariant.Success => ("DaisySuccessBrush", "DaisySuccessContentBrush"),
                DaisyCheckBoxVariant.Warning => ("DaisyWarningBrush", "DaisyWarningContentBrush"),
                DaisyCheckBoxVariant.Info => ("DaisyInfoBrush", "DaisyInfoContentBrush"),
                DaisyCheckBoxVariant.Error => ("DaisyErrorBrush", "DaisyErrorContentBrush"),
                _ => ("DaisyBase300Brush", "DaisyBaseContentBrush")
            };

            // Check for lightweight styling overrides
            var bgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyCheckBox", $"{variantName}Background")
                : null;
            bgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyCheckBox", "Background");

            var fgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyCheckBox", $"{variantName}Checkmark")
                : null;
            fgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyCheckBox", "Checkmark");

            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyCheckBox", "BorderBrush");

            var accentBrush = bgOverride ?? DaisyResourceLookup.GetBrush(bgBrushKey);
            var contentBrush = fgOverride ?? DaisyResourceLookup.GetBrush(fgBrushKey);
            var borderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            var labelBrush = _hasForegroundOverride
                ? Foreground
                : GetAppBrush("DaisyBaseContentBrush");

            if (isChecked)
            {
                _checkboxBorder.Background = accentBrush;
                _checkboxBorder.BorderBrush = accentBrush;
                _checkmark.Stroke = contentBrush;
                _checkmark.Visibility = Visibility.Visible;
            }
            else
            {
                _checkboxBorder.Background = new SolidColorBrush(Colors.Transparent);
                _checkboxBorder.BorderBrush = borderBrush;
                _checkmark.Visibility = Visibility.Collapsed;
            }

            if (!_hasForegroundOverride)
            {
                SetForeground(labelBrush);
            }

            if (_labelTextBlock != null)
            {
                _labelTextBlock.Foreground = labelBrush;
            }
            else if (_userContent is TextBlock textBlock)
            {
                if (textBlock.ReadLocalValue(TextBlock.ForegroundProperty) == DependencyProperty.UnsetValue)
                {
                    textBlock.Foreground = labelBrush;
                }
            }
            else if (_userContent is Control control)
            {
                if (control.ReadLocalValue(Control.ForegroundProperty) == DependencyProperty.UnsetValue)
                {
                    control.Foreground = labelBrush;
                }
            }
        }

        #endregion

        private void OnForegroundChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_isApplyingForeground)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            ApplyAll();
        }

        private void EnsureForegroundOverrideInitialized()
        {
            if (_foregroundOverrideInitialized)
                return;

            _hasForegroundOverride = ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue;
            _foregroundOverrideInitialized = true;
        }

        private void SetForeground(Brush brush)
        {
            if (ReferenceEquals(Foreground, brush))
                return;

            _isApplyingForeground = true;
            try
            {
                Foreground = brush;
            }
            finally
            {
                _isApplyingForeground = false;
            }
        }

        private static Brush GetAppBrush(string key)
        {
            var resources = Application.Current?.Resources;
            if (resources != null && resources.TryGetValue(key, out var value) && value is Brush brush)
                return brush;

            return DaisyResourceLookup.GetBrush(key);
        }
    }
}
