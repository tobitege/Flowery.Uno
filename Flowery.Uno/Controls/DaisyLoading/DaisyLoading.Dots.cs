namespace Flowery.Controls
{
    public partial class DaisyLoading
    {
        #region Orbit

        private void BuildOrbitVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var thickness = Math.Max(1, 2.0 * s);
            var dotSize = 4.0 * s;
            var travel = (float)(20.0 * s);

            var container = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            container.Children.Add(new Border
            {
                BorderBrush = brush,
                BorderThickness = new Thickness(thickness),
                CornerRadius = new CornerRadius(2),
                Opacity = 0.15
            });

            var canvas = new Canvas { Width = size, Height = size };
            container.Children.Add(canvas);

            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush,
                    Opacity = 1 - (i * 0.3),
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
                };

                Canvas.SetLeft(dot, 0);
                Canvas.SetTop(dot, 0);

                var index = i;
                dot.Loaded += (s2, e) =>
                {
                    PlatformCompatibility.TrySetIsTranslationEnabled(dot, true);
                    var visual = ElementCompositionPreview.GetElementVisual(dot);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);
                    StartSquareOrbitAnimation(visual, compositor, travel, index);
                };

                canvas.Children.Add(dot);
            }

            _rootGrid.Children.Add(container);
        }

        private void StartSquareOrbitAnimation(Visual visual, Compositor compositor, float travel, int index)
        {
            if (!_isAnimating) return;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.25f, new Vector3(travel, 0f, 0f));
            translation.InsertKeyFrame(0.5f, new Vector3(travel, travel, 0f));
            translation.InsertKeyFrame(0.75f, new Vector3(0f, travel, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(1200);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation, TimeSpan.FromMilliseconds(index * 400));
        }

        #endregion

        #region Snake

        private void BuildSnakeVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var segmentSize = Math.Max(3, 4.0 * s);
            var travel = (float)(20.0 * s);
            var opacities = new[] { 1.0, 0.85, 0.7, 0.55, 0.4 };

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            for (int i = 0; i < 5; i++)
            {
                var segment = new Border
                {
                    Width = segmentSize,
                    Height = segmentSize,
                    Background = brush,
                    CornerRadius = new CornerRadius(Math.Max(0, 1.0 * s)),
                    Opacity = opacities[i],
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
                };

                Canvas.SetLeft(segment, 0);
                Canvas.SetTop(segment, 10.0 * s);

                var index = i;
                segment.Loaded += (s2, e) =>
                {
                    PlatformCompatibility.TrySetIsTranslationEnabled(segment, true);
                    var visual = ElementCompositionPreview.GetElementVisual(segment);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);
                    StartSnakeAnimation(visual, compositor, travel, index);
                };

                canvas.Children.Add(segment);
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartSnakeAnimation(Visual visual, Compositor compositor, float travel, int index)
        {
            if (!_isAnimating) return;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.5f, new Vector3(travel, 0f, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(1600);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation, TimeSpan.FromMilliseconds(index * 320));
        }

        #endregion

        #region Wave

        private void BuildWaveVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var dotSize = 4.0 * s;
            var spacing = 3.0 * s;
            var amplitude = (float)(6.0 * s);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = spacing,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            for (int i = 0; i < 5; i++)
            {
                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush,
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
                };

                var index = i;
                dot.Loaded += (s2, e) =>
                {
                    PlatformCompatibility.TrySetIsTranslationEnabled(dot, true);
                    var visual = ElementCompositionPreview.GetElementVisual(dot);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);
                    StartWaveAnimation(visual, compositor, amplitude, index);
                };

                panel.Children.Add(dot);
            }

            _rootGrid.Children.Add(panel);
        }

        private void StartWaveAnimation(Visual visual, Compositor compositor, float amplitude, int index)
        {
            if (!_isAnimating) return;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.25f, new Vector3(0f, -amplitude, 0f));
            translation.InsertKeyFrame(0.5f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.75f, new Vector3(0f, amplitude, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(1000);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation, TimeSpan.FromMilliseconds(index * 200));
        }

        #endregion

        #region Bounce

        private void BuildBounceVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var cellMargin = 1.0 * s;
            var squareSize = (size / 2) - (2 * cellMargin);

            var grid = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var order = new[] { 0, 1, 3, 2 };

            for (int i = 0; i < 4; i++)
            {
                var square = new Border
                {
                    Width = squareSize,
                    Height = squareSize,
                    Background = brush,
                    Opacity = 0.25,
                    CornerRadius = new CornerRadius(2.0 * s),
                    Margin = new Thickness(cellMargin),
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
                };

                Grid.SetRow(square, i / 2);
                Grid.SetColumn(square, i % 2);

                var sequenceIndex = Array.IndexOf(order, i);
                square.Loaded += (s2, e) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(square);
                    var compositor = visual.Compositor;
                    visual.CenterPoint = new Vector3((float)(squareSize / 2), (float)(squareSize / 2), 0);
                    TrackVisual(visual);
                    StartBounceCellAnimation(visual, compositor, sequenceIndex);
                };

                grid.Children.Add(square);
            }

            _rootGrid.Children.Add(grid);
        }

        private void StartBounceCellAnimation(Visual visual, Compositor compositor, int index)
        {
            if (!_isAnimating) return;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(0.85f, 0.85f, 1f));
            scale.InsertKeyFrame(0.0625f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.1875f, new Vector3(0.85f, 0.85f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(0.85f, 0.85f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(1600);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 0.25f);
            opacity.InsertKeyFrame(0.0625f, 1.0f);
            opacity.InsertKeyFrame(0.1875f, 0.25f);
            opacity.InsertKeyFrame(1f, 0.25f);
            opacity.Duration = TimeSpan.FromMilliseconds(1600);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scale, TimeSpan.FromMilliseconds(index * 400));
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity, TimeSpan.FromMilliseconds(index * 400));
        }

        #endregion

        #region Matrix

        private void BuildMatrixVisual(double size)
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

            var xs = new[] { 0.0, 3.5, 7.0, 12.5, 16.0, 19.5 };
            var ys = new[] { 7.0, 14.5 };

            for (int colon = 1; colon <= 6; colon++)
            {
                for (int row = 0; row < 2; row++)
                {
                    var dot = new Ellipse
                    {
                        Width = dotSize,
                        Height = dotSize,
                        Fill = brush,
                        RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
                    };

                    Canvas.SetLeft(dot, xs[colon - 1] * s);
                    Canvas.SetTop(dot, ys[row] * s);

                    var colonIndex = colon;
                    dot.Loaded += (_, _) =>
                    {
                        var visual = ElementCompositionPreview.GetElementVisual(dot);
                        var compositor = visual.Compositor;
                        TrackVisual(visual);
                        StartMatrixColonOpacityAnimation(visual, compositor, colonIndex);
                    };

                    canvas.Children.Add(dot);
                }
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartMatrixColonOpacityAnimation(Visual visual, Compositor compositor, int colonIndex)
        {
            if (!_isAnimating) return;

            const float durationMs = 1800f;
            var opacity = compositor.CreateScalarKeyFrameAnimation();

            switch (colonIndex)
            {
                case 1:
                    opacity.InsertKeyFrame(0f / durationMs, 1f);
                    opacity.InsertKeyFrame(150f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1550f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(1f, 1f);
                    break;
                case 2:
                    opacity.InsertKeyFrame(0f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(100f / durationMs, 0.85f);
                    opacity.InsertKeyFrame(250f / durationMs, 1f);
                    opacity.InsertKeyFrame(400f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(550f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1550f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.6f);
                    break;
                case 3:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(250f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(350f / durationMs, 0.85f);
                    opacity.InsertKeyFrame(500f / durationMs, 1f);
                    opacity.InsertKeyFrame(650f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(800f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 4:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(500f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(600f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(750f / durationMs, 1f);
                    opacity.InsertKeyFrame(900f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(1050f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 5:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(750f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(850f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(1000f / durationMs, 1f);
                    opacity.InsertKeyFrame(1150f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(1300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 6:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1000f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1100f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(1250f / durationMs, 1f);
                    opacity.InsertKeyFrame(1400f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(1550f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
            }

            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region MatrixInward

        private void BuildMatrixInwardVisual(double size)
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

            var xs = new[] { 0.0, 3.5, 7.0, 12.5, 16.0, 19.5 };
            var ys = new[] { 7.0, 14.5 };

            for (int group = 1; group <= 6; group++)
            {
                for (int row = 0; row < 2; row++)
                {
                    var dot = new Ellipse
                    {
                        Width = dotSize,
                        Height = dotSize,
                        Fill = brush
                    };

                    Canvas.SetLeft(dot, xs[group - 1] * s);
                    Canvas.SetTop(dot, ys[row] * s);

                    var groupIndex = group;
                    dot.Loaded += (_, _) =>
                    {
                        var visual = ElementCompositionPreview.GetElementVisual(dot);
                        var compositor = visual.Compositor;
                        TrackVisual(visual);
                        StartMatrixInwardOpacityAnimation(visual, compositor, groupIndex);
                    };

                    canvas.Children.Add(dot);
                }
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartMatrixInwardOpacityAnimation(Visual visual, Compositor compositor, int groupIndex)
        {
            if (!_isAnimating) return;

            const float durationMs = 1200f;
            var opacity = compositor.CreateScalarKeyFrameAnimation();

            // Inner first: (3,4) -> (2,5) -> (1,6)
            if (groupIndex == 3 || groupIndex == 4)
            {
                opacity.InsertKeyFrame(0f / durationMs, 1f);
                opacity.InsertKeyFrame(150f / durationMs, 0.6f);
                opacity.InsertKeyFrame(300f / durationMs, 0.2f);
                opacity.InsertKeyFrame(900f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1050f / durationMs, 0.6f);
                opacity.InsertKeyFrame(1f, 1f);
            }
            else if (groupIndex == 2 || groupIndex == 5)
            {
                opacity.InsertKeyFrame(0f / durationMs, 0.6f);
                opacity.InsertKeyFrame(100f / durationMs, 0.85f);
                opacity.InsertKeyFrame(200f / durationMs, 1f);
                opacity.InsertKeyFrame(350f / durationMs, 0.6f);
                opacity.InsertKeyFrame(500f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1000f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1f, 0.6f);
            }
            else
            {
                opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                opacity.InsertKeyFrame(200f / durationMs, 0.6f);
                opacity.InsertKeyFrame(300f / durationMs, 0.85f);
                opacity.InsertKeyFrame(400f / durationMs, 1f);
                opacity.InsertKeyFrame(550f / durationMs, 0.6f);
                opacity.InsertKeyFrame(700f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1f, 0.2f);
            }

            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region MatrixOutward

        private void BuildMatrixOutwardVisual(double size)
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

            var xs = new[] { 0.0, 3.5, 7.0, 12.5, 16.0, 19.5 };
            var ys = new[] { 7.0, 14.5 };

            for (int group = 1; group <= 6; group++)
            {
                for (int row = 0; row < 2; row++)
                {
                    var dot = new Ellipse
                    {
                        Width = dotSize,
                        Height = dotSize,
                        Fill = brush
                    };

                    Canvas.SetLeft(dot, xs[group - 1] * s);
                    Canvas.SetTop(dot, ys[row] * s);

                    var groupIndex = group;
                    dot.Loaded += (_, _) =>
                    {
                        var visual = ElementCompositionPreview.GetElementVisual(dot);
                        var compositor = visual.Compositor;
                        TrackVisual(visual);
                        StartMatrixOutwardOpacityAnimation(visual, compositor, groupIndex);
                    };

                    canvas.Children.Add(dot);
                }
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartMatrixOutwardOpacityAnimation(Visual visual, Compositor compositor, int groupIndex)
        {
            if (!_isAnimating) return;

            const float durationMs = 1200f;
            var opacity = compositor.CreateScalarKeyFrameAnimation();

            // Outer first: (1,6) -> (2,5) -> (3,4)
            if (groupIndex == 1 || groupIndex == 6)
            {
                opacity.InsertKeyFrame(0f / durationMs, 1f);
                opacity.InsertKeyFrame(150f / durationMs, 0.6f);
                opacity.InsertKeyFrame(300f / durationMs, 0.2f);
                opacity.InsertKeyFrame(900f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1050f / durationMs, 0.6f);
                opacity.InsertKeyFrame(1f, 1f);
            }
            else if (groupIndex == 2 || groupIndex == 5)
            {
                opacity.InsertKeyFrame(0f / durationMs, 0.6f);
                opacity.InsertKeyFrame(100f / durationMs, 0.85f);
                opacity.InsertKeyFrame(200f / durationMs, 1f);
                opacity.InsertKeyFrame(350f / durationMs, 0.6f);
                opacity.InsertKeyFrame(500f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1000f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1f, 0.6f);
            }
            else
            {
                opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                opacity.InsertKeyFrame(200f / durationMs, 0.6f);
                opacity.InsertKeyFrame(300f / durationMs, 0.85f);
                opacity.InsertKeyFrame(400f / durationMs, 1f);
                opacity.InsertKeyFrame(550f / durationMs, 0.6f);
                opacity.InsertKeyFrame(700f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1f, 0.2f);
            }

            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region MatrixVertical

        private void BuildMatrixVerticalVisual(double size)
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

            var xs = new[] { 0.0, 3.5, 7.0, 12.5, 16.0, 19.5 };

            for (int col = 0; col < xs.Length; col++)
            {
                // Top
                var topDot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush
                };
                Canvas.SetLeft(topDot, xs[col] * s);
                Canvas.SetTop(topDot, 7.0 * s);
                topDot.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(topDot);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    StartMatrixVerticalOpacityAnimation(visual, compositor, isTopRow: true);
                };
                canvas.Children.Add(topDot);

                // Bottom
                var bottomDot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = brush
                };
                Canvas.SetLeft(bottomDot, xs[col] * s);
                Canvas.SetTop(bottomDot, 14.5 * s);
                bottomDot.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(bottomDot);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    StartMatrixVerticalOpacityAnimation(visual, compositor, isTopRow: false);
                };
                canvas.Children.Add(bottomDot);
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartMatrixVerticalOpacityAnimation(Visual visual, Compositor compositor, bool isTopRow)
        {
            if (!_isAnimating) return;

            const float durationMs = 1000f;
            var opacity = compositor.CreateScalarKeyFrameAnimation();

            if (isTopRow)
            {
                opacity.InsertKeyFrame(0f / durationMs, 1f);
                opacity.InsertKeyFrame(200f / durationMs, 0.6f);
                opacity.InsertKeyFrame(400f / durationMs, 0.2f);
                opacity.InsertKeyFrame(800f / durationMs, 0.2f);
                opacity.InsertKeyFrame(900f / durationMs, 0.6f);
                opacity.InsertKeyFrame(1f, 1f);
            }
            else
            {
                opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                opacity.InsertKeyFrame(300f / durationMs, 0.6f);
                opacity.InsertKeyFrame(500f / durationMs, 1f);
                opacity.InsertKeyFrame(700f / durationMs, 0.6f);
                opacity.InsertKeyFrame(900f / durationMs, 0.2f);
                opacity.InsertKeyFrame(1f, 0.2f);
            }

            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion
    }
}
