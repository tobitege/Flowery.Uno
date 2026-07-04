namespace Flowery.Controls
{
    public partial class DaisyLoading
    {
        #region Extended business helpers

        private void StartDaisyLoadingFrameTimer(int frameCount, int intervalMs, Action<int> applyFrame)
        {
            if (!_isAnimating || frameCount <= 0) return;

            var frame = 0;
            applyFrame(frame);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };
            timer.Tick += (_, _) =>
            {
                if (!_isAnimating)
                {
                    timer.Stop();
                    return;
                }

                frame = (frame + 1) % frameCount;
                applyFrame(frame);
            };
            timer.Start();
            TrackTimer(timer);
        }

        private static Border CreateOutlinedBox(Brush brush, double width, double height, double stroke, double corner = 0)
        {
            return new Border
            {
                Width = width,
                Height = height,
                BorderBrush = brush,
                BorderThickness = new Thickness(stroke),
                CornerRadius = new CornerRadius(corner),
                Background = new SolidColorBrush(Colors.Transparent)
            };
        }

        private static Border CreateFilledBox(Brush brush, double width, double height, double corner = 0, double opacity = 1)
        {
            return new Border
            {
                Width = width,
                Height = height,
                Background = brush,
                CornerRadius = new CornerRadius(corner),
                Opacity = opacity
            };
        }

        private static TextBlock CreateLoaderGlyph(string text, Brush brush, double fontSize)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = brush,
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static Brush GetDaisyLoadingResourceBrush(string key, Brush fallback)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Brush brush)
            {
                return brush;
            }

            return fallback;
        }

        #endregion

        #region PrinterOutput

        private void BuildPrinterOutputVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var input = CreateOutlinedBox(brush, 46.0 * u, 20.0 * u, stroke, 2.0 * u);
            Canvas.SetLeft(input, 25.0 * u);
            Canvas.SetTop(input, 14.0 * u);
            input.Opacity = 0.45;
            canvas.Children.Add(input);

            var body = CreateOutlinedBox(brush, 62.0 * u, 30.0 * u, stroke, 4.0 * u);
            Canvas.SetLeft(body, 17.0 * u);
            Canvas.SetTop(body, 34.0 * u);
            canvas.Children.Add(body);

            var slot = CreateFilledBox(brush, 42.0 * u, Math.Max(1, 3.0 * u), 1.5 * u, 0.45);
            Canvas.SetLeft(slot, 27.0 * u);
            Canvas.SetTop(slot, 45.0 * u);
            canvas.Children.Add(slot);

            var led = CreateFilledBox(brush, 6.0 * u, 6.0 * u, 1.5 * u, 0.3);
            Canvas.SetLeft(led, 66.0 * u);
            Canvas.SetTop(led, 40.0 * u);
            canvas.Children.Add(led);

            var page = CreateDocumentPage(brush, 42.0 * u, 34.0 * u, stroke);
            Canvas.SetLeft(page, 27.0 * u);
            Canvas.SetTop(page, 55.0 * u);
            canvas.Children.Add(page);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(8, 180, frame =>
            {
                var t = frame / 7.0;
                Canvas.SetTop(page, (50.0 + 24.0 * t) * u);
                page.Opacity = frame is 0 or 7 ? 0.1 : 1.0;
                led.Opacity = frame % 2 == 0 ? 1.0 : 0.25;
            });
        }

        #endregion

        #region PaperShredder

        private void BuildPaperShredderVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var head = CreateOutlinedBox(brush, 62.0 * u, 22.0 * u, stroke, 4.0 * u);
            Canvas.SetLeft(head, 17.0 * u);
            Canvas.SetTop(head, 38.0 * u);
            canvas.Children.Add(head);

            var paper = CreateDocumentPage(brush, 36.0 * u, 44.0 * u, stroke);
            Canvas.SetLeft(paper, 30.0 * u);
            Canvas.SetTop(paper, 6.0 * u);
            canvas.Children.Add(paper);

            var strips = new List<Border>();
            for (var i = 0; i < 6; i++)
            {
                var strip = CreateFilledBox(brush, 4.0 * u, 28.0 * u, 1.5 * u, 0.25);
                Canvas.SetLeft(strip, (31.0 + i * 6.0) * u);
                Canvas.SetTop(strip, 58.0 * u);
                canvas.Children.Add(strip);
                strips.Add(strip);
            }

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(6, 180, frame =>
            {
                Canvas.SetTop(paper, (6.0 + frame * 5.0) * u);
                paper.Opacity = frame < 5 ? 1.0 : 0.0;
                for (var i = 0; i < strips.Count; i++)
                {
                    strips[i].Opacity = frame > i / 2 ? 0.95 : 0.2;
                    Canvas.SetTop(strips[i], (58.0 + ((frame + i) % 3) * 4.0) * u);
                }
            });
        }

        #endregion

        #region SignaturePen

        private void BuildSignaturePenVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var paper = CreateDocumentPage(brush, 60.0 * u, 68.0 * u, stroke);
            Canvas.SetLeft(paper, 18.0 * u);
            Canvas.SetTop(paper, 14.0 * u);
            canvas.Children.Add(paper);

            var line = new Path
            {
                Stroke = brush,
                StrokeThickness = stroke * 1.6,
                Opacity = 0.25,
                Data = CreatePolylinePathGeometry(u, [(26.0, 64.0), (36.0, 57.0), (45.0, 65.0), (58.0, 55.0), (69.0, 62.0)])
            };
            PlatformCompatibility.SafeSetRoundedStroke(line, setLineJoin: true);
            canvas.Children.Add(line);

            var pen = new Path
            {
                Stroke = brush,
                StrokeThickness = stroke * 2,
                Data = CreatePolylinePathGeometry(u, [(0.0, 0.0), (18.0, -18.0)])
            };
            PlatformCompatibility.SafeSetRoundedStroke(pen, setLineJoin: true);
            canvas.Children.Add(pen);

            _rootGrid.Children.Add(canvas);

            var points = new[] { (26.0, 64.0), (36.0, 57.0), (45.0, 65.0), (58.0, 55.0), (69.0, 62.0) };
            StartDaisyLoadingFrameTimer(points.Length, 170, frame =>
            {
                var (x, y) = points[frame];
                Canvas.SetLeft(pen, x * u);
                Canvas.SetTop(pen, y * u);
                line.Opacity = 0.25 + (frame + 1) * 0.15;
            });
        }

        #endregion

        #region DocumentScan

        private void BuildDocumentScanVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var document = CreateDocumentPage(brush, 58.0 * u, 72.0 * u, stroke);
            Canvas.SetLeft(document, 19.0 * u);
            Canvas.SetTop(document, 12.0 * u);
            canvas.Children.Add(document);

            var beam = CreateFilledBox(brush, 58.0 * u, 5.0 * u, 2.0 * u, 0.75);
            Canvas.SetLeft(beam, 19.0 * u);
            Canvas.SetTop(beam, 14.0 * u);
            canvas.Children.Add(beam);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(10, 120, frame =>
            {
                Canvas.SetTop(beam, (14.0 + frame * 6.5) * u);
                beam.Opacity = frame is 0 or 9 ? 0.25 : 0.9;
            });
        }

        #endregion

        #region FolderSync

        private void BuildFolderSyncVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var leftFolder = CreateWin95Folder(brush, 30.0 * u, 24.0 * u, 13.0 * u, 6.0 * u, stroke, u);
            Canvas.SetLeft(leftFolder, 11.0 * u);
            Canvas.SetTop(leftFolder, 42.0 * u);
            canvas.Children.Add(leftFolder);

            var rightFolder = CreateWin95Folder(brush, 30.0 * u, 24.0 * u, 13.0 * u, 6.0 * u, stroke, u);
            Canvas.SetLeft(rightFolder, 55.0 * u);
            Canvas.SetTop(rightFolder, 42.0 * u);
            canvas.Children.Add(rightFolder);

            var document = CreateWin95Paper(brush, 12.0 * u, 16.0 * u, stroke * 0.7, u);
            Canvas.SetLeft(document, 26.0 * u);
            Canvas.SetTop(document, 28.0 * u);
            canvas.Children.Add(document);

            var arrow = new Path
            {
                Stroke = brush,
                StrokeThickness = stroke * 1.5,
                Data = CreatePolylinePathGeometry(u, [(36.0, 30.0), (48.0, 20.0), (60.0, 30.0), (55.0, 30.0), (55.0, 36.0)])
            };
            PlatformCompatibility.SafeSetRoundedStroke(arrow, setLineJoin: true);
            canvas.Children.Add(arrow);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(8, 160, frame =>
            {
                var forward = frame < 4;
                var step = forward ? frame : 7 - frame;
                Canvas.SetLeft(document, (26.0 + step * 10.0) * u);
                document.Opacity = frame is 3 or 4 ? 0.35 : 1.0;
            });
        }

        #endregion

        #region MailReceive

        private void BuildMailReceiveVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var envelope = CreateOutlinedBox(brush, 66.0 * u, 42.0 * u, stroke, 5.0 * u);
            Canvas.SetLeft(envelope, 15.0 * u);
            Canvas.SetTop(envelope, 43.0 * u);
            canvas.Children.Add(envelope);

            var flap = new Path
            {
                Stroke = brush,
                StrokeThickness = stroke,
                Opacity = 0.45,
                Data = CreatePolylinePathGeometry(u, [(17.0, 47.0), (48.0, 69.0), (79.0, 47.0)])
            };
            canvas.Children.Add(flap);

            var paper = CreateDocumentPage(brush, 46.0 * u, 36.0 * u, stroke);
            Canvas.SetLeft(paper, 25.0 * u);
            Canvas.SetTop(paper, 10.0 * u);
            canvas.Children.Add(paper);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(8, 170, frame =>
            {
                var y = 10.0 + Math.Min(frame, 5) * 6.5;
                Canvas.SetTop(paper, y * u);
                paper.Opacity = frame > 5 ? 0.2 : 1.0;
            });
        }

        #endregion

        #region PhoneRing

        private void BuildPhoneRingVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 6.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var handset = new Path
            {
                Stroke = brush,
                StrokeThickness = stroke,
                Fill = new SolidColorBrush(Colors.Transparent),
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                RenderTransform = new RotateTransform(),
                Data = CreatePhoneHandsetGeometry(u)
            };
            PlatformCompatibility.SafeSetRoundedStroke(handset, setLineJoin: true);
            canvas.Children.Add(handset);

            var ring1 = CreatePhoneRingArc(brush, stroke * 0.5, u, [(66.0, 20.0), (75.0, 26.0), (79.0, 34.0), (79.0, 44.0)]);
            var ring2 = CreatePhoneRingArc(brush, stroke * 0.5, u, [(77.0, 11.0), (89.0, 22.0), (93.0, 36.0), (90.0, 51.0)]);
            canvas.Children.Add(ring1);
            canvas.Children.Add(ring2);

            var soundDot = new Ellipse
            {
                Width = 5.0 * u,
                Height = 5.0 * u,
                Fill = brush
            };
            Canvas.SetLeft(soundDot, 81.0 * u);
            Canvas.SetTop(soundDot, 47.0 * u);
            canvas.Children.Add(soundDot);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(4, 180, frame =>
            {
                if (handset.RenderTransform is RotateTransform transform)
                {
                    transform.Angle = frame % 2 == 0 ? -6.0 : 6.0;
                }

                ring1.Opacity = frame is 1 or 2 ? 0.75 : 0.0;
                ring2.Opacity = frame is 2 or 3 ? 0.55 : 0.0;
                soundDot.Opacity = frame % 2 == 0 ? 0.25 : 0.85;
            });
        }

        private static PathGeometry CreatePhoneHandsetGeometry(double scale)
        {
            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(28.0 * scale, 34.0 * scale),
                IsClosed = true
            };

            figure.Segments.Add(new BezierSegment
            {
                Point1 = new Windows.Foundation.Point(38.0 * scale, 52.0 * scale),
                Point2 = new Windows.Foundation.Point(44.0 * scale, 58.0 * scale),
                Point3 = new Windows.Foundation.Point(62.0 * scale, 68.0 * scale)
            });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(71.0 * scale, 58.0 * scale) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(62.0 * scale, 49.0 * scale) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(55.0 * scale, 54.0 * scale) });
            figure.Segments.Add(new BezierSegment
            {
                Point1 = new Windows.Foundation.Point(48.0 * scale, 50.0 * scale),
                Point2 = new Windows.Foundation.Point(43.0 * scale, 45.0 * scale),
                Point3 = new Windows.Foundation.Point(40.0 * scale, 38.0 * scale)
            });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(45.0 * scale, 31.0 * scale) });
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(36.0 * scale, 22.0 * scale) });

            geometry.Figures.Add(figure);
            return geometry;
        }

        private static Path CreatePhoneRingArc(Brush brush, double stroke, double scale, (double x, double y)[] points)
        {
            var path = new Path
            {
                Stroke = brush,
                StrokeThickness = Math.Max(1, stroke),
                Opacity = 0,
                Data = CreateBezierPathGeometry(scale, points)
            };

            PlatformCompatibility.SafeSetRoundedStroke(path, setLineJoin: false);
            return path;
        }

        private static PathGeometry CreateBezierPathGeometry(double scale, (double x, double y)[] points)
        {
            var geometry = new PathGeometry();
            if (points.Length != 4)
            {
                return geometry;
            }

            var figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(points[0].x * scale, points[0].y * scale)
            };
            figure.Segments.Add(new BezierSegment
            {
                Point1 = new Windows.Foundation.Point(points[1].x * scale, points[1].y * scale),
                Point2 = new Windows.Foundation.Point(points[2].x * scale, points[2].y * scale),
                Point3 = new Windows.Foundation.Point(points[3].x * scale, points[3].y * scale)
            });

            geometry.Figures.Add(figure);
            return geometry;
        }

        #endregion

        #region CoinStack

        private void BuildCoinStackVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var separatorBrush = GetDaisyLoadingResourceBrush("DaisyBase100Brush", new SolidColorBrush(Colors.Transparent));
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var stack = new List<Ellipse>();
            for (var i = 0; i < 3; i++)
            {
                var coin = new Ellipse
                {
                    Width = 44.0 * u,
                    Height = 12.0 * u,
                    Stroke = separatorBrush,
                    StrokeThickness = stroke,
                    Fill = brush,
                    Opacity = 0.0
                };
                Canvas.SetLeft(coin, 26.0 * u);
                Canvas.SetTop(coin, (68.0 - i * 9.0) * u);
                canvas.Children.Add(coin);
                stack.Add(coin);
            }

            var falling = new Ellipse
            {
                Width = 44.0 * u,
                Height = 12.0 * u,
                Stroke = separatorBrush,
                StrokeThickness = stroke,
                Fill = brush,
                Opacity = 0.0
            };
            Canvas.SetLeft(falling, 26.0 * u);
            Canvas.SetTop(falling, 8.0 * u);
            canvas.Children.Add(falling);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(12, 150, frame =>
            {
                var dropIndex = frame / 4;
                var dropFrame = frame % 4;

                if (dropIndex < stack.Count && dropFrame < 3)
                {
                    var targetTop = 68.0 - dropIndex * 9.0;
                    var progress = dropFrame / 2.0;
                    Canvas.SetTop(falling, (8.0 + (targetTop - 8.0) * progress) * u);
                    falling.Opacity = 1.0;
                }
                else
                {
                    falling.Opacity = 0.0;
                }

                for (var i = 0; i < stack.Count; i++)
                {
                    stack[i].Opacity = i < dropIndex || (i == dropIndex && dropFrame == 3) ? 1.0 : 0.0;
                }
            });
        }

        #endregion

        #region InvoicePaid

        private void BuildInvoicePaidVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var invoice = CreateDocumentPage(brush, 58.0 * u, 72.0 * u, stroke);
            Canvas.SetLeft(invoice, 19.0 * u);
            Canvas.SetTop(invoice, 12.0 * u);
            canvas.Children.Add(invoice);

            var stamp = CreateOutlinedBox(brush, 42.0 * u, 24.0 * u, stroke * 1.5, 2.0 * u);
            stamp.Child = CreateLoaderGlyph("PAID", brush, 11.0 * u);
            Canvas.SetLeft(stamp, 35.0 * u);
            Canvas.SetTop(stamp, 50.0 * u);
            canvas.Children.Add(stamp);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(8, 180, frame =>
            {
                stamp.Opacity = frame < 2 || frame > 6 ? 0.0 : 1.0;
                stamp.RenderTransform = new ScaleTransform { ScaleX = frame == 2 ? 1.35 : 1.0, ScaleY = frame == 2 ? 1.35 : 1.0 };
            });
        }

        #endregion

        #region PiggyBank

        private void BuildPiggyBankVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var body = new Ellipse { Width = 54.0 * u, Height = 34.0 * u, Stroke = brush, StrokeThickness = stroke, Fill = new SolidColorBrush(Colors.Transparent) };
            Canvas.SetLeft(body, 22.0 * u);
            Canvas.SetTop(body, 43.0 * u);
            canvas.Children.Add(body);

            var snout = new Ellipse { Width = 14.0 * u, Height = 12.0 * u, Stroke = brush, StrokeThickness = stroke, Fill = new SolidColorBrush(Colors.Transparent) };
            Canvas.SetLeft(snout, 69.0 * u);
            Canvas.SetTop(snout, 52.0 * u);
            canvas.Children.Add(snout);

            var ear = new Polygon { Points = [new Windows.Foundation.Point(35.0 * u, 43.0 * u), new Windows.Foundation.Point(43.0 * u, 31.0 * u), new Windows.Foundation.Point(48.0 * u, 45.0 * u)], Stroke = brush, StrokeThickness = stroke, Fill = new SolidColorBrush(Colors.Transparent) };
            canvas.Children.Add(ear);

            var leg1 = CreateFilledBox(brush, 6.0 * u, 9.0 * u, 1.5 * u, 0.7);
            var leg2 = CreateFilledBox(brush, 6.0 * u, 9.0 * u, 1.5 * u, 0.7);
            Canvas.SetLeft(leg1, 35.0 * u);
            Canvas.SetLeft(leg2, 58.0 * u);
            Canvas.SetTop(leg1, 72.0 * u);
            Canvas.SetTop(leg2, 72.0 * u);
            canvas.Children.Add(leg1);
            canvas.Children.Add(leg2);

            var coin = new Ellipse { Width = 18.0 * u, Height = 18.0 * u, Stroke = brush, StrokeThickness = stroke, Fill = new SolidColorBrush(Colors.Transparent) };
            Canvas.SetLeft(coin, 39.0 * u);
            Canvas.SetTop(coin, 8.0 * u);
            canvas.Children.Add(coin);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(7, 150, frame =>
            {
                Canvas.SetTop(coin, (8.0 + Math.Min(frame, 5) * 6.0) * u);
                coin.Opacity = frame == 6 ? 0.0 : 1.0;
                body.Opacity = frame == 5 ? 1.0 : 0.75;
            });
        }

        #endregion

        #region PieChartFill

        private void BuildPieChartFillVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var circle = new Ellipse { Width = 58.0 * u, Height = 58.0 * u, Stroke = brush, StrokeThickness = stroke, Fill = new SolidColorBrush(Colors.Transparent) };
            Canvas.SetLeft(circle, 19.0 * u);
            Canvas.SetTop(circle, 19.0 * u);
            canvas.Children.Add(circle);

            var segments = new[]
            {
                new Polygon { Points = [new Windows.Foundation.Point(48.0 * u, 48.0 * u), new Windows.Foundation.Point(48.0 * u, 20.0 * u), new Windows.Foundation.Point(76.0 * u, 48.0 * u)], Fill = brush, Opacity = 0.15 },
                new Polygon { Points = [new Windows.Foundation.Point(48.0 * u, 48.0 * u), new Windows.Foundation.Point(76.0 * u, 48.0 * u), new Windows.Foundation.Point(48.0 * u, 76.0 * u)], Fill = brush, Opacity = 0.15 },
                new Polygon { Points = [new Windows.Foundation.Point(48.0 * u, 48.0 * u), new Windows.Foundation.Point(48.0 * u, 76.0 * u), new Windows.Foundation.Point(20.0 * u, 48.0 * u)], Fill = brush, Opacity = 0.15 },
                new Polygon { Points = [new Windows.Foundation.Point(48.0 * u, 48.0 * u), new Windows.Foundation.Point(20.0 * u, 48.0 * u), new Windows.Foundation.Point(48.0 * u, 20.0 * u)], Fill = brush, Opacity = 0.15 }
            };

            foreach (var segment in segments)
            {
                canvas.Children.Add(segment);
            }

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(5, 220, frame =>
            {
                for (var i = 0; i < segments.Length; i++)
                {
                    segments[i].Opacity = frame > i ? 0.8 : 0.15;
                }
            });
        }

        #endregion

        #region TrendLine

        private void BuildTrendLineVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var axisX = new Line { X1 = 18.0 * u, Y1 = 78.0 * u, X2 = 78.0 * u, Y2 = 78.0 * u, Stroke = brush, StrokeThickness = stroke, Opacity = 0.3 };
            var axisY = new Line { X1 = 18.0 * u, Y1 = 18.0 * u, X2 = 18.0 * u, Y2 = 78.0 * u, Stroke = brush, StrokeThickness = stroke, Opacity = 0.3 };
            canvas.Children.Add(axisX);
            canvas.Children.Add(axisY);

            var line = new Path { Stroke = brush, StrokeThickness = stroke * 1.6, Data = CreatePolylinePathGeometry(u, [(22.0, 70.0), (34.0, 60.0), (47.0, 64.0), (59.0, 42.0), (74.0, 28.0)]) };
            PlatformCompatibility.SafeSetRoundedStroke(line, setLineJoin: true);
            canvas.Children.Add(line);

            var dot = new Ellipse { Width = 8.0 * u, Height = 8.0 * u, Fill = brush };
            canvas.Children.Add(dot);

            _rootGrid.Children.Add(canvas);

            var points = new[] { (22.0, 70.0), (34.0, 60.0), (47.0, 64.0), (59.0, 42.0), (74.0, 28.0) };
            StartDaisyLoadingFrameTimer(points.Length, 180, frame =>
            {
                var (x, y) = points[frame];
                Canvas.SetLeft(dot, (x - 4.0) * u);
                Canvas.SetTop(dot, (y - 4.0) * u);
                line.Opacity = 0.35 + (frame + 1) * 0.13;
            });
        }

        #endregion

        #region ClockSpin

        private void BuildClockSpinVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.5 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var clock = new Ellipse { Width = 62.0 * u, Height = 62.0 * u, Stroke = brush, StrokeThickness = stroke, Fill = new SolidColorBrush(Colors.Transparent) };
            Canvas.SetLeft(clock, 17.0 * u);
            Canvas.SetTop(clock, 17.0 * u);
            canvas.Children.Add(clock);

            var minute = new Line { X1 = 48.0 * u, Y1 = 48.0 * u, X2 = 48.0 * u, Y2 = 25.0 * u, Stroke = brush, StrokeThickness = stroke };
            var hour = new Line { X1 = 48.0 * u, Y1 = 48.0 * u, X2 = 62.0 * u, Y2 = 48.0 * u, Stroke = brush, StrokeThickness = stroke };
            canvas.Children.Add(minute);
            canvas.Children.Add(hour);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(12, 120, frame =>
            {
                var minuteAngle = frame * Math.PI / 6.0;
                var hourAngle = frame * Math.PI / 18.0;
                minute.X2 = 48.0 * u + Math.Sin(minuteAngle) * 23.0 * u;
                minute.Y2 = 48.0 * u - Math.Cos(minuteAngle) * 23.0 * u;
                hour.X2 = 48.0 * u + Math.Sin(hourAngle) * 15.0 * u;
                hour.Y2 = 48.0 * u - Math.Cos(hourAngle) * 15.0 * u;
            });
        }

        #endregion

        #region CoffeeCup

        private void BuildCoffeeCupVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var cup = CreateOutlinedBox(brush, 44.0 * u, 34.0 * u, stroke, 4.0 * u);
            Canvas.SetLeft(cup, 24.0 * u);
            Canvas.SetTop(cup, 46.0 * u);
            canvas.Children.Add(cup);

            var handle = new Ellipse { Width = 22.0 * u, Height = 22.0 * u, Stroke = brush, StrokeThickness = stroke, Fill = new SolidColorBrush(Colors.Transparent) };
            Canvas.SetLeft(handle, 60.0 * u);
            Canvas.SetTop(handle, 52.0 * u);
            canvas.Children.Add(handle);

            var saucer = new Line { X1 = 20.0 * u, X2 = 75.0 * u, Y1 = 84.0 * u, Y2 = 84.0 * u, Stroke = brush, StrokeThickness = stroke, Opacity = 0.45 };
            canvas.Children.Add(saucer);

            var steam = new[]
            {
                new Path { Stroke = brush, StrokeThickness = stroke, Opacity = 0.55, Data = CreateBezierPathGeometry(u, [(34.0, 40.0), (27.0, 33.0), (40.0, 28.0), (33.0, 20.0)]) },
                new Path { Stroke = brush, StrokeThickness = stroke, Opacity = 0.72, Data = CreateBezierPathGeometry(u, [(49.0, 40.0), (39.0, 29.0), (59.0, 24.0), (48.0, 12.0)]) },
                new Path { Stroke = brush, StrokeThickness = stroke, Opacity = 0.6, Data = CreateBezierPathGeometry(u, [(64.0, 40.0), (56.0, 32.0), (73.0, 27.0), (63.0, 17.0)]) }
            };
            foreach (var path in steam)
            {
                PlatformCompatibility.SafeSetRoundedStroke(path, setLineJoin: false);
                canvas.Children.Add(path);
            }

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(6, 180, frame =>
            {
                var wave = frame % 6;
                for (var i = 0; i < steam.Length; i++)
                {
                    var phase = (wave + i * 2) % 6;
                    steam[i].Opacity = phase < 3 ? 0.75 - i * 0.06 : 0.5 + i * 0.04;
                    Canvas.SetTop(steam[i], -(phase % 3) * 1.5 * u);
                }
            });
        }

        #endregion
    }
}
