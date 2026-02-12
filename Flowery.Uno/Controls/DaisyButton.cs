using System.Numerics;
using Flowery.Services;
using Microsoft.UI.Xaml.Media;
using Uno.Toolkit.UI;
using Windows.Foundation.Metadata;
using Windows.UI.Text;
using ToolkitShadow = Uno.Toolkit.UI.Shadow;
using ToolkitShadowCollection = Uno.Toolkit.UI.ShadowCollection;

namespace Flowery.Controls
{
    public enum DaisyButtonVariant
    {
        Default,
        Neutral,
        Primary,
        Secondary,
        Accent,
        Ghost,
        Link,
        Info,
        Success,
        Warning,
        Error
    }

    public enum DaisyButtonStyle
    {
        Default,
        Outline,
        Dash,
        Soft
    }

    public enum DaisyButtonShape
    {
        Default,
        Wide,
        Block,
        Square,
        Circle
    }

    /// <summary>
    /// A Button control styled after DaisyUI's Button component (Uno/WinUI).
    /// </summary>
    public partial class DaisyButton : Button
    {
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyButtonVariant),
                typeof(DaisyButton),
                new PropertyMetadata(DaisyButtonVariant.Default, OnStylePropertyChanged));

        public DaisyButtonVariant Variant
        {
            get => (DaisyButtonVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyButton),
                new PropertyMetadata(DaisySize.Medium, OnStylePropertyChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ButtonStyleProperty =
            DependencyProperty.Register(
                nameof(ButtonStyle),
                typeof(DaisyButtonStyle),
                typeof(DaisyButton),
                new PropertyMetadata(DaisyButtonStyle.Default, OnStylePropertyChanged));

        public DaisyButtonStyle ButtonStyle
        {
            get => (DaisyButtonStyle)GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }

        public static readonly DependencyProperty ShapeProperty =
            DependencyProperty.Register(
                nameof(Shape),
                typeof(DaisyButtonShape),
                typeof(DaisyButton),
                new PropertyMetadata(DaisyButtonShape.Default, OnStylePropertyChanged));

        public DaisyButtonShape Shape
        {
            get => (DaisyButtonShape)GetValue(ShapeProperty);
            set => SetValue(ShapeProperty, value);
        }

        #region Unified Icon API
        // These properties provide a simpler, "batteries included" approach to icons.
        // They handle auto-scaling based on button Size and avoid the complex attached property syntax.
        // Use one of: IconSymbol (for Windows Symbols) OR IconData (for custom path data).

        public static readonly DependencyProperty IconSymbolProperty =
            DependencyProperty.Register(
                nameof(IconSymbol),
                typeof(Symbol?),
                typeof(DaisyButton),
                new PropertyMetadata(null, OnIconPropertyChanged));

        /// <summary>
        /// Gets or sets a Windows Symbol to display as the button icon.
        /// Auto-sized based on button Size. Inherits Foreground color.
        /// </summary>
        public Symbol? IconSymbol
        {
            get => (Symbol?)GetValue(IconSymbolProperty);
            set => SetValue(IconSymbolProperty, value);
        }

        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.Register(
                nameof(IconData),
                typeof(string),
                typeof(DaisyButton),
                new PropertyMetadata(null, OnIconPropertyChanged));

        /// <summary>
        /// Gets or sets a path data string (from a 24x24 viewBox) to display as the button icon.
        /// Auto-scaled using Viewbox based on button Size. Inherits Foreground color.
        /// </summary>
        public string? IconData
        {
            get => (string?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.Register(
                nameof(IconPlacement),
                typeof(IconPlacement),
                typeof(DaisyButton),
                new PropertyMetadata(IconPlacement.Left, OnIconPropertyChanged));

        /// <summary>
        /// Gets or sets where the icon appears relative to the content.
        /// Default is Left.
        /// </summary>
        public IconPlacement IconPlacement
        {
            get => (IconPlacement)GetValue(IconPlacementProperty);
            set => SetValue(IconPlacementProperty, value);
        }

        private static void OnIconPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButton button && button.IsLoaded)
            {
                button.RebuildIconLayout();
            }
        }

        #endregion
        protected const string ShadowContainerPartName = "PART_ShadowContainer";
        private bool _isPointerOver;
        private bool _isPressed;

        private ShadowContainer? _shadowContainer;
        private Border? _buttonBorder;
        private Canvas? _themeShadowReceiver;
        private ThemeShadow? _themeShadow;
        private ToolkitShadowCollection? _shadowCollection;
        private ToolkitShadow? _darkShadow;
        private ToolkitShadow? _lightShadow;

        protected ShadowContainer? ShadowContainer => _shadowContainer;
        protected ToolkitShadowCollection? ShadowCollection => _shadowCollection;
        protected ToolkitShadow? DarkShadow => _darkShadow;
        protected ToolkitShadow? LightShadow => _lightShadow;

        /// <summary>
        /// When true, the button's visual state (Background, Foreground, etc.) is managed
        /// by an external container (e.g. DaisyButtonGroup) and ApplyCurrentPointerState will not update visuals.
        /// </summary>
        public bool IsExternallyControlled { get; set; }

        private Brush? _normalBackground;
        private Brush? _pointerOverBackground;
        private Brush? _pressedBackground;

        private Brush? _normalForeground;
        private Brush? _pointerOverForeground;
        private Brush? _pressedForeground;

        private Brush? _normalBorderBrush;
        private Brush? _pointerOverBorderBrush;
        private Brush? _pressedBorderBrush;

        private Thickness _normalBorderThickness;
        private Thickness _pointerOverBorderThickness;
        private Thickness _pressedBorderThickness;

        private Rectangle? _dashedBorder;

        // Icon support fields
        private StackPanel? _iconContentPanel;
        private ContentPresenter? _iconPresenter;
        private ContentPresenter? _textPresenter;
        private object? _userContent;
        private bool _iconLayoutBuilt;
        private readonly DaisyControlLifecycle _lifecycle;

        public DaisyButton()
        {
            DefaultStyleKey = typeof(DaisyButton);

            _lifecycle = new DaisyControlLifecycle(
                this,
                ApplyAll,
                () => Size,
                s => Size = s,
                handleLifecycleEvents: false);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsEnabledChanged += (_, _) => ApplyCurrentPointerState();

            // Watch for Height changes to keep Width in sync for Circle/Square shapes
            RegisterPropertyChangedCallback(HeightProperty, OnHeightPropertyChanged);

            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
            FontWeight = new FontWeight { Weight = 600 };
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _shadowContainer = GetTemplateChild(ShadowContainerPartName) as ShadowContainer;
            _buttonBorder = GetTemplateChild("ButtonBorder") as Border;
            _themeShadowReceiver = GetTemplateChild("PART_NeumorphicHost") as Canvas;

            // Visual tree fallback if GetTemplateChild fails (same as DaisyButtonTechDemo)
            _buttonBorder ??= FindChildByType<Border>(this);

            RequestNeumorphicRefresh();
        }

        private static T? FindChildByType<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                var result = FindChildByType<T>(child);
                if (result != null)
                    return result;
            }
            return default;
        }

        #region Neumorphic Support

        public bool? NeumorphicEnabled
        {
            get => DaisyNeumorphic.GetIsEnabled(this);
            set => DaisyNeumorphic.SetIsEnabled(this, value);
        }

        public DaisyNeumorphicMode? NeumorphicMode
        {
            get => DaisyNeumorphic.GetMode(this);
            set => DaisyNeumorphic.SetMode(this, value);
        }

        public double? NeumorphicIntensity
        {
            get => DaisyNeumorphic.GetIntensity(this);
            set => DaisyNeumorphic.SetIntensity(this, value);
        }

        public double NeumorphicBlurRadius
        {
            get => DaisyNeumorphic.GetBlurRadius(this);
            set => DaisyNeumorphic.SetBlurRadius(this, value);
        }

        public double NeumorphicOffset
        {
            get => DaisyNeumorphic.GetOffset(this);
            set => DaisyNeumorphic.SetOffset(this, value);
        }

        public Color? NeumorphicDarkShadowColor
        {
            get => DaisyNeumorphic.GetDarkShadowColor(this);
            set => DaisyNeumorphic.SetDarkShadowColor(this, value);
        }

        public Color? NeumorphicLightShadowColor
        {
            get => DaisyNeumorphic.GetLightShadowColor(this);
            set => DaisyNeumorphic.SetLightShadowColor(this, value);
        }

        public bool? NeumorphicRimLightEnabled
        {
            get => DaisyNeumorphic.GetRimLightEnabled(this);
            set => DaisyNeumorphic.SetRimLightEnabled(this, value);
        }

        public bool? NeumorphicSurfaceGradientEnabled
        {
            get => DaisyNeumorphic.GetSurfaceGradientEnabled(this);
            set => DaisyNeumorphic.SetSurfaceGradientEnabled(this, value);
        }

        internal void InvokeApplyCurrentPointerState()
        {
            ApplyCurrentPointerState();
        }

        public void RefreshNeumorphicEffect()
        {
            SyncTemplateResources();

            // Retry getting template parts if they're still null (timing issue)
            EnsureTemplateParts();

            var isEnabled = IsNeumorphicEffectivelyEnabled();
            var mode = GetEffectiveNeumorphicMode();
            var intensity = GetEffectiveNeumorphicIntensity();
            var elevation = DaisyResourceLookup.GetDefaultElevation(Size);

            if (!isEnabled)
            {
                ClearNeumorphicShadows();
                return;
            }

            // On non-Windows (WASM, Skia, Android, iOS): use direct SetElevation on ButtonBorder.
            // This is EXACTLY what DaisyButtonTechDemo does - no ShadowContainer involvement.
            var useDirectElevation = DaisyNeumorphicHelper.ShouldUseDirectElevation();

            if (useDirectElevation && (_buttonBorder != null || _shadowContainer != null))
            {
                UIElement? elevationTarget = _buttonBorder;
                if (PlatformCompatibility.IsSkiaBackend && _shadowContainer != null)
                {
                    elevationTarget = _shadowContainer;
                }
                if (elevationTarget != null)
                {
                    DaisyNeumorphicHelper.ApplyDirectElevation(elevationTarget, mode, intensity, elevation);
                }
                return;
            }

            if (!TryEnsureShadowContainer())
            {
                if (_buttonBorder is UIElement borderElement)
                {
                    DaisyNeumorphicHelper.ApplyDirectElevation(borderElement, mode, intensity, elevation);
                }
                return;
            }

            SyncShadowContainerCornerRadius();
            EnsureShadowResources();

            var (offset, blur) = DaisyNeumorphicHelper.GetShadowMetrics(this, elevation, mode);

            if (mode == DaisyNeumorphicMode.Raised && TryApplyThemeShadow(elevation))
            {
                return;
            }

            var darkShadowColor = DaisyNeumorphic.GetDarkShadowColor(this)
                ?? DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor;
            var lightShadowColor = DaisyNeumorphic.GetLightShadowColor(this)
                ?? DaisyBaseContentControl.GlobalNeumorphicLightShadowColor;

            if (_shadowContainer == null || _shadowCollection == null || _darkShadow == null || _lightShadow == null)
            {
                return;
            }

            if (mode == DaisyNeumorphicMode.Inset)
            {
                _darkShadow.IsInner = true;
                _lightShadow.IsInner = true;
                _darkShadow.OffsetX = -offset;
                _darkShadow.OffsetY = -offset;
                _lightShadow.OffsetX = offset;
                _lightShadow.OffsetY = offset;
            }
            else
            {
                _darkShadow.IsInner = false;
                _lightShadow.IsInner = false;
                _darkShadow.OffsetX = offset;
                _darkShadow.OffsetY = offset;
                _lightShadow.OffsetX = -offset;
                _lightShadow.OffsetY = -offset;
            }

            _darkShadow.Color = DaisyNeumorphicHelper.ApplyIntensity(darkShadowColor, intensity);
            _lightShadow.Color = DaisyNeumorphicHelper.ApplyIntensity(lightShadowColor, intensity);
            _darkShadow.BlurRadius = blur;
            _lightShadow.BlurRadius = blur;
            _darkShadow.Spread = 0;
            _lightShadow.Spread = 0;
            _darkShadow.Opacity = 1;
            _lightShadow.Opacity = 1;

            _shadowCollection.Clear();
            _shadowCollection.Add(_darkShadow);
            if (mode == DaisyNeumorphicMode.Raised || mode == DaisyNeumorphicMode.Inset)
            {
                _shadowCollection.Add(_lightShadow);
            }

            _shadowContainer.Shadows = _shadowCollection;
        }

        protected internal void RequestNeumorphicRefresh()
        {
            if (!IsLoaded)
                return;
            Flowery.Helpers.DaisyNeumorphicRefreshHelper.QueueRefresh(this);
        }

        protected bool TryEnsureShadowContainer()
        {
            if (_shadowContainer != null)
            {
                return true;
            }

            ApplyTemplate();
            _shadowContainer = GetTemplateChild(ShadowContainerPartName) as ShadowContainer;
            return _shadowContainer != null;
        }

        protected void EnsureShadowResources()
        {
            _shadowCollection ??= new ToolkitShadowCollection();
            _darkShadow ??= new ToolkitShadow();
            _lightShadow ??= new ToolkitShadow();
        }

        protected void SyncShadowContainerCornerRadius()
        {
            if (_shadowContainer == null)
            {
                return;
            }

            _shadowContainer.CornerRadius = CornerRadius;
        }


        protected void ClearNeumorphicShadows()
        {
            if (_shadowContainer == null)
            {
                ClearThemeShadow();
                return;
            }

            _shadowContainer.Shadows = new ToolkitShadowCollection();

            if (_buttonBorder is UIElement borderElement)
            {
                global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(borderElement, 0);
            }
            if (_shadowContainer is UIElement shadowElement)
            {
                global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(shadowElement, 0);
            }

            ClearThemeShadow();
        }

        private bool TryApplyThemeShadow(double elevation)
        {
            if (_buttonBorder == null || _themeShadowReceiver == null)
            {
                return false;
            }

            try
            {
                _themeShadow ??= new ThemeShadow();
                _themeShadow.Receivers.Clear();
                _themeShadow.Receivers.Add(_themeShadowReceiver);
                _buttonBorder.Shadow = _themeShadow;
                _buttonBorder.Translation = new Vector3(_buttonBorder.Translation.X, _buttonBorder.Translation.Y, (float)elevation);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void EnsureTemplateParts()
        {
            if (_buttonBorder != null && _shadowContainer != null && _themeShadowReceiver != null)
            {
                return;
            }

            ApplyTemplate();
            _shadowContainer = GetTemplateChild(ShadowContainerPartName) as ShadowContainer;
            _buttonBorder = GetTemplateChild("ButtonBorder") as Border;
            _buttonBorder ??= FindChildByType<Border>(this);
            _themeShadowReceiver = GetTemplateChild("PART_NeumorphicHost") as Canvas;
        }

        private void ClearThemeShadow()
        {
            if (_buttonBorder == null)
            {
                return;
            }

            _buttonBorder.Shadow = null;
            _buttonBorder.Translation = new Vector3(_buttonBorder.Translation.X, _buttonBorder.Translation.Y, 0f);
        }

        protected internal bool IsNeumorphicEffectivelyEnabled()
        {
            var scope = DaisyNeumorphic.GetScopeEnabled(this);
            bool isActuallyEnabled = scope ?? NeumorphicEnabled ?? DaisyBaseContentControl.GlobalNeumorphicEnabled;

            return isActuallyEnabled && GetEffectiveNeumorphicMode() != DaisyNeumorphicMode.None;
        }

        protected internal DaisyNeumorphicMode GetEffectiveNeumorphicMode()
        {
            return NeumorphicMode ?? DaisyBaseContentControl.GlobalNeumorphicMode;
        }

        protected internal double GetEffectiveNeumorphicIntensity()
        {
            return Math.Clamp(NeumorphicIntensity ?? DaisyBaseContentControl.GlobalNeumorphicIntensity, 0.0, 1.0);
        }

        protected internal bool IsNeumorphicRimLightEffectivelyEnabled()
        {
            return NeumorphicRimLightEnabled == true && DaisyBaseContentControl.GlobalNeumorphicRimLightEnabled;
        }

        protected internal bool IsNeumorphicSurfaceGradientEffectivelyEnabled()
        {
            return NeumorphicSurfaceGradientEnabled == true && DaisyBaseContentControl.GlobalNeumorphicSurfaceGradientEnabled;
        }

        #endregion

        private void OnHeightPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (Shape != DaisyButtonShape.Square && Shape != DaisyButtonShape.Circle)
                return;

            // For Circle/Square, always sync Width to Height to maintain 1:1 aspect ratio
            if (!double.IsNaN(Height) && Height > 0)
            {
                Width = Height;

                if (Shape == DaisyButtonShape.Circle)
                {
                    CornerRadius = new CornerRadius(Height / 2);
                }
            }
        }

        private static void OnStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyButton button)
            {
                button.ApplyAll();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleLoaded();
            BuildIconLayoutIfNeeded();
            BuildDashedBorderIfNeeded();

            DaisyBaseContentControl.GlobalNeumorphicChanged += OnGlobalNeumorphicChanged;

            // Defer neumorphic update until the template is laid out.
            // The template isn't immediately available after Loaded; wait for first
            // layout so PART_ShadowContainer is ready.
            LayoutUpdated += OnFirstLayoutForNeumorphic;
        }

        private void OnFirstLayoutForNeumorphic(object? sender, object e)
        {
            LayoutUpdated -= OnFirstLayoutForNeumorphic;
            RequestNeumorphicRefresh();
        }

        private void OnGlobalNeumorphicChanged(object? sender, EventArgs e)
        {
            RequestNeumorphicRefresh();
        }

        private void BuildDashedBorderIfNeeded()
        {
            if (ButtonStyle != DaisyButtonStyle.Dash || _dashedBorder != null)
                return;

            // For Dash style: hide the solid border, we'll overlay a dashed rectangle
            BorderThickness = new Thickness(0);

            // Subscribe to SizeChanged to position the dashed rectangle
            SizeChanged += OnDashSizeChanged;
        }

        private void OnDashSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ButtonStyle != DaisyButtonStyle.Dash)
                return;

            EnsureDashedBorderOverlay();
            UpdateDashedBorderSize();
        }

        private void EnsureDashedBorderOverlay()
        {
            if (_dashedBorder != null)
                return;

            // Find the template root to add our dashed overlay
            if (VisualTreeHelper.GetChildrenCount(this) == 0)
                return;

            var root = VisualTreeHelper.GetChild(this, 0);
            Grid? hostGrid = null;

            // Find or create a Grid to host our dashed rectangle
            if (root is Grid grid)
            {
                hostGrid = grid;
            }
            else if (root is Border border)
            {
                // Wrap border's child in a Grid if needed
                if (border.Child is Grid childGrid)
                {
                    hostGrid = childGrid;
                }
                else if (border.Child != null)
                {
                    var wrapper = new Grid();
                    var existingChild = border.Child;
                    border.Child = null;
                    wrapper.Children.Add(existingChild);
                    border.Child = wrapper;
                    hostGrid = wrapper;
                }
            }

            if (hostGrid == null)
                return;

            // Create dashed rectangle - uses Grid.Row/Column to span all rows/columns
            _dashedBorder = new Rectangle
            {
                Stroke = BorderBrush ?? new SolidColorBrush(Microsoft.UI.Colors.Gray),
                StrokeThickness = 1,
                StrokeDashArray = [4, 2],
                Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                RadiusX = (float)CornerRadius.TopLeft,
                RadiusY = (float)CornerRadius.TopLeft,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Make it span all rows and columns if the grid has them
            if (hostGrid.RowDefinitions.Count > 0)
                Grid.SetRowSpan(_dashedBorder, hostGrid.RowDefinitions.Count);
            if (hostGrid.ColumnDefinitions.Count > 0)
                Grid.SetColumnSpan(_dashedBorder, hostGrid.ColumnDefinitions.Count);

            // Insert at top of Z-order
            hostGrid.Children.Add(_dashedBorder);
        }

        private void UpdateDashedBorderSize()
        {
            if (_dashedBorder == null)
                return;

            // With BorderThickness = 0, the template Grid fills the button.
            // The rectangle just needs to stretch to fill that Grid (no margin needed).
            _dashedBorder.Margin = new Thickness(0);
            _dashedBorder.RadiusX = CornerRadius.TopLeft;
            _dashedBorder.RadiusY = CornerRadius.TopLeft;
        }

        /// <summary>
        /// Builds an icon + content layout when DaisyControlExtensions.Icon, IconSymbol, or IconData is set.
        /// Supports both attached property pattern and the new unified icon API.
        /// </summary>
        private void BuildIconLayoutIfNeeded()
        {
            if (_iconLayoutBuilt)
                return;

            // Priority: Attached property Icon > IconSymbol > IconData
            UIElement? iconElement = null;

            var attachedIcon = DaisyControlExtensions.GetIcon(this);
            if (attachedIcon != null)
            {
                // Clear attached property to detach (UIElements can only have one parent)
                DaisyControlExtensions.SetIcon(this, null);
                iconElement = attachedIcon;
            }
            else if (IconSymbol.HasValue)
            {
                iconElement = CreateSymbolIcon(IconSymbol.Value);
            }
            else if (!string.IsNullOrEmpty(IconData))
            {
                iconElement = CreatePathIcon(IconData);
            }

            if (iconElement == null)
                return;

            _iconLayoutBuilt = true;

            // Capture user content before replacing
            _userContent = Content;
            Content = null; // Clear Content to avoid parentage issues

            // Get layout settings
            var placement = attachedIcon != null
                ? DaisyControlExtensions.GetIconPlacement(this)
                : IconPlacement;
            var spacing = attachedIcon != null
                ? DaisyControlExtensions.GetIconSpacing(this)
                : GetIconSpacingForSize();

            // Create the layout panel
            var isHorizontal = placement == IconPlacement.Left || placement == IconPlacement.Right;
            _iconContentPanel = new StackPanel
            {
                Orientation = isHorizontal ? Orientation.Horizontal : Orientation.Vertical,
                Spacing = spacing,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Create presenter for icon
            _iconPresenter = new ContentPresenter
            {
                Content = iconElement,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Only create text presenter if there's actual content
            if (_userContent != null)
            {
                _textPresenter = new ContentPresenter
                {
                    Content = _userContent,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            // Add in correct order based on placement
            if (placement == IconPlacement.Right || placement == IconPlacement.Bottom)
            {
                if (_textPresenter != null)
                    _iconContentPanel.Children.Add(_textPresenter);
                _iconContentPanel.Children.Add(_iconPresenter);
            }
            else
            {
                _iconContentPanel.Children.Add(_iconPresenter);
                if (_textPresenter != null)
                    _iconContentPanel.Children.Add(_textPresenter);
            }

            // Replace button content with the icon+content panel
            Content = _iconContentPanel;
        }

        /// <summary>
        /// Clears and rebuilds the icon layout. Called when IconSymbol, IconData, or IconPlacement changes at runtime.
        /// </summary>
        private void RebuildIconLayout()
        {
            // Clear existing icon layout
            if (_iconContentPanel != null)
            {
                _iconContentPanel.Children.Clear();

                // Restore original user content if it was captured
                if (_userContent != null)
                {
                    Content = _userContent;
                }
                else
                {
                    Content = null;
                }
            }

            _iconLayoutBuilt = false;
            _iconContentPanel = null;
            _iconPresenter = null;
            _textPresenter = null;

            // Rebuild
            BuildIconLayoutIfNeeded();
        }

        /// <summary>
        /// Creates a SymbolIcon wrapped in Viewbox for proper scaling.
        /// Uses native SymbolIcon on Windows, falls back to PathIcon on WASM/Skia.
        /// </summary>
        private Viewbox CreateSymbolIcon(Symbol symbol)
        {
            var iconSize = GetIconSizeForButtonSize();

            var viewbox = new Viewbox
            {
                Width = iconSize,
                Height = iconSize,
                Stretch = Stretch.Uniform
            };

            UIElement iconContent;
            if (OperatingSystem.IsWindows())
            {
                var icon = new SymbolIcon(symbol);
                // SymbolIcon automatically inherits Foreground from parent
                iconContent = icon;
            }
            else
            {
                // Convert Symbol enum to path data for cross-platform rendering
                var pathData = Helpers.FloweryPathHelpers.GetSymbolPathData(symbol);
                if (!string.IsNullOrEmpty(pathData))
                {
                    var path = new Microsoft.UI.Xaml.Shapes.Path
                    {
                        Data = Helpers.FloweryPathHelpers.ParseGeometry(pathData),
                        Width = 24,
                        Height = 24,
                        Stretch = Stretch.Fill
                    };
                    path.SetBinding(Microsoft.UI.Xaml.Shapes.Path.FillProperty, new Microsoft.UI.Xaml.Data.Binding
                    {
                        Source = this,
                        Path = new PropertyPath("Foreground")
                    });
                    iconContent = path;
                }
                else
                {
                    // Fallback: still try SymbolIcon (might render empty)
                    var icon = new SymbolIcon(symbol);
                    iconContent = icon;
                }
            }

            viewbox.Child = iconContent;
            return viewbox;
        }

        /// <summary>
        /// Creates a Path element wrapped in Viewbox for proper scaling from 24x24 viewBox path data.
        /// </summary>
        private Viewbox CreatePathIcon(string pathData)
        {
            var iconSize = GetIconSizeForButtonSize();

            // Use Viewbox to scale the 24x24 coordinate system path to our desired size
            var viewbox = new Viewbox
            {
                Width = iconSize,
                Height = iconSize,
                Stretch = Stretch.Uniform
            };

            // Use Path instead of PathIcon to avoid Uno runtime issues
            // Set explicit 24x24 size to match standard icon coordinate system
            // Viewbox will uniformly scale this to the target iconSize
            var path = new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = Helpers.FloweryPathHelpers.ParseGeometry(pathData),
                Width = 24,
                Height = 24,
                Stretch = Stretch.Fill
            };

            // Bind path fill to button foreground for color inheritance
            path.SetBinding(Microsoft.UI.Xaml.Shapes.Path.FillProperty, new Microsoft.UI.Xaml.Data.Binding
            {
                Source = this,
                Path = new PropertyPath("Foreground")
            });

            viewbox.Child = path;
            return viewbox;
        }

        /// <summary>
        /// Gets the appropriate icon size based on button Size.
        /// </summary>
        private double GetIconSizeForButtonSize() => DaisyResourceLookup.GetDefaultIconSize(Size);

        /// <summary>
        /// Gets the appropriate icon-to-text spacing based on button Size.
        /// </summary>
        private double GetIconSpacingForSize() => DaisyResourceLookup.GetDefaultIconSpacing(Size);

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lifecycle.HandleUnloaded();
            DaisyBaseContentControl.GlobalNeumorphicChanged -= OnGlobalNeumorphicChanged;
            LayoutUpdated -= OnFirstLayoutForNeumorphic;
            SizeChanged -= OnDashSizeChanged;
            ClearNeumorphicShadows();
            _shadowContainer = null;
            _shadowCollection = null;
            _darkShadow = null;
            _lightShadow = null;
            _dashedBorder = null;
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            _isPointerOver = true;
            ApplyCurrentPointerState();
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            _isPointerOver = false;
            _isPressed = false;
            ApplyCurrentPointerState();
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);
            _isPressed = true;
            ApplyCurrentPointerState();
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            _isPressed = false;
            ApplyCurrentPointerState();
        }

        private void ApplyAll()
        {
            var resources = Application.Current?.Resources;
            if (resources != null)
            {
                DaisyTokenDefaults.EnsureDefaults(resources);
            }

            ApplySizing(resources);
            ComputeThemeBrushes(resources);
            ApplyCurrentPointerState();
            RequestNeumorphicRefresh();
        }

        private void ApplySizing(ResourceDictionary? resources)
        {
            var size = Size;

            // Tokens are guaranteed by EnsureDefaults(); fallbacks only for null resources edge case
            Height = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", size, "Height",
                DaisyResourceLookup.GetDefaultHeight(size));

            FontSize = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", size, "FontSize",
                DaisyResourceLookup.GetDefaultFontSize(size));

            if (ReadLocalValue(PaddingProperty) == DependencyProperty.UnsetValue)
            {
                var padding = DaisyResourceLookup.GetSizeThickness(resources, "DaisyButton", size, "Padding",
                    DaisyResourceLookup.GetDefaultPadding(size));
                ApplyEffectivePadding(padding);
            }

            // Respect local CornerRadius overrides (e.g. DaisyButtonGroup joined corners).
            if (ReadLocalValue(CornerRadiusProperty) == DependencyProperty.UnsetValue)
            {
                CornerRadius = DaisyResourceLookup.GetSizeCornerRadius(resources, "DaisySize", size, "CornerRadius",
                    DaisyResourceLookup.GetDefaultCornerRadius(size));
            }

            ApplyShape();
            ApplyIconSizing();
            ApplyContentLineHeight(resources);
        }

        private void ApplyContentLineHeight(ResourceDictionary? resources)
        {
            var label = FindChildByType<TextBlock>(this);
            if (label == null)
                return;

            if (label.ReadLocalValue(TextBlock.LineHeightProperty) != DependencyProperty.UnsetValue)
                return;

            var lineHeight = DaisyResourceLookup.GetSizeDouble(resources, "DaisySize", Size, "LineHeight",
                DaisyResourceLookup.GetDefaultLineHeight(Size));
            label.LineHeight = lineHeight;
        }

        /// <summary>
        /// Updates icon sizing when the button Size changes.
        /// </summary>
        private void ApplyIconSizing()
        {
            if (_iconPresenter?.Content == null)
                return;

            var iconSize = GetIconSizeForButtonSize();
            var spacing = GetIconSpacingForSize();

            // Update spacing in icon panel
            if (_iconContentPanel != null)
            {
                _iconContentPanel.Spacing = spacing;
            }

            // Both IconData (Path) and IconSymbol (SymbolIcon) are wrapped in Viewbox
            if (_iconPresenter.Content is Viewbox viewbox)
            {
                viewbox.Width = iconSize;
                viewbox.Height = iconSize;
            }
        }

        private void ApplyShape()
        {
            var hasLocalHorizontalAlignment =
                ReadLocalValue(HorizontalAlignmentProperty) != DependencyProperty.UnsetValue;

            // Reset shape-related properties to reasonable defaults.
            MinWidth = 0;
            if (!hasLocalHorizontalAlignment)
                HorizontalAlignment = HorizontalAlignment.Left;

            if (Shape == DaisyButtonShape.Wide)
            {
                MinWidth = 200;
                if (!hasLocalHorizontalAlignment)
                    HorizontalAlignment = HorizontalAlignment.Center;
            }
            else if (Shape == DaisyButtonShape.Block)
            {
                if (!hasLocalHorizontalAlignment)
                    HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            else if (Shape == DaisyButtonShape.Square || Shape == DaisyButtonShape.Circle)
            {
                // For Square/Circle, always sync Width to Height to maintain 1:1 aspect ratio.
                if (!double.IsNaN(Height) && Height > 0)
                {
                    Width = Height;

                    if (Shape == DaisyButtonShape.Circle)
                    {
                        CornerRadius = new CornerRadius(Height / 2);
                    }
                }

                ApplyEffectivePadding(new Thickness(0));
                if (!hasLocalHorizontalAlignment)
                    HorizontalAlignment = HorizontalAlignment.Center;
            }
        }

        private void ComputeThemeBrushes(ResourceDictionary? resources)
        {
            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            var base100 = DaisyResourceLookup.GetBrush(resources, "DaisyBase100Brush", new SolidColorBrush(Microsoft.UI.Colors.White));
            var base200 = DaisyResourceLookup.GetBrush(resources, "DaisyBase200Brush", new SolidColorBrush(Color.FromArgb(255, 211, 211, 211)));
            var base300 = DaisyResourceLookup.GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)));
            var baseContent = DaisyResourceLookup.GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Microsoft.UI.Colors.Black));

            var neutral = DaisyResourceLookup.GetBrush(resources, "DaisyNeutralBrush", new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)));
            var neutralFocus = DaisyResourceLookup.GetBrush(resources, "DaisyNeutralFocusBrush", neutral);
            var neutralContent = DaisyResourceLookup.GetBrush(resources, "DaisyNeutralContentBrush", new SolidColorBrush(Microsoft.UI.Colors.White));

            var primary = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryBrush", new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)));
            var primaryFocus = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryFocusBrush", primary);
            var primaryContent = DaisyResourceLookup.GetBrush(resources, "DaisyPrimaryContentBrush", new SolidColorBrush(Microsoft.UI.Colors.White));

            var secondary = DaisyResourceLookup.GetBrush(resources, "DaisySecondaryBrush", new SolidColorBrush(Color.FromArgb(255, 255, 105, 180)));
            var secondaryFocus = DaisyResourceLookup.GetBrush(resources, "DaisySecondaryFocusBrush", secondary);
            var secondaryContent = DaisyResourceLookup.GetBrush(resources, "DaisySecondaryContentBrush", new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)));

            var accent = DaisyResourceLookup.GetBrush(resources, "DaisyAccentBrush", new SolidColorBrush(Color.FromArgb(255, 0, 128, 128)));
            var accentFocus = DaisyResourceLookup.GetBrush(resources, "DaisyAccentFocusBrush", accent);
            var accentContent = DaisyResourceLookup.GetBrush(resources, "DaisyAccentContentBrush", new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)));

            var info = DaisyResourceLookup.GetBrush(resources, "DaisyInfoBrush", new SolidColorBrush(Color.FromArgb(255, 0, 191, 255)));
            var infoContent = DaisyResourceLookup.GetBrush(resources, "DaisyInfoContentBrush", baseContent);

            var success = DaisyResourceLookup.GetBrush(resources, "DaisySuccessBrush", new SolidColorBrush(Color.FromArgb(255, 50, 205, 50)));
            var successContent = DaisyResourceLookup.GetBrush(resources, "DaisySuccessContentBrush", baseContent);

            var warning = DaisyResourceLookup.GetBrush(resources, "DaisyWarningBrush", new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)));
            var warningContent = DaisyResourceLookup.GetBrush(resources, "DaisyWarningContentBrush", baseContent);

            var error = DaisyResourceLookup.GetBrush(resources, "DaisyErrorBrush", new SolidColorBrush(Color.FromArgb(255, 205, 92, 92)));
            var errorContent = DaisyResourceLookup.GetBrush(resources, "DaisyErrorContentBrush", baseContent);

            // Get variant name for resource lookup
            var variantName = Variant switch
            {
                DaisyButtonVariant.Primary => "Primary",
                DaisyButtonVariant.Secondary => "Secondary",
                DaisyButtonVariant.Accent => "Accent",
                DaisyButtonVariant.Info => "Info",
                DaisyButtonVariant.Success => "Success",
                DaisyButtonVariant.Warning => "Warning",
                DaisyButtonVariant.Error => "Error",
                DaisyButtonVariant.Ghost => "Ghost",
                DaisyButtonVariant.Link => "Link",
                DaisyButtonVariant.Neutral => "Neutral",
                _ => ""
            };

            Brush variantBg;
            Brush variantHoverBg;
            Brush variantFg;
            Brush variantHoverFg;

            switch (Variant)
            {
                case DaisyButtonVariant.Primary:
                    variantBg = primary;
                    variantHoverBg = primaryFocus;
                    variantFg = primaryContent;
                    variantHoverFg = primaryContent;
                    break;
                case DaisyButtonVariant.Secondary:
                    variantBg = secondary;
                    variantHoverBg = secondaryFocus;
                    variantFg = secondaryContent;
                    variantHoverFg = secondaryContent;
                    break;
                case DaisyButtonVariant.Accent:
                    variantBg = accent;
                    variantHoverBg = accentFocus;
                    variantFg = accentContent;
                    variantHoverFg = accentContent;
                    break;
                case DaisyButtonVariant.Ghost:
                    variantBg = transparent;
                    variantHoverBg = base200;
                    variantFg = baseContent;
                    variantHoverFg = baseContent;
                    break;
                case DaisyButtonVariant.Link:
                    variantBg = transparent;
                    variantHoverBg = transparent;
                    variantFg = primary;
                    variantHoverFg = primary;
                    break;
                case DaisyButtonVariant.Info:
                    variantBg = info;
                    variantHoverBg = DaisyResourceLookup.WithOpacity(info, 0.8) ?? info;
                    variantFg = infoContent;
                    variantHoverFg = infoContent;
                    break;
                case DaisyButtonVariant.Success:
                    variantBg = success;
                    variantHoverBg = DaisyResourceLookup.WithOpacity(success, 0.8) ?? success;
                    variantFg = successContent;
                    variantHoverFg = successContent;
                    break;
                case DaisyButtonVariant.Warning:
                    variantBg = warning;
                    variantHoverBg = DaisyResourceLookup.WithOpacity(warning, 0.8) ?? warning;
                    variantFg = warningContent;
                    variantHoverFg = warningContent;
                    break;
                case DaisyButtonVariant.Error:
                    variantBg = error;
                    variantHoverBg = DaisyResourceLookup.WithOpacity(error, 0.8) ?? error;
                    variantFg = errorContent;
                    variantHoverFg = errorContent;
                    break;
                case DaisyButtonVariant.Neutral:
                    if (ButtonStyle == DaisyButtonStyle.Outline || ButtonStyle == DaisyButtonStyle.Dash)
                    {
                        variantBg = neutralContent;
                        variantHoverBg = neutralContent;
                        variantFg = neutral;
                        variantHoverFg = neutral;
                    }
                    else
                    {
                        variantBg = neutral;
                        variantHoverBg = neutralFocus;
                        variantFg = neutralContent;
                        variantHoverFg = neutralContent;
                    }
                    break;
                case DaisyButtonVariant.Default:
                default:
                    if (ButtonStyle == DaisyButtonStyle.Outline || ButtonStyle == DaisyButtonStyle.Dash)
                    {
                        // Use baseContent for the outline itself so it's visible on dark backgrounds
                        // In dark theme, base200 is too dark to be an outline.
                        variantBg = baseContent;
                        variantHoverBg = baseContent;
                        variantFg = base100;
                        variantHoverFg = base100;
                    }
                    else
                    {
                        variantBg = base200;
                        variantHoverBg = base300;
                        variantFg = baseContent;
                        variantHoverFg = baseContent;
                    }
                    break;
            }

            // Base per-style mapping (Default / Outline / Soft / Dash)
            var thinBorder = resources != null
                ? DaisyResourceLookup.GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1))
                : new Thickness(1);
            var noBorder = resources != null
                ? DaisyResourceLookup.GetThickness(resources, "DaisyBorderThicknessNone", new Thickness(0))
                : new Thickness(0);

            if (Variant == DaisyButtonVariant.Link)
            {
                // Link is special: no background/border, no padding by default.
                _normalBackground = transparent;
                _pointerOverBackground = transparent;
                _pressedBackground = transparent;

                _normalForeground = variantFg;
                _pointerOverForeground = variantHoverFg;
                _pressedForeground = variantHoverFg;

                _normalBorderBrush = transparent;
                _pointerOverBorderBrush = transparent;
                _pressedBorderBrush = transparent;

                _normalBorderThickness = noBorder;
                _pointerOverBorderThickness = noBorder;
                _pressedBorderThickness = noBorder;

                Padding = new Thickness(0);

                // Apply lightweight styling overrides for Link variant
                ApplyBrushOverrides(variantName);
                return;
            }

            var effectiveStyle = ButtonStyle == DaisyButtonStyle.Dash ? DaisyButtonStyle.Outline : ButtonStyle;

            if (effectiveStyle == DaisyButtonStyle.Outline)
            {
                _normalBackground = transparent;
                _pointerOverBackground = variantBg;
                _pressedBackground = DaisyResourceLookup.WithOpacity(variantBg, 0.9) ?? variantBg;

                _normalForeground = variantBg;
                _pointerOverForeground = variantFg;
                _pressedForeground = variantFg;

                _normalBorderBrush = variantBg;
                _pointerOverBorderBrush = variantBg;
                _pressedBorderBrush = variantBg;

                _normalBorderThickness = thinBorder;
                _pointerOverBorderThickness = thinBorder;
                _pressedBorderThickness = thinBorder;
            }
            else if (effectiveStyle == DaisyButtonStyle.Soft)
            {
                _normalBackground = DaisyResourceLookup.WithOpacity(variantBg, 0.18) ?? variantBg;
                _pointerOverBackground = DaisyResourceLookup.WithOpacity(variantBg, 0.28) ?? variantHoverBg;
                _pressedBackground = DaisyResourceLookup.WithOpacity(variantBg, 0.35) ?? variantHoverBg;

                _normalForeground = variantBg;
                _pointerOverForeground = variantBg;
                _pressedForeground = variantBg;

                _normalBorderBrush = transparent;
                _pointerOverBorderBrush = transparent;
                _pressedBorderBrush = transparent;

                _normalBorderThickness = noBorder;
                _pointerOverBorderThickness = noBorder;
                _pressedBorderThickness = noBorder;
            }
            else
            {
                _normalBackground = variantBg;
                _pointerOverBackground = variantHoverBg;
                _pressedBackground = DaisyResourceLookup.WithOpacity(variantHoverBg, 0.9) ?? variantHoverBg;

                _normalForeground = variantFg;
                _pointerOverForeground = variantHoverFg;
                _pressedForeground = variantHoverFg;

                _normalBorderBrush = transparent;
                _pointerOverBorderBrush = transparent;
                _pressedBorderBrush = transparent;

                _normalBorderThickness = thinBorder;
                _pointerOverBorderThickness = thinBorder;
                _pressedBorderThickness = thinBorder;
            }

            // Apply lightweight styling overrides
            ApplyBrushOverrides(variantName);
            SyncTemplateResources();
        }

        private void SyncTemplateResources()
        {
            if (!ShouldSyncTemplateResources())
            {
                return;
            }

            // Push our computed brushes into the local Resources dictionary.
            // This ensures that the system Button template's VisualStateManager (which usually targets
            // these specific resource keys) picks up our theme-specific colors instead of system defaults.
            // Wrapped in try-catch to handle occasional COMExceptions from WinUI/Uno interop timing issues.
            try
            {
                SetResourceIfNotNull("ButtonBackground", _normalBackground);
                SetResourceIfNotNull("ButtonBackgroundPointerOver", _pointerOverBackground);
                SetResourceIfNotNull("ButtonBackgroundPressed", _pressedBackground);
                // THIS causes exception:
                // SetResourceIfNotNull("ButtonForegroundPointerOver", _pointerOverForeground);
                SetResourceIfNotNull("ButtonForegroundPressed", _pressedForeground);
                SetResourceIfNotNull("ButtonBorderBrushPointerOver", _pointerOverBorderBrush);
                SetResourceIfNotNull("ButtonBorderBrushPressed", _pressedBorderBrush);

                // Disabled state is also often handled via resources
                SetResourceIfNotNull("ButtonBackgroundDisabled", DaisyResourceLookup.WithOpacity(_normalBackground, 0.5) ?? _normalBackground);
                SetResourceIfNotNull("ButtonForegroundDisabled", DaisyResourceLookup.WithOpacity(_normalForeground, 0.5) ?? _normalForeground);
                SetResourceIfNotNull("ButtonBorderBrushDisabled", DaisyResourceLookup.WithOpacity(_normalBorderBrush, 0.5) ?? _normalBorderBrush);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Occasionally Uno/WinUI throws COMException when setting resources during initialization.
                // This is non-fatal - the button will still work, just with default system colors for states.
            }
        }

        protected virtual bool ShouldSyncTemplateResources()
        {
            return true;
        }

        private void SetResourceIfNotNull(string key, object? value)
        {
            if (value != null)
                Resources[key] = value;
        }

        /// <summary>
        /// Applies user-defined lightweight styling overrides to the computed brushes.
        /// Checks for variant-specific resources first (e.g., DaisyButtonPrimaryBackground),
        /// then falls back to generic resources (e.g., DaisyButtonBackground).
        /// </summary>
        private void ApplyBrushOverrides(string variantName)
        {
            // Background overrides
            var bgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", $"{variantName}Background")
                : null;
            bgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "Background");
            if (bgOverride != null)
                _normalBackground = bgOverride;

            var bgHoverOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", $"{variantName}BackgroundPointerOver")
                : null;
            bgHoverOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "BackgroundPointerOver");
            if (bgHoverOverride != null)
                _pointerOverBackground = bgHoverOverride;

            var bgPressedOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", $"{variantName}BackgroundPressed")
                : null;
            bgPressedOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "BackgroundPressed");
            if (bgPressedOverride != null)
                _pressedBackground = bgPressedOverride;

            // Foreground overrides
            var fgOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", $"{variantName}Foreground")
                : null;
            fgOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "Foreground");
            if (fgOverride != null)
                _normalForeground = fgOverride;

            var fgHoverOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", $"{variantName}ForegroundPointerOver")
                : null;
            fgHoverOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "ForegroundPointerOver");
            if (fgHoverOverride != null)
                _pointerOverForeground = fgHoverOverride;

            // Border overrides
            var borderOverride = !string.IsNullOrEmpty(variantName)
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", $"{variantName}BorderBrush")
                : null;
            borderOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisyButton", "BorderBrush");
            if (borderOverride != null)
                _normalBorderBrush = borderOverride;
        }

        private void ApplyCurrentPointerState()
        {
            if (IsExternallyControlled)
                return;

            // 1. Determine standard visuals for the current state
            if (!IsEnabled)
            {
                Background = DaisyResourceLookup.WithOpacity(_normalBackground, 0.5) ?? _normalBackground;
                Foreground = DaisyResourceLookup.WithOpacity(_normalForeground, 0.5) ?? _normalForeground;
                BorderBrush = DaisyResourceLookup.WithOpacity(_normalBorderBrush, 0.5) ?? _normalBorderBrush;
                BorderThickness = _normalBorderThickness;
            }
            else if (_isPressed)
            {
                Background = _pressedBackground;
                Foreground = _pressedForeground;
                BorderBrush = _pressedBorderBrush;
                BorderThickness = _pressedBorderThickness;
            }
            else if (_isPointerOver)
            {
                Background = _pointerOverBackground;
                Foreground = _pointerOverForeground;
                BorderBrush = _pointerOverBorderBrush;
                BorderThickness = _pointerOverBorderThickness;
            }
            else
            {
                Background = _normalBackground;
                Foreground = _normalForeground;
                BorderBrush = _normalBorderBrush;
                BorderThickness = _normalBorderThickness;
            }

            // Note: Previously we set Background to Transparent when neumorphism was enabled,
            // to reveal injected neumorphic layers at Index 0. This is no longer needed because
            // the current implementation uses SetElevation directly on the button, which adds
            // shadows without requiring a transparent background. The button keeps its normal
            // background for proper rendering.

            // 3. Update related visuals
            PropagateIconForeground();
            UpdateDashedBorder();
        }

        /// <summary>
        /// Propagates the button's Foreground to any icon set via DaisyControlExtensions.Icon.
        /// This ensures icon color matches text color on different button variants and states.
        /// </summary>
        private void PropagateIconForeground()
        {
            // Propagate to icon set via attached property
            var icon = DaisyControlExtensions.GetIcon(this);
            if (icon != null)
            {
                icon.Foreground = Foreground;
            }

            // Also propagate if Content itself is an IconElement (legacy pattern)
            if (Content is IconElement contentIcon)
            {
                contentIcon.Foreground = Foreground;
            }

            // Propagate to DaisyIconText when used as button content
            if (Content is DaisyIconText daisyIconText)
            {
                daisyIconText.Foreground = Foreground;
            }
        }

        private void UpdateDashedBorder()
        {
            if (_dashedBorder == null)
                return;

            // For Dash style, update the dashed rectangle color to match the variant
            if (ButtonStyle == DaisyButtonStyle.Dash)
            {
                // Hide actual border, use dashed rectangle instead
                BorderThickness = new Thickness(0);

                // Update dashed border color based on state
                Brush strokeBrush;
                if (_isPressed)
                    strokeBrush = _pressedBorderBrush ?? _normalBorderBrush ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
                else if (_isPointerOver)
                    strokeBrush = _pointerOverBorderBrush ?? _normalBorderBrush ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
                else
                    strokeBrush = _normalBorderBrush ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);

                _dashedBorder.Stroke = strokeBrush;

                // Update corner radius to match button
                _dashedBorder.RadiusX = CornerRadius.TopLeft;
                _dashedBorder.RadiusY = CornerRadius.TopLeft;
            }
        }

        private void ApplyEffectivePadding(Thickness padding)
        {
            Padding = padding;
            // Dashed border doesn't need margin adjustment - it stretches to fill the template Grid
        }

    }
}
