using Windows.System;

namespace Flowery.Controls
{
    /// <summary>
    /// A slide-to-confirm control that requires the user to drag a handle to complete an action.
    /// Provides visual feedback with color transition and opacity changes.
    /// Supports variant colors and multiple sizes.
    /// </summary>
    public partial class DaisySlideToConfirm : DaisyBaseContentControl
    {
        private Border? _track;
        private Border? _handle;
        private TextBlock? _label;
        private TranslateTransform? _handleTransform;
        private Path? _icon;
        private Viewbox? _iconViewbox;

        private bool _isDragging;
        private double _dragOffsetX;
        private bool _slideCompleted;
        private SolidColorBrush? _originalTrackBrush;
        private SolidColorBrush? _originalLabelBrush;
        private Color _targetLabelColor;
        private DaisyNeumorphicHelper? _handleNeumorphicHelper;

        public DaisySlideToConfirm()
        {
            DefaultStyleKey = typeof(DaisySlideToConfirm);
            IsTabStop = true;
            UseSystemFocusVisuals = true;
            KeyDown += OnKeyDown;

            // Replaced DaisyControlLifecycle with inherited lifecycle by removing explicit instantiation
            // and relying on DaisyBaseContentControl's lifecycle management.
            // The base class constructor will now handle the lifecycle setup.
            // The Loaded/Unloaded events are still subscribed here for specific control logic.
            Loaded += (s, e) => OnLoaded();
            Unloaded += (s, e) => OnUnloaded();
        }

        #region Lifecycle Hooks

        protected override void OnLoaded()
        {
            base.OnLoaded();
            // Re-apply colors when loaded
            ApplyAll();
            UpdateAutomationName();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            _handleNeumorphicHelper?.Dispose();
            _handleNeumorphicHelper = null;
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            // Re-apply colors when theme changes
            ApplyVariantColors();

            // Update original track brush after theme change
            if (_track?.Background is SolidColorBrush brush)
            {
                _originalTrackBrush = brush;
            }
            
            RefreshNeumorphicEffect();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            ApplyAll();
        }

        protected override DaisySize? GetControlSize() => Size;
        protected override void SetControlSize(DaisySize size) => Size = size;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisySlideToConfirmVariant),
                typeof(DaisySlideToConfirm),
                new PropertyMetadata(DaisySlideToConfirmVariant.Default, OnAppearanceChanged));

        /// <summary>
        /// The color variant for the slide control. Determines the slide and icon colors.
        /// </summary>
        public DaisySlideToConfirmVariant Variant
        {
            get => (DaisySlideToConfirmVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty DepthStyleProperty =
            DependencyProperty.Register(
                nameof(DepthStyle),
                typeof(DaisyDepthStyle),
                typeof(DaisySlideToConfirm),
                new PropertyMetadata(DaisyDepthStyle.Flat, OnAppearanceChanged));

        /// <summary>
        /// The 3D depth style for the control. Flat (no shadow), ThreeDimensional (subtle shadow), or Raised (deep shadow).
        /// </summary>
        public DaisyDepthStyle DepthStyle
        {
            get => (DaisyDepthStyle)GetValue(DepthStyleProperty);
            set => SetValue(DepthStyleProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisySlideToConfirm),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        /// <summary>
        /// The size of the slide control.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(DaisySlideToConfirm),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("SlideToConfirm_Text", "SLIDE TO CONFIRM"), OnTextChanged));

        /// <summary>
        /// The text displayed in the track.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty ConfirmingTextProperty =
            DependencyProperty.Register(nameof(ConfirmingText), typeof(string), typeof(DaisySlideToConfirm),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("SlideToConfirm_Confirming", "CONFIRMING...")));

        /// <summary>
        /// The text displayed when the slide is completed.
        /// </summary>
        public string ConfirmingText
        {
            get => (string)GetValue(ConfirmingTextProperty);
            set => SetValue(ConfirmingTextProperty, value);
        }

        public static readonly DependencyProperty SlideColorProperty =
            DependencyProperty.Register(nameof(SlideColor), typeof(Color), typeof(DaisySlideToConfirm),
                new PropertyMetadata(default(Color)));

        /// <summary>
        /// The color the track transitions to when sliding.
        /// If not set, uses the variant color.
        /// </summary>
        public Color SlideColor
        {
            get => (Color)GetValue(SlideColorProperty);
            set => SetValue(SlideColorProperty, value);
        }

        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.Register(nameof(IconData), typeof(string), typeof(DaisySlideToConfirm),
                new PropertyMetadata(FloweryPathHelpers.GetIconPathData("DaisyIconPowerOff"), OnIconDataChanged));

        /// <summary>
        /// The path data for the icon in the handle.
        /// </summary>
        public string IconData
        {
            get => (string)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public static readonly DependencyProperty IconForegroundProperty =
            DependencyProperty.Register(nameof(IconForeground), typeof(Brush), typeof(DaisySlideToConfirm),
                new PropertyMetadata(null));

        /// <summary>
        /// The foreground color of the icon.
        /// If not set, uses the variant color.
        /// </summary>
        public Brush IconForeground
        {
            get => (Brush)GetValue(IconForegroundProperty);
            set => SetValue(IconForegroundProperty, value);
        }

        public static readonly DependencyProperty HandleBackgroundProperty =
            DependencyProperty.Register(nameof(HandleBackground), typeof(Brush), typeof(DaisySlideToConfirm),
                new PropertyMetadata(null));

        /// <summary>
        /// The background color of the handle.
        /// </summary>
        public Brush HandleBackground
        {
            get => (Brush)GetValue(HandleBackgroundProperty);
            set => SetValue(HandleBackgroundProperty, value);
        }

        public static readonly DependencyProperty ResetDelayProperty =
            DependencyProperty.Register(nameof(ResetDelay), typeof(TimeSpan), typeof(DaisySlideToConfirm),
                new PropertyMetadata(TimeSpan.FromMilliseconds(900)));

        /// <summary>
        /// The delay before the control resets after completion.
        /// </summary>
        public TimeSpan ResetDelay
        {
            get => (TimeSpan)GetValue(ResetDelayProperty);
            set => SetValue(ResetDelayProperty, value);
        }

        public static readonly DependencyProperty AutoResetProperty =
            DependencyProperty.Register(nameof(AutoReset), typeof(bool), typeof(DaisySlideToConfirm),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the control automatically resets after a delay when slide is completed.
        /// </summary>
        public bool AutoReset
        {
            get => (bool)GetValue(AutoResetProperty);
            set => SetValue(AutoResetProperty, value);
        }

        public static readonly DependencyProperty TrackWidthProperty =
            DependencyProperty.Register(nameof(TrackWidth), typeof(double), typeof(DaisySlideToConfirm),
                new PropertyMetadata(double.NaN));

        /// <summary>
        /// The width of the slide track. If not set, uses size-based defaults.
        /// </summary>
        public double TrackWidth
        {
            get => (double)GetValue(TrackWidthProperty);
            set => SetValue(TrackWidthProperty, value);
        }

        public static readonly DependencyProperty TrackHeightProperty =
            DependencyProperty.Register(nameof(TrackHeight), typeof(double), typeof(DaisySlideToConfirm),
                new PropertyMetadata(double.NaN));

        /// <summary>
        /// The height of the slide track. If not set, uses size-based defaults.
        /// </summary>
        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the slide is completed.
        /// </summary>
        public event EventHandler? SlideCompleted;

        #endregion

        #region Property Changed Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySlideToConfirm control)
            {
                control.ApplyAll();
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySlideToConfirm control)
            {
                if (control._label != null)
                {
                    control._label.Text = (string)e.NewValue;
                }

                control.UpdateAutomationName();
            }
        }

        private static void OnIconDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySlideToConfirm control)
            {
                control.UpdateIconData();
            }
        }

        #endregion

        #region Template Handling

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _track = GetTemplateChild("PART_Track") as Border;
            _handle = GetTemplateChild("PART_Handle") as Border;
            _label = GetTemplateChild("PART_Label") as TextBlock;
            _handleTransform = GetTemplateChild("PART_HandleTransform") as TranslateTransform;
            _icon = GetTemplateChild("PART_Icon") as Path;
            _iconViewbox = GetTemplateChild("PART_IconViewbox") as Viewbox;

            if (_handle != null)
            {
                _handle.PointerPressed += Handle_PointerPressed;
                _handle.PointerMoved += Handle_PointerMoved;
                _handle.PointerReleased += Handle_PointerReleased;
                _handle.PointerCaptureLost += Handle_PointerCaptureLost;
            }

            ApplyAll();
        }

        private void ApplyAll()
        {
            ApplySizing();
            ApplyVariantColors();
            UpdateIconData();

            // Store original track brush after applying colors
            if (_track?.Background is SolidColorBrush brush)
            {
                _originalTrackBrush = brush;
            }

            RefreshNeumorphicEffect();
        }

        private void UpdateAutomationName()
        {
            var name = _slideCompleted ? ConfirmingText : Text;
            DaisyAccessibility.SetAutomationNameOrClear(this, name);
        }

        private void ApplySizing()
        {
            if (_track == null || _handle == null)
                return;

            // Ensure design tokens are available
            var resources = Application.Current?.Resources;
            if (resources != null)
            {
                Theming.DaisyTokenDefaults.EnsureDefaults(resources);
            }

            // Get size key for token lookup
            var sizeKey = Theming.DaisyResourceLookup.GetSizeKeyFull(Size);

            // Get dimensions from design tokens
            var minTrackWidth = Theming.DaisyResourceLookup.GetDouble($"DaisySlideToConfirm{sizeKey}TrackWidth", 100);
            var trackHeight = Theming.DaisyResourceLookup.GetDouble($"DaisySlideToConfirm{sizeKey}TrackHeight", 48);
            var handleSize = Theming.DaisyResourceLookup.GetDouble($"DaisySlideToConfirm{sizeKey}HandleSize", 40);
            var iconSize = Theming.DaisyResourceLookup.GetDouble($"DaisySlideToConfirm{sizeKey}IconSize", 20);
            var fontSize = Theming.DaisyResourceLookup.GetDouble($"DaisySlideToConfirm{sizeKey}FontSize", 11);
            var cornerRadius = Theming.DaisyResourceLookup.GetDouble($"DaisySlideToConfirm{sizeKey}CornerRadius", 24);

            // Auto-size to content: use MinWidth from token, auto width unless explicit
            // MinWidth must account for: handle + handle gap + text width (using wider of Text/ConfirmingText)
            var baseMinWidth = minTrackWidth + handleSize + 8; // Token is for text area, add handle + gaps
            
            // Calculate width needed for both texts to prevent layout jumps
            var textWidth = MeasureTextWidth(Text ?? "", fontSize);
            var confirmingTextWidth = MeasureTextWidth(ConfirmingText ?? "", fontSize);
            var maxTextWidth = Math.Max(textWidth, confirmingTextWidth);
            var calculatedMinWidth = handleSize + 8 + maxTextWidth + 8; // handle + gap + text + right margin
            var minWidth = Math.Max(baseMinWidth, calculatedMinWidth);
            
            if (!double.IsNaN(TrackWidth))
            {
                _track.Width = TrackWidth;
                _track.MinWidth = 0;
            }
            else
            {
                _track.Width = double.NaN; // Auto width
                _track.MinWidth = minWidth;
            }
            _track.Height = !double.IsNaN(TrackHeight) ? TrackHeight : trackHeight;
            _track.CornerRadius = new CornerRadius(cornerRadius);
            this.CornerRadius = _track.CornerRadius; // Ensure neumorphic helper sees the same radius
            _track.Padding = new Thickness(2); // Compact padding for smaller control

            _handle.Width = handleSize;
            _handle.Height = handleSize;
            _handle.CornerRadius = new CornerRadius(handleSize / 2);

            if (_iconViewbox != null)
            {
                _iconViewbox.Width = iconSize;
                _iconViewbox.Height = iconSize;
            }

            if (_label != null)
            {
                _label.FontSize = fontSize;
                // Add left margin to prevent text from being cut off by the handle
                _label.Margin = new Thickness(handleSize + 4, 0, 4, 0);
                // Set initial opacity (dimmed until slid)
                _label.Opacity = 0.7;
            }
            // Vertical centering is handled by VerticalAlignment="Center" in XAML
        }

        private void ApplyVariantColors()
        {
            // Get variant brush key
            var brushKey = Variant switch
            {
                DaisySlideToConfirmVariant.Primary => "DaisyPrimaryBrush",
                DaisySlideToConfirmVariant.Secondary => "DaisySecondaryBrush",
                DaisySlideToConfirmVariant.Accent => "DaisyAccentBrush",
                DaisySlideToConfirmVariant.Success => "DaisySuccessBrush",
                DaisySlideToConfirmVariant.Warning => "DaisyWarningBrush",
                DaisySlideToConfirmVariant.Info => "DaisyInfoBrush",
                DaisySlideToConfirmVariant.Error => "DaisyErrorBrush",
                _ => "DaisyPrimaryBrush"
            };

            // Resolve variant brush
            var variantBrush = Theming.DaisyResourceLookup.GetBrush(brushKey);
            var variantColor = (variantBrush as SolidColorBrush)?.Color ?? Colors.Gray;

            // Always calculate contrasting color based on luminance
            // This is more reliable than depending on theme content brushes
            _targetLabelColor = GetContrastingTextColor(variantColor);

            // Apply track background from theme (DaisyBase300Brush)
            // Apply border in variant color for immediate visual identity
            // In neumorphic mode, we use the helper instead of manual borders
            if (NeumorphicEnabled == true && _track != null)
            {
                _track.BorderThickness = new Thickness(0);
                _track.Background = new SolidColorBrush(Colors.Transparent);
            }
            else if (_track != null)
            {
                // Apply depth style with visual effect
                var shadowColor = FloweryColorHelpers.Darken(variantColor, 0.4);

                switch (DepthStyle)
                {
                    case DaisyDepthStyle.ThreeDimensional:
                        // Subtle 3D: thicker bottom border, slight offset
                        _track.BorderBrush = new SolidColorBrush(shadowColor);
                        _track.BorderThickness = new Thickness(1, 1, 1, 3);
                        _track.Margin = new Thickness(0, 0, 0, 2); // Push up to show shadow
                        break;
                    case DaisyDepthStyle.Raised:
                        // Deep 3D: even thicker bottom border, more offset
                        _track.BorderBrush = new SolidColorBrush(shadowColor);
                        _track.BorderThickness = new Thickness(1, 1, 1, 5);
                        _track.Margin = new Thickness(0, 0, 0, 4); // Push up more
                        break;
                    default:
                        // Flat: uniform border in variant color
                        _track.BorderBrush = variantBrush;
                        _track.BorderThickness = new Thickness(2);
                        _track.Margin = new Thickness(0);
                        break;
                }
            }

            // Apply label foreground from theme (DaisyBaseContentBrush)
            if (_label != null)
            {
                var labelBrush = Theming.DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");
                _label.Foreground = labelBrush;
                _originalLabelBrush = labelBrush as SolidColorBrush;
            }

            // Apply IconForeground if not explicitly set
            if (IconForeground == null && _icon != null)
            {
                _icon.Fill = variantBrush;
            }
            else if (_icon != null)
            {
                _icon.Fill = IconForeground;
            }

            // Apply SlideColor if not explicitly set
            if (SlideColor == default && variantBrush is SolidColorBrush solidBrush)
            {
                SlideColor = solidBrush.Color;
            }

            // Apply HandleBackground: variant color at 20% opacity for a subtle tint
            if (HandleBackground == null && _handle != null)
            {
                if (NeumorphicEnabled == true)
                {
                     // In neumorphic mode, handle is typically the material color
                    _handle.Background = new SolidColorBrush(Colors.Transparent);
                }
                else
                {
                    var tintedHandle = Color.FromArgb(51, variantColor.R, variantColor.G, variantColor.B); // 51/255 = 20%
                    _handle.Background = new SolidColorBrush(tintedHandle);
                }
            }
            else if (_handle != null)
            {
                _handle.Background = HandleBackground;
            }
        }

        private void UpdateIconData()
        {
            if (_icon == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(IconData))
            {
                _icon.Data = null;
                if (_iconViewbox != null)
                {
                    _iconViewbox.Visibility = Visibility.Collapsed;
                }
                return;
            }

            FloweryPathHelpers.TrySetPathData(_icon, () => FloweryPathHelpers.ParseGeometry(IconData));
            if (_iconViewbox != null)
            {
                _iconViewbox.Visibility = Visibility.Visible;
            }
        }

        #endregion


        #region Pointer Handlers

        private void Handle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_slideCompleted || _handle == null || _handleTransform == null || _track == null)
                return;

            Focus(FocusState.Pointer);
            _isDragging = true;
            _handle.CapturePointer(e.Pointer);

            var currentX = _handleTransform.X;
            var pos = e.GetCurrentPoint(_track).Position;
            _dragOffsetX = pos.X - currentX;
        }

        private void Handle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || _slideCompleted || _handle == null || _handleTransform == null || _track == null)
                return;

            var pos = e.GetCurrentPoint(_track).Position;
            var newX = pos.X - _dragOffsetX;

            var maxX = GetMaxLeft();
            if (newX < 0) newX = 0;
            if (newX > maxX) newX = maxX;

            _handleTransform.X = newX;
            UpdateVisualFeedback(newX, maxX);

            if (newX >= maxX)
            {
                _ = CompleteSlideAsync(maxX);
            }
        }

        private void Handle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndDrag(resetIfNeeded: true);
        }

        private void Handle_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            EndDrag(resetIfNeeded: true);
        }

        private void EndDrag(bool resetIfNeeded)
        {
            _isDragging = false;
            _handle?.ReleasePointerCaptures();

            if (!_slideCompleted && resetIfNeeded)
            {
                AnimateHandleTo(0);
                ResetVisualFeedback();
            }
        }

        #endregion

        #region Keyboard Handling

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_handleTransform == null || _track == null || _handle == null)
                return;

            var maxLeft = GetMaxLeft();
            if (maxLeft <= 0)
                return;

            switch (e.Key)
            {
                case VirtualKey.Enter:
                case VirtualKey.Space:
                case VirtualKey.End:
                    if (!_slideCompleted)
                    {
                        _ = CompleteSlideAsync(maxLeft);
                        e.Handled = true;
                    }
                    else if (e.Key == VirtualKey.End)
                    {
                        e.Handled = true;
                    }
                    return;
                case VirtualKey.Home:
                    if (_slideCompleted)
                    {
                        Reset();
                    }
                    else
                    {
                        SetHandlePosition(0, maxLeft);
                    }
                    e.Handled = true;
                    return;
                case VirtualKey.Left:
                case VirtualKey.Down:
                    if (!_slideCompleted)
                    {
                        MoveHandleBy(-GetKeyboardStep(maxLeft), maxLeft);
                        e.Handled = true;
                    }
                    return;
                case VirtualKey.Right:
                case VirtualKey.Up:
                    if (!_slideCompleted)
                    {
                        MoveHandleBy(GetKeyboardStep(maxLeft), maxLeft);
                        e.Handled = true;
                    }
                    return;
                case VirtualKey.PageDown:
                    if (!_slideCompleted)
                    {
                        MoveHandleBy(-GetKeyboardStep(maxLeft) * 2, maxLeft);
                        e.Handled = true;
                    }
                    return;
                case VirtualKey.PageUp:
                    if (!_slideCompleted)
                    {
                        MoveHandleBy(GetKeyboardStep(maxLeft) * 2, maxLeft);
                        e.Handled = true;
                    }
                    return;
            }
        }

        private void MoveHandleBy(double delta, double maxLeft)
        {
            if (_handleTransform == null)
                return;

            var next = Math.Clamp(_handleTransform.X + delta, 0, maxLeft);
            SetHandlePosition(next, maxLeft);
        }

        private void SetHandlePosition(double position, double maxLeft)
        {
            if (_handleTransform == null)
                return;

            _handleTransform.X = position;
            UpdateVisualFeedback(position, maxLeft);

            if (!_slideCompleted && position >= maxLeft)
            {
                _ = CompleteSlideAsync(maxLeft);
            }
        }

        private static double GetKeyboardStep(double maxLeft)
        {
            return Math.Max(4, maxLeft / 10);
        }

        #endregion

        #region Slide Logic

        private async Task CompleteSlideAsync(double maxLeft)
        {
            if (_slideCompleted)
                return;

            _slideCompleted = true;
            _isDragging = false;
            _handle?.ReleasePointerCaptures();

            if (_label != null)
                _label.Text = ConfirmingText;
            UpdateAutomationName();

            AnimateHandleTo(maxLeft);

            // Raise the event
            SlideCompleted?.Invoke(this, EventArgs.Empty);

            if (AutoReset)
            {
                await Task.Delay(ResetDelay);
                Reset();
            }
        }

        /// <summary>
        /// Resets the control to its initial state.
        /// </summary>
        public void Reset()
        {
            if (_label != null)
                _label.Text = Text;

            _slideCompleted = false;
            UpdateAutomationName();
            AnimateHandleTo(0);
            ResetVisualFeedback();
        }

        private double GetMaxLeft()
        {
            if (_track == null || _handle == null)
                return 0;

            var trackWidth = !double.IsNaN(_track.Width) && _track.Width > 0 ? _track.Width : _track.ActualWidth;
            var handleWidth = !double.IsNaN(_handle.Width) && _handle.Width > 0 ? _handle.Width : _handle.ActualWidth;
            var padding = _track.Padding;

            var max = trackWidth - padding.Left - padding.Right - handleWidth;
            return max < 0 ? 0 : max;
        }

        private void AnimateHandleTo(double targetX)
        {
            if (_handleTransform == null)
                return;

            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                To = targetX,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, _handleTransform);
            Storyboard.SetTargetProperty(animation, "X");
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        #endregion

        #region Visual Feedback

        private void UpdateVisualFeedback(double currentLeft, double maxLeft)
        {
            if (_track == null || _label == null || maxLeft <= 0)
                return;

            var progress = Math.Clamp(currentLeft / maxLeft, 0, 1);

            // Interpolate track background color (Base300 -> SlideColor)
            var originalColor = _originalTrackBrush?.Color ?? Color.FromArgb(255, 100, 100, 100);
            var targetColor = SlideColor;
            var trackR = (byte)(originalColor.R + ((targetColor.R - originalColor.R) * progress));
            var trackG = (byte)(originalColor.G + ((targetColor.G - originalColor.G) * progress));
            var trackB = (byte)(originalColor.B + ((targetColor.B - originalColor.B) * progress));
            
            var currentColor = Color.FromArgb(255, trackR, trackG, trackB);
            
            if (NeumorphicEnabled == true)
            {
                // In Neumorphic mode, we update the composition surface color
                RefreshNeumorphicEffect(currentColor);
            }
            else
            {
                _track.Background = new SolidColorBrush(currentColor);
            }

            // Interpolate label foreground color (BaseContent -> VariantContent)
            var originalLabelColor = _originalLabelBrush?.Color ?? Color.FromArgb(255, 0, 0, 0);
            var labelR = (byte)(originalLabelColor.R + ((_targetLabelColor.R - originalLabelColor.R) * progress));
            var labelG = (byte)(originalLabelColor.G + ((_targetLabelColor.G - originalLabelColor.G) * progress));
            var labelB = (byte)(originalLabelColor.B + ((_targetLabelColor.B - originalLabelColor.B) * progress));
            _label.Foreground = new SolidColorBrush(Color.FromArgb(255, labelR, labelG, labelB));

            // Animate label opacity from 0.7 to 1.0
            _label.Opacity = 0.7 + (0.3 * progress);
        }

        private void ResetVisualFeedback()
        {
            if (_track != null && _originalTrackBrush != null)
            {
                _track.Background = _originalTrackBrush;
            }

            if (_label != null)
            {
                _label.Opacity = 0.7;
                if (_originalLabelBrush != null)
                {
                    _label.Foreground = _originalLabelBrush;
                }
            }
        }

        #endregion

        #region Neumorphism Upgrade

        protected override bool IsNeumorphicEffectivelyEnabled() => base.IsNeumorphicEffectivelyEnabled();

        public new void RefreshNeumorphicEffect() => RefreshNeumorphicEffect(null);

        private void RefreshNeumorphicEffect(Color? trackColorOverride)
        {
            if (_track == null || _handle == null) return;

            bool isEnabled = IsNeumorphicEffectivelyEnabled();
            var intensity = GetEffectiveNeumorphicIntensity();

            if (!isEnabled)
            {
                _neumorphicHelper?.Dispose();
                _neumorphicHelper = null;
                _handleNeumorphicHelper?.Dispose();
                _handleNeumorphicHelper = null;
                return;
            }
            
            // 1. Update Track (Inset)
            _neumorphicHelper ??= new DaisyNeumorphicHelper(_track);
            
            Color? trackColor = trackColorOverride;
            if (trackColor == null && DaisyResourceLookup.GetBrush("DaisyBase300Brush") is SolidColorBrush scb) trackColor = scb.Color;
            
            _neumorphicHelper.Update(
                isEnabled,
                DaisyNeumorphicMode.Inset,
                intensity,
                DaisyResourceLookup.GetDefaultElevation(Size)
            );

            // 2. Update Handle (Raised)
            _handleNeumorphicHelper ??= new DaisyNeumorphicHelper(_handle);
            if (_handleNeumorphicHelper != null)
            {
                _handleNeumorphicHelper.Update(
                    isEnabled,
                    DaisyNeumorphicMode.Raised,
                    intensity,
                    DaisyResourceLookup.GetDefaultElevation(Size)
                );
            }
        }

        #endregion

        #region Helpers

        private static double MeasureTextWidth(string text, double fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = FontWeights.Bold // Match the label style
            };
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.DesiredSize.Width;
        }

        private static Color GetContrastingTextColor(Color backgroundColor)
        {
            // Calculate relative luminance using sRGB formula
            var r = backgroundColor.R / 255.0;
            var g = backgroundColor.G / 255.0;
            var b = backgroundColor.B / 255.0;
            var luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            
            // Use white text for dark backgrounds, black for light
            return luminance > 0.5 ? Colors.Black : Colors.White;
        }

        #endregion
    }
}
