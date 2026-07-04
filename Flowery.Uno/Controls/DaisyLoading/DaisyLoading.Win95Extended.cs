namespace Flowery.Controls
{
    public partial class DaisyLoading
    {
        #region Win95Defrag

        private void BuildWin95DefragVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var shell = CreateOutlinedBox(brush, 68.0 * u, 54.0 * u, stroke);
            Canvas.SetLeft(shell, 14.0 * u);
            Canvas.SetTop(shell, 21.0 * u);
            canvas.Children.Add(shell);

            var cells = new List<Border>();
            for (var row = 0; row < 5; row++)
            {
                for (var col = 0; col < 7; col++)
                {
                    var filled = (row + col) % 3 == 0;
                    var cell = filled
                        ? CreateFilledBox(brush, 6.0 * u, 6.0 * u, 0, 0.25)
                        : CreateOutlinedBox(brush, 6.0 * u, 6.0 * u, Math.Max(1, u));
                    cell.Opacity = 0.25;
                    Canvas.SetLeft(cell, (20.0 + col * 8.0) * u);
                    Canvas.SetTop(cell, (28.0 + row * 8.0) * u);
                    canvas.Children.Add(cell);
                    cells.Add(cell);
                }
            }

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(7, 160, frame =>
            {
                for (var i = 0; i < cells.Count; i++)
                {
                    cells[i].Opacity = i % 7 == frame || (i + 3) % 7 == frame ? 0.95 : 0.25;
                }
            });
        }

        #endregion

        #region Win95Download

        private void BuildWin95DownloadVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var globe = new Ellipse { Width = 28.0 * u, Height = 28.0 * u, Stroke = brush, StrokeThickness = stroke, Fill = new SolidColorBrush(Colors.Transparent) };
            Canvas.SetLeft(globe, 12.0 * u);
            Canvas.SetTop(globe, 21.0 * u);
            canvas.Children.Add(globe);

            var globeLines = new Path
            {
                Stroke = brush,
                StrokeThickness = Math.Max(1, 1.3 * u),
                Opacity = 0.5,
                Data = CreatePolylinePathGeometry(u, [(12.0, 35.0), (40.0, 35.0), (26.0, 21.0), (26.0, 49.0)])
            };
            canvas.Children.Add(globeLines);

            var folder = CreateWin95Folder(brush, 32.0 * u, 31.0 * u, 16.0 * u, 7.0 * u, stroke, u);
            Canvas.SetLeft(folder, 56.0 * u);
            Canvas.SetTop(folder, 55.0 * u);
            canvas.Children.Add(folder);

            var page = CreateWin95Paper(brush, 12.0 * u, 16.0 * u, stroke * 0.7, u);
            Canvas.SetLeft(page, 29.0 * u);
            Canvas.SetTop(page, 27.0 * u);
            canvas.Children.Add(page);

            var arrow = new Path
            {
                Stroke = brush,
                StrokeThickness = stroke * 1.5,
                Data = CreatePolylinePathGeometry(u, [(49.0, 28.0), (49.0, 56.0), (40.0, 48.0), (49.0, 57.0), (58.0, 48.0)])
            };
            PlatformCompatibility.SafeSetRoundedStroke(arrow, setLineJoin: true);
            canvas.Children.Add(arrow);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(8, 160, frame =>
            {
                var t = Math.Min(frame, 5) / 5.0;
                Canvas.SetLeft(page, (29.0 + 34.0 * t) * u);
                Canvas.SetTop(page, (27.0 + 38.0 * t) * u);
                page.Opacity = frame is 0 or 7 ? 0.0 : 1.0;
                arrow.Opacity = frame % 2 == 0 ? 0.9 : 0.25;
            });
        }

        #endregion

        #region Win95Install

        private void BuildWin95InstallVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var drive = CreateOutlinedBox(brush, 64.0 * u, 30.0 * u, stroke);
            Canvas.SetLeft(drive, 16.0 * u);
            Canvas.SetTop(drive, 48.0 * u);
            canvas.Children.Add(drive);

            var led = CreateFilledBox(brush, 6.0 * u, 6.0 * u, 0, 0.25);
            Canvas.SetLeft(led, 67.0 * u);
            Canvas.SetTop(led, 67.0 * u);

            var floppy = CreateOutlinedBox(brush, 38.0 * u, 34.0 * u, stroke);
            var shutter = CreateFilledBox(brush, 20.0 * u, 10.0 * u, 0, 0.25);
            Canvas.SetLeft(shutter, 5.0 * u);
            Canvas.SetTop(shutter, 4.0 * u);
            var label = CreateFilledBox(brush, 24.0 * u, 10.0 * u, 0, 0.25);
            Canvas.SetLeft(label, 7.0 * u);
            Canvas.SetTop(label, 20.0 * u);
            var floppyCanvas = new Canvas { Width = 38.0 * u, Height = 34.0 * u };
            floppyCanvas.Children.Add(shutter);
            floppyCanvas.Children.Add(label);
            floppy.Child = floppyCanvas;
            Canvas.SetLeft(floppy, 29.0 * u);
            Canvas.SetTop(floppy, 9.0 * u);

            var insertClip = new Canvas
            {
                Width = size,
                Height = 58.0 * u,
                Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, 58.0 * u) }
            };
            insertClip.Children.Add(floppy);
            canvas.Children.Add(insertClip);

            var slot = CreateFilledBox(brush, 46.0 * u, 5.0 * u, 0, 0.35);
            Canvas.SetLeft(slot, 25.0 * u);
            Canvas.SetTop(slot, 57.0 * u);
            canvas.Children.Add(slot);
            canvas.Children.Add(led);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(9, 150, frame =>
            {
                Canvas.SetTop(floppy, (9.0 + Math.Min(frame, 6) * 8.0) * u);
                floppy.Opacity = frame is 0 or 8 ? 0.0 : 1.0;
                led.Opacity = frame % 2 == 0 ? 1.0 : 0.2;
            });
        }

        #endregion

        #region Win95ScanDisk

        private void BuildWin95ScanDiskVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var panel = CreateOutlinedBox(brush, 70.0 * u, 50.0 * u, stroke);
            Canvas.SetLeft(panel, 13.0 * u);
            Canvas.SetTop(panel, 23.0 * u);
            canvas.Children.Add(panel);

            var label = CreateLoaderGlyph("SCAN", brush, 11.0 * u);
            Canvas.SetLeft(label, 28.0 * u);
            Canvas.SetTop(label, 28.0 * u);
            canvas.Children.Add(label);

            var boxes = new List<Border>();
            for (var i = 0; i < 6; i++)
            {
                var box = CreateOutlinedBox(brush, 7.0 * u, 7.0 * u, Math.Max(1, 1.3 * u));
                box.Background = brush;
                box.Opacity = 0.3;
                Canvas.SetLeft(box, (20.0 + i * 10.0) * u);
                Canvas.SetTop(box, 53.0 * u);
                canvas.Children.Add(box);
                boxes.Add(box);
            }

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(boxes.Count, 160, frame =>
            {
                for (var i = 0; i < boxes.Count; i++)
                {
                    boxes[i].Opacity = i == frame ? 0.95 : 0.3;
                }
            });
        }

        #endregion

        #region Win95Hourglass

        private void BuildWin95HourglassVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 4.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var group = new Canvas { Width = 70.0 * u, Height = 70.0 * u, RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5) };
            Canvas.SetLeft(group, 13.0 * u);
            Canvas.SetTop(group, 13.0 * u);

            var top = CreateFilledBox(brush, 46.0 * u, 6.0 * u);
            var bottom = CreateFilledBox(brush, 46.0 * u, 6.0 * u);
            Canvas.SetLeft(top, 12.0 * u);
            Canvas.SetLeft(bottom, 12.0 * u);
            Canvas.SetTop(bottom, 64.0 * u);
            group.Children.Add(top);
            group.Children.Add(bottom);

            var framePath = new Path { Stroke = brush, StrokeThickness = stroke, Data = CreatePolylinePathGeometry(u, [(20.0, 6.0), (50.0, 6.0), (20.0, 64.0), (50.0, 64.0), (20.0, 6.0)]) };
            group.Children.Add(framePath);

            var sandTop = new Polygon { Points = [new Windows.Foundation.Point(26.0 * u, 12.0 * u), new Windows.Foundation.Point(44.0 * u, 12.0 * u), new Windows.Foundation.Point(35.0 * u, 32.0 * u)], Fill = brush, Opacity = 0.35 };
            var sandBottom = new Polygon { Points = [new Windows.Foundation.Point(35.0 * u, 38.0 * u), new Windows.Foundation.Point(25.0 * u, 60.0 * u), new Windows.Foundation.Point(45.0 * u, 60.0 * u)], Fill = brush, Opacity = 0.35 };
            group.Children.Add(sandTop);
            group.Children.Add(sandBottom);
            canvas.Children.Add(group);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(4, 400, frame =>
            {
                group.RenderTransform = new RotateTransform { Angle = frame * 90.0 };
            });
        }

        #endregion

        #region Win95DialUp

        private void BuildWin95DialUpVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var left = CreateWin95Computer(brush, stroke, u);
            var right = CreateWin95Computer(brush, stroke, u);
            Canvas.SetLeft(left, 10.0 * u);
            Canvas.SetTop(left, 38.0 * u);
            Canvas.SetLeft(right, 61.0 * u);
            Canvas.SetTop(right, 38.0 * u);
            canvas.Children.Add(left);
            canvas.Children.Add(right);

            var dot = new Ellipse { Width = 6.0 * u, Height = 6.0 * u, Fill = brush };
            Canvas.SetLeft(dot, 45.0 * u);
            Canvas.SetTop(dot, 46.0 * u);
            canvas.Children.Add(dot);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(6, 180, frame =>
            {
                var positions = new[] { -18.0, -9.0, 0.0, 18.0, 9.0, 0.0 };
                Canvas.SetLeft(dot, (45.0 + positions[frame]) * u);
                left.Opacity = frame < 3 ? 1.0 : 0.45;
                right.Opacity = frame >= 3 ? 1.0 : 0.45;
            });
        }

        private static Canvas CreateWin95Computer(Brush brush, double stroke, double u)
        {
            var canvas = new Canvas { Width = 35.0 * u, Height = 30.0 * u };
            var monitor = CreateOutlinedBox(brush, 25.0 * u, 19.0 * u, stroke);
            var baseBox = CreateOutlinedBox(brush, 35.0 * u, 5.0 * u, stroke);
            var screen = CreateFilledBox(brush, 15.0 * u, 9.0 * u, 0, 0.2);
            Canvas.SetLeft(screen, 5.0 * u);
            Canvas.SetTop(screen, 5.0 * u);
            Canvas.SetLeft(baseBox, -5.0 * u);
            Canvas.SetTop(baseBox, 23.0 * u);
            canvas.Children.Add(monitor);
            canvas.Children.Add(baseBox);
            canvas.Children.Add(screen);
            return canvas;
        }

        #endregion

        #region Win95Solitaire

        private void BuildWin95SolitaireVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var cards = new List<Border>();
            for (var i = 0; i < 4; i++)
            {
                var card = CreateOutlinedBox(brush, 28.0 * u, 38.0 * u, stroke);
                Canvas.SetLeft(card, 11.0 * u);
                Canvas.SetTop(card, 15.0 * u);
                canvas.Children.Add(card);
                cards.Add(card);
            }

            _rootGrid.Children.Add(canvas);

            var targets = new[] { (12.0, 33.0), (25.0, 38.0), (39.0, 31.0), (52.0, 21.0) };
            StartDaisyLoadingFrameTimer(8, 150, frame =>
            {
                for (var i = 0; i < cards.Count; i++)
                {
                    var active = frame >= i && frame < i + 5;
                    var t = active ? (frame - i) / 4.0 : 0.0;
                    Canvas.SetLeft(cards[i], (11.0 + targets[i].Item1 * t) * u);
                    Canvas.SetTop(cards[i], (15.0 + targets[i].Item2 * t) * u);
                    cards[i].Opacity = active ? 1.0 : 0.0;
                }
            });
        }

        #endregion

        #region Win95PrintQueue

        private void BuildWin95PrintQueueVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size, Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) } };

            var body = CreateOutlinedBox(brush, 62.0 * u, 29.0 * u, stroke);
            Canvas.SetLeft(body, 17.0 * u);
            Canvas.SetTop(body, 42.0 * u);
            canvas.Children.Add(body);

            var tray = CreateOutlinedBox(brush, 48.0 * u, 18.0 * u, stroke);
            tray.Opacity = 0.45;
            Canvas.SetLeft(tray, 24.0 * u);
            Canvas.SetTop(tray, 28.0 * u);
            canvas.Children.Add(tray);

            var page = CreateDocumentPage(brush, 42.0 * u, 34.0 * u, stroke);
            Canvas.SetLeft(page, 27.0 * u);
            Canvas.SetTop(page, 39.0 * u);
            canvas.Children.Add(page);

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(8, 160, frame =>
            {
                Canvas.SetTop(page, (39.0 + frame * 5.0) * u);
                page.Opacity = frame is 0 or 7 ? 0.0 : 1.0;
            });
        }

        #endregion

        #region Win95FindComputer

        private void BuildWin95FindComputerVisual(double size)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var pc = CreateWin95Computer(brush, stroke, u);
            Canvas.SetLeft(pc, 28.0 * u);
            Canvas.SetTop(pc, 34.0 * u);
            canvas.Children.Add(pc);

            var lens = new Canvas { Width = 38.0 * u, Height = 38.0 * u };
            var glass = new Ellipse { Width = 24.0 * u, Height = 24.0 * u, Stroke = brush, StrokeThickness = stroke * 1.5, Fill = new SolidColorBrush(Colors.Transparent) };
            var handle = new Path { Stroke = brush, StrokeThickness = stroke * 2, Data = CreatePolylinePathGeometry(u, [(20.0, 20.0), (33.0, 33.0)]) };
            lens.Children.Add(glass);
            lens.Children.Add(handle);
            canvas.Children.Add(lens);

            _rootGrid.Children.Add(canvas);

            var points = new[] { (16.0, 18.0), (50.0, 22.0), (40.0, 48.0), (13.0, 42.0) };
            StartDaisyLoadingFrameTimer(points.Length, 280, frame =>
            {
                Canvas.SetLeft(lens, points[frame].Item1 * u);
                Canvas.SetTop(lens, points[frame].Item2 * u);
            });
        }

        #endregion

        #region Win95Startup

        private void BuildWin95StartupVisual(double size)
        {
            BuildWin95StartupVisual(size, useColor: false);
        }

        private void BuildWin95StartupColorVisual(double size)
        {
            BuildWin95StartupVisual(size, useColor: true);
        }

        private void BuildWin95StartupVisual(double size, bool useColor)
        {
            if (_rootGrid == null) return;

            var brush = GetColorBrush();
            var u = size / 96.0;
            var stroke = Math.Max(1, 2.0 * u);
            var canvas = new Canvas { Width = size, Height = size };

            var shell = CreateOutlinedBox(brush, 60.0 * u, 60.0 * u, stroke);
            Canvas.SetLeft(shell, 18.0 * u);
            Canvas.SetTop(shell, 18.0 * u);
            canvas.Children.Add(shell);

            var colors = new Brush[]
            {
                useColor ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 242, 80, 34)) : brush,
                useColor ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 127, 186, 0)) : brush,
                useColor ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 164, 239)) : brush,
                useColor ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 185, 0)) : brush
            };

            var panes = new List<Border>();
            for (var i = 0; i < 4; i++)
            {
                var pane = CreateFilledBox(colors[i], 20.0 * u, 20.0 * u, 0, 0.25);
                Canvas.SetLeft(pane, (i % 2 == 0 ? 26.0 : 50.0) * u);
                Canvas.SetTop(pane, (i < 2 ? 26.0 : 50.0) * u);
                canvas.Children.Add(pane);
                panes.Add(pane);
            }

            _rootGrid.Children.Add(canvas);

            StartDaisyLoadingFrameTimer(4, 200, frame =>
            {
                for (var i = 0; i < panes.Count; i++)
                {
                    panes[i].Opacity = i == frame ? 0.95 : 0.25;
                }
            });
        }

        #endregion
    }
}
