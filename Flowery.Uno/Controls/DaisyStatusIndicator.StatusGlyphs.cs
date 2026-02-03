namespace Flowery.Controls
{
    /// <summary>
    /// Status glyph visual builders for Battery, TrafficLight, WiFi, and Cellular indicators.
    /// </summary>
    public partial class DaisyStatusIndicator
    {
        private void BuildBatteryStatusVisual(double size, Brush brush)
        {
            if (_rootGrid == null) return;

            // All sizing tokens are integers and sum to Size for each DaisySize.
            var barWidth = (int)DaisyResourceLookup.GetBatteryBarWidth(Size);
            var barGap = (int)DaisyResourceLookup.GetBatteryBarGap(Size);
            var stroke = (int)DaisyResourceLookup.GetBatteryStroke(Size);
            var barCorner = (int)DaisyResourceLookup.GetBatteryBarCornerRadius(Size);
            var shellCorner = (int)DaisyResourceLookup.GetBatteryShellCornerRadius(Size);
            var tipWidth = (int)DaisyResourceLookup.GetBatteryTipWidth(Size);
            var tipCorner = (int)DaisyResourceLookup.GetBatteryTipCornerRadius(Size);
            var padding = (int)DaisyResourceLookup.GetBatteryInternalPadding(Size);

            const int barCount = 4;

            var interiorWidth = (barCount * barWidth) + ((barCount - 1) * barGap);
            var bodyWidth = interiorWidth + (2 * padding) + (2 * stroke);

            // Bar height based on size, rounded to int
            var barHeight = Math.Max(4, (int)Math.Round(size * 0.35));
            var bodyHeight = barHeight + (2 * padding) + (2 * stroke);

            var tipHeight = Math.Max(2, (int)Math.Round(bodyHeight * 0.5));
            var tipOverlap = tipWidth > 0 ? stroke : 0;
            var totalWidth = bodyWidth + tipWidth - tipOverlap;

            var containerSize = (int)size;
            var containerWidth = Math.Max(containerSize, totalWidth);
            var containerHeight = Math.Max(containerSize, bodyHeight);

            _rootGrid.Width = containerWidth;
            _rootGrid.Height = containerHeight;

            var horizontalExtra = containerWidth - totalWidth;
            var verticalExtra = containerHeight - bodyHeight;
            var leftOffset = horizontalExtra / 2;
            var rightOffset = horizontalExtra - leftOffset;
            var topOffset = verticalExtra / 2;
            var bottomOffset = verticalExtra - topOffset;

            // Calculate charge state
            var chargePercent = Math.Clamp(BatteryChargePercent, 0d, 100d);
            var fillBars = (int)Math.Round(chargePercent / 100d * barCount, MidpointRounding.AwayFromZero);
            fillBars = Math.Clamp(fillBars, 0, barCount);

            const double activeOpacity = 1.0;
            const double inactiveOpacity = 0.15;

            // Build the bars using StackPanel with Spacing - guarantees uniform gaps
            var barsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Width = interiorWidth,
                Height = barHeight,
                Spacing = barGap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            for (var i = 0; i < barCount; i++)
            {
                var bar = new Border
                {
                    Width = barWidth,
                    Height = barHeight,
                    Background = brush,
                    CornerRadius = new CornerRadius(barCorner),
                    Opacity = i < fillBars ? activeOpacity : inactiveOpacity
                };
                barsPanel.Children.Add(bar);
            }

            // Battery shell (outline) wrapping the bars
            var body = new Border
            {
                Width = bodyWidth,
                Height = bodyHeight,
                BorderThickness = new Thickness(stroke),
                BorderBrush = brush,
                CornerRadius = new CornerRadius(shellCorner),
                Padding = new Thickness(padding),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Child = barsPanel,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Battery tip (positive terminal)
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
                    Margin = new Thickness(-tipOverlap, 0, 0, 0) // Overlap with body by stroke width
                };
            }

            // Horizontal layout: body + tip
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
        }

        private void BuildTrafficLightStatusVisual(double size, double rotationAngle)
        {
            if (_rootGrid == null) return;

            var stroke = Math.Max(1, size * 0.08);
            var borderBrush = DaisyResourceLookup.GetBrush(
                "DaisyBaseContentBrush",
                new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 230, 230)));

            var redBrush = new SolidColorBrush(Colors.IndianRed);
            var yellowBrush = new SolidColorBrush(Colors.Gold);
            var greenBrush = new SolidColorBrush(Colors.LimeGreen);

            var bodyWidth = size * 0.55;
            var bodyHeight = size * 0.9;
            var corner = Math.Max(1, bodyWidth * 0.25);

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
                Margin = new Thickness(Math.Max(1, size * 0.08))
            };
            lights.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            lights.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            lights.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            const double activeOpacity = 1.0;
            const double inactiveOpacity = 0.2;

            var red = new Ellipse { Fill = redBrush };
            var yellow = new Ellipse { Fill = yellowBrush };
            var green = new Ellipse { Fill = greenBrush };

            red.Opacity = TrafficLightActive == DaisyTrafficLightState.Red ? activeOpacity : inactiveOpacity;
            yellow.Opacity = TrafficLightActive == DaisyTrafficLightState.Yellow ? activeOpacity : inactiveOpacity;
            green.Opacity = TrafficLightActive == DaisyTrafficLightState.Green ? activeOpacity : inactiveOpacity;

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
        }

        private void BuildWifiSignalVisual(double size, Brush brush)
        {
            if (_rootGrid == null) return;

            // WiFi signal indicator with 3 arc bars (classic WiFi icon style)
            // The bars are drawn as arc segments radiating from a center dot
            const int barCount = 3;
            var signalLevel = Math.Clamp(SignalStrength, 0, barCount);

            var stroke = Math.Max(1.5, size * 0.08);
            var centerDotRadius = Math.Max(2, size * 0.08);
            var arcSpacing = (size * 0.38) / barCount;

            const double activeOpacity = 1.0;
            const double inactiveOpacity = 0.2;

            // Create container with proper sizing
            var containerHeight = size;
            var containerWidth = size;
            _rootGrid.Width = containerWidth;
            _rootGrid.Height = containerHeight;

            var canvas = new Canvas
            {
                Width = containerWidth,
                Height = containerHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Center point is at bottom center
            var centerX = containerWidth / 2;
            var centerY = containerHeight * 0.85;

            // Draw the center dot
            var centerDot = new Ellipse
            {
                Width = centerDotRadius * 2,
                Height = centerDotRadius * 2,
                Fill = brush,
                Opacity = signalLevel > 0 ? activeOpacity : inactiveOpacity
            };
            Canvas.SetLeft(centerDot, centerX - centerDotRadius);
            Canvas.SetTop(centerDot, centerY - centerDotRadius);
            canvas.Children.Add(centerDot);

            // Draw arc bars from inside out
            for (var i = 0; i < barCount; i++)
            {
                var radius = centerDotRadius + ((i + 1) * arcSpacing) + (stroke / 2);
                var arcAngle = 120.0; // degrees
                var startAngle = 180 + ((180 - arcAngle) / 2); // centered at top
                var endAngle = startAngle + arcAngle;

                // Create arc path
                var startRad = startAngle * Math.PI / 180;
                var endRad = endAngle * Math.PI / 180;

                var startX = centerX + (radius * Math.Cos(startRad));
                var startY = centerY + (radius * Math.Sin(startRad));
                var endX = centerX + (radius * Math.Cos(endRad));
                var endY = centerY + (radius * Math.Sin(endRad));

                var arcPath = new Path
                {
                    Stroke = brush,
                    StrokeThickness = stroke,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Opacity = (i + 1) <= signalLevel ? activeOpacity : inactiveOpacity,
                    Data = new PathGeometry
                    {
                        Figures =
                        {
                            new PathFigure
                            {
                                StartPoint = new Windows.Foundation.Point(startX, startY),
                                IsClosed = false,
                                Segments =
                                {
                                    new ArcSegment
                                    {
                                        Point = new Windows.Foundation.Point(endX, endY),
                                        Size = new Windows.Foundation.Size(radius, radius),
                                        SweepDirection = SweepDirection.Clockwise,
                                        IsLargeArc = arcAngle > 180
                                    }
                                }
                            }
                        }
                    }
                };

                canvas.Children.Add(arcPath);
            }

            _rootGrid.Children.Add(canvas);
        }

        private void BuildCellularSignalVisual(double size, Brush brush)
        {
            if (_rootGrid == null) return;

            // Cellular/5G signal indicator with 5 ascending bars
            const int barCount = 5;
            var signalLevel = Math.Clamp(SignalStrength, 0, barCount);

            var stroke = Math.Max(1, size * 0.06);
            var barGap = Math.Max(1, size * 0.04);
            var barWidth = Math.Max(2, (size - ((barCount - 1) * barGap) - (2 * stroke)) / barCount);
            var minBarHeight = Math.Max(3, size * 0.15);
            var maxBarHeight = size * 0.85;
            var barCorner = Math.Max(1, barWidth * 0.25);

            const double activeOpacity = 1.0;
            const double inactiveOpacity = 0.2;

            var totalWidth = (barCount * barWidth) + ((barCount - 1) * barGap);
            var containerWidth = Math.Max(size, totalWidth);
            _rootGrid.Width = containerWidth;
            _rootGrid.Height = size;

            var barsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = barGap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            for (var i = 0; i < barCount; i++)
            {
                // Height increases linearly from min to max
                var heightFraction = (double)(i + 1) / barCount;
                var barHeight = minBarHeight + ((maxBarHeight - minBarHeight) * heightFraction);

                var bar = new Border
                {
                    Width = barWidth,
                    Height = barHeight,
                    Background = brush,
                    CornerRadius = new CornerRadius(barCorner, barCorner, 0, 0),
                    Opacity = (i + 1) <= signalLevel ? activeOpacity : inactiveOpacity
                };

                barsPanel.Children.Add(bar);
            }

            _rootGrid.Children.Add(barsPanel);
        }

        private static Ellipse CreateFillRing(double size, Brush brush, double opacity)
        {
            return new Ellipse
            {
                Width = size,
                Height = size,
                Fill = brush,
                Opacity = opacity,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };
        }

        private static Ellipse CreateStrokeRing(double size, Brush brush, double thickness, double opacity)
        {
            return new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = brush,
                StrokeThickness = thickness,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Opacity = opacity,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
            };
        }
    }
}
