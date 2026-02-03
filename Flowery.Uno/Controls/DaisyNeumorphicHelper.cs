using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using Flowery.Services;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Toolkit;
using Uno.Toolkit.UI;
using Windows.Foundation;
using Windows.UI;
using ToolkitShadow = Uno.Toolkit.UI.Shadow;
using ToolkitShadowCollection = Uno.Toolkit.UI.ShadowCollection;

namespace Flowery.Controls
{
    /// <summary>
    /// Helper class to manage neumorphic visual effects for any FrameworkElement.
    /// </summary>
    /// <remarks>
    /// <para><b>What is Neumorphism?</b></para>
    /// <para>
    /// Neumorphism (new skeuomorphism) is a design style that combines the realism of skeuomorphism
    /// with the simplicity of flat design. It creates soft, extruded UI elements that appear to push
    /// out from or press into the background surface, using carefully positioned light and dark shadows.
    /// </para>
    ///
    /// <para><b>The Three Essential Properties:</b></para>
    /// <list type="number">
    ///   <item><b>Background Match:</b> Element background must match its container (seamless surface)</item>
    ///   <item><b>Dark Shadow:</b> Applied to one corner (bottom-right for raised, top-left for inset)</item>
    ///   <item><b>Light Shadow:</b> Applied to the opposite corner (top-left for raised, bottom-right for inset)</item>
    /// </list>
    ///
    /// <para><b>Neumorphic Modes:</b></para>
    /// <list type="bullet">
    ///   <item><b>Raised (Outer):</b> Element appears to pop OUT of the surface. Light shadow top-left, dark shadow bottom-right.</item>
    ///   <item><b>Inset (Inner):</b> Element appears pressed INTO the surface. Dark shadow top-left, light shadow bottom-right.</item>
    ///   <item><b>Flat:</b> Subtle single shadow for a softer, less pronounced effect.</item>
    /// </list>
    ///
    /// <para><b>Cross-Platform Implementation:</b></para>
    /// <para>
    /// For Raised/Flat modes: Injects a <see cref="ShadowContainer"/> with two explicit shadows behind the owner's content.
    /// This avoids native elevation inconsistencies and makes offset control explicit.
    /// </para>
    /// <para>
    /// For Inset mode: Uses gradient overlay borders injected into the visual tree.
    /// These semi-transparent gradients create the inner shadow effect.
    /// </para>
    /// </remarks>
    public sealed partial class DaisyNeumorphicHelper : IDisposable
    {
        private const string LogCategory = "Controls";

        public static bool ShouldUseDirectElevation()
        {
            return PlatformCompatibility.IsSkiaBackend
                || PlatformCompatibility.IsWasmBackend
                || !PlatformCompatibility.IsWindows;
        }

        private readonly FrameworkElement _owner;

        // Container grid that holds the inset overlay layers (only used for Inset mode)
        private Grid? _containerGrid;

        // Inner shadow overlays - for Inset mode (gradient Background on borders)
        private Border? _insetTopLeftOverlay;
        private Border? _insetBottomRightOverlay;

        // Outer shadow layers - for Raised/Flat mode
        private ShadowContainer? _raisedShadowContainer;
        private Border? _raisedShadowShape;
        private ToolkitShadow? _raisedDarkShadow;
        private ToolkitShadow? _raisedLightShadow;

        private DaisyNeumorphicMode _currentMode = DaisyNeumorphicMode.None;
        private bool _lastEnabled;
        private bool _hasEverUpdated;
        private bool _monitorAttachment;
        private bool _pendingLayoutReapply;
        private long _visibilityCallbackToken = -1;
        private bool _isDisposed;
        private bool _isApplyingLayoutRefresh;
        private bool _overlayInjectionFailed;
        private double _lastElevation;
        private double _lastIntensity;
        private DispatcherQueueTimer? _layoutUpdateTimer;
        private bool _layoutUpdateScheduled;
        private bool _layoutTimerInitialized;
        private bool _hasAppliedState;
        private DaisyNeumorphicMode _lastAppliedMode = DaisyNeumorphicMode.None;
        private double _lastAppliedIntensity;
        private double _lastAppliedElevation;
        private double _lastAppliedWidth;
        private double _lastAppliedHeight;

        public DaisyNeumorphicHelper(FrameworkElement owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner.SizeChanged += OnSizeChanged;
            _owner.Loaded += OnOwnerLoaded;
            _owner.Unloaded += OnOwnerUnloaded;
            _visibilityCallbackToken = _owner.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, OnOwnerVisibilityChanged);
        }

        public void Update(
            bool enabled,
            DaisyNeumorphicMode mode,
            double intensity,
            double elevation)
        {
            if (_isDisposed) return;

            _hasEverUpdated = true;
            _lastEnabled = enabled;

            if (!enabled || mode == DaisyNeumorphicMode.None)
            {
                DisposeLayers(resetState: true);
                ResetAppliedState();
                return;
            }

            _currentMode = mode;
            _lastIntensity = intensity;
            _lastElevation = elevation;

            EnsureContainerGrid();
            if (_overlayInjectionFailed)
            {
                DisposeLayers(resetState: false);
                if (mode == DaisyNeumorphicMode.Raised || mode == DaisyNeumorphicMode.Flat)
                {
                    UpdateAppearanceDirectElevation(mode, intensity, elevation);
                    MarkAppliedState();
                }
                return;
            }

            EnsureAttachmentMonitoring();
            UpdateContainerZIndex(mode);

            if (mode == DaisyNeumorphicMode.Raised || mode == DaisyNeumorphicMode.Flat)
            {
                EnsureRaisedLayers();
                SetInsetVisibility(false);
                UpdateAppearanceRaised(mode, intensity, elevation);
                MarkAppliedState();
                return;
            }

            EnsureInsetLayers();
            SetRaisedVisibility(false);
            UpdateAppearanceInset(intensity);
            MarkAppliedState();
        }

        // Partial method hook for platform-specific shadow implementations (e.g., Windows)
        partial void OnApplyDirectElevation(DaisyNeumorphicMode mode, double intensity, double elevation, ref bool handled);

        // Partial method hook for platform-specific cleanup
        partial void OnDisposeWindowsShadows();

        // Partial method hook to tweak inset intensity per platform (e.g., Android)
        partial void OnAdjustInsetIntensity(ref double intensity);

        /// <summary>
        /// Applies elevation directly to the owner for Raised/Flat modes.
        /// </summary>
        /// <remarks>
        /// Uses Uno's <c>UIElementExtensions.SetElevation</c> which handles platform-specific shadows:
        /// - Windows: Uses Composition API DropShadow (via partial class implementation)
        /// - Skia (Desktop): Uses ShadowState
        /// - WASM: Uses CSS box-shadow
        /// - Android: Uses ViewCompat.SetElevation
        /// - iOS: Uses CALayer.shadow*
        /// </remarks>
        private void UpdateAppearanceDirectElevation(DaisyNeumorphicMode mode, double intensity, double elevation)
        {
            // Allow platform-specific implementation to handle shadows
            bool handled = false;
            OnApplyDirectElevation(mode, intensity, elevation, ref handled);
            if (handled) return;

            // Default path for non-Windows platforms
            if (_owner is UIElement ownerElement)
            {
                ApplyDirectElevation(ownerElement, mode, intensity, elevation);
            }
        }

        private void UpdateAppearanceRaised(DaisyNeumorphicMode mode, double intensity, double elevation)
        {
            if (_raisedShadowContainer == null || _raisedShadowShape == null ||
                _raisedDarkShadow == null)
            {
                UpdateAppearanceDirectElevation(mode, intensity, elevation);
                return;
            }

            var surfaceBrush = GetSurfaceBrush() ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            if (surfaceBrush == null)
            {
                SetRaisedVisibility(false);
                UpdateAppearanceDirectElevation(mode, intensity, elevation);
                // var dialog = new Windows.UI.Popups.MessageDialog("Fallback");
                // _ = dialog.ShowAsync();
                return;
            }

            var radius = GetCornerRadius(_owner);
            _raisedShadowShape.CornerRadius = radius;
            _raisedShadowContainer.CornerRadius = radius;
            _raisedShadowContainer.Background = surfaceBrush;

            var (offset, blur) = GetShadowMetrics(_owner, elevation, mode);

            var darkShadowColor = DaisyNeumorphic.GetDarkShadowColor(_owner)
                ?? DaisyBaseContentControl.GlobalNeumorphicDarkShadowColor;
            var lightShadowColor = DaisyNeumorphic.GetLightShadowColor(_owner)
                ?? DaisyBaseContentControl.GlobalNeumorphicLightShadowColor;

            _raisedDarkShadow.Color = ApplyIntensity(darkShadowColor, intensity);
            _raisedDarkShadow.OffsetX = offset;
            _raisedDarkShadow.OffsetY = offset;
            _raisedDarkShadow.BlurRadius = blur;
            _raisedDarkShadow.Spread = 0;
            _raisedDarkShadow.Opacity = 1;
            _raisedDarkShadow.IsInner = false;

            if (_raisedLightShadow != null)
            {
                _raisedLightShadow.Color = ApplyIntensity(lightShadowColor, intensity);
                _raisedLightShadow.OffsetX = -offset;
                _raisedLightShadow.OffsetY = -offset;
                _raisedLightShadow.BlurRadius = blur;
                _raisedLightShadow.Spread = 0;
                _raisedLightShadow.Opacity = 1;
                _raisedLightShadow.IsInner = false;
            }

            var surfaceMargin = GetShadowSurfaceMargin(offset, blur);
            _raisedShadowContainer.Margin = new Thickness(
                -surfaceMargin.Left,
                -surfaceMargin.Top,
                -surfaceMargin.Right,
                -surfaceMargin.Bottom);
            _raisedShadowShape.Margin = surfaceMargin;

            var shadows = _raisedShadowContainer.Shadows ?? new ToolkitShadowCollection();
            shadows.Clear();
            shadows.Add(_raisedDarkShadow);
            if (mode == DaisyNeumorphicMode.Raised && _raisedLightShadow != null)
            {
                shadows.Add(_raisedLightShadow);
            }
            _raisedShadowContainer.Shadows = shadows;

            SetRaisedVisibility(true);
        }

        /// <summary>
        /// Updates the inset mode appearance with gradient overlays.
        /// </summary>
        private void UpdateAppearanceInset(double intensity)
        {
            if (_insetTopLeftOverlay == null || _insetBottomRightOverlay == null) return;

            var effectiveIntensity = intensity;
            OnAdjustInsetIntensity(ref effectiveIntensity);

            // Clear any direct elevation on owner
            if (_owner is UIElement ownerElement)
            {
                global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(ownerElement, 0);
            }

            // Update corner radius
            var radius = GetCornerRadius(_owner);
            _insetTopLeftOverlay.CornerRadius = radius;
            _insetBottomRightOverlay.CornerRadius = radius;

            SetInsetVisibility(true);

            // Create inner shadow gradients
            byte insetDarkAlpha = (byte)(200 * effectiveIntensity);
            byte insetLightAlpha = (byte)(160 * effectiveIntensity);

            // Dark inner shadow: gradient from top-left corner inward
            var darkGradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };
            darkGradient.GradientStops.Add(new GradientStop
            {
                Color = Color.FromArgb(insetDarkAlpha, 0, 0, 0),
                Offset = 0
            });
            darkGradient.GradientStops.Add(new GradientStop
            {
                Color = Colors.Transparent,
                Offset = 0.2
            });
            darkGradient.GradientStops.Add(new GradientStop
            {
                Color = Colors.Transparent,
                Offset = 1
            });
            _insetTopLeftOverlay.Background = darkGradient;

            // Light inner shadow: gradient from bottom-right corner inward
            var lightGradient = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(1, 1),
                EndPoint = new Windows.Foundation.Point(0, 0)
            };
            lightGradient.GradientStops.Add(new GradientStop
            {
                Color = Color.FromArgb(insetLightAlpha, 255, 255, 255),
                Offset = 0
            });
            lightGradient.GradientStops.Add(new GradientStop
            {
                Color = Colors.Transparent,
                Offset = 0.2
            });
            lightGradient.GradientStops.Add(new GradientStop
            {
                Color = Colors.Transparent,
                Offset = 1
            });
            _insetBottomRightOverlay.Background = lightGradient;
        }

        /// <summary>
        /// Builds the XAML-based neumorphic layers for Inset mode.
        /// </summary>
        /// <remarks>
        /// <para><b>Layer Stack for Inset Mode:</b></para>
        /// <para>
        /// Only gradient overlays are created for inner shadows.
        /// Dark gradient on top-left, light gradient on bottom-right.
        /// These are semi-transparent and overlay on top of the owner's content.
        /// </para>
        /// </remarks>
        private void EnsureInsetLayers()
        {
            try
            {
                EnsureContainerGrid();

                if (_insetTopLeftOverlay != null || _insetBottomRightOverlay != null)
                {
                    return;
                }

                var radius = GetCornerRadius(_owner);

                // ---------------------------------------------------------------------
                // INSET SHADOW OVERLAYS (gradient Background for inner shadows - works cross-platform)
                // ---------------------------------------------------------------------
                // Note: LinearGradientBrush on BorderBrush doesn't work with CornerRadius on WASM.
                // Instead, we use gradient Background on overlay borders that cover the edges.

                // Dark inner shadow: top-left overlay (gradient from top-left corner inward)
                _insetTopLeftOverlay = new Border
                {
                    CornerRadius = radius,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    IsHitTestVisible = false
                };

                // Light inner shadow: bottom-right overlay (gradient from bottom-right corner inward)
                _insetBottomRightOverlay = new Border
                {
                    CornerRadius = radius,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    IsHitTestVisible = false
                };

                // Add inset overlays
                _containerGrid?.Children.Add(_insetTopLeftOverlay);
                _containerGrid?.Children.Add(_insetBottomRightOverlay);

            }
            catch (Exception ex)
            {
                FloweryLogging.Log(FloweryLogLevel.Error, LogCategory, $"[DaisyNeumorphicHelper] Failed to build layers: {ex.Message}");
            }
        }

        /// <summary>
        /// Injects the neumorphic container into the owner's visual tree at z-index 0.
        /// </summary>
        private enum InjectionResult
        {
            Injected,
            NotReady,
            Unsupported
        }

        private InjectionResult TryInjectIntoVisualTree()
        {
            if (_containerGrid == null)
                return InjectionResult.Unsupported;

            if (TryInsertIntoPanel(_owner as Panel))
                return InjectionResult.Injected;

            if (_owner is Border && _owner is FrameworkElement ownerElement)
            {
                if (TryInsertIntoParentGrid(ownerElement, out var notReady))
                {
                    return InjectionResult.Injected;
                }
                if (notReady)
                {
                    return InjectionResult.NotReady;
                }
            }

            if (VisualTreeHelper.GetChildrenCount(_owner) == 0)
                return InjectionResult.NotReady;

            var root = VisualTreeHelper.GetChild(_owner, 0);
            if (TryInsertIntoPanel(root as Panel))
                return InjectionResult.Injected;

            if (root is ContentPresenter presenter)
            {
                if (VisualTreeHelper.GetChildrenCount(presenter) == 0)
                    return InjectionResult.NotReady;

                var presenterChild = VisualTreeHelper.GetChild(presenter, 0);
                return TryInsertIntoPanel(presenterChild as Panel)
                    ? InjectionResult.Injected
                    : InjectionResult.Unsupported;
            }

            if (root is Border border)
            {
                return TryInsertIntoPanel(border.Child as Panel)
                    ? InjectionResult.Injected
                    : InjectionResult.Unsupported;
            }

            return InjectionResult.Unsupported;
        }

        private void EnsureContainerGrid()
        {
            if (_overlayInjectionFailed)
                return;

            if (_containerGrid != null)
            {
                if (!IsContainerAttachedToOwner())
                {
                    if (_containerGrid.Parent is Panel parentPanel)
                    {
                        parentPanel.Children.Remove(_containerGrid);
                    }

                    _containerGrid = null;
                }
                else if (_containerGrid.Parent == null)
                {
                    var injectResult = TryInjectIntoVisualTree();
                    if (injectResult == InjectionResult.Unsupported)
                    {
                        _overlayInjectionFailed = true;
                        StopLayoutMonitoring();
                    }
                    else if (injectResult == InjectionResult.NotReady || !IsContainerAttachedToOwner())
                    {
                        QueueLayoutReapply();
                    }
                    return;
                }

                if (_containerGrid != null)
                {
                    return;
                }
            }

            _containerGrid = new Grid
            {
                Tag = "DaisyNeumorphicContainer",
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var injectionResult = TryInjectIntoVisualTree();
            if (injectionResult == InjectionResult.Unsupported)
            {
                _overlayInjectionFailed = true;
                StopLayoutMonitoring();
            }
            else if (injectionResult == InjectionResult.NotReady || !IsContainerAttachedToOwner())
            {
                QueueLayoutReapply();
            }
        }

        private bool TryInsertIntoPanel(Panel? panel)
        {
            if (panel == null)
                return false;

            foreach (var child in panel.Children)
            {
                if (child is Grid g && g.Tag?.ToString() == "DaisyNeumorphicContainer")
                {
                    return true;
                }
            }

            panel.Children.Insert(0, _containerGrid!);
            EnsureGridSpan(panel, _containerGrid!);
            return true;
        }

        private bool TryInsertIntoParentGrid(FrameworkElement ownerElement, out bool notReady)
        {
            notReady = false;
            var parent = VisualTreeHelper.GetParent(ownerElement);
            if (parent == null)
            {
                parent = ownerElement.Parent;
            }

            if (parent == null)
            {
                notReady = true;
                return false;
            }

            if (parent is not Grid grid)
            {
                return false;
            }

            foreach (var child in grid.Children)
            {
                if (child is Grid g && g.Tag?.ToString() == "DaisyNeumorphicContainer")
                {
                    return true;
                }
            }

            var insertIndex = grid.Children.IndexOf(ownerElement);
            if (insertIndex < 0)
            {
                insertIndex = 0;
            }

            grid.Children.Insert(insertIndex, _containerGrid!);
            CopyGridPlacement(ownerElement, _containerGrid!);
            CopySizingAndAlignment(ownerElement, _containerGrid!);
            return true;
        }

        private void StopLayoutMonitoring()
        {
            _layoutUpdateTimer?.Stop();
            _layoutUpdateScheduled = false;
            _pendingLayoutReapply = false;
            _monitorAttachment = false;
        }

        private bool IsContainerAttachedToOwner()
        {
            if (_containerGrid == null)
                return false;

            if (_owner is Panel ownerPanel && ownerPanel.Children.Contains(_containerGrid))
            {
                return true;
            }

            var ownerParent = VisualTreeHelper.GetParent(_owner);
            if (ownerParent == null && _owner is FrameworkElement ownerElement)
            {
                ownerParent = ownerElement.Parent;
            }

            if (ownerParent is Panel ownerParentPanel && ownerParentPanel.Children.Contains(_containerGrid))
            {
                return true;
            }

            if (ownerParent != null && ReferenceEquals(_containerGrid.Parent, ownerParent))
            {
                return true;
            }

            DependencyObject? current = _containerGrid;
            while (current != null)
            {
                if (ReferenceEquals(current, _owner))
                    return true;

                var parent = VisualTreeHelper.GetParent(current);
                if (parent == null && current is FrameworkElement fe)
                {
                    parent = fe.Parent;
                }
                current = parent;
            }

            return false;
        }

        private void EnsureRaisedLayers()
        {
            if (_raisedShadowContainer != null || _raisedShadowShape != null)
            {
                return;
            }

            EnsureContainerGrid();

            var radius = GetCornerRadius(_owner);
            _raisedShadowShape = new Border
            {
                CornerRadius = radius,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false
            };

            _raisedDarkShadow = new ToolkitShadow();
            _raisedLightShadow = new ToolkitShadow();

            _raisedShadowContainer = new ShadowContainer
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false,
                Background = new SolidColorBrush(Colors.Transparent),
                Content = _raisedShadowShape,
                Shadows = new ToolkitShadowCollection { _raisedDarkShadow, _raisedLightShadow }
            };

            _containerGrid?.Children.Insert(0, _raisedShadowContainer);
        }

        private static void EnsureGridSpan(Panel panel, FrameworkElement host)
        {
            if (panel is not Grid grid)
                return;

            Grid.SetRow(host, 0);
            Grid.SetColumn(host, 0);
            Grid.SetRowSpan(host, Math.Max(1, grid.RowDefinitions.Count));
            Grid.SetColumnSpan(host, Math.Max(1, grid.ColumnDefinitions.Count));
        }

        private static void CopyGridPlacement(FrameworkElement source, FrameworkElement target)
        {
            Grid.SetRow(target, Grid.GetRow(source));
            Grid.SetColumn(target, Grid.GetColumn(source));
            Grid.SetRowSpan(target, Grid.GetRowSpan(source));
            Grid.SetColumnSpan(target, Grid.GetColumnSpan(source));
        }

        private static void CopySizingAndAlignment(FrameworkElement source, FrameworkElement target)
        {
            target.HorizontalAlignment = source.HorizontalAlignment;
            target.VerticalAlignment = source.VerticalAlignment;
            target.Margin = source.Margin;

            if (!double.IsNaN(source.Width))
            {
                target.Width = source.Width;
            }

            if (!double.IsNaN(source.Height))
            {
                target.Height = source.Height;
            }

            target.MinWidth = source.MinWidth;
            target.MinHeight = source.MinHeight;
            target.MaxWidth = source.MaxWidth;
            target.MaxHeight = source.MaxHeight;
        }

        private static CornerRadius GetCornerRadius(FrameworkElement element)
        {
            if (element is Control control) return control.CornerRadius;
            if (element is Border border) return border.CornerRadius;
            if (element is Grid grid) return grid.CornerRadius;
            if (element is StackPanel stack) return stack.CornerRadius;
            if (element is RelativePanel relative) return relative.CornerRadius;
            return default;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_currentMode == DaisyNeumorphicMode.Raised || _currentMode == DaisyNeumorphicMode.Flat)
            {
                QueueLayoutReapply();
            }
        }

        private void OnOwnerLoaded(object sender, RoutedEventArgs e)
        {
            if (_isDisposed || !_hasEverUpdated || !_lastEnabled || _currentMode == DaisyNeumorphicMode.None)
                return;

            QueueLayoutReapply();
        }

        private void OnOwnerUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isDisposed)
                return;

            StopLayoutMonitoring();

            if (_containerGrid != null)
            {
                DisposeLayers(resetState: false);
            }
        }

        private void OnOwnerVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_isDisposed || !_lastEnabled || _currentMode == DaisyNeumorphicMode.None)
                return;

            if (_owner.Visibility == Visibility.Visible)
            {
                QueueLayoutReapply();
            }
        }

        private void QueueLayoutReapply()
        {
            if (_overlayInjectionFailed)
                return;

            if (_pendingLayoutReapply || !_owner.IsLoaded)
                return;

            _pendingLayoutReapply = true;
            ScheduleLayoutUpdate();
        }

        private void EnsureAttachmentMonitoring()
        {
            if (_overlayInjectionFailed)
                return;

            if (_monitorAttachment || _pendingLayoutReapply || !_owner.IsLoaded)
                return;

            if (IsContainerAttachedToOwner())
                return;

            _monitorAttachment = true;
            ScheduleLayoutUpdate();
        }

        private void ScheduleLayoutUpdate()
        {
            if (_layoutUpdateScheduled)
                return;

            var dispatcher = _owner.DispatcherQueue;
            if (dispatcher == null)
            {
                ApplyLayoutUpdate();
                return;
            }

            _layoutUpdateTimer ??= dispatcher.CreateTimer();
            _layoutUpdateTimer.Interval = TimeSpan.FromMilliseconds(50);
            if (!_layoutTimerInitialized)
            {
                _layoutUpdateTimer.Tick += OnLayoutUpdateTimerTick;
                _layoutTimerInitialized = true;
            }
            _layoutUpdateScheduled = true;
            _layoutUpdateTimer.Start();
        }

        private void OnLayoutUpdateTimerTick(DispatcherQueueTimer sender, object args)
        {
            sender.Stop();
            _layoutUpdateScheduled = false;
            ApplyLayoutUpdate();
        }

        private void ApplyLayoutUpdate()
        {
            if (_isApplyingLayoutRefresh || _isDisposed || !_owner.IsLoaded)
                return;

            if (_overlayInjectionFailed)
            {
                StopLayoutMonitoring();
                return;
            }

            if (_pendingLayoutReapply)
            {
                if (IsAppliedStateCurrent())
                {
                    _pendingLayoutReapply = false;
                    if (!_monitorAttachment)
                        return;
                }

                _pendingLayoutReapply = false;
                if (_lastEnabled && _currentMode != DaisyNeumorphicMode.None)
                {
                    _isApplyingLayoutRefresh = true;
                    Update(_lastEnabled, _currentMode, _lastIntensity, _lastElevation);
                    _isApplyingLayoutRefresh = false;
                }
            }

            if (_monitorAttachment && _lastEnabled && _currentMode != DaisyNeumorphicMode.None)
            {
                if (IsContainerAttachedToOwner())
                {
                    _monitorAttachment = false;
                }
            }

            if (_pendingLayoutReapply || _monitorAttachment)
            {
                ScheduleLayoutUpdate();
            }
        }

        private bool IsAppliedStateCurrent()
        {
            if (!_hasAppliedState)
                return false;

            if (_lastAppliedMode != _currentMode)
                return false;

            if (Math.Abs(_lastAppliedIntensity - _lastIntensity) > 0.0001)
                return false;

            if (Math.Abs(_lastAppliedElevation - _lastElevation) > 0.0001)
                return false;

            var widthDelta = Math.Abs(_lastAppliedWidth - _owner.ActualWidth);
            var heightDelta = Math.Abs(_lastAppliedHeight - _owner.ActualHeight);
            return widthDelta <= 0.1 && heightDelta <= 0.1;
        }

        private void MarkAppliedState()
        {
            _hasAppliedState = true;
            _lastAppliedMode = _currentMode;
            _lastAppliedIntensity = _lastIntensity;
            _lastAppliedElevation = _lastElevation;
            _lastAppliedWidth = _owner.ActualWidth;
            _lastAppliedHeight = _owner.ActualHeight;
        }

        private void ResetAppliedState()
        {
            _hasAppliedState = false;
            _lastAppliedMode = DaisyNeumorphicMode.None;
            _lastAppliedIntensity = 0;
            _lastAppliedElevation = 0;
            _lastAppliedWidth = 0;
            _lastAppliedHeight = 0;
        }

        private void SetInsetVisibility(bool isVisible)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            if (_insetTopLeftOverlay != null) _insetTopLeftOverlay.Visibility = visibility;
            if (_insetBottomRightOverlay != null) _insetBottomRightOverlay.Visibility = visibility;
        }

        private void SetRaisedVisibility(bool isVisible)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            if (_raisedShadowContainer != null) _raisedShadowContainer.Visibility = visibility;
        }

        private void UpdateContainerZIndex(DaisyNeumorphicMode mode)
        {
            if (_containerGrid == null)
                return;

            var zIndex = mode == DaisyNeumorphicMode.Inset ? 1 : -1;
            Canvas.SetZIndex(_containerGrid, zIndex);
        }

        internal static Color ApplyIntensity(Color color, double intensity)
        {
            byte alpha = (byte)Math.Clamp(color.A * intensity, 0, 255);
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        private Brush? GetSurfaceBrush()
        {
            Brush? brush = _owner switch
            {
                Control control => control.Background,
                Panel panel => panel.Background,
                Border border => border.Background,
                _ => null
            };

            if (brush is SolidColorBrush scb && scb.Color.A == 0)
            {
                return null;
            }

            return brush;
        }

        internal static (double Offset, double Blur) GetShadowMetrics(FrameworkElement owner, double elevation, DaisyNeumorphicMode mode)
        {
            var baseOffset = DaisyNeumorphic.GetOffset(owner);
            var baseBlur = DaisyNeumorphic.GetBlurRadius(owner);

            var hasOffsetOverride = owner.ReadLocalValue(DaisyNeumorphic.OffsetProperty) != DependencyProperty.UnsetValue;
            var hasBlurOverride = owner.ReadLocalValue(DaisyNeumorphic.BlurRadiusProperty) != DependencyProperty.UnsetValue;

            var derivedOffset = hasOffsetOverride ? baseOffset : Math.Max(baseOffset, elevation * 0.35);
            var derivedBlur = hasBlurOverride ? baseBlur : Math.Max(baseBlur, derivedOffset * 1.5);

            if (mode == DaisyNeumorphicMode.Flat)
            {
                derivedOffset *= 0.6;
                derivedBlur *= 0.6;
            }

            return (derivedOffset, derivedBlur);
        }

        internal static double GetDirectElevation(DaisyNeumorphicMode mode, double intensity, double elevation)
        {
            return mode switch
            {
                DaisyNeumorphicMode.Raised => elevation * intensity,
                DaisyNeumorphicMode.Flat => (elevation / 2) * intensity,
                _ => 0
            };
        }

        internal static void ApplyDirectElevation(UIElement ownerElement, DaisyNeumorphicMode mode, double intensity, double elevation)
        {
            var finalElevation = GetDirectElevation(mode, intensity, elevation);
            global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(ownerElement, finalElevation);
        }

        private Thickness GetShadowSurfaceMargin(double offset, double blur)
        {
            var width = _owner.ActualWidth;
            var height = _owner.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                return new Thickness(0);
            }

            var maxExtent = Math.Min(width, height) * 0.75;
            var shadowExtent = Math.Abs(offset) + blur;
            var extent = Math.Min(shadowExtent, maxExtent);
            return new Thickness(extent);
        }

        public void Suspend()
        {
            if (_isDisposed)
                return;

            StopLayoutMonitoring();

            if (_containerGrid != null)
            {
                DisposeLayers(resetState: false);
            }
        }

        private void DisposeLayers(bool resetState)
        {
            try
            {
                // Clean up platform-specific shadows (Windows)
                OnDisposeWindowsShadows();

                // Clear elevation from owner
                if (_owner is UIElement ownerElement)
                {
                    global::Uno.UI.Toolkit.UIElementExtensions.SetElevation(ownerElement, 0);
                }

                if (_containerGrid == null) return;

                // Remove from parent
                if (_containerGrid.Parent is Panel parentPanel)
                {
                    parentPanel.Children.Remove(_containerGrid);
                }

                _containerGrid.Children.Clear();
                _containerGrid = null;
                _insetTopLeftOverlay = null;
                _insetBottomRightOverlay = null;
                _raisedShadowContainer = null;
                _raisedShadowShape = null;
                _raisedDarkShadow = null;
                _raisedLightShadow = null;
                if (resetState)
                {
                    _currentMode = DaisyNeumorphicMode.None;
                    _lastEnabled = false;
                    _monitorAttachment = false;
                }
            }
            catch (Exception ex)
            {
                FloweryLogging.Log(FloweryLogLevel.Error, LogCategory, $"[DaisyNeumorphicHelper] Failed to dispose layers: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _owner.SizeChanged -= OnSizeChanged;
            _owner.Loaded -= OnOwnerLoaded;
            _owner.Unloaded -= OnOwnerUnloaded;
            if (_visibilityCallbackToken >= 0)
            {
                _owner.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, _visibilityCallbackToken);
                _visibilityCallbackToken = -1;
            }
            StopLayoutMonitoring();
            DisposeLayers(resetState: true);
            _isDisposed = true;
        }
    }
}
