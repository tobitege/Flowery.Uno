namespace Flowery.Controls
{
    public partial class DaisyLoading
    {
        #region DocumentFlip

        private void BuildDocumentFlipOnVisual(double size)
        {
            BuildDocumentFlipVisual(size, isFlipOn: true);
        }

        private void BuildDocumentFlipOffVisual(double size)
        {
            BuildDocumentFlipVisual(size, isFlipOn: false);
        }

        private void BuildDocumentFlipVisual(double size, bool isFlipOn)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;

            var container = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var pageWidth = 62.0 * u;
            var pageHeight = 76.0 * u;
            var pageLeft = (size - pageWidth) / 2;
            var pageTop = (size - pageHeight) / 2;
            var borderThickness = Math.Max(1, 2.0 * u);

            var backPage = CreateDocumentPage(brush, pageWidth, pageHeight, borderThickness);
            backPage.Opacity = 0.25;
            backPage.Margin = new Thickness(pageLeft + (6.0 * u), pageTop + (6.0 * u), 0, 0);

            var midPage = CreateDocumentPage(brush, pageWidth, pageHeight, borderThickness);
            midPage.Opacity = 0.45;
            midPage.Margin = new Thickness(pageLeft + (3.0 * u), pageTop + (3.0 * u), 0, 0);

            var frontPage = CreateDocumentPage(brush, pageWidth, pageHeight, borderThickness);
            frontPage.Margin = new Thickness(pageLeft, pageTop, 0, 0);

            container.Children.Add(backPage);
            container.Children.Add(midPage);
            container.Children.Add(frontPage);
            _rootGrid.Children.Add(container);

            frontPage.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(frontPage, true);
                var visual = ElementCompositionPreview.GetElementVisual(frontPage);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3(0f, (float)(pageHeight / 2), 0);
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                if (isFlipOn)
                {
                    StartDocumentFlipOnAnimation(visual, compositor, (float)u);
                }
                else
                {
                    StartDocumentFlipOffAnimation(visual, compositor, (float)u);
                }
            };

            midPage.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(midPage);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(pageWidth / 2), (float)(pageHeight / 2), 0);
                TrackVisual(visual);
                StartDocumentFlipMidAnimation(visual, compositor);
            };
        }

        private static Border CreateDocumentPage(Brush brush, double width, double height, double borderThickness)
        {
            Brush backgroundBrush = new SolidColorBrush(Colors.White);
            if (brush is SolidColorBrush solid)
            {
                var c = solid.Color;
                const double shade = 0.85;
                var r = (byte)Math.Clamp(c.R * shade, 0, 255);
                var g = (byte)Math.Clamp(c.G * shade, 0, 255);
                var b = (byte)Math.Clamp(c.B * shade, 0, 255);
                backgroundBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(220, r, g, b));
            }

            var page = new Border
            {
                Width = width,
                Height = height,
                CornerRadius = new CornerRadius(Math.Max(2, borderThickness * 2)),
                BorderThickness = new Thickness(borderThickness),
                BorderBrush = brush,
                Background = backgroundBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransformOrigin = new Windows.Foundation.Point(0.0, 0.5)
            };

            var lines = new StackPanel
            {
                Spacing = borderThickness * 2,
                Margin = new Thickness(borderThickness * 6, borderThickness * 8, borderThickness * 6, borderThickness * 6)
            };

            for (int i = 0; i < 6; i++)
            {
                lines.Children.Add(new Rectangle
                {
                    Height = Math.Max(1, borderThickness * 1.5),
                    Width = double.NaN,
                    Fill = brush,
                    Opacity = i == 0 ? 0.5 : 0.25
                });
            }

            page.Child = lines;
            return page;
        }

        private void StartDocumentFlipOnAnimation(Visual visual, Compositor compositor, float u)
        {
            if (!_isAnimating) return;

            const float durationMs = 1500f;
            var endOffset = 3f * u;
            var startOffsetX = -30f * u;
            var startOffsetY = -30f * u;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f / durationMs, new Vector3(startOffsetX, startOffsetY, 0f));
            translation.InsertKeyFrame(1500f / durationMs, new Vector3(endOffset, endOffset, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(durationMs);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
        }

        private void StartDocumentFlipOffAnimation(Visual visual, Compositor compositor, float u)
        {
            if (!_isAnimating) return;

            const float durationMs = 1500f;
            var startOffsetX = 3f * u;
            var startOffsetY = 3f * u;
            var endOffsetX = 30f * u;
            var endOffsetY = -30f * u;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f / durationMs, new Vector3(startOffsetX, startOffsetY, 0f));
            translation.InsertKeyFrame(1500f / durationMs, new Vector3(endOffsetX, endOffsetY, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(durationMs);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f / durationMs, 1f);
            opacity.InsertKeyFrame(1500f / durationMs, 0f);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        private void StartDocumentFlipMidAnimation(Visual visual, Compositor compositor)
        {
            if (!_isAnimating) return;

            const float durationMs = 1500f;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f / durationMs, 0.45f);
            opacity.InsertKeyFrame(600f / durationMs, 0.65f);
            opacity.InsertKeyFrame(900f / durationMs, 0.65f);
            opacity.InsertKeyFrame(1500f / durationMs, 0.45f);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region Battery

        private void BuildBatteryChargingVisual(double size)
        {
            BuildBatteryVisual(size, isCharging: true);
        }

        private void BuildBatteryEmptyingVisual(double size)
        {
            BuildBatteryVisual(size, isCharging: false);
        }

        private void BuildBatteryVisual(double size, bool isCharging)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var usesFixedPixelSize = !double.IsNaN(PixelSize) && PixelSize > 0;
            var baseSize = DaisyResourceLookup.GetSizeValue(Size);
            var targetBodyHeight = usesFixedPixelSize ? size * 0.5 : size;
            var widthScale = baseSize > 0 ? size / baseSize : 1.0;
            var heightScale = baseSize > 0 ? targetBodyHeight / baseSize : 1.0;
            var strokeScale = Math.Min(widthScale, heightScale);

            var barWidth = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryBarWidth(Size) * widthScale));
            var barGap = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryBarGap(Size) * widthScale));
            var stroke = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryStroke(Size) * strokeScale));
            var barCorner = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryBarCornerRadius(Size) * strokeScale));
            var shellCorner = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryShellCornerRadius(Size) * strokeScale));
            var tipWidth = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryTipWidth(Size) * widthScale));
            var tipCorner = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryTipCornerRadius(Size) * strokeScale));
            var paddingX = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryInternalPadding(Size) * widthScale)) + 1;
            var paddingY = Math.Max(1, (int)Math.Round(DaisyResourceLookup.GetBatteryInternalPadding(Size) * heightScale)) + 1;

            const int barCount = 4;
            var tipOverlap = tipWidth > 0 ? stroke : 0;

            if (usesFixedPixelSize)
            {
                var targetTotalWidth = size;
                var bodyWidthTarget = targetTotalWidth - tipWidth + tipOverlap;
                var interiorWidthTarget = bodyWidthTarget - (2 * paddingX) - (2 * stroke);
                var totalGapWidth = (barCount - 1) * barGap;
                var availableForBars = interiorWidthTarget - totalGapWidth;

                if (availableForBars > 0)
                {
                    barWidth = Math.Max(1, (int)Math.Floor(availableForBars / barCount));
                }
            }

            var interiorWidth = (barCount * barWidth) + ((barCount - 1) * barGap);
            var bodyWidth = interiorWidth + (2 * paddingX) + (2 * stroke);

            var barHeight = usesFixedPixelSize
                ? Math.Max(4, (int)Math.Round(targetBodyHeight - (2 * paddingY) - (2 * stroke)))
                : Math.Max(4, (int)Math.Round(size * 0.35));
            var bodyHeight = barHeight + (2 * paddingY) + (2 * stroke);

            var tipHeight = Math.Max(2, (int)Math.Round(bodyHeight * 0.5));
            var totalWidth = bodyWidth + tipWidth - tipOverlap;

            var containerSize = (int)size;
            var containerWidth = usesFixedPixelSize ? containerSize : Math.Max(containerSize, totalWidth);
            var containerHeight = usesFixedPixelSize ? containerSize : Math.Max(containerSize, bodyHeight);

            _rootGrid.Width = containerWidth;
            _rootGrid.Height = containerHeight;

            var horizontalExtra = containerWidth - totalWidth;
            var verticalExtra = containerHeight - bodyHeight;
            var leftOffset = horizontalExtra / 2;
            var rightOffset = horizontalExtra - leftOffset;
            var topOffset = verticalExtra / 2;
            var bottomOffset = verticalExtra - topOffset;

            var barsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Width = interiorWidth,
                Height = barHeight,
                Spacing = barGap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var bars = new List<Border>(barCount);
            for (var i = 0; i < barCount; i++)
            {
                var bar = new Border
                {
                    Width = barWidth,
                    Height = barHeight,
                    Background = brush,
                    CornerRadius = new CornerRadius(barCorner),
                    Opacity = 0.2
                };
                barsPanel.Children.Add(bar);
                bars.Add(bar);
            }

            var body = new Border
            {
                Width = bodyWidth,
                Height = bodyHeight,
                BorderThickness = new Thickness(stroke),
                BorderBrush = brush,
                CornerRadius = new CornerRadius(shellCorner),
                Padding = new Thickness(paddingX, paddingY, paddingX, paddingY),
                Background = new SolidColorBrush(Colors.Transparent),
                Child = barsPanel,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Border? tip = null;
            if (tipWidth > 0)
            {
                tip = new Border
                {
                    Width = tipWidth,
                    Height = tipHeight,
                    Background = brush,
                    CornerRadius = new CornerRadius(0, tipCorner, tipCorner, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(-tipOverlap, 0, 0, 0)
                };
            }

            var batteryLayout = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Width = totalWidth,
                Height = bodyHeight,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(leftOffset, topOffset, rightOffset, bottomOffset)
            };
            batteryLayout.Children.Add(body);
            if (tip != null)
            {
                batteryLayout.Children.Add(tip);
            }

            _rootGrid.Children.Add(batteryLayout);
            batteryLayout.Loaded += (_, _) => StartBatteryBarCycle(bars, isCharging);
        }

        private void StartBatteryBarCycle(List<Border> bars, bool isCharging)
        {
            if (!_isAnimating || bars.Count == 0) return;

            const double inactive = 0.15;
            const double active = 1.0;
            const int pauseTicks = 2;
            var level = isCharging ? 1 : bars.Count;
            var pauseRemaining = level == 0 || level == bars.Count ? pauseTicks : 0;

            void ApplyLevel(int fill)
            {
                for (var i = 0; i < bars.Count; i++)
                {
                    bars[i].Opacity = i < fill ? active : inactive;
                }
            }

            ApplyLevel(level);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            timer.Tick += (_, _) =>
            {
                if (!_isAnimating)
                {
                    timer.Stop();
                    return;
                }

                if (pauseRemaining > 0)
                {
                    pauseRemaining--;
                    return;
                }

                if (isCharging)
                {
                    level++;
                    if (level > bars.Count)
                    {
                        level = 0;
                    }
                }
                else
                {
                    level--;
                    if (level < 0)
                    {
                        level = bars.Count;
                    }
                }

                ApplyLevel(level);

                if (level == 0 || level == bars.Count)
                {
                    pauseRemaining = pauseTicks;
                }
            };
            timer.Start();
            TrackTimer(timer);
        }

        #endregion

        #region TrafficLight

        private void BuildTrafficLightUpVisual(double size)
        {
            BuildTrafficLightVisual(size, rotationAngle: 0, reverseCycle: false);
        }

        private void BuildTrafficLightRightVisual(double size)
        {
            BuildTrafficLightVisual(size, rotationAngle: 90, reverseCycle: false);
        }

        private void BuildTrafficLightDownVisual(double size)
        {
            BuildTrafficLightVisual(size, rotationAngle: 0, reverseCycle: true);
        }

        private void BuildTrafficLightLeftVisual(double size)
        {
            BuildTrafficLightVisual(size, rotationAngle: 270, reverseCycle: false);
        }

        private void BuildTrafficLightVisual(double size, double rotationAngle, bool reverseCycle)
        {
            if (_rootGrid == null) return;

            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);

            var borderBrush = DaisyResourceLookup.GetBrush(
                "DaisyBaseContentBrush",
                new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230)));

            var redBrush = new SolidColorBrush(Colors.IndianRed);
            var yellowBrush = new SolidColorBrush(Colors.Gold);
            var greenBrush = new SolidColorBrush(Colors.LimeGreen);

            var bodyWidth = 38.0 * u;
            var bodyHeight = 84.0 * u;
            var corner = 10.0 * u;

            var body = new Border
            {
                Width = bodyWidth,
                Height = bodyHeight,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(stroke),
                CornerRadius = new CornerRadius(corner),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var lights = new Grid
            {
                Margin = new Thickness(6.0 * u, 8.0 * u, 6.0 * u, 8.0 * u)
            };
            lights.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            lights.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            lights.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var red = new Ellipse { Fill = redBrush, Opacity = 0.2 };
            var yellow = new Ellipse { Fill = yellowBrush, Opacity = 0.2 };
            var green = new Ellipse { Fill = greenBrush, Opacity = 0.2 };
            Grid.SetRow(red, 0);
            Grid.SetRow(yellow, 1);
            Grid.SetRow(green, 2);
            lights.Children.Add(red);
            lights.Children.Add(yellow);
            lights.Children.Add(green);

            body.Child = lights;

            var container = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                RenderTransform = new RotateTransform { Angle = rotationAngle }
            };
            container.Children.Add(body);

            _rootGrid.Children.Add(container);

            container.Loaded += (_, _) => StartTrafficLightCycle(red, yellow, green, reverseCycle);
        }

        private void StartTrafficLightCycle(UIElement red, UIElement yellow, UIElement green, bool reverseCycle)
        {
            if (!_isAnimating) return;

            const double inactive = 0.15;
            const double active = 1.0;
            UIElement[] lights = reverseCycle
                ? [red, yellow, green]
                : [green, yellow, red];
            var index = 0;

            void ApplyState()
            {
                for (var i = 0; i < lights.Length; i++)
                {
                    lights[i].Opacity = i == index ? active : inactive;
                }
            }

            ApplyState();

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(900)
            };
            timer.Tick += (_, _) =>
            {
                if (!_isAnimating)
                {
                    timer.Stop();
                    return;
                }

                index = (index + 1) % lights.Length;
                ApplyState();
            };
            timer.Start();
            TrackTimer(timer);
        }

        #endregion

        #region MailSend

        private void BuildMailSendVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;

            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var envelopeW = 70.0 * u;
            var envelopeH = 44.0 * u;
            var envelopeLeft = (size - envelopeW) / 2;
            var envelopeTop = size * 0.52;

            var paperWidth = envelopeW - (12.0 * u);
            var paperHeight = envelopeH - (4.0 * u);
            var paperLeft = envelopeLeft + (6.0 * u);
            var paperStartY = envelopeTop - paperHeight - (6.0 * u);

            var paper = new Border
            {
                Width = paperWidth,
                Height = paperHeight,
                CornerRadius = new CornerRadius(3.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(Math.Max(1, 2.0 * u)),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Canvas.SetLeft(paper, paperLeft);
            Canvas.SetTop(paper, paperStartY);
            canvas.Children.Add(paper);

            var linesPanel = new StackPanel
            {
                Spacing = 3.0 * u,
                Margin = new Thickness(6.0 * u, 8.0 * u, 6.0 * u, 6.0 * u)
            };
            for (int i = 0; i < 4; i++)
            {
                linesPanel.Children.Add(new Rectangle
                {
                    Height = Math.Max(1, 2.0 * u),
                    Fill = brush,
                    Opacity = i == 0 ? 0.5 : 0.25
                });
            }
            paper.Child = linesPanel;

            var envelopeBack = new Border
            {
                Width = envelopeW,
                Height = envelopeH,
                CornerRadius = new CornerRadius(6.0 * u),
                Background = DaisyResourceLookup.GetBrush("DaisyBase100Brush", new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 30)))
            };
            Canvas.SetLeft(envelopeBack, envelopeLeft);
            Canvas.SetTop(envelopeBack, envelopeTop);
            canvas.Children.Add(envelopeBack);

            var envelope = new Border
            {
                Width = envelopeW,
                Height = envelopeH,
                CornerRadius = new CornerRadius(6.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(Math.Max(1, 2.0 * u)),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Canvas.SetLeft(envelope, envelopeLeft);
            Canvas.SetTop(envelope, envelopeTop);
            canvas.Children.Add(envelope);

            var flap = new Path
            {
                Fill = brush,
                Opacity = 0.35,
                Data = CreateClosedPathGeometry(1.0, [
                    (envelopeLeft + (2.0 * u), envelopeTop + (4.0 * u)),
                    (envelopeLeft + (envelopeW / 2), envelopeTop + (envelopeH * 0.55)),
                    (envelopeLeft + envelopeW - (2.0 * u), envelopeTop + (4.0 * u))
                ])
            };
            canvas.Children.Add(flap);

            _rootGrid.Children.Add(canvas);

            paper.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(paper, true);
                var visual = ElementCompositionPreview.GetElementVisual(paper);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartMailPaperSlideAnimation(visual, compositor, (float)u, (float)paperHeight);
            };
        }

        private void StartMailPaperSlideAnimation(Visual visual, Compositor compositor, float u, float paperHeight)
        {
            if (!_isAnimating) return;

            const float durationMs = 2000f;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f / durationMs, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(800f / durationMs, new Vector3(0f, paperHeight + 6f * u, 0f));
            translation.InsertKeyFrame(1400f / durationMs, new Vector3(0f, paperHeight + 6f * u, 0f));
            translation.InsertKeyFrame(2000f / durationMs, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(durationMs);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
        }

        #endregion

        #region CloudUpload

        private void BuildCloudUploadVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 3.0 * u);

            var host = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var cloudSize = size * 0.46;
            var cloud = new Path
            {
                Width = cloudSize,
                Height = cloudSize,
                Data = Flowery.Helpers.FloweryPathHelpers.GetWeatherIconGeometry("WeatherIconOvercast"),
                Fill = brush,
                Opacity = 0.65,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(cloud, 0);
            host.Children.Add(cloud);

            // Arrow (up) in lower half
            var arrowSize = size * 0.28;
            var arrow = new Path
            {
                Width = arrowSize,
                Height = arrowSize,
                Stroke = brush,
                StrokeThickness = stroke,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Data = CreatePolylinePathGeometry(1.0, [
                    (12.0, 19.0), (12.0, 7.0),
                    (7.0, 12.0), (12.0, 7.0), (17.0, 12.0)
                ])
            };
            PlatformCompatibility.SafeSetRoundedStroke(arrow, setLineJoin: true);
            Grid.SetRow(arrow, 1);
            host.Children.Add(arrow);

            arrow.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(arrow, true);
                var visual = ElementCompositionPreview.GetElementVisual(arrow);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartCloudUploadArrowRiseAnimation(visual, compositor, (float)u);
            };

            _rootGrid.Children.Add(host);
        }

        private void StartCloudUploadArrowRiseAnimation(Visual visual, Compositor compositor, float u)
        {
            if (!_isAnimating) return;

            const float durationMs = 1400f;
            var rise = 10f * u;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f / durationMs, new Vector3(0f, rise, 0f));
            translation.InsertKeyFrame(700f / durationMs, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(1400f / durationMs, new Vector3(0f, rise, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(durationMs);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f / durationMs, 0.3f);
            opacity.InsertKeyFrame(700f / durationMs, 1f);
            opacity.InsertKeyFrame(1400f / durationMs, 0.3f);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region CloudDownload

        private void BuildCloudDownloadVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 3.0 * u);

            var host = new Grid
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var cloudSize = size * 0.46;
            var cloud = new Path
            {
                Width = cloudSize,
                Height = cloudSize,
                Data = Flowery.Helpers.FloweryPathHelpers.GetWeatherIconGeometry("WeatherIconOvercast"),
                Fill = brush,
                Opacity = 0.65,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(cloud, 0);
            host.Children.Add(cloud);

            var arrowSize = size * 0.28;
            var arrow = new Path
            {
                Width = arrowSize,
                Height = arrowSize,
                Stroke = brush,
                StrokeThickness = stroke,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Data = CreatePolylinePathGeometry(1.0, [
                    (12.0, 5.0), (12.0, 17.0),
                    (7.0, 12.0), (12.0, 17.0), (17.0, 12.0)
                ])
            };
            PlatformCompatibility.SafeSetRoundedStroke(arrow, setLineJoin: true);
            Grid.SetRow(arrow, 1);
            host.Children.Add(arrow);

            arrow.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(arrow, true);
                var visual = ElementCompositionPreview.GetElementVisual(arrow);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartCloudDownloadArrowFallAnimation(visual, compositor, (float)u);
            };

            _rootGrid.Children.Add(host);
        }

        private void StartCloudDownloadArrowFallAnimation(Visual visual, Compositor compositor, float u)
        {
            if (!_isAnimating) return;

            const float durationMs = 1400f;
            var fall = 10f * u;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f / durationMs, new Vector3(0f, -fall, 0f));
            translation.InsertKeyFrame(700f / durationMs, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(1400f / durationMs, new Vector3(0f, -fall, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(durationMs);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f / durationMs, 0.3f);
            opacity.InsertKeyFrame(700f / durationMs, 1f);
            opacity.InsertKeyFrame(1400f / durationMs, 0.3f);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region InvoiceStamp

        private void BuildInvoiceStampVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);

            var canvas = new Canvas
            {
                Width = size,
                Height = size
            };

            var invoiceW = 60.0 * u;
            var invoiceH = 76.0 * u;
            var invoiceLeft = (size - invoiceW) / 2;
            var invoiceTop = (size - invoiceH) / 2;

            var invoice = new Border
            {
                Width = invoiceW,
                Height = invoiceH,
                CornerRadius = new CornerRadius(4.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Canvas.SetLeft(invoice, invoiceLeft);
            Canvas.SetTop(invoice, invoiceTop);

            var invoiceLines = new StackPanel
            {
                Margin = new Thickness(6.0 * u, 8.0 * u, 6.0 * u, 8.0 * u),
                Spacing = 6.0 * u
            };

            for (int i = 0; i < 4; i++)
            {
                invoiceLines.Children.Add(new Border
                {
                    Height = 4.0 * u,
                    CornerRadius = new CornerRadius(2.0 * u),
                    Background = brush,
                    Opacity = 0.3,
                    HorizontalAlignment = i == 3 ? HorizontalAlignment.Left : HorizontalAlignment.Stretch,
                    Width = i == 3 ? 24.0 * u : double.NaN
                });
            }
            invoice.Child = invoiceLines;
            canvas.Children.Add(invoice);

            var baseStampSize = 32.0 * u;
            var stampSize = baseStampSize + (8.0 * u);
            var stampLeft = invoiceLeft + (invoiceW - stampSize) / 2 + 6.0 * u;
            var stampTop = invoiceTop + invoiceH - stampSize - 8.0 * u;
            var baseStampInnerSize = Math.Max(0.0, baseStampSize - (stroke * 4.0));
            var stampFontSize = Math.Max(4.0, baseStampInnerSize * 0.7);

            var stamp = new Border
            {
                Width = stampSize,
                Height = stampSize,
                CornerRadius = new CornerRadius(stampSize / 2),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke * 2),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Canvas.SetLeft(stamp, stampLeft);
            Canvas.SetTop(stamp, stampTop);

            var stampText = new TextBlock
            {
                Text = "OK",
                FontWeight = FontWeights.Bold,
                FontSize = stampFontSize,
                Foreground = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stamp.Child = stampText;
            canvas.Children.Add(stamp);

            stamp.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(stamp);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(stampSize / 2), (float)(stampSize / 2), 0);
                visual.Scale = new Vector3(2.5f, 2.5f, 1f);
                visual.Opacity = 0f;
                TrackVisual(visual);
                StartInvoiceStampAnimation(visual, compositor);
            };

            _rootGrid.Children.Add(canvas);
        }

        private void StartInvoiceStampAnimation(Visual visual, Compositor compositor)
        {
            if (!_isAnimating) return;

            const float durationMs = 2000f;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f / durationMs, new Vector3(2.5f, 2.5f, 1f));
            scale.InsertKeyFrame(300f / durationMs, new Vector3(0.9f, 0.9f, 1f));
            scale.InsertKeyFrame(400f / durationMs, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(1600f / durationMs, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(2000f / durationMs, new Vector3(2.5f, 2.5f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(durationMs);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f / durationMs, 0f);
            opacity.InsertKeyFrame(200f / durationMs, 1f);
            opacity.InsertKeyFrame(1600f / durationMs, 1f);
            opacity.InsertKeyFrame(1800f / durationMs, 0f);
            opacity.InsertKeyFrame(2000f / durationMs, 0f);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scale);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region ChartPulse

        private void BuildChartPulseVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;

            var host = new Grid
            {
                Width = size,
                Height = size
            };

            var canvas = new Canvas
            {
                Width = size,
                Height = size
            };
            host.Children.Add(canvas);

            var barCount = 5;
            var barWidth = 10.0 * u;
            var barSpacing = 6.0 * u;
            var totalW = barCount * barWidth + (barCount - 1) * barSpacing;
            var startX = (size - totalW) / 2;
            var baseY = size * 0.75;
            var maxH = size * 0.55;

            double[] targetHeights = [0.4, 0.7, 0.5, 0.9, 0.6];

            for (int i = 0; i < barCount; i++)
            {
                var barH = maxH * targetHeights[i];
                var bar = new Border
                {
                    Width = barWidth,
                    Height = barH,
                    CornerRadius = new CornerRadius(barWidth / 2, barWidth / 2, 0, 0),
                    Background = brush
                };
                Canvas.SetLeft(bar, startX + i * (barWidth + barSpacing));
                Canvas.SetTop(bar, baseY - barH);
                canvas.Children.Add(bar);

                var idx = i;
                bar.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(bar);
                    var compositor = visual.Compositor;
                    visual.CenterPoint = new Vector3((float)(barWidth / 2), (float)barH, 0);
                    TrackVisual(visual);
                    StartChartPulseBarAnimation(visual, compositor, idx);
                };
            }

            var sweepLine = new Border
            {
                Width = 3.0 * u,
                Height = maxH + 8.0 * u,
                Background = brush,
                Opacity = 0.5,
                CornerRadius = new CornerRadius(1.5 * u)
            };
            Canvas.SetLeft(sweepLine, startX - 8.0 * u);
            Canvas.SetTop(sweepLine, baseY - maxH - 4.0 * u);
            canvas.Children.Add(sweepLine);

            sweepLine.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(sweepLine, true);
                var visual = ElementCompositionPreview.GetElementVisual(sweepLine);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartChartPulseSweepAnimation(visual, compositor, (float)totalW, (float)(16.0 * u));
            };

            _rootGrid.Children.Add(host);
        }

        private void StartChartPulseBarAnimation(Visual visual, Compositor compositor, int barIndex)
        {
            if (!_isAnimating) return;

            var delay = barIndex * 150;
            const float durationMs = 1200f;

            var scaleY = compositor.CreateScalarKeyFrameAnimation();
            scaleY.InsertKeyFrame(0f, 0.3f);
            scaleY.InsertKeyFrame(0.5f, 1f);
            scaleY.InsertKeyFrame(1f, 0.3f);
            scaleY.Duration = TimeSpan.FromMilliseconds(durationMs);
            scaleY.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale.Y", scaleY, TimeSpan.FromMilliseconds(delay));
        }

        private void StartChartPulseSweepAnimation(Visual visual, Compositor compositor, float totalW, float offset)
        {
            if (!_isAnimating) return;

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.5f, new Vector3(totalW + offset, 0f, 0f));
            translation.InsertKeyFrame(0.51f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(2400);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
        }

        #endregion

        #region CalendarTick

        private void BuildCalendarTickVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);

            var canvas = new Canvas
            {
                Width = size,
                Height = size
            };

            var calW = 64.0 * u;
            var calH = 72.0 * u;
            var calLeft = (size - calW) / 2;
            var calTop = (size - calH) / 2 + 4.0 * u;

            var calendar = new Border
            {
                Width = calW,
                Height = calH,
                CornerRadius = new CornerRadius(6.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Canvas.SetLeft(calendar, calLeft);
            Canvas.SetTop(calendar, calTop);
            canvas.Children.Add(calendar);

            var headerH = 16.0 * u;
            var header = new Border
            {
                Width = calW,
                Height = headerH,
                CornerRadius = new CornerRadius(6.0 * u, 6.0 * u, 0, 0),
                Background = brush,
                Opacity = 0.3
            };
            Canvas.SetLeft(header, calLeft);
            Canvas.SetTop(header, calTop);
            canvas.Children.Add(header);

            var ringW = 4.0 * u;
            var ringH = 10.0 * u;
            for (int i = 0; i < 2; i++)
            {
                var ring = new Border
                {
                    Width = ringW,
                    Height = ringH,
                    CornerRadius = new CornerRadius(ringW / 2),
                    Background = brush
                };
                Canvas.SetLeft(ring, calLeft + 14.0 * u + i * 32.0 * u);
                Canvas.SetTop(ring, calTop - 4.0 * u);
                canvas.Children.Add(ring);
            }

            var checkSize = 28.0 * u;
            var checkLeft = calLeft + (calW - checkSize) / 2;
            var checkTop = calTop + headerH + (calH - headerH - checkSize) / 2;

            var check = new Path
            {
                Width = checkSize,
                Height = checkSize,
                Stroke = brush,
                StrokeThickness = stroke * 2,
                Stretch = Stretch.Uniform,
                Data = CreatePolylinePathGeometry(1.0, [
                    (4.0, 12.0), (9.0, 17.0), (20.0, 6.0)
                ])
            };
            PlatformCompatibility.SafeSetRoundedStroke(check, setLineJoin: true);
            Canvas.SetLeft(check, checkLeft);
            Canvas.SetTop(check, checkTop);
            canvas.Children.Add(check);

            check.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(check);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(checkSize / 2), (float)(checkSize / 2), 0);
                visual.Scale = new Vector3(0f, 0f, 1f);
                visual.Opacity = 0f;
                TrackVisual(visual);
                StartCalendarTickCheckAnimation(visual, compositor);
            };

            _rootGrid.Children.Add(canvas);
        }

        private void StartCalendarTickCheckAnimation(Visual visual, Compositor compositor)
        {
            if (!_isAnimating) return;

            const float durationMs = 2000f;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f / durationMs, new Vector3(0f, 0f, 1f));
            scale.InsertKeyFrame(400f / durationMs, new Vector3(1.2f, 1.2f, 1f));
            scale.InsertKeyFrame(500f / durationMs, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(1600f / durationMs, new Vector3(1f, 1f, 1f));
            scale.InsertKeyFrame(2000f / durationMs, new Vector3(0f, 0f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(durationMs);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f / durationMs, 0f);
            opacity.InsertKeyFrame(200f / durationMs, 1f);
            opacity.InsertKeyFrame(1600f / durationMs, 1f);
            opacity.InsertKeyFrame(1800f / durationMs, 0f);
            opacity.InsertKeyFrame(2000f / durationMs, 0f);
            opacity.Duration = TimeSpan.FromMilliseconds(durationMs);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Scale", scale);
            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        #endregion

        #region ApprovalFlow

        private void BuildApprovalFlowVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);

            var canvas = new Canvas
            {
                Width = size,
                Height = size
            };

            var nodeRadius = 10.0 * u;
            var nodeSpacing = 26.0 * u;
            var startX = (size - (3 * nodeRadius * 2 + 2 * nodeSpacing)) / 2 + nodeRadius;
            var centerY = size / 2;

            for (int i = 0; i < 2; i++)
            {
                var lineStartX = startX + nodeRadius + i * (nodeRadius * 2 + nodeSpacing);
                var line = new Border
                {
                    Width = nodeSpacing,
                    Height = stroke,
                    Background = brush,
                    Opacity = 0.3
                };
                Canvas.SetLeft(line, lineStartX);
                Canvas.SetTop(line, centerY - stroke / 2);
                canvas.Children.Add(line);
            }

            var innerDotSize = 8.0 * u;

            for (int i = 0; i < 3; i++)
            {
                var node = new Ellipse
                {
                    Width = nodeRadius * 2,
                    Height = nodeRadius * 2,
                    Stroke = brush,
                    StrokeThickness = stroke,
                    Fill = new SolidColorBrush(Colors.Transparent)
                };
                var nodeX = startX + i * (nodeRadius * 2 + nodeSpacing) - nodeRadius;
                Canvas.SetLeft(node, nodeX);
                Canvas.SetTop(node, centerY - nodeRadius);
                canvas.Children.Add(node);

                var innerDot = new Ellipse
                {
                    Width = innerDotSize,
                    Height = innerDotSize,
                    Fill = brush
                };
                Canvas.SetLeft(innerDot, nodeX + nodeRadius - innerDotSize / 2);
                Canvas.SetTop(innerDot, centerY - innerDotSize / 2);
                canvas.Children.Add(innerDot);

                var idx = i;
                innerDot.Loaded += (_, _) =>
                {
                    var visual = ElementCompositionPreview.GetElementVisual(innerDot);
                    var compositor = visual.Compositor;
                    visual.CenterPoint = new Vector3((float)(innerDotSize / 2), (float)(innerDotSize / 2), 0);
                    visual.Opacity = 0.2f;
                    TrackVisual(visual);
                    StartApprovalFlowInnerDotAnimation(visual, compositor, idx);
                };
            }

            _rootGrid.Children.Add(canvas);
        }

        private void StartApprovalFlowInnerDotAnimation(Visual visual, Compositor compositor, int nodeIndex)
        {
            if (!_isAnimating) return;

            var totalCycle = 2400;
            var nodeTime = totalCycle / 3.0f;
            var nodeStart = nodeIndex * nodeTime / totalCycle;
            var nodePeak = nodeStart + (nodeTime * 0.4f) / totalCycle;
            var nodeEnd = nodeStart + nodeTime / totalCycle;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 0.2f);
            if (nodeIndex > 0)
                opacity.InsertKeyFrame(nodeStart, 0.2f);
            opacity.InsertKeyFrame(nodePeak, 1f);
            opacity.InsertKeyFrame(nodeEnd, 0.5f);
            if (nodeIndex < 2)
                opacity.InsertKeyFrame(1f, 0.2f);
            else
                opacity.InsertKeyFrame(1f, 0.2f);
            opacity.Duration = TimeSpan.FromMilliseconds(totalCycle);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(0f, new Vector3(0.6f, 0.6f, 1f));
            if (nodeIndex > 0)
                scale.InsertKeyFrame(nodeStart, new Vector3(0.6f, 0.6f, 1f));
            scale.InsertKeyFrame(nodePeak, new Vector3(1.3f, 1.3f, 1f));
            scale.InsertKeyFrame(nodeEnd, new Vector3(1f, 1f, 1f));
            if (nodeIndex < 2)
                scale.InsertKeyFrame(1f, new Vector3(0.6f, 0.6f, 1f));
            else
                scale.InsertKeyFrame(1f, new Vector3(0.6f, 0.6f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(totalCycle);
            scale.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
            PlatformCompatibility.StartAnimation(visual, "Scale", scale);
        }

        #endregion

        #region BriefcaseSpin

        private void BuildBriefcaseSpinVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.5 * u);

            var canvas = new Canvas
            {
                Width = size,
                Height = size
            };

            var caseW = 60.0 * u;
            var caseH = 44.0 * u;
            var caseLeft = (size - caseW) / 2;
            var caseTop = (size - caseH) / 2 + 6.0 * u;

            var briefcase = new Border
            {
                Width = caseW,
                Height = caseH,
                CornerRadius = new CornerRadius(6.0 * u),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Canvas.SetLeft(briefcase, caseLeft);
            Canvas.SetTop(briefcase, caseTop);
            canvas.Children.Add(briefcase);

            var handleW = 24.0 * u;
            var handleH = 12.0 * u;
            var handleLeft = (size - handleW) / 2;
            var handleTop = caseTop - handleH + 2.0 * u;

            var handle = new Border
            {
                Width = handleW,
                Height = handleH,
                CornerRadius = new CornerRadius(4.0 * u, 4.0 * u, 0, 0),
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke, stroke, stroke, 0),
                Background = new SolidColorBrush(Colors.Transparent)
            };
            Canvas.SetLeft(handle, handleLeft);
            Canvas.SetTop(handle, handleTop);
            canvas.Children.Add(handle);

            var latchW = 16.0 * u;
            var latchH = 8.0 * u;
            var latch = new Border
            {
                Width = latchW,
                Height = latchH,
                CornerRadius = new CornerRadius(2.0 * u),
                Background = brush,
                Opacity = 0.6
            };
            Canvas.SetLeft(latch, (size - latchW) / 2);
            Canvas.SetTop(latch, caseTop + caseH / 2 - latchH / 2);
            canvas.Children.Add(latch);

            latch.Loaded += (_, _) =>
            {
                var visual = ElementCompositionPreview.GetElementVisual(latch);
                var compositor = visual.Compositor;
                TrackVisual(visual);
                StartBriefcaseLatchBlinkAnimation(visual, compositor);
            };

            var caseGroup = new Grid
            {
                Width = size,
                Height = size
            };
            foreach (var child in canvas.Children.ToArray())
            {
                canvas.Children.Remove(child);
                caseGroup.Children.Add(child);
            }

            caseGroup.Loaded += (_, _) =>
            {
                PlatformCompatibility.TrySetIsTranslationEnabled(caseGroup, true);
                var visual = ElementCompositionPreview.GetElementVisual(caseGroup);
                var compositor = visual.Compositor;
                visual.CenterPoint = new Vector3((float)(size / 2), (float)(size / 2), 0);
                TrackVisual(visual);
                TrackTranslationVisual(visual);
                StartBriefcaseSpinAnimation(caseGroup, visual, compositor, (float)u);
            };

            _rootGrid.Children.Add(caseGroup);
        }

        private void StartBriefcaseLatchBlinkAnimation(Visual visual, Compositor compositor)
        {
            if (!_isAnimating) return;

            var opacity = compositor.CreateScalarKeyFrameAnimation();
            opacity.InsertKeyFrame(0f, 0.6f);
            opacity.InsertKeyFrame(0.5f, 1f);
            opacity.InsertKeyFrame(1f, 0.6f);
            opacity.Duration = TimeSpan.FromMilliseconds(1200);
            opacity.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Opacity", opacity);
        }

        private void StartBriefcaseSpinAnimation(UIElement element, Visual visual, Compositor compositor, float u)
        {
            if (!_isAnimating) return;

            if (PlatformCompatibility.IsSkiaBackend || PlatformCompatibility.IsWasmBackend)
            {
                var storyboard = PlatformCompatibility.StartRotationKeyframes(
                    element,
                    [(0d, -5d), (0.5d, 5d), (1d, -5d)],
                    TimeSpan.FromMilliseconds(1200));
                TrackStoryboard(storyboard);
            }
            else
            {
                PlatformCompatibility.StartRotationAnimationInDegrees(
                    visual,
                    [(0f, -5f), (0.5f, 5f), (1f, -5f)],
                    TimeSpan.FromMilliseconds(1200));
            }

            var translation = compositor.CreateVector3KeyFrameAnimation();
            translation.InsertKeyFrame(0f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.25f, new Vector3(0f, -4f * u, 0f));
            translation.InsertKeyFrame(0.5f, new Vector3(0f, 0f, 0f));
            translation.InsertKeyFrame(0.75f, new Vector3(0f, -4f * u, 0f));
            translation.InsertKeyFrame(1f, new Vector3(0f, 0f, 0f));
            translation.Duration = TimeSpan.FromMilliseconds(1200);
            translation.IterationBehavior = AnimationIterationBehavior.Forever;

            PlatformCompatibility.StartAnimation(visual, "Translation", translation);
        }

        #endregion
    }
}
