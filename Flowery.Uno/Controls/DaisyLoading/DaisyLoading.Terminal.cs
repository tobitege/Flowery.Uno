namespace Flowery.Controls
{
    public partial class DaisyLoading
    {
        #region Win95FileCopy

        private void BuildWin95FileCopyVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);

            var container = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var folderW = 32.0 * u;
            var folderH = 26.0 * u;
            var tabW = 14.0 * u;
            var tabH = 6.0 * u;
            var gap = 10.0 * u;
            var folderY = (size - folderH) / 2 + 4.0 * u;

            var leftFolder = CreateWin95Folder(brush, folderW, folderH, tabW, tabH, stroke, u);
            leftFolder.Margin = new Thickness((size / 2 - folderW - gap / 2), folderY, 0, 0);
            leftFolder.HorizontalAlignment = HorizontalAlignment.Left;
            leftFolder.VerticalAlignment = VerticalAlignment.Top;

            var rightFolder = CreateWin95Folder(brush, folderW, folderH, tabW, tabH, stroke, u);
            rightFolder.Margin = new Thickness((size / 2 + gap / 2), folderY, 0, 0);
            rightFolder.HorizontalAlignment = HorizontalAlignment.Left;
            rightFolder.VerticalAlignment = VerticalAlignment.Top;

            container.Children.Add(leftFolder);
            container.Children.Add(rightFolder);

            var paperW = 10.0 * u;
            var paperH = 12.0 * u;
            var paperY = folderY - paperH / 2 + 2.0 * u;

            for (int i = 0; i < 3; i++)
            {
                var paper = CreateWin95Paper(brush, paperW, paperH, stroke, u);
                paper.Margin = new Thickness(size / 2 - folderW - gap / 2 + folderW / 2 - paperW / 2, paperY - (i * 2.0 * u), 0, 0);
                paper.HorizontalAlignment = HorizontalAlignment.Left;
                paper.VerticalAlignment = VerticalAlignment.Top;
                paper.Opacity = 0;
                container.Children.Add(paper);

                int index = i;
                paper.Loaded += (_, _) =>
                {
                    paper.Opacity = 1;
                    PlatformCompatibility.TrySetIsTranslationEnabled(paper, true);
                    var visual = ElementCompositionPreview.GetElementVisual(paper);
                    var compositor = visual.Compositor;
                    visual.Opacity = 0f;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);

                    var flyDist = (float)(folderW + gap);
                    StartWin95PaperFlyAnimation(visual, compositor, flyDist, index * 400, (float)(8.0 * u));
                };
            }

            _rootGrid.Children.Add(container);
        }

        private static Grid CreateWin95Folder(Brush brush, double w, double h, double tabW, double tabH, double stroke, double u)
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(tabH) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(h - tabH) });

            var tab = new Border
            {
                Width = tabW,
                Height = tabH,
                CornerRadius = new CornerRadius(2.0 * u, 2.0 * u, 0, 0),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke, stroke, stroke, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Grid.SetRow(tab, 0);

            var body = new Border
            {
                Width = w,
                Height = h - tabH,
                CornerRadius = new CornerRadius(0, 2.0 * u, 2.0 * u, 2.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Grid.SetRow(body, 1);

            grid.Children.Add(tab);
            grid.Children.Add(body);
            return grid;
        }

        private static Border CreateWin95Paper(Brush brush, double w, double h, double stroke, double u)
        {
            var paper = new Border
            {
                Width = w,
                Height = h,
                CornerRadius = new CornerRadius(1.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke * 0.6),
                Background = new SolidColorBrush(Colors.Transparent)
            };

            var lines = new StackPanel
            {
                Spacing = 1.5 * u,
                Margin = new Thickness(1.5 * u, 2.0 * u, 1.5 * u, 1.5 * u)
            };

            for (int i = 0; i < 3; i++)
            {
                lines.Children.Add(new Rectangle
                {
                    Height = Math.Max(1, stroke * 0.5),
                    Fill = brush,
                    Opacity = 0.4
                });
            }

            paper.Child = lines;
            return paper;
        }

        private void StartWin95PaperFlyAnimation(Visual visual, Compositor compositor, float distance, int delayMs, float arcHeight)
        {
            if (!_isAnimating) return;

            var totalCycle = 1600;

            var offsetX = compositor.CreateScalarKeyFrameAnimation();
            offsetX.InsertKeyFrame(0f, 0f);
            offsetX.InsertKeyFrame(0.5f, distance, compositor.CreateLinearEasingFunction());
            offsetX.InsertKeyFrame(1f, distance);
            offsetX.Duration = TimeSpan.FromMilliseconds(totalCycle);
            offsetX.IterationBehavior = AnimationIterationBehavior.Forever;

            var offsetY = compositor.CreateScalarKeyFrameAnimation();
            offsetY.InsertKeyFrame(0f, 0f);
            offsetY.InsertKeyFrame(0.25f, -arcHeight);
            offsetY.InsertKeyFrame(0.5f, 0f);
            offsetY.InsertKeyFrame(1f, 0f);
            offsetY.Duration = TimeSpan.FromMilliseconds(totalCycle);
            offsetY.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f);
            opacity.InsertKeyFrame(0.5f, 1f);
            opacity.InsertKeyFrame(0.51f, 0f);
            opacity.InsertKeyFrame(0.99f, 0f);
            opacity.InsertKeyFrame(1f, 1f);
            opacity.Duration = TimeSpan.FromMilliseconds(totalCycle);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation.X", offsetX, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Translation.Y", offsetY, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity, TimeSpan.FromMilliseconds(delayMs));
        }

        #endregion

        #region Win95Delete

        private void BuildWin95DeleteVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);

            var container = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var binW = 32.0 * u;
            var binH = 40.0 * u;
            var lidH = 7.0 * u;
            var binX = (size - binW) / 2;
            var binY = (size - binH) / 2 + 8.0 * u;

            var bin = CreateWin95RecycleBin(brush, binW, binH, lidH, stroke, u);
            bin.Margin = new Thickness(binX, binY, 0, 0);
            bin.HorizontalAlignment = HorizontalAlignment.Left;
            bin.VerticalAlignment = VerticalAlignment.Top;

            var paperW = 8.0 * u;
            var paperH = 10.0 * u;
            var binCenterX = binX + binW / 2 - paperW / 2;
            var binTopY = binY + lidH;

            for (int i = 0; i < 4; i++)
            {
                var angle = (i - 1.5) * 25.0;
                var outerX = (float)(Math.Sin(angle * Math.PI / 180) * 30.0 * u);
                var outerY = (float)(-40.0 * u);

                var paper = CreateWin95Paper(brush, paperW, paperH, stroke * 0.8, u);
                paper.Margin = new Thickness(binCenterX + outerX, binTopY + outerY, 0, 0);
                paper.HorizontalAlignment = HorizontalAlignment.Left;
                paper.VerticalAlignment = VerticalAlignment.Top;
                paper.Opacity = 0;
                container.Children.Add(paper);

                int index = i;
                float capturedOuterX = outerX;
                float capturedOuterY = outerY;
                paper.Loaded += (_, _) =>
                {
                    paper.Opacity = 1;
                    PlatformCompatibility.TrySetIsTranslationEnabled(paper, true);
                    var visual = ElementCompositionPreview.GetElementVisual(paper);
                    var compositor = visual.Compositor;
                    visual.Opacity = 0f;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);

                    var targetX = -capturedOuterX;
                    var targetY = -capturedOuterY;
                    StartWin95DeletePaperAnimation(visual, compositor, targetX, targetY, index * 250);
                };
            }

            container.Children.Add(bin);

            _rootGrid.Children.Add(container);
        }

        private static Grid CreateWin95RecycleBin(Brush brush, double w, double h, double lidH, double stroke, double u)
        {
            var grid = new Grid();

            var lid = new Border
            {
                Width = w + 4.0 * u,
                Height = lidH,
                CornerRadius = new CornerRadius(2.0 * u, 2.0 * u, 0, 0),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                Background = new SolidColorBrush(Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            var lidHandle = new Rectangle
            {
                Width = 8.0 * u,
                Height = 3.0 * u,
                Fill = brush,
                RadiusX = 1.5 * u,
                RadiusY = 1.5 * u,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -1.5 * u, 0, 0)
            };

            var body = new Border
            {
                Width = w,
                Height = h - lidH,
                CornerRadius = new CornerRadius(0, 0, 4.0 * u, 4.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                Background = new SolidColorBrush(Colors.Transparent),
                Margin = new Thickness(0, lidH, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            var stripes = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4.0 * u,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            for (int i = 0; i < 3; i++)
            {
                stripes.Children.Add(new Rectangle
                {
                    Width = stroke,
                    Height = (h - lidH) * 0.6,
                    Fill = brush,
                    Opacity = 0.3
                });
            }
            body.Child = stripes;

            grid.Children.Add(body);
            grid.Children.Add(lid);
            grid.Children.Add(lidHandle);
            return grid;
        }

        private void StartWin95DeletePaperAnimation(Visual visual, Compositor compositor, float targetX, float targetY, int delayMs)
        {
            if (!_isAnimating) return;

            var totalCycle = 1600;

            var offsetX = compositor.CreateScalarKeyFrameAnimation();
            offsetX.InsertKeyFrame(0f, 0f);
            offsetX.InsertKeyFrame(0.5f, targetX);
            offsetX.InsertKeyFrame(1f, targetX);
            offsetX.Duration = TimeSpan.FromMilliseconds(totalCycle);
            offsetX.IterationBehavior = AnimationIterationBehavior.Forever;

            var offsetY = compositor.CreateScalarKeyFrameAnimation();
            offsetY.InsertKeyFrame(0f, 0f);
            offsetY.InsertKeyFrame(0.5f, targetY);
            offsetY.InsertKeyFrame(1f, targetY);
            offsetY.Duration = TimeSpan.FromMilliseconds(totalCycle);
            offsetY.IterationBehavior = AnimationIterationBehavior.Forever;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(0.6f, 0.6f, 1f));
            scale.InsertKeyFrame(0.2f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(0.5f, 0.5f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(0.5f, 0.5f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(totalCycle);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f);
            opacity.InsertKeyFrame(0.4f, 1f);
            opacity.InsertKeyFrame(0.5f, 0f);
            opacity.InsertKeyFrame(0.99f, 0f);
            opacity.InsertKeyFrame(1f, 1f);
            opacity.Duration = TimeSpan.FromMilliseconds(totalCycle);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation.X", offsetX, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Translation.Y", offsetY, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Scale", scale, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity, TimeSpan.FromMilliseconds(delayMs));
        }

        #endregion

        #region Win95Search

        private void BuildWin95SearchVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);

            var container = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var folderW = 20.0 * u;
            var folderH = 16.0 * u;
            var tabW = 10.0 * u;
            var tabH = 4.0 * u;
            var filesY = size / 2 + 6.0 * u;

            for (int i = 0; i < 4; i++)
            {
                var folder = CreateWin95Folder(brush, folderW, folderH, tabW, tabH, stroke * 0.8, u);
                folder.Margin = new Thickness(12.0 * u + i * (folderW + 4.0 * u), filesY, 0, 0);
                folder.HorizontalAlignment = HorizontalAlignment.Left;
                folder.VerticalAlignment = VerticalAlignment.Top;
                folder.Opacity = 0.4;
                container.Children.Add(folder);
            }

            var flashlightLen = 28.0 * u;
            var flashlightW = 10.0 * u;
            var flashlight = CreateWin95Flashlight(brush, flashlightLen, flashlightW, stroke, u);
            flashlight.Margin = new Thickness(size / 2 - flashlightLen / 2 - 6.0 * u, size / 2 - flashlightW * 2, 0, 0);
            flashlight.HorizontalAlignment = HorizontalAlignment.Left;
            flashlight.VerticalAlignment = VerticalAlignment.Top;

            var beamTopW = 8.0 * u;
            var beamBottomW = 28.0 * u;
            var beamH = 18.0 * u;
            var beamCenterX = size / 2;
            var beamTopY = filesY - beamH + 2.0 * u;
            var beam = new Polygon
            {
                Points =
                [
                    new Windows.Foundation.Point(beamCenterX - beamTopW / 2, beamTopY),
                    new Windows.Foundation.Point(beamCenterX + beamTopW / 2, beamTopY),
                    new Windows.Foundation.Point(beamCenterX + beamBottomW / 2, beamTopY + beamH),
                    new Windows.Foundation.Point(beamCenterX - beamBottomW / 2, beamTopY + beamH)
                ],
                Fill = brush,
                Opacity = 0.15
            };

            var flashlightGroup = new Grid { Width = size, Height = size };
            flashlightGroup.Children.Add(flashlight);
            flashlightGroup.Children.Add(beam);
            container.Children.Add(flashlightGroup);

            flashlightGroup.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(flashlightGroup, true);
                var visual = ElementCompositionPreview.GetElementVisual(flashlightGroup);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartWin95SearchSweepAnimation(visual, compositor, (float)(size * 0.5));
            };

            _rootGrid.Children.Add(container);
        }

        private static Grid CreateWin95Flashlight(Brush brush, double len, double w, double stroke, double u)
        {
            var grid = new Grid();

            var handle = new Border
            {
                Width = len * 0.5,
                Height = w * 0.7,
                CornerRadius = new CornerRadius(2.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                Background = new SolidColorBrush(Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            var head = new Border
            {
                Width = len * 0.55,
                Height = w,
                CornerRadius = new CornerRadius(0, 3.0 * u, 3.0 * u, 0),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                Background = new SolidColorBrush(Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(len * 0.45, 0, 0, 0)
            };

            var lens = new Ellipse
            {
                Width = w * 0.5,
                Height = w * 0.5,
                Fill = brush,
                Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(len * 0.85, 0, 0, 0)
            };

            grid.Children.Add(handle);
            grid.Children.Add(head);
            grid.Children.Add(lens);
            return grid;
        }

        private void StartWin95SearchSweepAnimation(Visual visual, Compositor compositor, float sweepRange)
        {
            if (!_isAnimating) return;

            var sweep = compositor.CreateScalarKeyFrameAnimation();
            sweep.InsertKeyFrame(0f, -sweepRange / 2);
            sweep.InsertKeyFrame(0.5f, sweepRange / 2);
            sweep.InsertKeyFrame(1f, -sweepRange / 2);
            sweep.Duration = TimeSpan.FromMilliseconds(2000);
            sweep.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation.X", sweep);
        }

        #endregion

        #region Win95EmptyRecycle

        private void BuildWin95EmptyRecycleVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);

            var container = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var binW = 32.0 * u;
            var binH = 40.0 * u;
            var lidH = 7.0 * u;
            var binX = (size - binW) / 2;
            var binY = (size - binH) / 2 + 8.0 * u;

            var bin = CreateWin95RecycleBin(brush, binW, binH, lidH, stroke, u);
            bin.Margin = new Thickness(binX, binY, 0, 0);
            bin.HorizontalAlignment = HorizontalAlignment.Left;
            bin.VerticalAlignment = VerticalAlignment.Top;
            container.Children.Add(bin);

            var paperW = 8.0 * u;
            var paperH = 10.0 * u;
            var startX = binX + binW / 2 - paperW / 2;
            var startY = binY + lidH;

            for (int i = 0; i < 4; i++)
            {
                var paper = CreateWin95Paper(brush, paperW, paperH, stroke * 0.8, u);
                paper.Margin = new Thickness(startX, startY, 0, 0);
                paper.HorizontalAlignment = HorizontalAlignment.Left;
                paper.VerticalAlignment = VerticalAlignment.Top;
                paper.Opacity = 0;
                container.Children.Add(paper);

                int index = i;
                paper.Loaded += (_, _) =>
                {
                    paper.Opacity = 1;
                    PlatformCompatibility.TrySetIsTranslationEnabled(paper, true);
                    var visual = ElementCompositionPreview.GetElementVisual(paper);
                    var compositor = visual.Compositor;
                    visual.Opacity = 0f;
                    TrackVisual(visual);
                    TrackTranslationVisual(visual);

                    var angle = (index - 1.5) * 25.0;
                    var targetX = (float)(Math.Sin(angle * Math.PI / 180) * 30.0 * u);
                    var targetY = (float)(-40.0 * u);
                    StartWin95EmptyPaperAnimation(paper, visual, compositor, targetX, targetY, index * 250);
                };
            }

            _rootGrid.Children.Add(container);
        }

        private void StartWin95EmptyPaperAnimation(UIElement element, Visual visual, Compositor compositor, float targetX, float targetY, int delayMs)
        {
            if (!_isAnimating) return;

            var totalCycle = 1600;

            var offsetX = compositor.CreateScalarKeyFrameAnimation();
            offsetX.InsertKeyFrame(0f, 0f);
            offsetX.InsertKeyFrame(0.5f, targetX);
            offsetX.InsertKeyFrame(1f, targetX);
            offsetX.Duration = TimeSpan.FromMilliseconds(totalCycle);
            offsetX.IterationBehavior = AnimationIterationBehavior.Forever;

            var offsetY = compositor.CreateScalarKeyFrameAnimation();
            offsetY.InsertKeyFrame(0f, 0f);
            offsetY.InsertKeyFrame(0.5f, targetY);
            offsetY.InsertKeyFrame(1f, targetY);
            offsetY.Duration = TimeSpan.FromMilliseconds(totalCycle);
            offsetY.IterationBehavior = AnimationIterationBehavior.Forever;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(0.5f, 0.5f, 1f));
            scale.InsertKeyFrame(0.3f, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(0.5f, new Vector3(0.6f, 0.6f, 1f));
            scale.InsertKeyFrame(1f, new Vector3(0.6f, 0.6f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(totalCycle);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 1f);
            opacity.InsertKeyFrame(0.4f, 1f);
            opacity.InsertKeyFrame(0.5f, 0f);
            opacity.InsertKeyFrame(0.99f, 0f);
            opacity.InsertKeyFrame(1f, 1f);
            opacity.Duration = TimeSpan.FromMilliseconds(totalCycle);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
            {
                var targetAngle = targetX > 0 ? 45d : -45d;
                var storyboard = PlatformCompatibility.StartRotationKeyframes(
                    element,
                    [(0d, 0d), (0.5d, targetAngle), (1d, targetAngle)],
                    TimeSpan.FromMilliseconds(totalCycle));
                TrackStoryboard(storyboard);
            }
            else
            {
                var targetAngle = targetX > 0 ? 45f : -45f;
                var rotation = PlatformCompatibility.CreateRotationAnimation(
                    compositor,
                    [(0f, 0f), (0.5f, targetAngle), (1f, targetAngle)],
                    TimeSpan.FromMilliseconds(totalCycle));
                PlatformCompatibility.StartAnimation(visual, "RotationAngle", rotation, TimeSpan.FromMilliseconds(delayMs));
            }

            PlatformCompatibility.StartAnimation(visual, "Translation.X", offsetX, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Translation.Y", offsetY, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Scale", scale, TimeSpan.FromMilliseconds(delayMs));
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity, TimeSpan.FromMilliseconds(delayMs));
        }

        #endregion
    }
}
