namespace Flowery.Controls
{
    public partial class DaisyLoading
    {
        #region MatrixRain

        private void BuildMatrixRainVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var dotSize = 3.0 * s;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) }
            };

            var columns = new[] { 1.0, 7.0, 13.0, 19.0 };

            // Each column has a lead dot (a) and a trail dot (b)
            for (int i = 0; i < columns.Length; i++)
            {
                var x = columns[i] * s;

                var a = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush
                };
                Canvas.SetLeft(a, x);
                Canvas.SetTop(a, 0);

                var b = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush,
                    Opacity = 0.5
                };
                Canvas.SetLeft(b, x);
                Canvas.SetTop(b, 0);

                canvas.Children.Add(a);
                canvas.Children.Add(b);

                var columnIndex = i;
                a.Loaded += (_, _) =>
                {
                    PlatformCompatibility.TrySetIsTranslationEnabled(a, true);
                    var visual = ElementCompositionPreview.GetElementVisual(a);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);
                    StartMatrixRainDropAnimation(visual, compositor, columnIndex, isTrail: false, scale: (float)s);
                };

                b.Loaded += (_, _) =>
                {
                    PlatformCompatibility.TrySetIsTranslationEnabled(b, true);
                    var visual = ElementCompositionPreview.GetElementVisual(b);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);
                    StartMatrixRainDropAnimation(visual, compositor, columnIndex, isTrail: true, scale: (float)s);
                };
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartMatrixRainDropAnimation(Visual visual, Compositor compositor, int columnIndex, bool isTrail, float scale)
        {
            if (!_isAnimating) return;

            float durationMs;
            int delayMs;
            float startOpacity;
            float endOpacity;

            // Column: 0..3
            switch (columnIndex)
            {
                case 0:
                    durationMs = 800f;
                    delayMs = isTrail ? 100 : 0;
                    break;
                case 1:
                    durationMs = 1100f;
                    delayMs = isTrail ? 300 : 200;
                    break;
                case 2:
                    durationMs = 900f;
                    delayMs = isTrail ? 500 : 400;
                    break;
                default:
                    durationMs = 700f;
                    delayMs = isTrail ? 250 : 150;
                    break;
            }

            startOpacity = isTrail ? 0.5f : 1f;
            endOpacity = isTrail ? 0f : 0.3f;

            var travelEndMs = durationMs - 100f;
            var travelEndProgress = travelEndMs / durationMs;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, -3f * scale, 0f));
            translation.InsertKeyFrame(travelEndProgress, new Vector3(0f, 24f * scale, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, -3f * scale, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(durationMs);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, startOpacity);
            opacity.InsertKeyFrame(travelEndProgress, endOpacity);
            opacity.InsertKeyFrame(1f, startOpacity);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity, TimeSpan.FromMilliseconds(delayMs));
        }

        #endregion

        #region BitFlip

        private void BuildBitFlipVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var dotSize = 4.0 * s;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var positions = new[]
            {
                (2.0, 6.0), (8.0, 6.0), (14.0, 6.0), (20.0, 6.0),
                (2.0, 14.0), (8.0, 14.0), (14.0, 14.0), (20.0, 14.0)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var (x, y) = positions[i];
                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush
                };

                Canvas.SetLeft(dot, x * s);
                Canvas.SetTop(dot, y * s);

                var bitIndex = i + 1;
                dot.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(dot);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    StartBitFlipOpacityAnimation(visual, compositor, bitIndex);
                };

                canvas.Children.Add(dot);
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartBitFlipOpacityAnimation(Visual visual, Compositor compositor, int bitIndex)
        {
            if (!_isAnimating) return;

            const float durationMs = 1600f;
            var opacity = compositor.CreateScalarKeyFrameAnimation();

            switch (bitIndex)
            {
                case 1:
                    opacity.InsertKeyFrame(0f / durationMs, 1f);
                    opacity.InsertKeyFrame(200f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(800f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1000f / durationMs, 1f);
                    opacity.InsertKeyFrame(1f, 1f);
                    break;
                case 2:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(400f / durationMs, 1f);
                    opacity.InsertKeyFrame(600f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1200f / durationMs, 1f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 3:
                    opacity.InsertKeyFrame(0f / durationMs, 1f);
                    opacity.InsertKeyFrame(300f / durationMs, 1f);
                    opacity.InsertKeyFrame(500f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1100f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1300f / durationMs, 1f);
                    opacity.InsertKeyFrame(1f, 1f);
                    break;
                case 4:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(200f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(400f / durationMs, 1f);
                    opacity.InsertKeyFrame(1000f / durationMs, 1f);
                    opacity.InsertKeyFrame(1200f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 5:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(100f / durationMs, 1f);
                    opacity.InsertKeyFrame(700f / durationMs, 1f);
                    opacity.InsertKeyFrame(900f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1400f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 6:
                    opacity.InsertKeyFrame(0f / durationMs, 1f);
                    opacity.InsertKeyFrame(500f / durationMs, 1f);
                    opacity.InsertKeyFrame(700f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(900f / durationMs, 1f);
                    opacity.InsertKeyFrame(1300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 1f);
                    break;
                case 7:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(500f / durationMs, 1f);
                    opacity.InsertKeyFrame(800f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1100f / durationMs, 1f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                default:
                    opacity.InsertKeyFrame(0f / durationMs, 1f);
                    opacity.InsertKeyFrame(200f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(600f / durationMs, 1f);
                    opacity.InsertKeyFrame(1000f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1400f / durationMs, 1f);
                    opacity.InsertKeyFrame(1f, 1f);
                    break;
            }

            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region PacketBurst

        private void BuildPacketBurstVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var centerSize = 4.0 * s;
            var burstSize = 3.0 * s;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var center = new Ellipse
            {
                Width = centerSize,
                Height = centerSize,
                Fill = brush,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };
            Canvas.SetLeft(center, 10.0 * s);
            Canvas.SetTop(center, 10.0 * s);

            canvas.Children.Add(center);

            center.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(center);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(centerSize / 2), (float)(centerSize / 2), 0);
                TrackVisual(visual);
                StartPacketBurstCenterAnimation(visual, compositor);
            };

            var burstStartX = 10.5 * s;
            var burstStartY = 10.5 * s;
            var burstTravel = 10.5 * s;

            BuildPacketBurstDot(canvas, brush, burstSize, burstStartX, burstStartY, dx: -(float)burstTravel, dy: 0f);
            BuildPacketBurstDot(canvas, brush, burstSize, burstStartX, burstStartY, dx: (float)burstTravel, dy: 0f);
            BuildPacketBurstDot(canvas, brush, burstSize, burstStartX, burstStartY, dx: 0f, dy: -(float)burstTravel);
            BuildPacketBurstDot(canvas, brush, burstSize, burstStartX, burstStartY, dx: 0f, dy: (float)burstTravel);

            _rootGrid.Children.Add(canvas);
        }

        private void BuildPacketBurstDot(Canvas canvas, Brush brush, double dotSize, double startX, double startY, float dx, float dy)
        {
            var dot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = brush,
                Opacity = 0
            };

            Canvas.SetLeft(dot, startX);
            Canvas.SetTop(dot, startY);

            dot.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(dot, true);
                var visual = ElementCompositionPreview.GetElementVisual(dot);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartPacketBurstDotAnimation(visual, compositor, dx, dy);
            };

            canvas.Children.Add(dot);
        }

        private void StartPacketBurstCenterAnimation(Visual visual, Compositor compositor)
        {
            if (!_isAnimating) return;

            const float durationMs = 1200f;
            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f / durationMs, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(100f / durationMs, new Vector3(1.5f, 1.5f, 1f));
            scale.InsertKeyFrame(300f / durationMs, new Vector3(0.8f, 0.8f, 1f));
            scale.InsertKeyFrame(900f / durationMs, new Vector3(0.8f, 0.8f, 1f));
            scale.InsertKeyFrame(1000f / durationMs, new Vector3(1.3f, 1.3f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(durationMs);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scale);
        }

        private void StartPacketBurstDotAnimation(Visual visual, Compositor compositor, float dx, float dy)
        {
            if (!_isAnimating) return;

            const float durationMs = 1200f;
            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f / durationMs, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(100f / durationMs, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(400f / durationMs, new Vector3(dx, dy, 0f));
            translation.InsertKeyFrame(500f / durationMs, new Vector3(dx, dy, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(durationMs);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f / durationMs, 0f);
            opacity.InsertKeyFrame(100f / durationMs, 1f);
            opacity.InsertKeyFrame(400f / durationMs, 0.5f);
            opacity.InsertKeyFrame(500f / durationMs, 0f);
            opacity.InsertKeyFrame(1f, 0f);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region CometTrail

        private void BuildCometTrailVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            BuildCometDot(canvas, brush, size: 2.0 * s, opacity: 0.15, startX: 11.0 * s, startY: 3.0 * s, delayMs: 300, s: (float)s);
            BuildCometDot(canvas, brush, size: 2.5 * s, opacity: 0.3, startX: 10.75 * s, startY: 2.75 * s, delayMs: 200, s: (float)s);
            BuildCometDot(canvas, brush, size: 3.0 * s, opacity: 0.5, startX: 10.5 * s, startY: 2.5 * s, delayMs: 100, s: (float)s);
            BuildCometDot(canvas, brush, size: 4.0 * s, opacity: 1.0, startX: 10.0 * s, startY: 2.0 * s, delayMs: 0, s: (float)s);

            _rootGrid.Children.Add(canvas);
        }

        private void BuildCometDot(Canvas canvas, Brush brush, double size, double opacity, double startX, double startY, int delayMs, float s)
        {
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = brush,
                Opacity = opacity
            };

            Canvas.SetLeft(dot, startX);
            Canvas.SetTop(dot, startY);

            dot.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(dot, true);
                var visual = ElementCompositionPreview.GetElementVisual(dot);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartCometTrailAnimation(visual, compositor, delayMs, s);
            };

            canvas.Children.Add(dot);
        }

        private void StartCometTrailAnimation(Visual visual, Compositor compositor, int delayMs, float scale)
        {
            if (!_isAnimating) return;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.25f, new Vector3(8f * scale, 8f * scale, 0f));
            translation.InsertKeyFrame(0.5f, new Vector3(0f, 16f * scale, 0f));
            translation.InsertKeyFrame(0.75f, new Vector3(-8f * scale, 8f * scale, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(1500);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation, TimeSpan.FromMilliseconds(delayMs));
        }

        #endregion

        #region RippleMatrix

        private void BuildRippleMatrixVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var center = new Ellipse { Width = 4.0 * s, Height = 4.0 * s, Fill = brush };
            Canvas.SetLeft(center, 10.0 * s);
            Canvas.SetTop(center, 10.0 * s);
            center.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(center);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                StartRippleMatrixOpacityAnimation(visual, compositor, RippleGroup.Center);
            };
            canvas.Children.Add(center);

            // Ring 1
            AddRippleDot(canvas, brush, 3.0 * s, 10.5 * s, 3.0 * s, RippleGroup.Ring1);
            AddRippleDot(canvas, brush, 3.0 * s, 10.5 * s, 18.0 * s, RippleGroup.Ring1);
            AddRippleDot(canvas, brush, 3.0 * s, 3.0 * s, 10.5 * s, RippleGroup.Ring1);
            AddRippleDot(canvas, brush, 3.0 * s, 18.0 * s, 10.5 * s, RippleGroup.Ring1);

            // Ring 2
            AddRippleDot(canvas, brush, 2.5 * s, 3.0 * s, 3.0 * s, RippleGroup.Ring2);
            AddRippleDot(canvas, brush, 2.5 * s, 18.5 * s, 3.0 * s, RippleGroup.Ring2);
            AddRippleDot(canvas, brush, 2.5 * s, 3.0 * s, 18.5 * s, RippleGroup.Ring2);
            AddRippleDot(canvas, brush, 2.5 * s, 18.5 * s, 18.5 * s, RippleGroup.Ring2);

            _rootGrid.Children.Add(canvas);
        }

        private void AddRippleDot(Canvas canvas, Brush brush, double dotSize, double left, double top, RippleGroup group)
        {
            var dot = new Ellipse { Width = dotSize, Height = dotSize, Fill = brush };
            Canvas.SetLeft(dot, left);
            Canvas.SetTop(dot, top);
            dot.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(dot);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                StartRippleMatrixOpacityAnimation(visual, compositor, group);
            };
            canvas.Children.Add(dot);
        }

        private void StartRippleMatrixOpacityAnimation(Visual visual, Compositor compositor, RippleGroup group)
        {
            if (!_isAnimating) return;

            const float durationMs = 1200f;
            var opacity = compositor.CreateScalarKeyFrameAnimation();

            switch (group)
            {
                case RippleGroup.Center:
                    opacity.InsertKeyFrame(0f / durationMs, 1f);
                    opacity.InsertKeyFrame(200f / durationMs, 0.3f);
                    opacity.InsertKeyFrame(1000f / durationMs, 0.3f);
                    opacity.InsertKeyFrame(1f, 1f);
                    break;
                case RippleGroup.Ring1:
                    opacity.InsertKeyFrame(0f / durationMs, 0.3f);
                    opacity.InsertKeyFrame(150f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(300f / durationMs, 1f);
                    opacity.InsertKeyFrame(500f / durationMs, 0.3f);
                    opacity.InsertKeyFrame(1f, 0.3f);
                    break;
                default:
                    opacity.InsertKeyFrame(0f / durationMs, 0.3f);
                    opacity.InsertKeyFrame(400f / durationMs, 0.3f);
                    opacity.InsertKeyFrame(500f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(600f / durationMs, 1f);
                    opacity.InsertKeyFrame(800f / durationMs, 0.3f);
                    opacity.InsertKeyFrame(1f, 0.3f);
                    break;
            }

            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region CountdownSpinner

        private void BuildCountdownSpinnerVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var dotSize = 2.5 * s;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var positions = new[]
            {
                (10.75, 1.0), (15.5, 2.5), (19.0, 6.0), (20.5, 10.75),
                (19.0, 15.5), (15.5, 19.0), (10.75, 20.5), (6.0, 19.0),
                (2.5, 15.5), (1.0, 10.75), (2.5, 6.0), (6.0, 2.5)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var (x, y) = positions[i];
                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush,
                    Opacity = 1
                };

                Canvas.SetLeft(dot, x * s);
                Canvas.SetTop(dot, y * s);

                var delayMs = i * 100;
                dot.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(dot);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    visual.Opacity = 0.08f;
                    StartCountdownSpinnerOpacityAnimation(visual, compositor, delayMs);
                };

                canvas.Children.Add(dot);
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartCountdownSpinnerOpacityAnimation(Visual visual, Compositor compositor, int delayMs)
        {
            if (!_isAnimating) return;

            var linear = compositor.CreateLinearEasingFunction();

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f, linear);
            opacity.InsertKeyFrame(0.12f, 1f, linear);
            opacity.InsertKeyFrame(0.13f, 0.08f, linear);
            opacity.InsertKeyFrame(1f, 0.08f, linear);
            opacity.Duration = TimeSpan.FromMilliseconds(1200);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity, TimeSpan.FromMilliseconds(delayMs));
        }

        #endregion
    }
}
