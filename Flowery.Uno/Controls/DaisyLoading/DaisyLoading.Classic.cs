namespace Flowery.Controls
{
    public partial class DaisyLoading
    {
        #region Spinner

        private void BuildSpinnerVisual(double size)
        {
            if (_rootGrid == null) return;

            var thickness = Math.Max(2, size / 8);
            var brush = GetColorBrush();

            // Create a partial ring using Path
            var path = new Path
            {
                Stroke = brush,
                StrokeThickness = thickness,
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };
            PlatformCompatibility.SafeSetRoundedStroke(path);

            // Create arc path data
            var radius = (size - thickness) / 2;
            var center = size / 2;

            // Arc from 0 to 270 degrees
            var startX = center + radius;
            var endY = center - radius;

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = new Windows.Foundation.Point(startX, center) };

            // First arc segment (0 to 180)
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = new Windows.Foundation.Point(center - radius, center),
                Size = new Windows.Foundation.Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = true
            });

            // Second arc segment (180 to 270)
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = new Windows.Foundation.Point(center, endY),
                Size = new Windows.Foundation.Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = false
            });

            pathGeometry.Figures.Add(pathFigure);
            path.Data = pathGeometry;

            _rootGrid.Children.Add(path);

            // Set up composition animation after element is in tree
            path.Loaded += (_, _) =>
            {
                _spinnerElement = path;
                _spinnerVisual = ElementCompositionPreview.GetElementVisual(path);
                _compositor = _spinnerVisual.Compositor;
                _spinnerVisual.CenterPoint = new Vector3((float)(size / 2), (float)(size / 2), 0);
                TrackVisual(_spinnerVisual);
                StartSpinnerAnimation();
            };
        }

        private void StartSpinnerAnimation()
        {
            if (_spinnerVisual == null || _spinnerElement == null || !_isAnimating) return;

            var storyboard = PlatformCompatibility.StartRotationAnimation(_spinnerElement, _spinnerVisual, 0.75);
            TrackStoryboard(storyboard);
        }

        #endregion

        #region Ring

        private void BuildRingVisual(double size)
        {
            if (_rootGrid == null) return;

            var thickness = Math.Max(2, size / 8);
            var brush = GetColorBrush();

            var ringBackground = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = brush,
                StrokeThickness = thickness,
                Opacity = 0.2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var path = new Path
            {
                Stroke = brush,
                StrokeThickness = thickness,
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };
            PlatformCompatibility.SafeSetRoundedStroke(path);

            var radius = (size - thickness) / 2;
            var center = size / 2;

            var startX = center + radius;
            var endY = center - radius;

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = new Windows.Foundation.Point(startX, center) };
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = new Windows.Foundation.Point(center, endY),
                Size = new Windows.Foundation.Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = false
            });
            pathGeometry.Figures.Add(pathFigure);
            path.Data = pathGeometry;

            _rootGrid.Children.Add(ringBackground);
            _rootGrid.Children.Add(path);

            path.Loaded += (_, _) =>
            {
                _spinnerElement = path;
                _spinnerVisual = ElementCompositionPreview.GetElementVisual(path);
                _compositor = _spinnerVisual.Compositor;
                _spinnerVisual.CenterPoint = new Vector3((float)(size / 2), (float)(size / 2), 0);
                TrackVisual(_spinnerVisual);
                StartSpinnerAnimation();
            };
        }

        #endregion

        #region Dots

        private void BuildDotsVisual(double size)
        {
            if (_rootGrid == null) return;

            var dotSize = size / 4;
            var spacing = dotSize / 2;
            var brush = GetColorBrush();

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = spacing,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush,
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
                };

                var index = i;
                dot.Loaded += (_, _) =>
                {
                    PlatformCompatibility.TrySetIsTranslationEnabled(dot, true);
                    var visual = ElementCompositionPreview.GetElementVisual(dot);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);
                    var bounce = (float)(size * (6.0 / 24.0));
                    StartDotBounceAnimation(visual, compositor, index, bounce);
                };

                panel.Children.Add(dot);
            }

            _rootGrid.Children.Add(panel);
        }

        private void StartDotBounceAnimation(Visual visual, Compositor compositor, int index, float bounce)
        {
            if (!_isAnimating) return;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.25f, new Vector3(0f, -bounce, 0f));
            translation.InsertKeyFrame(0.5f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(600);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation, TimeSpan.FromMilliseconds(index * 200));
        }

        #endregion

        #region Pulse

        private void BuildPulseVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();

            var ringThickness = Math.Max(1, size * (2.0 / 24.0));

            var ring1 = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = brush,
                StrokeThickness = ringThickness,
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };

            var ring2 = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = brush,
                StrokeThickness = ringThickness,
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };

            var centerDotSize = size * (8.0 / 24.0);
            var centerDot = new Ellipse
            {
                Width = centerDotSize,
                Height = centerDotSize,
                Fill = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };

            _rootGrid.Children.Add(ring1);
            _rootGrid.Children.Add(ring2);
            _rootGrid.Children.Add(centerDot);

            ring1.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(ring1);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(size / 2), (float)(size / 2), 0);
                TrackVisual(visual);
                StartPulseRingAnimation(visual, compositor, delayMs: 0);
            };

            ring2.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(ring2);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(size / 2), (float)(size / 2), 0);
                TrackVisual(visual);
                StartPulseRingAnimation(visual, compositor, delayMs: 500);
            };

            centerDot.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(centerDot);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(centerDotSize / 2), (float)(centerDotSize / 2), 0);
                TrackVisual(visual);
                StartPulseCenterAnimation(visual, compositor);
            };
        }

        private void StartPulseRingAnimation(Visual visual, Compositor compositor, int delayMs)
        {
            if (!_isAnimating) return;

            var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(0f, new Vector3(0.3f, 0.3f, 1f));
            scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(1500);
            scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(0f, 0.8f);
            opacityAnimation.InsertKeyFrame(1f, 0f);
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(1500);
            opacityAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", opacityAnimation, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Scale", scaleAnimation, TimeSpan.FromMilliseconds(delayMs));
        }

        private void StartPulseCenterAnimation(Visual visual, Compositor compositor)
        {
            if (!_isAnimating) return;

            var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scaleAnimation.InsertKeyFrame(0.5f, new Vector3(1.3f, 1.3f, 1f));
            scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(1500);
            scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scaleAnimation);
        }

        #endregion

        #region Ball

        private void BuildBallVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var ballSize = size * (10.0 / 24.0);
            var bounce = size * (10.0 / 24.0);

            var ball = new Ellipse
            {
                Width = ballSize,
                Height = ballSize,
                Fill = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };

            ball.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(ball, true);
                var visual = ElementCompositionPreview.GetElementVisual(ball);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(ballSize / 2), (float)(ballSize / 2), 0);
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartBallBounceAnimation(visual, compositor, (float)bounce);
            };

            _rootGrid.Children.Add(ball);
        }

        private void StartBallBounceAnimation(Visual visual, Compositor compositor, float bounce)
        {
            if (!_isAnimating) return;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.41666666f, new Vector3(0f, -bounce, 0f));
            translation.InsertKeyFrame(0.8333333f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(600);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.41666666f, new Vector3(0.9f, 1.1f, 1f));
            scale.InsertKeyFrame(0.8333333f, new Vector3(1.2f, 0.8f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(600);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
            PlatformCompatibility.StartAnimation(visual, "Scale", scale);
        }

        #endregion

        #region Bars

        private void BuildBarsVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var barWidth = Math.Max(3, size * (4.0 / 24.0));
            var barHeight = size * (12.0 / 24.0);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = barWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            for (int i = 0; i < 3; i++)
            {
                var bar = new Border
                {
                    Width = barWidth,
                    Height = barHeight,
                    Background = brush,
                    CornerRadius = new CornerRadius(barWidth / 2),
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 1.0)
                };

                var index = i;
                bar.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(bar);
                    var compositor = visual.Compositor;
                    visual.CenterPoint = new Vector3((float)(barWidth / 2), (float)barHeight, 0);
                    TrackVisual(visual);
                    StartBarScaleAnimation(visual, compositor, index);
                };

                panel.Children.Add(bar);
            }

            _rootGrid.Children.Add(panel);
        }

        private void StartBarScaleAnimation(Visual visual, Compositor compositor, int index)
        {
            if (!_isAnimating) return;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(1f, 0.4f, 1f));
            scale.InsertKeyFrame(0.25f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(1f, 0.4f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1f, 0.4f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(800);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scale, TimeSpan.FromMilliseconds(index * (800.0 / 3.0)));
        }

        #endregion

        #region Infinity

        private void BuildInfinityVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var thickness = Math.Max(2, size / 10);

            var loopWidth = size * 0.5;
            var loopHeight = size * 0.35;
            var y = (size - loopHeight) / 2;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var left = new Ellipse
            {
                Width = loopWidth,
                Height = loopHeight,
                Stroke = brush,
                StrokeThickness = thickness
            };
            Canvas.SetLeft(left, size * 0.1);
            Canvas.SetTop(left, y);

            var right = new Ellipse
            {
                Width = loopWidth,
                Height = loopHeight,
                Stroke = brush,
                StrokeThickness = thickness
            };
            Canvas.SetLeft(right, size * 0.4);
            Canvas.SetTop(right, y);

            canvas.Children.Add(left);
            canvas.Children.Add(right);
            _rootGrid.Children.Add(canvas);

            left.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(left);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                visual.Opacity = 0.35f;
                StartInfinityOpacityAnimation(visual, compositor, delayMs: 0);
            };

            right.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(right);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                visual.Opacity = 0.35f;
                StartInfinityOpacityAnimation(visual, compositor, delayMs: 600);
            };
        }

        private void StartInfinityOpacityAnimation(Visual visual, Compositor compositor, int delayMs)
        {
            if (!_isAnimating) return;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 0.1f);
            opacity.InsertKeyFrame(0.5f, 1f);
            opacity.InsertKeyFrame(1f, 0.1f);
            opacity.Duration = TimeSpan.FromMilliseconds(1200);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity, TimeSpan.FromMilliseconds(delayMs));
        }

        #endregion
    }
}
