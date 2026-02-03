namespace Flowery.Controls
{
    public partial class DaisyLoading
    {
        #region Hourglass

        private void BuildHourglassVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;

            var grid = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var outline = new Path
            {
                Stroke = brush,
                StrokeThickness = Math.Max(1, 1.5 * s),
                Opacity = 0.4,
                Data = CreateClosedPathGeometry(s, [
                    (4.0, 2.0), (20.0, 2.0), (20.0, 4.0), (14.0, 10.0), (14.0, 14.0),
                    (20.0, 20.0), (20.0, 22.0), (4.0, 22.0), (4.0, 20.0), (10.0, 14.0),
                    (10.0, 10.0), (4.0, 4.0)
                ])
            };

            var sandTop = new Path
            {
                Fill = brush,
                Data = CreateClosedPathGeometry(s, [
                    (5.0, 3.0), (19.0, 3.0), (19.0, 4.0), (13.0, 10.0), (11.0, 10.0), (5.0, 4.0)
                ])
            };

            var sandBottom = new Path
            {
                Fill = brush,
                Data = CreateClosedPathGeometry(s, [
                    (11.0, 14.0), (13.0, 14.0), (19.0, 20.0), (19.0, 21.0), (5.0, 21.0), (5.0, 20.0)
                ])
            };

            var stream = new Border
            {
                Width = Math.Max(1, 2.0 * s),
                Height = Math.Max(2, 4.0 * s),
                Background = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(sandTop);
            grid.Children.Add(stream);
            grid.Children.Add(sandBottom);
            grid.Children.Add(outline);
            _rootGrid.Children.Add(grid);

            sandTop.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(sandTop);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(size / 2), (float)(10.0 * s), 0);
                visual.Scale = new Vector3(1f, 1f, 1f);
                TrackVisual(visual);
                StartHourglassSandAnimation(visual, compositor, isTop: true);
            };

            sandBottom.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(sandBottom);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(size / 2), (float)(21.0 * s), 0);
                visual.Scale = new Vector3(0.3f, 0.1f, 1f);
                TrackVisual(visual);
                StartHourglassSandAnimation(visual, compositor, isTop: false);
            };

            stream.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(stream);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                StartHourglassStreamOpacityAnimation(visual, compositor);
            };
        }

        private void StartHourglassSandAnimation(Visual visual, Compositor compositor, bool isTop)
        {
            if (!_isAnimating) return;

            const float durationMs = 2000f;
            var linear = compositor.CreateLinearEasingFunction();
            var scale = compositor.CreateVector3KeyFrameAnimation();

            if (isTop)
            {
                scale.InsertKeyFrame(0f / durationMs, new Vector3(1f, 1f, 1f), linear);
                scale.InsertKeyFrame(1800f / durationMs, new Vector3(0.3f, 0.1f, 1f), linear);
                scale.InsertKeyFrame(1900f / durationMs, new Vector3(0.3f, 0.1f, 1f), linear);
                scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f), linear);
            }
            else
            {
                scale.InsertKeyFrame(0f / durationMs, new Vector3(0.3f, 0.1f, 1f), linear);
                scale.InsertKeyFrame(1800f / durationMs, new Vector3(1f, 1f, 1f), linear);
                scale.InsertKeyFrame(1900f / durationMs, new Vector3(1f, 1f, 1f), linear);
                scale.InsertKeyFrame(1f, new Vector3(0.3f, 0.1f, 1f), linear);
            }

            scale.Duration = TimeSpan.FromMilliseconds(durationMs);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;
            PlatformCompatibility.StartAnimation(visual, "Scale", scale);
        }

        private void StartHourglassStreamOpacityAnimation(Visual visual, Compositor compositor)
        {
            if (!_isAnimating) return;

            const float durationMs = 2000f;
            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f / durationMs, 1f);
            opacity.InsertKeyFrame(1700f / durationMs, 1f);
            opacity.InsertKeyFrame(1900f / durationMs, 0f);
            opacity.InsertKeyFrame(1f, 1f);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region SignalSweep

        private void BuildSignalSweepVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var sc = (float)s;

            Color color = Colors.White;
            if (brush is SolidColorBrush solid)
            {
                color = solid.Color;
            }

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) }
            };

            var line1 = new Line { X1 = 0, X2 = size, Y1 = 6.0 * s, Y2 = 6.0 * s, Stroke = brush, StrokeThickness = Math.Max(0.5, 0.5 * s), Opacity = 0.15 };
            var line2 = new Line { X1 = 0, X2 = size, Y1 = 12.0 * s, Y2 = 12.0 * s, Stroke = brush, StrokeThickness = Math.Max(0.5, 0.5 * s), Opacity = 0.15 };
            var line3 = new Line { X1 = 0, X2 = size, Y1 = 18.0 * s, Y2 = 18.0 * s, Stroke = brush, StrokeThickness = Math.Max(0.5, 0.5 * s), Opacity = 0.15 };

            canvas.Children.Add(line1);
            canvas.Children.Add(line2);
            canvas.Children.Add(line3);

            var trailBrush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0.5),
                EndPoint = new Windows.Foundation.Point(1, 0.5)
            };
            trailBrush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = 0 });
            trailBrush.GradientStops.Add(new GradientStop { Color = color, Offset = 1 });

            var trail = new Border
            {
                Width = 8.0 * s,
                Height = 20.0 * s,
                Background = trailBrush,
                Opacity = 0.3
            };
            Canvas.SetLeft(trail, 0);
            Canvas.SetTop(trail, 2.0 * s);

            var bar = new Border
            {
                Width = Math.Max(1, 2.0 * s),
                Height = 20.0 * s,
                Background = brush,
                CornerRadius = new CornerRadius(Math.Max(0.5, 1.0 * s))
            };
            Canvas.SetLeft(bar, 0);
            Canvas.SetTop(bar, 2.0 * s);

            canvas.Children.Add(trail);
            canvas.Children.Add(bar);

            trail.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(trail, true);
                var visual = ElementCompositionPreview.GetElementVisual(trail);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartSignalSweepTranslation(visual, compositor, startX: -8f * sc, endX: 16f * sc);
            };

            bar.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(bar, true);
                var visual = ElementCompositionPreview.GetElementVisual(bar);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartSignalSweepTranslation(visual, compositor, startX: -2f * sc, endX: 24f * sc);
            };

            _rootGrid.Children.Add(canvas);
        }

        private void StartSignalSweepTranslation(Visual visual, Compositor compositor, float startX, float endX)
        {
            if (!_isAnimating) return;

            const float durationMs = 1200f;
            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f / durationMs, new Vector3(startX, 0f, 0f));
            translation.InsertKeyFrame(1000f / durationMs, new Vector3(endX, 0f, 0f));
            translation.InsertKeyFrame(1f, new Vector3(startX, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(durationMs);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
        }

        #endregion

        #region Heartbeat

        private void BuildHeartbeatVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) }
            };

            var baseline = new Line
            {
                X1 = 0,
                Y1 = 12.0 * s,
                X2 = size,
                Y2 = 12.0 * s,
                Stroke = brush,
                StrokeThickness = Math.Max(1, 1.0 * s),
                Opacity = 0.2
            };
            canvas.Children.Add(baseline);

            var heartbeatPath = new Path
            {
                Stroke = brush,
                StrokeThickness = Math.Max(1, 1.5 * s),
                Data = CreatePolylinePathGeometry(s, [
                    (-24.0, 12.0), (-18.0, 12.0), (-15.0, 12.0), (-12.0, 4.0), (-9.0, 20.0), (-6.0, 12.0),
                    (0.0, 12.0), (6.0, 12.0), (9.0, 12.0), (12.0, 4.0), (15.0, 20.0), (18.0, 12.0),
                    (24.0, 12.0), (30.0, 12.0), (33.0, 12.0), (36.0, 4.0), (39.0, 20.0), (42.0, 12.0), (48.0, 12.0)
                ])
            };
            PlatformCompatibility.SafeSetRoundedStroke(heartbeatPath, setLineJoin: true);
            Canvas.SetLeft(heartbeatPath, 0);
            Canvas.SetTop(heartbeatPath, 0);
            canvas.Children.Add(heartbeatPath);

            heartbeatPath.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(heartbeatPath, true);
                var visual = ElementCompositionPreview.GetElementVisual(heartbeatPath);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartHeartbeatTranslation(visual, compositor, (float)s);
            };

            _rootGrid.Children.Add(canvas);
        }

        private void StartHeartbeatTranslation(Visual visual, Compositor compositor, float scale)
        {
            if (!_isAnimating) return;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            var linear = compositor.CreateLinearEasingFunction();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f), linear);
            translation.InsertKeyFrame(1f, new Vector3(-24f * scale, 0f, 0f), linear);
            translation.Duration = TimeSpan.FromMilliseconds(1500);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
        }

        #endregion

        #region TunnelZoom

        private void BuildTunnelZoomVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;

            var grid = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            for (int i = 0; i < 3; i++)
            {
                var ring = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Stroke = brush,
                    StrokeThickness = Math.Max(1, 1.5 * s),
                    Opacity = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
                };

                var index = i;
                ring.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(ring);
                    var compositor = visual.Compositor;
                    visual.CenterPoint = new Vector3((float)(size / 2), (float)(size / 2), 0);
                    visual.Opacity = 0f;
                    visual.Scale = new Vector3(0.1f, 0.1f, 1f);
                    TrackVisual(visual);
                    StartTunnelZoomRingAnimation(visual, compositor, delayMs: index * 500);
                };

                grid.Children.Add(ring);
            }

            _rootGrid.Children.Add(grid);
        }

        private void StartTunnelZoomRingAnimation(Visual visual, Compositor compositor, int delayMs)
        {
            if (!_isAnimating) return;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(0.1f, 0.1f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(1500);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f);
            opacity.InsertKeyFrame(1f, 0f);
            opacity.Duration = TimeSpan.FromMilliseconds(1500);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scale, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity, TimeSpan.FromMilliseconds(delayMs));
        }

        #endregion

        #region GlitchReveal

        private void BuildGlitchRevealVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var barWidth = 3.0 * s;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var xs = new[] { 0.0, 4.0, 8.0, 12.0, 16.0, 20.0 };
            for (int i = 0; i < xs.Length; i++)
            {
                var column = new Border
                {
                    Width = barWidth,
                    Height = size,
                    Background = brush,
                    Opacity = 1
                };
                Canvas.SetLeft(column, xs[i] * s);
                Canvas.SetTop(column, 0);

                var colIndex = i + 1;
                column.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(column);
                    var compositor = visual.Compositor;
                    TrackVisual(visual);
                    visual.Opacity = 0.2f;
                    StartGlitchRevealOpacityAnimation(visual, compositor, colIndex);
                };

                canvas.Children.Add(column);
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartGlitchRevealOpacityAnimation(Visual visual, Compositor compositor, int colIndex)
        {
            if (!_isAnimating) return;

            const float durationMs = 2000f;
            var opacity = compositor.CreateScalarKeyFrameAnimation();

            switch (colIndex)
            {
                case 1:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(100f / durationMs, 1f);
                    opacity.InsertKeyFrame(150f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(800f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(850f / durationMs, 0.8f);
                    opacity.InsertKeyFrame(900f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 2:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(350f / durationMs, 1f);
                    opacity.InsertKeyFrame(500f / durationMs, 1f);
                    opacity.InsertKeyFrame(550f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1200f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1250f / durationMs, 0.9f);
                    opacity.InsertKeyFrame(1300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 3:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(200f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(250f / durationMs, 0.7f);
                    opacity.InsertKeyFrame(300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(900f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(950f / durationMs, 1f);
                    opacity.InsertKeyFrame(1100f / durationMs, 1f);
                    opacity.InsertKeyFrame(1150f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 4:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(500f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(550f / durationMs, 0.8f);
                    opacity.InsertKeyFrame(600f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(650f / durationMs, 1f);
                    opacity.InsertKeyFrame(700f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1500f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1550f / durationMs, 0.9f);
                    opacity.InsertKeyFrame(1600f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                case 5:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(150f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(200f / durationMs, 1f);
                    opacity.InsertKeyFrame(350f / durationMs, 1f);
                    opacity.InsertKeyFrame(400f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1300f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1350f / durationMs, 0.7f);
                    opacity.InsertKeyFrame(1400f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
                default:
                    opacity.InsertKeyFrame(0f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(700f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(750f / durationMs, 1f);
                    opacity.InsertKeyFrame(800f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(850f / durationMs, 0.6f);
                    opacity.InsertKeyFrame(900f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1700f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1750f / durationMs, 1f);
                    opacity.InsertKeyFrame(1850f / durationMs, 1f);
                    opacity.InsertKeyFrame(1900f / durationMs, 0.2f);
                    opacity.InsertKeyFrame(1f, 0.2f);
                    break;
            }

            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region CursorBlink

        private void BuildCursorBlinkVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var s = size / 24.0;
            var fontSize = Math.Max(8, 14.0 * s);

            var container = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var prompt = new TextBlock
            {
                Text = ">",
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = fontSize,
                Foreground = brush,
                Opacity = 0.6,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 2.0 * s, 0)
            };

            var cursor = new Border
            {
                Width = 6.0 * s,
                Height = fontSize,
                Background = brush,
                VerticalAlignment = VerticalAlignment.Center
            };

            var row = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(prompt, 0);
            Grid.SetColumn(cursor, 1);
            row.Children.Add(prompt);
            row.Children.Add(cursor);
            container.Children.Add(row);

            cursor.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(cursor);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                StartCursorBlinkOpacityAnimation(visual, compositor);
            };

            _rootGrid.Children.Add(container);
        }

        private void StartCursorBlinkOpacityAnimation(Visual visual, Compositor compositor)
        {
            if (!_isAnimating) return;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f);
            opacity.InsertKeyFrame(0.5f, 1f);
            opacity.InsertKeyFrame(0.51f, 0f);
            opacity.InsertKeyFrame(1f, 0f);
            opacity.Duration = TimeSpan.FromMilliseconds(1000);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region Path Geometry Helpers

        private static PathGeometry CreateClosedPathGeometry(double scale, (double x, double y)[] points)
        {
            var geometry = new PathGeometry();
            if (points.Length == 0) return geometry;

            var figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(points[0].x * scale, points[0].y * scale),
                IsClosed = true
            };

            for (int i = 1; i < points.Length; i++)
            {
                figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(points[i].x * scale, points[i].y * scale) });
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        private static PathGeometry CreatePolylinePathGeometry(double scale, (double x, double y)[] points)
        {
            var geometry = new PathGeometry();
            if (points.Length == 0) return geometry;

            var figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(points[0].x * scale, points[0].y * scale),
                IsClosed = false
            };

            for (int i = 1; i < points.Length; i++)
            {
                figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(points[i].x * scale, points[i].y * scale) });
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        #endregion
    }
}
