namespace Flowery.Controls
{
    /// <summary>
    /// Dot animation implementations (Pulse, Blink, Bounce, Heartbeat, etc.)
    /// and ring/effect animations (Glow, Orbit rotation, Beacon burst).
    /// </summary>
    public partial class DaisyStatusIndicator
    {
        private void StartDotAnimation()
        {
            if (_dotVisual == null || _compositor == null || !_isAnimating) return;

            switch (Variant)
            {
                case DaisyStatusIndicatorVariant.Pulse:
                    StartPulseAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Blink:
                    StartBlinkAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Bounce:
                    StartBounceAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Heartbeat:
                    StartHeartbeatAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Wave:
                    StartWaveAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Morph:
                    StartMorphAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Shake:
                    StartShakeAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Wobble:
                    StartWobbleAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Pop:
                    StartPopAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Flicker:
                    StartFlickerAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Breathe:
                    StartBreatheAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Flash:
                    StartFlashAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Swing:
                    StartSwingAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Jiggle:
                    StartJiggleAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Throb:
                    StartThrobAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Twinkle:
                    StartTwinkleAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Beacon:
                    StartBeaconFlashAnimation();
                    break;
                case DaisyStatusIndicatorVariant.Splash:
                    StartSplashAnimation();
                    break;
                // Default/Ping/Ripple/Ring/Sonar/Glow/Orbit/Spin/Radar don't animate the main dot directly
            }
        }

        private void StartPulseAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var opacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(0f, 1f);
            opacityAnimation.InsertKeyFrame(0.5f, 0.4f);
            opacityAnimation.InsertKeyFrame(1f, 1f);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(1500);
            opacityAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Opacity", opacityAnimation);
        }

        private void StartBlinkAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var opacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(0f, 1f);
            opacityAnimation.InsertKeyFrame(0.49f, 1f);
            opacityAnimation.InsertKeyFrame(0.5f, 0f);
            opacityAnimation.InsertKeyFrame(0.99f, 0f);
            opacityAnimation.InsertKeyFrame(1f, 1f);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(1000);
            opacityAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Opacity", opacityAnimation);
        }

        private void StartBounceAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var amplitude = Math.Max(2f, (float)(DaisyResourceLookup.GetSizeValue(Size) * 0.5));

            var translation = _compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0, 0, 0));
            translation.InsertKeyFrame(0.5f, new Vector3(0, -amplitude, 0));
            translation.InsertKeyFrame(1f, new Vector3(0, 0, 0));
            translation.Duration = TimeSpan.FromMilliseconds(500);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Translation", translation);
        }

        private void StartHeartbeatAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scaleAnimation.InsertKeyFrame(0.2f, new Vector3(1.3f, 1.3f, 1f));
            scaleAnimation.InsertKeyFrame(0.35f, new Vector3(1f, 1f, 1f));
            scaleAnimation.InsertKeyFrame(0.55f, new Vector3(1.2f, 1.2f, 1f));
            scaleAnimation.InsertKeyFrame(0.75f, new Vector3(1f, 1f, 1f));
            scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(1200);
            scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scaleAnimation);
        }

        private void StartWaveAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scaleAnimation.InsertKeyFrame(0.5f, new Vector3(1f, 1.55f, 1f));
            scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(900);
            scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scaleAnimation);
        }

        private void StartMorphAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scaleAnimation.InsertKeyFrame(0.33f, new Vector3(1.55f, 0.75f, 1f));
            scaleAnimation.InsertKeyFrame(0.66f, new Vector3(0.8f, 1.4f, 1f));
            scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(1400);
            scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scaleAnimation);
        }

        private void StartShakeAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var amplitude = Math.Max(1f, (float)(DaisyResourceLookup.GetSizeValue(Size) * 0.25));

            var translation = _compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0, 0, 0));
            translation.InsertKeyFrame(0.2f, new Vector3(-amplitude, 0, 0));
            translation.InsertKeyFrame(0.4f, new Vector3(amplitude, 0, 0));
            translation.InsertKeyFrame(0.6f, new Vector3(-amplitude * 0.66f, 0, 0));
            translation.InsertKeyFrame(0.8f, new Vector3(amplitude * 0.66f, 0, 0));
            translation.InsertKeyFrame(1f, new Vector3(0, 0, 0));
            translation.Duration = TimeSpan.FromMilliseconds(500);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Translation", translation);
        }

        private void StartWobbleAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            // Rotation alone is visually ambiguous on a perfect circle; pair it with non-uniform scale (Avalonia behavior).
            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.25f, new Vector3(1.1f, 0.9f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.75f, new Vector3(0.9f, 1.1f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(1000);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            if ((PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend) && _dotElement != null)
            {
                var storyboard = PlatformCompatibility.StartRotationKeyframes(
                    _dotElement,
                    [(0d, 0d), (0.25d, 15d), (0.5d, 0d), (0.75d, -15d), (1d, 0d)],
                    TimeSpan.FromMilliseconds(1000));
                TrackStoryboard(storyboard);
            }
            else
            {
                PlatformCompatibility.StartRotationAnimationInDegrees(
                    _dotVisual,
                    [(0f, 0f), (0.25f, 15f), (0.5f, 0f), (0.75f, -15f), (1f, 0f)],
                    TimeSpan.FromMilliseconds(1000));
            }
            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scale);
        }

        private void StartPopAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(0.65f, 0.65f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(1.25f, 1.25f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(900);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scale);
        }

        private void StartSplashAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            // Splash animation: droplet impact effect
            // - Starts normal size
            // - Quickly compresses (impact)
            // - Rapidly expands (splash)
            // - Settles back to normal
            // Duration matches the ripple animation (900ms)
            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));           // Normal
            scale.InsertKeyFrame(0.15f, new Vector3(0.5f, 0.5f, 1f));    // Compress on impact
            scale.InsertKeyFrame(0.4f, new Vector3(1.4f, 1.4f, 1f));     // Splash expand
            scale.InsertKeyFrame(0.7f, new Vector3(0.9f, 0.9f, 1f));     // Slight recoil
            scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));           // Settle
            scale.Duration = TimeSpan.FromMilliseconds(900);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scale);
        }

        private void StartFlickerAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var opacity = _compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f);
            opacity.InsertKeyFrame(0.15f, 0.2f);
            opacity.InsertKeyFrame(0.25f, 0.85f);
            opacity.InsertKeyFrame(0.35f, 0.1f);
            opacity.InsertKeyFrame(0.5f, 0.9f);
            opacity.InsertKeyFrame(0.65f, 0.3f);
            opacity.InsertKeyFrame(0.8f, 1f);
            opacity.InsertKeyFrame(1f, 1f);
            opacity.Duration = TimeSpan.FromMilliseconds(1000);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Opacity", opacity);
        }

        private void StartBreatheAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(1.25f, 1.25f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(1600);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scale);
        }

        private void StartFlashAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var opacity = _compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f);
            opacity.InsertKeyFrame(0.1f, 0.2f);
            opacity.InsertKeyFrame(0.2f, 1f);
            opacity.InsertKeyFrame(1f, 1f);
            opacity.Duration = TimeSpan.FromMilliseconds(900);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Opacity", opacity);
        }

        private void StartSwingAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var amplitude = Math.Max(2f, (float)(DaisyResourceLookup.GetSizeValue(Size) * 0.5));

            var translation = _compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0, 0, 0));
            translation.InsertKeyFrame(0.25f, new Vector3(amplitude, 0, 0));
            translation.InsertKeyFrame(0.5f, new Vector3(0, 0, 0));
            translation.InsertKeyFrame(0.75f, new Vector3(-amplitude, 0, 0));
            translation.InsertKeyFrame(1f, new Vector3(0, 0, 0));
            translation.Duration = TimeSpan.FromMilliseconds(1500);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Translation", translation);
        }

        private void StartJiggleAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var step = Math.Max(1f, (float)(DaisyResourceLookup.GetSizeValue(Size) * 0.1));

            var translation = _compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0, 0, 0));
            translation.InsertKeyFrame(0.25f, new Vector3(step, -step, 0));
            translation.InsertKeyFrame(0.5f, new Vector3(-step, step, 0));
            translation.InsertKeyFrame(0.75f, new Vector3(step, 0, 0));
            translation.InsertKeyFrame(1f, new Vector3(0, 0, 0));
            translation.Duration = TimeSpan.FromMilliseconds(300);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Translation", translation);
        }

        private void StartThrobAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(1.35f, 1.35f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(1100);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = _compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f);
            opacity.InsertKeyFrame(0.5f, 0.55f);
            opacity.InsertKeyFrame(1f, 1f);
            opacity.Duration = TimeSpan.FromMilliseconds(1100);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scale);
            PlatformCompatibility.StartAnimation(_dotVisual, "Opacity", opacity);
        }

        private void StartTwinkleAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            var opacity = _compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 0.5f);
            opacity.InsertKeyFrame(0.25f, 1f);
            opacity.InsertKeyFrame(0.5f, 0.4f);
            opacity.InsertKeyFrame(0.75f, 1f);
            opacity.InsertKeyFrame(1f, 0.5f);
            opacity.Duration = TimeSpan.FromMilliseconds(1000);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(0.9f, 0.9f, 1f));
            scale.InsertKeyFrame(0.25f, new Vector3(1.2f, 1.2f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(0.85f, 0.85f, 1f));
            scale.InsertKeyFrame(0.75f, new Vector3(1.15f, 1.15f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(0.9f, 0.9f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(1000);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Opacity", opacity);
            PlatformCompatibility.StartAnimation(_dotVisual, "Scale", scale);
        }

        private void StartBeaconFlashAnimation()
        {
            if (_dotVisual == null || _compositor == null) return;

            // Avalonia: 2s loop, quick flash at the start then settles back.
            var linear = _compositor.CreateLinearEasingFunction();

            var opacity = _compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 0.3f, linear);
            opacity.InsertKeyFrame(0.05f, 1f, linear);  // 0.1s of 2s
            opacity.InsertKeyFrame(0.15f, 0.3f, linear); // 0.3s of 2s
            opacity.InsertKeyFrame(1f, 0.3f, linear);
            opacity.Duration = TimeSpan.FromMilliseconds(2000);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(_dotVisual, "Opacity", opacity);
        }

        private void StopAnimation()
        {
            _isAnimating = false;
            var dotVisual = _dotVisual;
            _dotVisual = null;
            PlatformCompatibility.StopRotationAnimation(_dotElement, dotVisual, null);
            _dotElement = null;

            foreach (var visual in _activeVisuals)
            {
                visual.StopAnimation("Opacity");
                visual.StopAnimation("Scale");
                visual.StopAnimation("Offset");
                visual.StopAnimation("RotationAngle");
            }

            // Translation is only enabled on the main dot element (see TrySetIsTranslationEnabled(dot, true)).
            // Stopping it on other visuals throws in Uno.
            try
            {
                dotVisual?.StopAnimation("Translation");
            }
            catch
            {
            }

            _activeVisuals.Clear();
            foreach (var storyboard in _activeStoryboards)
            {
                storyboard.Stop();
            }
            _activeStoryboards.Clear();
        }

        private void RestartAnimations()
        {
            // For status glyph variants (Battery, TrafficLight, Signal), they're static - no restart needed
            if (IsStatusGlyphVariant())
                return;

            // Default variant has no animation
            if (Variant == DaisyStatusIndicatorVariant.Default)
                return;

            // Stop any existing animations first
            // (StopAnimation clears visual tracking but not _ringAnimations or _animatedContainer)
            StopAnimation();

            if (_rootGrid == null || _rootGrid.Children.Count == 0)
                return;

            // Find and restore the main dot element
            Ellipse? dot = null;
            foreach (var child in _rootGrid.Children)
            {
                if (child is Ellipse ellipse && ellipse.HorizontalAlignment == HorizontalAlignment.Center)
                {
                    dot = ellipse;
                    break;
                }
            }

            if (dot == null)
                return;

            // Re-initialize for animation
            _dotElement = dot;
            _isAnimating = true;

            var size = _cachedSize > 0 ? _cachedSize : DaisyResourceLookup.GetSizeValue(Size);

            // Re-obtain the visual and restart dot animation
            PlatformCompatibility.TrySetIsTranslationEnabled(dot, true);
            _dotVisual = ElementCompositionPreview.GetElementVisual(dot);
            TrackVisual(_dotVisual);
            _compositor ??= _dotVisual.Compositor;
            _dotVisual.CenterPoint = new Vector3((float)(size / 2), (float)(size / 2), 0);
            StartDotAnimation();

            // Restart ring animations using stored data
            foreach (var ring in _ringAnimations)
            {
                StartRingExpandFadeAnimation(ring.Element, ring.Size, ring.StartOpacity, ring.EndOpacity, ring.EndScale, ring.DurationMs, ring.DelayMs);
            }

            // Restart container animations based on variant
            if (_animatedContainer != null)
            {
                switch (Variant)
                {
                    case DaisyStatusIndicatorVariant.Glow:
                        StartGlowAnimation(_animatedContainer, size);
                        break;
                    case DaisyStatusIndicatorVariant.Radar:
                        StartOrbitRotationAnimation(_animatedContainer, size * 1.1, 2000);
                        break;
                    case DaisyStatusIndicatorVariant.Beacon:
                        StartBeaconRingBurstAnimation(_animatedContainer, size);
                        break;
                    case DaisyStatusIndicatorVariant.Spin:
                        StartOrbitRotationAnimation(_animatedContainer, size, 800);
                        break;
                    case DaisyStatusIndicatorVariant.Orbit:
                        StartOrbitRotationAnimation(_animatedContainer, size, 1300);
                        break;
                }
            }
        }

        private void TrackVisual(Visual visual)
        {
            _activeVisuals.Add(visual);
        }

        private void TrackStoryboard(Storyboard? storyboard)
        {
            if (storyboard != null)
            {
                _activeStoryboards.Add(storyboard);
            }
        }

        private void StartRingExpandFadeAnimation(UIElement element, double elementSize, float startOpacity, float endOpacity, float endScale, int durationMs, int delayMs = 0)
        {
            if (!_isAnimating) return;

            // On Android/iOS, composition animations track time from creation, not from when they're registered.
            // For delayed animations, we must defer the entire creation to after the delay.
            if (delayMs > 0 && PlatformCompatibility.UseManualCompositionAnimations)
            {
                _ = StartRingExpandFadeAnimationDelayed(element, elementSize, startOpacity, endOpacity, endScale, durationMs, delayMs);
                return;
            }

            StartRingExpandFadeAnimationCore(element, elementSize, startOpacity, endOpacity, endScale, durationMs, delayMs);
        }

        private async Task StartRingExpandFadeAnimationDelayed(UIElement element, double elementSize, float startOpacity, float endOpacity, float endScale, int durationMs, int delayMs)
        {
            // Capture the dispatcher BEFORE awaiting (we're on the UI thread now)
            var queue = element.DispatcherQueue;

            try
            {
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            // After ConfigureAwait(false), we're on a background thread - use the captured dispatcher
            queue?.TryEnqueue(() =>
            {
                if (_isAnimating)
                {
                    StartRingExpandFadeAnimationCore(element, elementSize, startOpacity, endOpacity, endScale, durationMs, 0);
                }
            });
        }

        private void StartRingExpandFadeAnimationCore(UIElement element, double elementSize, float startOpacity, float endOpacity, float endScale, int durationMs, int delayMs)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            TrackVisual(visual);
            _compositor ??= visual.Compositor;
            visual.CenterPoint = new Vector3((float)(elementSize / 2), (float)(elementSize / 2), 0);

            var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scaleAnimation.InsertKeyFrame(1f, new Vector3(endScale, endScale, 1f));
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(0f, startOpacity);
            opacityAnimation.InsertKeyFrame(1f, endOpacity);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacityAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            var delay = TimeSpan.FromMilliseconds(Math.Max(0, delayMs));
            PlatformCompatibility.StartAnimation(visual, "Scale", scaleAnimation, delay);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacityAnimation, delay);
        }

        private void StartGlowAnimation(UIElement element, double elementSize)
        {
            if (!_isAnimating) return;

            var visual = ElementCompositionPreview.GetElementVisual(element);
            TrackVisual(visual);
            _compositor ??= visual.Compositor;
            visual.CenterPoint = new Vector3((float)(elementSize / 2), (float)(elementSize / 2), 0);

            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(1.2f, 1.2f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(2.0f, 2.0f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1.2f, 1.2f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(1400);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = _compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 0.12f);
            opacity.InsertKeyFrame(0.5f, 0.28f);
            opacity.InsertKeyFrame(1f, 0.12f);
            opacity.Duration = TimeSpan.FromMilliseconds(1400);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scale);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        private void StartOrbitRotationAnimation(UIElement element, double elementSize, int durationMs)
        {
            if (!_isAnimating) return;

            var visual = ElementCompositionPreview.GetElementVisual(element);
            TrackVisual(visual);
            _compositor ??= visual.Compositor;
            visual.CenterPoint = new Vector3((float)(elementSize), (float)(elementSize), 0);

            if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
            {
                var storyboard = PlatformCompatibility.StartRotationAnimation(element, visual, durationMs / 1000.0);
                TrackStoryboard(storyboard);
                return;
            }

            var rotation = _compositor.CreateScalarKeyFrameAnimation();
            // NOTE: In Uno/WinUI, using an end value of 360 can cause a visible hitch when the
            // animation loops (360° is equivalent to 0°). Ending at 359.99 keeps the loop seamless.
            var linear = _compositor.CreateLinearEasingFunction();
            rotation.InsertKeyFrame(0f, 0f, linear);
            rotation.InsertKeyFrame(1f, PlatformCompatibility.DegreesToRadians(359.99f), linear);
            rotation.Duration = TimeSpan.FromMilliseconds(durationMs);
            rotation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "RotationAngle", rotation);
        }

        private void StartBeaconRingBurstAnimation(UIElement element, double elementSize)
        {
            if (!_isAnimating) return;

            var visual = ElementCompositionPreview.GetElementVisual(element);
            TrackVisual(visual);
            _compositor ??= visual.Compositor;
            visual.CenterPoint = new Vector3((float)(elementSize / 2), (float)(elementSize / 2), 0);

            var linear = _compositor.CreateLinearEasingFunction();

            // Set initial opacity to 0 to prevent flash before animation takes over
            visual.Opacity = 0f;

            var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f), linear);
            scaleAnimation.InsertKeyFrame(0.05f, new Vector3(1.2f, 1.2f, 1f), linear); // 0.1s of 2s
            scaleAnimation.InsertKeyFrame(0.25f, new Vector3(2f, 2f, 1f), linear);     // 0.5s of 2s
            scaleAnimation.InsertKeyFrame(1f, new Vector3(2f, 2f, 1f), linear);
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(2000);
            scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(0f, 0f, linear);
            opacityAnimation.InsertKeyFrame(0.05f, 0.6f, linear);
            opacityAnimation.InsertKeyFrame(0.25f, 0f, linear);
            opacityAnimation.InsertKeyFrame(1f, 0f, linear);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(2000);
            opacityAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scaleAnimation);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacityAnimation);
        }

        private Brush GetColorBrush()
        {
            var resourceKey = Color switch
            {
                DaisyColor.Primary => "DaisyPrimaryBrush",
                DaisyColor.Secondary => "DaisySecondaryBrush",
                DaisyColor.Accent => "DaisyAccentBrush",
                DaisyColor.Neutral => "DaisyNeutralBrush",
                DaisyColor.Info => "DaisyInfoBrush",
                DaisyColor.Success => "DaisySuccessBrush",
                DaisyColor.Warning => "DaisyWarningBrush",
                DaisyColor.Error => "DaisyErrorBrush",
                _ => "DaisyNeutralBrush"
            };

            if (Application.Current.Resources.TryGetValue(resourceKey, out var brush) && brush is Brush b)
            {
                return b;
            }

            // Fallback - green for success
            return new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
        }
    }
}
