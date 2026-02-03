using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Flowery.Helpers
{
    /// <summary>
    /// Static helper class for creating reusable Storyboard-based animations.
    /// Provides common animation patterns like zoom, pan, pulse, and fade effects
    /// that can be used across multiple controls.
    /// </summary>
    public static class FloweryAnimationHelpers
    {
        private static readonly Random _random = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Storyboard, PanAndZoomState> _PanAndZoomStates = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UIElement, PanAndZoomSizingSnapshot> _PanAndZoomSizingSnapshots = new();
        private static bool PanAndZoomDebugEnabled = false;
        private const string PanAndZoomDebugGridTag = "PanAndZoomDebugGrid";

        #region High-Level Slide Effects

        /// <summary>
        /// Applies a slide effect to an element. This is the high-level method that controls can call
        /// to get cinematic effects like Pan And Zoom, pan, zoom, etc. Returns the actual effect that was applied.
        /// </summary>
        /// <param name="storyboard">The Storyboard to add animations to.</param>
        /// <param name="transform">The CompositeTransform to animate.</param>
        /// <param name="effect">The slide effect to apply.</param>
        /// <param name="duration">Animation duration.</param>
        /// <param name="params">Effect parameters (intensity, distance, etc.). Uses defaults if null.</param>
        /// <param name="effectTarget">Element to animate; if null, the element itself is used.</param>
        public static FlowerySlideEffect ApplySlideEffect(
            Storyboard storyboard,
            CompositeTransform transform,
            FlowerySlideEffect effect,
            TimeSpan duration,
            FlowerySlideEffectParams? @params = null,
            UIElement? effectTarget = null)
        {
            @params ??= new FlowerySlideEffectParams();

            // Pan And Zoom: combine zoom + pan for true documentary feel
            if (effect == FlowerySlideEffect.PanAndZoom)
            {
                ApplyPanAndZoomEffect(storyboard, transform, duration, @params, effectTarget);
                return FlowerySlideEffect.PanAndZoom;
            }

            // Breath: keyframe-based breathing zoom
            if (effect == FlowerySlideEffect.Breath)
            {
                ApplyBreathEffect(storyboard, transform, duration, @params, effectTarget);
                return FlowerySlideEffect.Breath;
            }

            // Throw: dramatic parallax fly-through
            if (effect == FlowerySlideEffect.Throw)
            {
                ApplyThrowEffect(storyboard, transform, duration, @params, effectTarget);
                return FlowerySlideEffect.Throw;
            }

            // Drift: random pan direction
            if (effect == FlowerySlideEffect.Drift)
            {
                FlowerySlideEffect[] panEffects = new[] {
                    FlowerySlideEffect.PanLeft,
                    FlowerySlideEffect.PanRight,
                    FlowerySlideEffect.PanUp,
                    FlowerySlideEffect.PanDown
                };
                effect = panEffects[_random.Next(panEffects.Length)];
            }

            // Apply individual effects
            switch (effect)
            {
                case FlowerySlideEffect.ZoomIn:
                    AddZoomInAnimation(storyboard, transform, @params.ZoomIntensity, duration);
                    break;

                case FlowerySlideEffect.ZoomOut:
                    AddZoomOutAnimation(storyboard, transform, @params.ZoomIntensity, duration);
                    break;

                case FlowerySlideEffect.PanLeft:
                    AddHorizontalPanAnimation(storyboard, transform, -@params.PanDistance, duration);
                    AddZoomAnimation(storyboard, transform, 1.0, 1.0 + (@params.ZoomIntensity * @params.SubtleZoomRatio), duration);
                    break;

                case FlowerySlideEffect.PanRight:
                    AddHorizontalPanAnimation(storyboard, transform, @params.PanDistance, duration);
                    AddZoomAnimation(storyboard, transform, 1.0, 1.0 + (@params.ZoomIntensity * @params.SubtleZoomRatio), duration);
                    break;

                case FlowerySlideEffect.PanUp:
                    AddVerticalPanAnimation(storyboard, transform, -@params.PanDistance * @params.VerticalPanRatio, duration);
                    AddZoomAnimation(storyboard, transform, 1.0, 1.0 + (@params.ZoomIntensity * @params.SubtleZoomRatio), duration);
                    break;

                case FlowerySlideEffect.PanDown:
                    AddVerticalPanAnimation(storyboard, transform, @params.PanDistance * @params.VerticalPanRatio, duration);
                    AddZoomAnimation(storyboard, transform, 1.0, 1.0 + (@params.ZoomIntensity * @params.SubtleZoomRatio), duration);
                    break;

                case FlowerySlideEffect.Pulse:
                    AddPulseAnimation(storyboard, transform, @params.PulseIntensity, duration);
                    break;
                case FlowerySlideEffect.PanAndZoom:
                    break;
                case FlowerySlideEffect.Drift:
                    break;
                case FlowerySlideEffect.Breath:
                    break;
                case FlowerySlideEffect.Throw:
                    break;
                case FlowerySlideEffect.None:
                default:
                    // No effect
                    break;
            }

            return effect;
        }

        #region Pan And Zoom Effect (Simplified)

        /// <summary>
        /// Tracks state for the Pan And Zoom looping effect, allowing direction alternation.
        /// </summary>
        private sealed class PanAndZoomState
        {
            public bool LastZoomIn { get; set; } = true;
            public bool LastPanHorizontal { get; set; } = true;
            public bool IsActive { get; set; }

            // Track last pan position as NORMALIZED 0..1 coordinates (robust across zoom/size changes)
            // 0 = leftmost/topmost valid position, 1 = rightmost/bottommost valid position
            // 0.5 = centered
            public double LastPanX01 { get; set; } = 0.5;
            public double LastPanY01 { get; set; } = 0.5;
            public Border? DebugImageBorder { get; set; }
            public Border? DebugViewportBorder { get; set; }
            public FrameworkElement? SizingTarget { get; set; }
            public bool HasSizingSnapshot { get; set; }
            public double OriginalWidth { get; set; }
            public double OriginalHeight { get; set; }
            public Thickness OriginalMargin { get; set; }
            public HorizontalAlignment OriginalHorizontalAlignment { get; set; }
            public VerticalAlignment OriginalVerticalAlignment { get; set; }
            public bool OriginalIsHitTestVisible { get; set; }

            public void Deactivate(Storyboard storyboard)
            {
                IsActive = false;
                storyboard.Completed -= OnCompleted;
            }

            // Set by ApplyPanAndZoomEffect
            public Action<bool>? OnCycleComplete { get; set; }

            public void OnCompleted(object? sender, object e)
            {
                if (!IsActive)
                {
                    return;
                }

                OnCycleComplete?.Invoke(true);
            }
        }

        private sealed class PanAndZoomSizingSnapshot
        {
            public double Width { get; init; }
            public double Height { get; init; }
            public Thickness Margin { get; init; }
            public HorizontalAlignment HorizontalAlignment { get; init; }
            public VerticalAlignment VerticalAlignment { get; init; }
            public bool IsHitTestVisible { get; init; }
        }

        /// <summary>
        /// Applies a simplified Pan And Zoom effect based on the VB.NET reference implementation.
        /// Uses axis-aligned motion (horizontal or vertical), smart portrait handling, and alternating zoom.
        /// </summary>
        private static void ApplyPanAndZoomEffect(
            Storyboard storyboard,
            CompositeTransform transform,
            TimeSpan duration,
            FlowerySlideEffectParams @params,
            UIElement? effectTarget)
        {
            // Get or create state for looping
            if (!_PanAndZoomStates.TryGetValue(storyboard, out PanAndZoomState? state))
            {
                state = new PanAndZoomState();
                _ = _PanAndZoomStates.Remove(storyboard);
                _PanAndZoomStates.Add(storyboard, state);
            }

            state.IsActive = true;

            // Set up cycle callback
            state.OnCycleComplete = _ =>
            {
                if (!state.IsActive)
                {
                    return;
                }

                // Alternate zoom direction only when not locked
                if (!@params.PanAndZoomLockZoom)
                {
                    state.LastZoomIn = !state.LastZoomIn;
                }

                // Randomize pan axis for next cycle
                state.LastPanHorizontal = _random.Next(2) == 0;

                // Configure and restart
                ConfigurePanAndZoomCycle(storyboard, transform, duration, @params, effectTarget, state);
            };

            // Defensive: remove before adding to prevent accumulation
            storyboard.Completed -= state.OnCompleted;
            storyboard.Completed += state.OnCompleted;

            // Initial cycle
            ConfigurePanAndZoomCycle(storyboard, transform, duration, @params, effectTarget, state);
        }

        private static void ConfigurePanAndZoomCycle(
            Storyboard storyboard,
            CompositeTransform transform,
            TimeSpan duration,
            FlowerySlideEffectParams @params,
            UIElement? effectTarget,
            PanAndZoomState state)
        {
            storyboard.Stop();
            storyboard.Children.Clear();

            double zoomIntensity = Math.Max(0.0, @params.PanAndZoomZoom);
            bool verticalLock = @params.VerticalLock;
            double verticalLockRatio = @params.VerticalLockRatio;
            double optimizeRatio = @params.OptimizeRatio;

            // Determine image vs viewport dimensions for smart handling
            FrameworkElement? container = ResolveEffectContainer(effectTarget);
            FrameworkElement? effectElement = effectTarget as FrameworkElement;
            double viewportWidth = container?.ActualWidth ?? 0;
            double viewportHeight = container?.ActualHeight ?? 0;

            if (viewportWidth <= 0 || viewportHeight <= 0)
            {
                if (effectElement is { ActualWidth: > 0, ActualHeight: > 0 })
                {
                    viewportWidth = effectElement.ActualWidth;
                    viewportHeight = effectElement.ActualHeight;
                }
                else
                {
                    return;
                }
            }

            Image? targetImage = effectElement as Image ?? (effectElement is not null ? FindFirstImage(effectElement) : null);
            bool hasBitmap = TryGetBitmapPixelSize(targetImage, out int pixelWidth, out int pixelHeight);

            // Recalculate dimensions based on deterministic image content size when possible
            double imageWidth = effectElement is { ActualWidth: > 0 } ? effectElement.ActualWidth : viewportWidth;
            double imageHeight = effectElement is { ActualHeight: > 0 } ? effectElement.ActualHeight : viewportHeight;
            if (targetImage is not null &&
                ReferenceEquals(effectElement, targetImage) &&
                TryGetImageContentSize(targetImage, viewportWidth, viewportHeight, out double contentWidth, out double contentHeight))
            {
                imageWidth = contentWidth;
                imageHeight = contentHeight;
                ApplyImageContentSizing(targetImage, viewportWidth, viewportHeight, contentWidth, contentHeight, state);
            }

            if (imageWidth > 0 && imageHeight > 0)
            {
                transform.CenterX = imageWidth / 2.0;
                transform.CenterY = imageHeight / 2.0;
            }

            // Determine which axis to pan on and zoom direction
            double viewportAspectRatio = viewportWidth / viewportHeight;
            double imageAspectRatio = hasBitmap
                ? (double)pixelWidth / pixelHeight
                : imageWidth / imageHeight;
            bool isWiderThanTall = imageAspectRatio > viewportAspectRatio;
            // Use state's last zoom direction (lockZoom prevents alternation in the callback, not here)
            bool zoomIn = state.LastZoomIn;
            bool panHorizontal = state.LastPanHorizontal;

            // Base overflow at scale=1 (deterministic)
            double baseOverflowX = Math.Max(0, imageWidth - viewportWidth);
            double baseOverflowY = Math.Max(0, imageHeight - viewportHeight);
            bool baseHasHorizontalOverflow = baseOverflowX > 1;
            bool baseHasVerticalOverflow = baseOverflowY > 1;

            bool useHorizontalPan;
            if (baseHasHorizontalOverflow && baseHasVerticalOverflow)
            {
                useHorizontalPan = panHorizontal;
            }
            else if (baseHasHorizontalOverflow)
            {
                useHorizontalPan = true;
            }
            else if (baseHasVerticalOverflow)
            {
                useHorizontalPan = false;
            }
            else
            {
                useHorizontalPan = isWiderThanTall;
            }

            const double minPanRunwayRatio = 0.1;
            const double panSpeedMarginRatio = 0.1;
            const double edgeSafetyPixels = 2.0;
            double maxPanSpeedPixelsPerSecond = Math.Clamp(@params.PanAndZoomPanSpeed, 0.0, 10.0);
            double maxTravel = maxPanSpeedPixelsPerSecond * Math.Max(0.0, duration.TotalSeconds);
            double minPanRunway = (useHorizontalPan ? viewportWidth : viewportHeight) * minPanRunwayRatio;
            double speedRunway = maxTravel > 0
                ? (maxTravel * (1.0 + panSpeedMarginRatio)) + edgeSafetyPixels
                : 0.0;
            double requiredOverflow = Math.Max(minPanRunway, speedRunway);
            double requiredScale = GetRequiredScale(
                useHorizontalPan ? imageWidth : imageHeight,
                useHorizontalPan ? viewportWidth : viewportHeight,
                requiredOverflow);
            const double minFillMarginRatio = 0.02;
            const double panEdgeMarginRatio = 0.04;
            double minScaleRequired = Math.Max(1.0, requiredScale * (1.0 + minFillMarginRatio));
            double zoomRatio = Math.Min(0.5, zoomIntensity);
            double neutralScale = zoomRatio > 0
                ? minScaleRequired / (1.0 - zoomRatio)
                : minScaleRequired;
            double maxScale = neutralScale * (1.0 + zoomRatio);

            // Calculate scale values (NEVER go below minScaleRequired to maintain fill + margin)
            double startScale, endScale;
            if (zoomIn)
            {
                startScale = minScaleRequired;
                endScale = maxScale;
            }
            else
            {
                // Zoom OUT means we start zoomed in and end at minScaleRequired (never below)
                startScale = maxScale;
                endScale = minScaleRequired;
            }

            // The pan bounds must be computed for the MINIMUM scale during the animation
            // to ensure we never expose background at any point in the animation.
            // Min scale determines the tightest constraint on pan range.
            double minScale = Math.Min(startScale, endScale);
            double maxScaleResolved = Math.Max(startScale, endScale);

            // Compute overflow at MINIMUM scale (the constraining factor)
            double scaledWidthMin = imageWidth * minScale;
            double scaledHeightMin = imageHeight * minScale;
            double overflowXMin = Math.Max(0, scaledWidthMin - viewportWidth);
            double overflowYMin = Math.Max(0, scaledHeightMin - viewportHeight);

            // Compute overflow at MAXIMUM scale (for smoother movement when zoomed in)
            double scaledWidthMax = imageWidth * maxScaleResolved;
            double scaledHeightMax = imageHeight * maxScaleResolved;
            double overflowXMax = Math.Max(0, scaledWidthMax - viewportWidth);
            double overflowYMax = Math.Max(0, scaledHeightMax - viewportHeight);

            // Apply optimize ratio for very wide/tall images
            if (overflowXMin > viewportWidth * 0.7)
            {
                overflowXMin *= optimizeRatio;
                overflowXMax *= optimizeRatio;
            }
            if (overflowYMin > viewportHeight * 0.7)
            {
                overflowYMin *= optimizeRatio;
                overflowYMax *= optimizeRatio;
            }

            // Calculate overflows to determine if both axes are viable for panning
            bool hasHorizontalOverflow = overflowXMin > 10;
            bool hasVerticalOverflow = overflowYMin > 10;

            if (useHorizontalPan && !hasHorizontalOverflow && hasVerticalOverflow)
            {
                useHorizontalPan = false;
            }
            else if (!useHorizontalPan && !hasVerticalOverflow && hasHorizontalOverflow)
            {
                useHorizontalPan = true;
            }

            // For very tall portrait images, force vertical panning downward only
            bool isVeryTallPortrait = imageHeight > viewportHeight * verticalLockRatio;
            bool forceDownOnly = verticalLock && !useHorizontalPan && isVeryTallPortrait;

            // Determine new normalized end positions (0..1 range)
            double endPanX01, endPanY01;

            double panEdgeMin = panEdgeMarginRatio;
            double panEdgeMax = 1.0 - panEdgeMarginRatio;

            if (useHorizontalPan && overflowXMin > 1)
            {
                // Pan horizontally: pick 0 (left) or 1 (right)
                endPanX01 = _random.Next(2) == 0 ? panEdgeMin : panEdgeMax;
                endPanY01 = 0.5; // Center vertically
            }
            else if (!useHorizontalPan && overflowYMin > 1)
            {
                // Pan vertically
                endPanX01 = 0.5; // Center horizontally
                if (forceDownOnly)
                {
                    // Force pan toward bottom (1.0) to keep top/faces visible
                    endPanY01 = panEdgeMax;
                }
                else
                {
                    endPanY01 = _random.Next(2) == 0 ? panEdgeMin : panEdgeMax;
                }
            }
            else
            {
                // No meaningful overflow - stay centered
                endPanX01 = 0.5;
                endPanY01 = 0.5;
            }

            // Get start positions from state (last cycle's end position)
            double startPanX01 = state.LastPanX01;
            double startPanY01 = state.LastPanY01;

            if (useHorizontalPan)
            {
                startPanY01 = 0.5;
            }
            else
            {
                startPanX01 = 0.5;
            }

            if (overflowXMin > 1)
            {
                startPanX01 = Math.Clamp(startPanX01, panEdgeMin, panEdgeMax);
                endPanX01 = Math.Clamp(endPanX01, panEdgeMin, panEdgeMax);
            }
            else
            {
                startPanX01 = 0.5;
                endPanX01 = 0.5;
            }

            if (overflowYMin > 1)
            {
                startPanY01 = Math.Clamp(startPanY01, panEdgeMin, panEdgeMax);
                endPanY01 = Math.Clamp(endPanY01, panEdgeMin, panEdgeMax);
            }
            else
            {
                startPanY01 = 0.5;
                endPanY01 = 0.5;
            }

            // Convert normalized positions to pixel translations
            // TranslateX/Y range: negative values pan right/down (image moves left/up)
            // At normalized 0: translate = 0 (image anchored at one edge)
            // At normalized 1: translate = -overflow (image anchored at opposite edge)
            // At normalized 0.5: translate = -overflow/2 (centered)

            // Use the appropriate overflow for start vs end based on which scale they're at
            double startOverflowX, startOverflowY, endOverflowX, endOverflowY;
            if (zoomIn)
            {
                // Start at min scale, end at max scale
                startOverflowX = overflowXMin;
                startOverflowY = overflowYMin;
                endOverflowX = overflowXMax;
                endOverflowY = overflowYMax;
            }
            else
            {
                // Start at max scale, end at min scale
                startOverflowX = overflowXMax;
                startOverflowY = overflowYMax;
                endOverflowX = overflowXMin;
                endOverflowY = overflowYMin;
            }

            // Convert to pixel translations (negative because we're moving image, not viewport)
            double startTx = (-startOverflowX * startPanX01) + (startOverflowX / 2.0);
            double startTy = (-startOverflowY * startPanY01) + (startOverflowY / 2.0);
            double endTx = (-endOverflowX * endPanX01) + (endOverflowX / 2.0);
            double endTy = (-endOverflowY * endPanY01) + (endOverflowY / 2.0);

            // Clamp translations to valid range (prevents any background exposure)
            double safeOverflowX = Math.Max(0, overflowXMin - edgeSafetyPixels);
            double safeOverflowY = Math.Max(0, overflowYMin - edgeSafetyPixels);
            double safeMaxTx = safeOverflowX / 2.0;
            double safeMaxTy = safeOverflowY / 2.0;

            // Round to device pixels, then clamp to prevent rounding overshoot
            startTx = Math.Clamp(Math.Round(startTx), -safeMaxTx, safeMaxTx);
            startTy = Math.Clamp(Math.Round(startTy), -safeMaxTy, safeMaxTy);
            endTx = Math.Clamp(Math.Round(endTx), -safeMaxTx, safeMaxTx);
            endTy = Math.Clamp(Math.Round(endTy), -safeMaxTy, safeMaxTy);

            // Speed drives distance: pixels/second * duration = target travel distance.
            if (useHorizontalPan && overflowXMin > 1 && maxTravel > 0)
            {
                double preferredDirection = endPanX01 >= 0.5 ? -1.0 : 1.0;
                endTx = ApplySpeedPan(startTx, safeMaxTx, maxTravel, preferredDirection);
                endTy = startTy;
                if (endOverflowX > 0)
                {
                    endPanX01 = Math.Clamp((endOverflowX / 2.0 - endTx) / endOverflowX, 0.0, 1.0);
                }
                else
                {
                    endPanX01 = startPanX01;
                }
            }
            else if (!useHorizontalPan && overflowYMin > 1 && maxTravel > 0)
            {
                double preferredDirection = endPanY01 >= 0.5 ? -1.0 : 1.0;
                endTy = ApplySpeedPan(startTy, safeMaxTy, maxTravel, preferredDirection);
                endTx = startTx;
                if (endOverflowY > 0)
                {
                    endPanY01 = Math.Clamp((endOverflowY / 2.0 - endTy) / endOverflowY, 0.0, 1.0);
                }
                else
                {
                    endPanY01 = startPanY01;
                }
            }
            else
            {
                endTx = startTx;
                endTy = startTy;
                endPanX01 = startPanX01;
                endPanY01 = startPanY01;
            }

            // Store normalized end position for next cycle
            state.LastPanX01 = endPanX01;
            state.LastPanY01 = endPanY01;

            ApplyPanAndZoomDebug(state, container, effectElement, transform, viewportWidth, viewportHeight, imageWidth, imageHeight);

            // Create animations
            AddLinearZoomAnimation(storyboard, transform, startScale, endScale, duration);

            // Always add pan animations (even if small) for smooth continuity
            AddLinearPanAnimation(storyboard, transform, "TranslateX", startTx, endTx, duration);
            AddLinearPanAnimation(storyboard, transform, "TranslateY", startTy, endTy, duration);

            storyboard.Begin();

            static double ApplySpeedPan(double start, double safeMax, double maxTravel, double preferredDirection)
            {
                if (safeMax <= 0 || maxTravel <= 0)
                {
                    return start;
                }

                double min = -safeMax;
                double max = safeMax;
                double direction = Math.Sign(preferredDirection);
                if (direction == 0)
                {
                    direction = 1;
                }

                double availablePrimary = direction < 0 ? start - min : max - start;
                double availableSecondary = direction < 0 ? max - start : start - min;
                if (availablePrimary <= 0 && availableSecondary > 0)
                {
                    direction = -direction;
                    availablePrimary = availableSecondary;
                }

                double travel = Math.Min(maxTravel, Math.Max(0.0, availablePrimary));
                double end = start + (direction * travel);
                return Math.Clamp(Math.Round(end), min, max);
            }
        }

        private static bool TryGetBitmapPixelSize(Image? image, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (image?.Source is BitmapSource { PixelWidth: > 0, PixelHeight: > 0 } bitmap)
            {
                width = bitmap.PixelWidth;
                height = bitmap.PixelHeight;
                return true;
            }

            return false;
        }

        private static void AddLinearZoomAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            double fromScale,
            double toScale,
            TimeSpan duration)
        {
            DoubleAnimation scaleXAnim = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = new Duration(duration)
            };
            Storyboard.SetTarget(scaleXAnim, transform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
            storyboard.Children.Add(scaleXAnim);

            DoubleAnimation scaleYAnim = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = new Duration(duration)
            };
            Storyboard.SetTarget(scaleYAnim, transform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
            storyboard.Children.Add(scaleYAnim);
        }

        private static void AddLinearPanAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            string property,
            double from,
            double to,
            TimeSpan duration)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration)
            };
            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }

        private static bool TryGetImageContentSize(
            Image? image,
            double viewportWidth,
            double viewportHeight,
            out double contentWidth,
            out double contentHeight)
        {
            contentWidth = 0;
            contentHeight = 0;

            if (viewportWidth <= 0 || viewportHeight <= 0)
            {
                return false;
            }

            if (!TryGetBitmapPixelSize(image, out int pixelWidth, out int pixelHeight))
            {
                return false;
            }

            double coverScale = Math.Max(viewportWidth / pixelWidth, viewportHeight / pixelHeight);
            double scale = Math.Max(1.0, coverScale);
            if (double.IsNaN(scale) || double.IsInfinity(scale))
            {
                return false;
            }

            contentWidth = pixelWidth * scale;
            contentHeight = pixelHeight * scale;
            return contentWidth > 0 && contentHeight > 0;
        }

        private static void ApplyImageContentSizing(
            FrameworkElement image,
            double viewportWidth,
            double viewportHeight,
            double contentWidth,
            double contentHeight,
            PanAndZoomState state)
        {
            if (contentWidth <= 0 || contentHeight <= 0)
            {
                return;
            }

            if (!state.HasSizingSnapshot || !ReferenceEquals(state.SizingTarget, image))
            {
                state.HasSizingSnapshot = true;
                state.SizingTarget = image;
                state.OriginalWidth = image.Width;
                state.OriginalHeight = image.Height;
                state.OriginalMargin = image.Margin;
                state.OriginalHorizontalAlignment = image.HorizontalAlignment;
                state.OriginalVerticalAlignment = image.VerticalAlignment;
                state.OriginalIsHitTestVisible = image.IsHitTestVisible;
            }

            image.Width = contentWidth;
            image.Height = contentHeight;
            image.HorizontalAlignment = HorizontalAlignment.Center;
            image.VerticalAlignment = VerticalAlignment.Center;

            Thickness originalMargin = state.OriginalMargin;
            if (viewportWidth > 0 && viewportHeight > 0)
            {
                double deltaX = Math.Max(0, contentWidth - viewportWidth);
                double deltaY = Math.Max(0, contentHeight - viewportHeight);
                image.Margin = new Thickness(
                    originalMargin.Left - (deltaX / 2.0),
                    originalMargin.Top - (deltaY / 2.0),
                    originalMargin.Right - (deltaX / 2.0),
                    originalMargin.Bottom - (deltaY / 2.0));
            }
            else
            {
                image.Margin = originalMargin;
            }

            image.IsHitTestVisible = false;
        }

        private static double GetRequiredScale(double contentSize, double viewportSize, double requiredOverflow)
        {
            if (contentSize <= 0 || viewportSize <= 0)
            {
                return 1.0;
            }

            double targetSize = viewportSize + Math.Max(0, requiredOverflow);
            return targetSize / contentSize;
        }

        private static void ApplyPanAndZoomDebug(
            PanAndZoomState state,
            FrameworkElement? container,
            FrameworkElement? effectElement,
            CompositeTransform transform,
            double viewportWidth,
            double viewportHeight,
            double imageWidth,
            double imageHeight)
        {
            if (!PanAndZoomDebugEnabled)
            {
                RemovePanAndZoomDebug(state);
                return;
            }

            if (container is null || effectElement is null)
            {
                return;
            }

            Grid? debugHost = EnsurePanAndZoomDebugHost(container, effectElement);
            if (debugHost is null)
            {
                return;
            }

            Border viewportBorder = state.DebugViewportBorder ??= CreatePanAndZoomDebugBorder(Microsoft.UI.Colors.OrangeRed);
            AttachDebugBorder(debugHost, viewportBorder);
            UpdateDebugBorder(viewportBorder, viewportWidth, viewportHeight, null);

            Border imageBorder = state.DebugImageBorder ??= CreatePanAndZoomDebugBorder(Microsoft.UI.Colors.LimeGreen);
            AttachDebugBorder(debugHost, imageBorder);
            UpdateDebugBorder(imageBorder, imageWidth, imageHeight, transform);
        }

        private static Grid? EnsurePanAndZoomDebugHost(FrameworkElement container, FrameworkElement effectElement)
        {
            if (effectElement.Parent is Grid { Tag: PanAndZoomDebugGridTag } existingGrid)
            {
                return existingGrid;
            }

            if (container is Border border && border.Child is UIElement child)
            {
                if (child is Grid { Tag: PanAndZoomDebugGridTag } taggedGrid)
                {
                    return taggedGrid;
                }

                Grid grid = new Grid
                {
                    Tag = PanAndZoomDebugGridTag,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                border.Child = null;
                grid.Children.Add(child);
                border.Child = grid;
                return grid;
            }

            if (container is Panel panel)
            {
                if (panel is Grid grid && Equals(grid.Tag, PanAndZoomDebugGridTag))
                {
                    return grid;
                }

                int index = panel.Children.IndexOf(effectElement);
                if (index >= 0)
                {
                    Grid wrapper = new Grid
                    {
                        Tag = PanAndZoomDebugGridTag,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };
                    panel.Children.RemoveAt(index);
                    wrapper.Children.Add(effectElement);
                    panel.Children.Insert(index, wrapper);
                    return wrapper;
                }
            }

            return null;
        }

        private static Border CreatePanAndZoomDebugBorder(Windows.UI.Color color)
        {
            return new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(color),
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static void AttachDebugBorder(Grid host, Border border)
        {
            if (border.Parent is Panel existingParent && !ReferenceEquals(existingParent, host))
            {
                existingParent.Children.Remove(border);
            }

            if (!host.Children.Contains(border))
            {
                host.Children.Add(border);
            }
        }

        private static void UpdateDebugBorder(Border border, double width, double height, CompositeTransform? transform)
        {
            border.Width = width;
            border.Height = height;
            border.RenderTransform = transform;
            border.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        }

        private static void RemovePanAndZoomDebug(PanAndZoomState state)
        {
            if (state.DebugImageBorder?.Parent is Panel imageParent)
            {
                imageParent.Children.Remove(state.DebugImageBorder);
            }

            if (state.DebugViewportBorder?.Parent is Panel viewportParent)
            {
                viewportParent.Children.Remove(state.DebugViewportBorder);
            }

            state.DebugImageBorder = null;
            state.DebugViewportBorder = null;
        }

        private static void RestorePanAndZoomSizing(PanAndZoomState state)
        {
            if (!state.HasSizingSnapshot || state.SizingTarget is not FrameworkElement target)
            {
                return;
            }

            target.Width = state.OriginalWidth;
            target.Height = state.OriginalHeight;
            target.Margin = state.OriginalMargin;
            target.HorizontalAlignment = state.OriginalHorizontalAlignment;
            target.VerticalAlignment = state.OriginalVerticalAlignment;
            target.IsHitTestVisible = state.OriginalIsHitTestVisible;

            state.HasSizingSnapshot = false;
            state.SizingTarget = null;
        }

        private static void StorePanAndZoomSizingSnapshot(PanAndZoomState state)
        {
            if (!state.HasSizingSnapshot || state.SizingTarget is not FrameworkElement target)
            {
                return;
            }

            var snapshot = new PanAndZoomSizingSnapshot
            {
                Width = state.OriginalWidth,
                Height = state.OriginalHeight,
                Margin = state.OriginalMargin,
                HorizontalAlignment = state.OriginalHorizontalAlignment,
                VerticalAlignment = state.OriginalVerticalAlignment,
                IsHitTestVisible = state.OriginalIsHitTestVisible
            };

            _ = _PanAndZoomSizingSnapshots.Remove(target);
            _PanAndZoomSizingSnapshots.Add(target, snapshot);
        }

        internal static void RestorePanAndZoomSizingSnapshot(UIElement element)
        {
            if (element is not FrameworkElement target)
            {
                return;
            }

            if (!_PanAndZoomSizingSnapshots.TryGetValue(target, out PanAndZoomSizingSnapshot? snapshot))
            {
                return;
            }

            target.Width = snapshot.Width;
            target.Height = snapshot.Height;
            target.Margin = snapshot.Margin;
            target.HorizontalAlignment = snapshot.HorizontalAlignment;
            target.VerticalAlignment = snapshot.VerticalAlignment;
            target.IsHitTestVisible = snapshot.IsHitTestVisible;

            _ = _PanAndZoomSizingSnapshots.Remove(target);
        }

        internal static void StopPanAndZoom(Storyboard storyboard, bool preserveSizing)
        {
            if (_PanAndZoomStates.TryGetValue(storyboard, out PanAndZoomState? state))
            {
                state.Deactivate(storyboard);
                if (preserveSizing)
                {
                    StorePanAndZoomSizingSnapshot(state);
                }
                else
                {
                    RestorePanAndZoomSizing(state);
                }
                RemovePanAndZoomDebug(state);
                _ = _PanAndZoomStates.Remove(storyboard);
            }
        }

        #endregion

        #region Breath Effect (Keyframe-based zoom bounce)

        /// <summary>
        /// Applies a breathing zoom effect: zooms in to peak, then settles back slightly.
        /// Based on the VB.NET "Anim_Breath" pattern.
        /// </summary>
        private static void ApplyBreathEffect(
            Storyboard storyboard,
            CompositeTransform transform,
            TimeSpan duration,
            FlowerySlideEffectParams @params,
            UIElement? effectTarget)
        {
            double intensity = @params.BreathIntensity;
            bool zoomIn = _random.Next(2) == 0;

            CubicEase easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            CubicEase easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };

            // Keyframe animation: start -> peak -> settle
            // If zooming in: 1.0 -> 1.3 -> 1.1
            // If zooming out: 1.3 -> 1.0 -> 1.2
            double startScale, peakScale, endScale;
            if (zoomIn)
            {
                startScale = 1.0;
                peakScale = 1.0 + intensity;
                endScale = 1.0 + (intensity * 0.33); // Settle at 1/3 of intensity
            }
            else
            {
                startScale = 1.0 + intensity;
                peakScale = 1.0;
                endScale = 1.0 + (intensity * 0.66); // Settle at 2/3 of intensity
            }

            TimeSpan halfTime = TimeSpan.FromSeconds(duration.TotalSeconds * 0.5);

            // ScaleX keyframes
            DoubleAnimationUsingKeyFrames scaleXAnim = new DoubleAnimationUsingKeyFrames();
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = startScale });
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(halfTime), Value = peakScale, EasingFunction = easeOut });
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(duration), Value = endScale, EasingFunction = easeIn });
            Storyboard.SetTarget(scaleXAnim, transform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
            storyboard.Children.Add(scaleXAnim);

            // ScaleY keyframes
            DoubleAnimationUsingKeyFrames scaleYAnim = new DoubleAnimationUsingKeyFrames();
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = startScale });
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(halfTime), Value = peakScale, EasingFunction = easeOut });
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(duration), Value = endScale, EasingFunction = easeIn });
            Storyboard.SetTarget(scaleYAnim, transform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
            storyboard.Children.Add(scaleYAnim);

            // Add subtle pan during the breath
            double panDistance = @params.PanDistance * 0.5;
            double panX = (_random.Next(2) == 0 ? 1 : -1) * panDistance;
            double panY = (_random.Next(2) == 0 ? 1 : -1) * panDistance * @params.VerticalPanRatio;

            AddPanAnimation(storyboard, transform, "TranslateX", 0, panX, duration, new CubicEase { EasingMode = EasingMode.EaseInOut });
            AddPanAnimation(storyboard, transform, "TranslateY", 0, panY, duration, new CubicEase { EasingMode = EasingMode.EaseInOut });
        }

        #endregion

        #region Throw Effect (Parallax fly-through)

        // Static field to track throw direction for consecutive slides
        private static bool _lastThrowDirectionPositive = true;

        /// <summary>
        /// Applies a dramatic fly-through effect: image scales up at center while panning across.
        /// Based on the VB.NET "Anim_Throw" pattern.
        /// </summary>
        private static void ApplyThrowEffect(
            Storyboard storyboard,
            CompositeTransform transform,
            TimeSpan duration,
            FlowerySlideEffectParams @params,
            UIElement? effectTarget)
        {
            // Alternate throw direction to avoid repetition
            _lastThrowDirectionPositive = !_lastThrowDirectionPositive;
            bool direction = _lastThrowDirectionPositive;

            FrameworkElement? container = ResolveEffectContainer(effectTarget);
            double viewportWidth = container?.ActualWidth ?? 400;
            double viewportHeight = container?.ActualHeight ?? 300;
            double imageWidth = (effectTarget as FrameworkElement)?.ActualWidth ?? viewportWidth;
            double imageHeight = (effectTarget as FrameworkElement)?.ActualHeight ?? viewportHeight;

            bool isWiderThanTall = imageWidth / imageHeight > viewportWidth / viewportHeight;
            // Fix: Start/End at 1.0 (Fit) to prevent bars. Zoom IN at center instead.
            double centerScale = Math.Max(1.25, @params.ThrowScale);
            double edgeScale = 1.0;

            CubicEase easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            CubicEase easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };

            // Scale animation: edge -> center -> edge (1.0 -> 1.25 -> 1.0)
            TimeSpan halfTime = TimeSpan.FromSeconds(duration.TotalSeconds * 0.5);

            DoubleAnimationUsingKeyFrames scaleXAnim = new DoubleAnimationUsingKeyFrames();
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = edgeScale });
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(halfTime), Value = centerScale, EasingFunction = easeOut });
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(duration), Value = edgeScale, EasingFunction = easeIn });
            Storyboard.SetTarget(scaleXAnim, transform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
            storyboard.Children.Add(scaleXAnim);

            DoubleAnimationUsingKeyFrames scaleYAnim = new DoubleAnimationUsingKeyFrames();
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = edgeScale });
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(halfTime), Value = centerScale, EasingFunction = easeOut });
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(duration), Value = edgeScale, EasingFunction = easeIn });
            Storyboard.SetTarget(scaleYAnim, transform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
            storyboard.Children.Add(scaleYAnim);

            // Pan animation: crosses the safe zone (overflow)
            if (isWiderThanTall)
            {
                // Horizontal throw
                double overflowX = Math.Max(0, imageWidth - viewportWidth);
                double safePanX = overflowX / 2.0;

                double startX = direction ? safePanX : -safePanX;
                double endX = direction ? -safePanX : safePanX;

                DoubleAnimationUsingKeyFrames panXAnim = new DoubleAnimationUsingKeyFrames();
                panXAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = startX });
                panXAnim.KeyFrames.Add(new SplineDoubleKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(duration),
                    Value = endX,
                    KeySpline = new KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.8, 0.2) }
                });
                Storyboard.SetTarget(panXAnim, transform);
                Storyboard.SetTargetProperty(panXAnim, "TranslateX");
                storyboard.Children.Add(panXAnim);
            }
            else
            {
                // Vertical throw
                double overflowY = Math.Max(0, imageHeight - viewportHeight);
                double safePanY = overflowY / 2.0;

                double startY = direction ? safePanY : -safePanY;
                double endY = direction ? -safePanY : safePanY;

                DoubleAnimationUsingKeyFrames panYAnim = new DoubleAnimationUsingKeyFrames();
                panYAnim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = startY });
                panYAnim.KeyFrames.Add(new SplineDoubleKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(duration),
                    Value = endY,
                    KeySpline = new KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.2, 0.8), ControlPoint2 = new Windows.Foundation.Point(0.8, 0.2) }
                });
                Storyboard.SetTarget(panYAnim, transform);
                Storyboard.SetTargetProperty(panYAnim, "TranslateY");
                storyboard.Children.Add(panYAnim);
            }
        }

        #endregion

        #endregion

        private static FrameworkElement? ResolveEffectContainer(UIElement? effectTarget)
        {
            return effectTarget is FrameworkElement { Parent: FrameworkElement parent } ? parent : null;
        }

        #region Transform Preparation

        /// <summary>
        /// Ensures the element has a CompositeTransform set up for animation.
        /// Sets RenderTransformOrigin to center (0.5, 0.5) for symmetric transformations.
        /// </summary>
        /// <param name="element">The UIElement to prepare.</param>
        /// <returns>The CompositeTransform attached to the element.</returns>
        public static CompositeTransform EnsureCompositeTransform(UIElement element)
        {
            if (element.RenderTransform is not CompositeTransform)
            {
                element.RenderTransform = new CompositeTransform();
            }
            element.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
            return (CompositeTransform)element.RenderTransform;
        }

        /// <summary>
        /// Resets a CompositeTransform to its default state (no transformation).
        /// </summary>
        public static void ResetTransform(CompositeTransform transform)
        {
            transform.ScaleX = 1.0;
            transform.ScaleY = 1.0;
            transform.TranslateX = 0;
            transform.TranslateY = 0;
            transform.Rotation = 0;
            transform.SkewX = 0;
            transform.SkewY = 0;
            transform.CenterX = 0;
            transform.CenterY = 0;
        }

        #endregion

        #region Zoom Animations

        /// <summary>
        /// Creates a zoom animation that scales uniformly on both axes.
        /// </summary>
        /// <param name="storyboard">The Storyboard to add animations to.</param>
        /// <param name="transform">The CompositeTransform to animate.</param>
        /// <param name="fromScale">Starting scale factor (1.0 = 100%).</param>
        /// <param name="toScale">Ending scale factor.</param>
        /// <param name="duration">Animation duration.</param>
        /// <param name="easing">Optional easing function (defaults to QuadraticEase InOut).</param>
        public static void AddZoomAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            double fromScale,
            double toScale,
            TimeSpan duration,
            EasingFunctionBase? easing = null,
            TimeSpan? beginTime = null)
        {
            easing ??= new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            DoubleAnimation scaleXAnim = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = new Duration(duration),
                EasingFunction = easing
            };
            if (beginTime.HasValue)
            {
                scaleXAnim.BeginTime = beginTime.Value;
            }
            Storyboard.SetTarget(scaleXAnim, transform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
            storyboard.Children.Add(scaleXAnim);

            DoubleAnimation scaleYAnim = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = new Duration(duration),
                EasingFunction = easing
            };
            if (beginTime.HasValue)
            {
                scaleYAnim.BeginTime = beginTime.Value;
            }
            Storyboard.SetTarget(scaleYAnim, transform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
            storyboard.Children.Add(scaleYAnim);
        }

        /// <summary>
        /// Creates a zoom-in animation (scales up from 1.0).
        /// </summary>
        public static void AddZoomInAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            double intensity,
            TimeSpan duration)
        {
            AddZoomAnimation(storyboard, transform, 1.0, 1.0 + intensity, duration);
        }

        /// <summary>
        /// Creates a zoom-out animation (scales down to 1.0).
        /// </summary>
        public static void AddZoomOutAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            double intensity,
            TimeSpan duration)
        {
            AddZoomAnimation(storyboard, transform, 1.0 + intensity, 1.0, duration);
        }

        #endregion

        #region Pan/Translate Animations

        /// <summary>
        /// Creates a pan (translation) animation on a specified axis.
        /// </summary>
        /// <param name="storyboard">The Storyboard to add animations to.</param>
        /// <param name="transform">The CompositeTransform to animate.</param>
        /// <param name="property">The property to animate ("TranslateX" or "TranslateY").</param>
        /// <param name="from">Starting position in pixels.</param>
        /// <param name="to">Ending position in pixels.</param>
        /// <param name="duration">Animation duration.</param>
        /// <param name="easing">Optional easing function (defaults to QuadraticEase InOut).</param>
        public static void AddPanAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            string property,
            double from,
            double to,
            TimeSpan duration,
            EasingFunctionBase? easing = null,
            TimeSpan? beginTime = null)
        {
            easing ??= new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                EasingFunction = easing
            };
            if (beginTime.HasValue)
            {
                animation.BeginTime = beginTime.Value;
            }
            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }

        /// <summary>
        /// Creates a horizontal pan animation (left = negative, right = positive).
        /// </summary>
        public static void AddHorizontalPanAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            double distance,
            TimeSpan duration)
        {
            AddPanAnimation(storyboard, transform, "TranslateX", 0, distance, duration);
        }

        /// <summary>
        /// Creates a vertical pan animation (up = negative, down = positive).
        /// </summary>
        public static void AddVerticalPanAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            double distance,
            TimeSpan duration)
        {
            AddPanAnimation(storyboard, transform, "TranslateY", 0, distance, duration);
        }

        #endregion

        #region Pulse Animation

        /// <summary>
        /// Creates a pulse (breathing) animation that scales up then back down.
        /// </summary>
        /// <param name="storyboard">The Storyboard to add animations to.</param>
        /// <param name="transform">The CompositeTransform to animate.</param>
        /// <param name="intensity">Peak scale increase (e.g., 0.1 = pulse to 110%).</param>
        /// <param name="duration">Total animation duration (half for up, half for down).</param>
        public static void AddPulseAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            double intensity,
            TimeSpan duration)
        {
            TimeSpan halfDuration = TimeSpan.FromSeconds(duration.TotalSeconds / 2);
            QuadraticEase easing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            // ScaleX keyframes: 1.0 -> 1.0+intensity -> 1.0
            DoubleAnimationUsingKeyFrames scaleXAnim = new DoubleAnimationUsingKeyFrames();
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                Value = 1.0
            });
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(halfDuration),
                Value = 1.0 + intensity,
                EasingFunction = easing
            });
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(duration),
                Value = 1.0,
                EasingFunction = easing
            });
            Storyboard.SetTarget(scaleXAnim, transform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
            storyboard.Children.Add(scaleXAnim);

            // ScaleY keyframes: same pattern
            DoubleAnimationUsingKeyFrames scaleYAnim = new DoubleAnimationUsingKeyFrames();
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                Value = 1.0
            });
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(halfDuration),
                Value = 1.0 + intensity,
                EasingFunction = easing
            });
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(duration),
                Value = 1.0,
                EasingFunction = easing
            });
            Storyboard.SetTarget(scaleYAnim, transform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
            storyboard.Children.Add(scaleYAnim);
        }

        #endregion

        #region Fade Animations

        /// <summary>
        /// Creates a fade animation on an element's Opacity.
        /// </summary>
        /// <param name="storyboard">The Storyboard to add animations to.</param>
        /// <param name="element">The UIElement to animate.</param>
        /// <param name="fromOpacity">Starting opacity (0.0 to 1.0).</param>
        /// <param name="toOpacity">Ending opacity (0.0 to 1.0).</param>
        /// <param name="duration">Animation duration.</param>
        /// <param name="easing">Optional easing function.</param>
        public static void AddFadeAnimation(
            Storyboard storyboard,
            UIElement element,
            double fromOpacity,
            double toOpacity,
            TimeSpan duration,
            EasingFunctionBase? easing = null,
            TimeSpan? beginTime = null)
        {
            easing ??= new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            DoubleAnimation animation = new DoubleAnimation
            {
                From = fromOpacity,
                To = toOpacity,
                Duration = new Duration(duration),
                EasingFunction = easing
            };
            if (beginTime.HasValue)
            {
                animation.BeginTime = beginTime.Value;
            }
            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Children.Add(animation);
        }

        /// <summary>
        /// Creates a fade-in animation (0 to 1).
        /// </summary>
        // ReSharper disable UnusedMember.Global
        public static void AddFadeInAnimation(Storyboard storyboard, UIElement element, TimeSpan duration)
        {
            AddFadeAnimation(storyboard, element, 0.0, 1.0, duration);
        }

        /// <summary>
        /// Creates a fade-out animation (1 to 0).
        /// </summary>
        public static void AddFadeOutAnimation(Storyboard storyboard, UIElement element, TimeSpan duration)
        {
            AddFadeAnimation(storyboard, element, 1.0, 0.0, duration);
        }

        #endregion

        #region Property Animations

        /// <summary>
        /// Creates a double animation on a dependency property.
        /// </summary>
        public static void AddDoubleAnimation(
            Storyboard storyboard,
            DependencyObject target,
            string property,
            double from,
            double to,
            TimeSpan duration,
            EasingFunctionBase? easing = null,
            TimeSpan? beginTime = null)
        {
            easing ??= new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                EasingFunction = easing
            };
            if (beginTime.HasValue)
            {
                animation.BeginTime = beginTime.Value;
            }
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }

        #endregion

        #region Pop Animations

        /// <summary>
        /// Creates a combined fade/scale/translate animation suitable for pop-in/out effects.
        /// </summary>
        /// <param name="storyboard">The Storyboard to add animations to.</param>
        /// <param name="element">The UIElement to animate.</param>
        /// <param name="transform">The CompositeTransform to animate.</param>
        /// <param name="fromOpacity">Starting opacity.</param>
        /// <param name="toOpacity">Ending opacity.</param>
        /// <param name="fromScale">Starting uniform scale.</param>
        /// <param name="toScale">Ending uniform scale.</param>
        /// <param name="fromTranslateX">Starting X translation.</param>
        /// <param name="toTranslateX">Ending X translation.</param>
        /// <param name="fromTranslateY">Starting Y translation.</param>
        /// <param name="toTranslateY">Ending Y translation.</param>
        /// <param name="duration">Animation duration.</param>
        /// <param name="easing">Optional easing function.</param>
        public static void AddPopAnimation(
            Storyboard storyboard,
            UIElement element,
            CompositeTransform transform,
            double fromOpacity,
            double toOpacity,
            double fromScale,
            double toScale,
            double fromTranslateX,
            double toTranslateX,
            double fromTranslateY,
            double toTranslateY,
            TimeSpan duration,
            EasingFunctionBase? easing = null,
            TimeSpan? beginTime = null)
        {
            AddFadeAnimation(storyboard, element, fromOpacity, toOpacity, duration, easing, beginTime);

            if (fromScale != toScale)
            {
                AddZoomAnimation(storyboard, transform, fromScale, toScale, duration, easing, beginTime);
            }

            if (fromTranslateX != toTranslateX)
            {
                AddPanAnimation(storyboard, transform, "TranslateX", fromTranslateX, toTranslateX, duration, easing, beginTime);
            }

            if (fromTranslateY != toTranslateY)
            {
                AddPanAnimation(storyboard, transform, "TranslateY", fromTranslateY, toTranslateY, duration, easing, beginTime);
            }
        }

        #endregion

        #region Rotation Animations

        /// <summary>
        /// Creates a rotation animation.
        /// </summary>
        /// <param name="storyboard">The Storyboard to add animations to.</param>
        /// <param name="transform">The CompositeTransform to animate.</param>
        /// <param name="fromDegrees">Starting rotation in degrees.</param>
        /// <param name="toDegrees">Ending rotation in degrees.</param>
        /// <param name="duration">Animation duration.</param>
        /// <param name="easing">Optional easing function.</param>
        public static void AddRotationAnimation(
            Storyboard storyboard,
            CompositeTransform transform,
            double fromDegrees,
            double toDegrees,
            TimeSpan duration,
            EasingFunctionBase? easing = null)
        {
            easing ??= new QuadraticEase { EasingMode = EasingMode.EaseInOut };

            DoubleAnimation animation = new DoubleAnimation
            {
                From = fromDegrees,
                To = toDegrees,
                Duration = new Duration(duration),
                EasingFunction = easing
            };
            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, "Rotation");
            storyboard.Children.Add(animation);
        }
        // ReSharper restore UnusedMember.Global

        #endregion

        #region Storyboard Management

        /// <summary>
        /// Safely stops and clears a storyboard.
        /// </summary>
        public static void StopAndClear(ref Storyboard? storyboard)
        {
            if (storyboard is not null)
            {
                storyboard.Stop();
                storyboard = null;
            }
        }

        /// <summary>
        /// Creates a new Storyboard with optional repeat behavior.
        /// </summary>
        /// <param name="repeatForever">If true, the storyboard will repeat indefinitely.</param>
        public static Storyboard CreateStoryboard(bool repeatForever = false)
        {
            Storyboard storyboard = new Storyboard();
            if (repeatForever)
            {
                storyboard.RepeatBehavior = RepeatBehavior.Forever;
            }
            return storyboard;
        }

        #endregion
        private static Image? FindFirstImage(DependencyObject? parent)
        {
            if (parent is Image img)
            {
                return img;
            }

            if (parent is ContentControl cc && cc.Content is DependencyObject content)
            {
                return FindFirstImage(content);
            }

            if (parent is Border border && border.Child is DependencyObject child)
            {
                return FindFirstImage(child);
            }

            if (parent is Panel panel)
            {
                foreach (UIElement? panelChild in panel.Children)
                {
                    Image? result = FindFirstImage(panelChild);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

    }
}
