namespace Flowery.Controls
{
    /// <summary>
    /// Overlay visual builders for animated status indicator variants.
    /// </summary>
    public partial class DaisyStatusIndicator
    {
        private void BuildVariantOverlay(Ellipse dot, double size, Brush brush)
        {
            switch (Variant)
            {
                case DaisyStatusIndicatorVariant.Ping:
                    BuildPingOverlay(size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Ripple:
                    BuildRippleOverlay(size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Sonar:
                    BuildSonarOverlay(size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Splash:
                    BuildSplashOverlay(size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Glow:
                    BuildGlowOverlay(size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Ring:
                    BuildRingOverlay(size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Radar:
                    BuildRadarOverlay(dot, size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Beacon:
                    BuildBeaconOverlay(dot, size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Spin:
                    BuildSpinOverlay(size, brush);
                    break;
                case DaisyStatusIndicatorVariant.Orbit:
                    BuildOrbitOverlay(size, brush);
                    break;
            }
        }

        private void BuildPingOverlay(double size, Brush brush)
        {
            var pingRing = CreateFillRing(size, brush, 0.75);
            _rootGrid!.Children.Insert(0, pingRing);
            _ringAnimations.Add(new RingAnimationData(pingRing, size, 0.75f, 0f, 2f, 1000));
            pingRing.Loaded += (s, e) => StartRingExpandFadeAnimation(pingRing, size, 0.75f, 0f, 2f, 1000);
        }

        private void BuildRippleOverlay(double size, Brush brush)
        {
            var r1 = CreateFillRing(size, brush, 0.35);
            var r2 = CreateFillRing(size, brush, 0.25);
            var r3 = CreateFillRing(size, brush, 0.2);
            _rootGrid!.Children.Insert(0, r1);
            _rootGrid.Children.Insert(0, r2);
            _rootGrid.Children.Insert(0, r3);

            _ringAnimations.Add(new RingAnimationData(r1, size, 0.35f, 0f, 2.1f, 1200, 0));
            _ringAnimations.Add(new RingAnimationData(r2, size, 0.25f, 0f, 2.1f, 1200, 400));
            _ringAnimations.Add(new RingAnimationData(r3, size, 0.2f, 0f, 2.1f, 1200, 800));
            r1.Loaded += (s, e) => StartRingExpandFadeAnimation(r1, size, 0.35f, 0f, 2.1f, 1200, delayMs: 0);
            r2.Loaded += (s, e) => StartRingExpandFadeAnimation(r2, size, 0.25f, 0f, 2.1f, 1200, delayMs: 400);
            r3.Loaded += (s, e) => StartRingExpandFadeAnimation(r3, size, 0.2f, 0f, 2.1f, 1200, delayMs: 800);
        }

        private void BuildSonarOverlay(double size, Brush brush)
        {
            var s1 = CreateStrokeRing(size, brush, thickness: 1, opacity: 0.55);
            var s2 = CreateStrokeRing(size, brush, thickness: 1, opacity: 0.4);
            _rootGrid!.Children.Insert(0, s1);
            _rootGrid.Children.Insert(0, s2);

            _ringAnimations.Add(new RingAnimationData(s1, size, 0.55f, 0f, 2.2f, 1400, 0));
            _ringAnimations.Add(new RingAnimationData(s2, size, 0.4f, 0f, 2.2f, 1400, 700));
            s1.Loaded += (s, e) => StartRingExpandFadeAnimation(s1, size, 0.55f, 0f, 2.2f, 1400, delayMs: 0);
            s2.Loaded += (s, e) => StartRingExpandFadeAnimation(s2, size, 0.4f, 0f, 2.2f, 1400, delayMs: 700);
        }

        private void BuildSplashOverlay(double size, Brush brush)
        {
            var sp1 = CreateStrokeRing(size, brush, thickness: 1, opacity: 0.6);
            var sp2 = CreateStrokeRing(size, brush, thickness: 1, opacity: 0.45);
            _rootGrid!.Children.Insert(0, sp1);
            _rootGrid.Children.Insert(0, sp2);

            _ringAnimations.Add(new RingAnimationData(sp1, size, 0.6f, 0f, 2.0f, 900, 0));
            _ringAnimations.Add(new RingAnimationData(sp2, size, 0.45f, 0f, 2.0f, 900, 300));
            sp1.Loaded += (s, e) => StartRingExpandFadeAnimation(sp1, size, 0.6f, 0f, 2.0f, 900, delayMs: 0);
            sp2.Loaded += (s, e) => StartRingExpandFadeAnimation(sp2, size, 0.45f, 0f, 2.0f, 900, delayMs: 300);
        }

        private void BuildGlowOverlay(double size, Brush brush)
        {
            var halo = CreateFillRing(size, brush, 0.18);
            _rootGrid!.Children.Insert(0, halo);
            _animatedContainer = halo;
            halo.Loaded += (s, e) => StartGlowAnimation(halo, size);
        }

        private void BuildRingOverlay(double size, Brush brush)
        {
            var ring = CreateStrokeRing(size, brush, thickness: 1, opacity: 0.65);
            _rootGrid!.Children.Insert(0, ring);
            _ringAnimations.Add(new RingAnimationData(ring, size, 0.65f, 0f, 2.2f, 1200));
            ring.Loaded += (s, e) => StartRingExpandFadeAnimation(ring, size, 0.65f, 0f, 2.2f, 1200);
        }

        private void BuildRadarOverlay(Ellipse dot, double size, Brush brush)
        {
            dot.Opacity = 0.4;
            var orbitContainer = new Grid
            {
                Width = size * 2.2,
                Height = size * 2.2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var sweepPath = CreateRadarSweepPath(size, brush);
            orbitContainer.Children.Add(sweepPath);
            _rootGrid!.Children.Add(orbitContainer);
            _animatedContainer = orbitContainer;

            orbitContainer.Loaded += (s, e) => StartOrbitRotationAnimation(orbitContainer, size * 1.1, 2000);
        }

        private Path CreateRadarSweepPath(double size, Brush brush)
        {
            var sweepRadius = size * 1.1;
            var sweepAngle = 60;

            var pathFigure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(sweepRadius, sweepRadius),
                IsClosed = true
            };

            var startAngleRad = -sweepAngle / 2.0 * Math.PI / 180.0 - Math.PI / 2;
            var endAngleRad = sweepAngle / 2.0 * Math.PI / 180.0 - Math.PI / 2;

            var startX = sweepRadius + sweepRadius * Math.Cos(startAngleRad);
            var startY = sweepRadius + sweepRadius * Math.Sin(startAngleRad);
            var endX = sweepRadius + sweepRadius * Math.Cos(endAngleRad);
            var endY = sweepRadius + sweepRadius * Math.Sin(endAngleRad);

            pathFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(startX, startY) });
            pathFigure.Segments.Add(new ArcSegment
            {
                Point = new Windows.Foundation.Point(endX, endY),
                Size = new Windows.Foundation.Size(sweepRadius, sweepRadius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = false
            });

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            var baseColor = ((SolidColorBrush)brush).Color;
            var gradientBrush = new RadialGradientBrush
            {
                Center = new Windows.Foundation.Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientStops =
                {
                    new GradientStop { Color = baseColor, Offset = 0.3 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(120, baseColor.R, baseColor.G, baseColor.B), Offset = 0.7 },
                    new GradientStop { Color = Windows.UI.Color.FromArgb(40, baseColor.R, baseColor.G, baseColor.B), Offset = 1.0 }
                }
            };

            return new Path { Data = pathGeometry, Fill = gradientBrush, Opacity = 0.85 };
        }

        private void BuildBeaconOverlay(Ellipse dot, double size, Brush brush)
        {
            dot.Opacity = 0.3;
            var beaconRing = CreateFillRing(size, brush, 1);
            _rootGrid!.Children.Insert(0, beaconRing);
            _animatedContainer = beaconRing;
            beaconRing.Loaded += (s, e) => StartBeaconRingBurstAnimation(beaconRing, size);
        }

        private void BuildSpinOverlay(double size, Brush brush)
        {
            var spinContainer = new Grid
            {
                Width = size * 2,
                Height = size * 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var satelliteSize = Math.Max(2, size * 0.25);

            var satellite1 = new Ellipse
            {
                Width = satelliteSize,
                Height = satelliteSize,
                Fill = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 1, 0, 0),
                Opacity = 0.85
            };

            var satellite2 = new Ellipse
            {
                Width = satelliteSize,
                Height = satelliteSize,
                Fill = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 1),
                Opacity = 0.85
            };

            spinContainer.Children.Add(satellite1);
            spinContainer.Children.Add(satellite2);
            _rootGrid!.Children.Add(spinContainer);
            _animatedContainer = spinContainer;

            spinContainer.Loaded += (s, e) => StartOrbitRotationAnimation(spinContainer, size, 800);
        }

        private void BuildOrbitOverlay(double size, Brush brush)
        {
            var orbitContainer = new Grid
            {
                Width = size * 2,
                Height = size * 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var satellite = new Ellipse
            {
                Width = Math.Max(2, size * 0.35),
                Height = Math.Max(2, size * 0.35),
                Fill = brush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 1, 0, 0),
                Opacity = 0.85
            };

            orbitContainer.Children.Add(satellite);
            _rootGrid!.Children.Add(orbitContainer);
            _animatedContainer = orbitContainer;

            orbitContainer.Loaded += (s, e) => StartOrbitRotationAnimation(orbitContainer, size, 1300);
        }
    }
}
