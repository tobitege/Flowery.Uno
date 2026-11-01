using Flowery.Uno.Photos.Services;
using SkiaSharp;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// "View" options (like Paint's View menu): zoom, rulers, gridlines, status bar, auto-hiding toolbar,
    /// full screen and a navigator thumbnail (mini map) for panning zoomed photos.
    /// </summary>
    public sealed partial class ViewerView
    {
        private const float RulerSize = 22;
        private const float NavigatorMaxEdge = 170;
        private Point? _lastPointer;
        private bool _draggingNavigator;

        private bool RulersVisible => PhotosSettings.ShowRulers && !_slideshow;

        private float RulerInset => RulersVisible ? RulerSize : 0;

        private void ViewFlyout_Opening(object sender, object e) => BuildViewPanel();

        private void BuildViewPanel()
        {
            ViewPanel.Children.Clear();

            ViewPanel.Children.Add(Caption("Photos_View_Zoom", new Thickness(0)));
            var zoom = new Grid { ColumnSpacing = 6 };
            for (var i = 0; i < 4; i++)
                zoom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            (string? Icon, Symbol? Symbol, string Text, Action Action)[] zoomButtons =
            [
                (null, Symbol.ZoomOut, L("Photos_ZoomOut"), () => StepZoom(-1)),
                ("PhotosIconFitScreen", null, L("Photos_ZoomFit"), ZoomToFit),
                (null, null, "100 %", () => SetZoomPercent(100)),
                (null, Symbol.ZoomIn, L("Photos_ZoomIn"), () => StepZoom(1))
            ];
            for (var i = 0; i < zoomButtons.Length; i++)
            {
                var (icon, symbol, text, action) = zoomButtons[i];
                var button = new DaisyButton
                {
                    Size = DaisySize.Small,
                    Variant = DaisyButtonVariant.Primary,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                if (symbol is { } s)
                    button.IconSymbol = s;
                else if (icon != null)
                    button.IconData = Icon(icon);
                else
                    button.Content = text;
                Tip(button, text);
                button.Click += (_, _) => action();
                Grid.SetColumn(button, i);
                zoom.Children.Add(button);
            }
            ViewPanel.Children.Add(zoom);

            ViewPanel.Children.Add(Caption("Photos_View_Show"));
            ViewPanel.Children.Add(ToggleRow("Photos_View_Rulers", PhotosSettings.ShowRulers, v =>
            {
                PhotosSettings.ShowRulers = v;
                PhotosSettings.SaveOptions();
                ClampPan();
                UpdateZoomUi();
                Canvas.Invalidate();
            }));
            ViewPanel.Children.Add(ToggleRow("Photos_View_Gridlines", PhotosSettings.ShowGrid, v =>
            {
                PhotosSettings.ShowGrid = v;
                PhotosSettings.SaveOptions();
                Canvas.Invalidate();
            }));
            ViewPanel.Children.Add(ToggleRow("Photos_View_StatusBar", PhotosSettings.ShowStatusBar, v =>
            {
                PhotosSettings.ShowStatusBar = v;
                PhotosSettings.SaveOptions();
                ApplyChromeLayout();
            }));
            ViewPanel.Children.Add(ToggleRow("Photos_View_Navigator", PhotosSettings.ShowNavigator, v =>
            {
                PhotosSettings.ShowNavigator = v;
                PhotosSettings.SaveOptions();
                Canvas.Invalidate();
            }));

            ViewPanel.Children.Add(Caption("Photos_View_Toolbar"));
            ViewPanel.Children.Add(ToggleRow("Photos_View_AutoHideToolbar", PhotosSettings.AutoHideToolbar, v =>
            {
                PhotosSettings.AutoHideToolbar = v;
                PhotosSettings.SaveOptions();
                ApplyChromeLayout();
            }));

            var fullScreen = new DaisyButton
            {
                Content = L(_immersive ? "Photos_ExitFullScreen" : "Photos_FullScreen"),
                IconSymbol = _immersive ? Symbol.BackToWindow : Symbol.FullScreen,
                Variant = DaisyButtonVariant.Primary,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0)
            };
            fullScreen.Click += (_, _) =>
            {
                ViewFlyout.Hide();
                ToggleImmersive();
            };
            ViewPanel.Children.Add(fullScreen);
        }

        #region Overlay drawing

        private static Color ThemeColor(string key, Color fallback) =>
            DaisyResourceLookup.GetBrush(key) is SolidColorBrush b ? b.Color : fallback;

        private static SKColor ToSk(Color c) => new(c.R, c.G, c.B, c.A);

        private static SKPaint Fill(Color color) => new() { Color = ToSk(color), IsAntialias = true, Style = SKPaintStyle.Fill };

        private static SKPaint Stroke(Color color, float width) =>
            new() { Color = ToSk(color), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width };

        private static SKRect ToSk(Rect r) => new((float)r.Left, (float)r.Top, (float)r.Right, (float)r.Bottom);

        /// <summary>DIPs per full-resolution pixel for the current transform.</summary>
        private double ScreenPerFullPixel(ViewTransform t) =>
            _image == null ? t.Scale : t.Scale * t.Source.Width / _image.Width;

        private void DrawGridlines(SKCanvas canvas, ViewTransform t)
        {
            var r = t.ScreenRect;
            var ppx = ScreenPerFullPixel(t);
            var step = ViewerMath.NiceStep(48 / ppx);
            using var light = Stroke(Color.FromArgb(70, 255, 255, 255), 1);
            using var dark = Stroke(Color.FromArgb(55, 0, 0, 0), 1);
            var left = Math.Max(r.X, RulerInset);
            var top = Math.Max(r.Y, RulerInset);
            var right = Math.Min(r.Right, Canvas.ActualWidth);
            var bottom = Math.Min(r.Bottom, Canvas.ActualHeight);

            for (var v = step; v * ppx < r.Width; v += step)
            {
                var x = (float)(r.X + v * ppx);
                if (x < left || x > right)
                    continue;
                canvas.DrawLine(x + 0.5f, (float)top, x + 0.5f, (float)bottom, dark);
                canvas.DrawLine(x, (float)top, x, (float)bottom, light);
            }
            for (var v = step; v * ppx < r.Height; v += step)
            {
                var y = (float)(r.Y + v * ppx);
                if (y < top || y > bottom)
                    continue;
                canvas.DrawLine((float)left, y + 0.5f, (float)right, y + 0.5f, dark);
                canvas.DrawLine((float)left, y, (float)right, y, light);
            }
        }

        private void DrawRulers(SKCanvas canvas, ViewTransform t)
        {
            var width = (float)Canvas.ActualWidth;
            var height = (float)Canvas.ActualHeight;
            var backgroundColor = ThemeColor("DaisyBase200Brush", Color.FromArgb(255, 30, 30, 30));
            var content = ThemeColor("DaisyBaseContentBrush", Color.FromArgb(255, 230, 230, 230));
            var tickColor = Color.FromArgb(150, content.R, content.G, content.B);
            var accentColor = ThemeColor("DaisyPrimaryBrush", Color.FromArgb(255, 99, 102, 241));

            using var background = Fill(backgroundColor);
            using var tick = Stroke(tickColor, 1);
            using var accent = Stroke(accentColor, 1.5f);
            using var text = Fill(content);
            using var font = new SKFont(SKTypeface.Default, 9.5f);

            canvas.DrawRect(0, 0, width, RulerSize, background);
            canvas.DrawRect(0, 0, RulerSize, height, background);
            canvas.DrawLine(RulerSize, RulerSize - 0.5f, width, RulerSize - 0.5f, tick);
            canvas.DrawLine(RulerSize - 0.5f, RulerSize, RulerSize - 0.5f, height, tick);

            var r = t.ScreenRect;
            var ppx = ScreenPerFullPixel(t);
            var major = ViewerMath.NiceStep(70 / ppx);
            var minor = major / 5;

            // Horizontal ruler
            var startX = Math.Floor((RulerSize - r.X) / ppx / minor) * minor;
            for (var v = startX; r.X + v * ppx < width; v += minor)
            {
                var x = (float)(r.X + v * ppx);
                if (x < RulerSize)
                    continue;
                var isMajor = Math.Abs(v / major - Math.Round(v / major)) < 1e-6;
                canvas.DrawLine(x, RulerSize - (isMajor ? 10 : 4), x, RulerSize, tick);
                if (isMajor)
                    canvas.DrawText(((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture), x + 2, 10, SKTextAlign.Left, font, text);
            }

            // Vertical ruler (labels rotated)
            var startY = Math.Floor((RulerSize - r.Y) / ppx / minor) * minor;
            for (var v = startY; r.Y + v * ppx < height; v += minor)
            {
                var y = (float)(r.Y + v * ppx);
                if (y < RulerSize)
                    continue;
                var isMajor = Math.Abs(v / major - Math.Round(v / major)) < 1e-6;
                canvas.DrawLine(RulerSize - (isMajor ? 10 : 4), y, RulerSize, y, tick);
                if (isMajor)
                {
                    canvas.Save();
                    canvas.RotateDegrees(-90, 1, y - 2);
                    canvas.DrawText(((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture), 1, y - 2 + 10, SKTextAlign.Left, font, text);
                    canvas.Restore();
                }
            }

            // Pointer position markers
            if (_lastPointer is { } p)
            {
                if (p.X >= RulerSize)
                    canvas.DrawLine((float)p.X, 0, (float)p.X, RulerSize, accent);
                if (p.Y >= RulerSize)
                    canvas.DrawLine(0, (float)p.Y, RulerSize, (float)p.Y, accent);
            }

            canvas.DrawRect(0, 0, RulerSize, RulerSize, background);
        }

        /// <summary>
        /// Navigator thumbnail rect (bottom right) and the viewport inside it, in screen coordinates.
        /// </summary>
        private (Rect Thumb, Rect Viewport)? GetNavigatorRects(ViewTransform t)
        {
            if (!PhotosSettings.ShowNavigator || IsCropMode || _slideshow)
                return null;
            var aspect = t.ImageSize.Width / t.ImageSize.Height;
            var w = aspect >= 1 ? NavigatorMaxEdge : NavigatorMaxEdge * aspect;
            var h = aspect >= 1 ? NavigatorMaxEdge / aspect : NavigatorMaxEdge;
            var margin = 14;
            var thumb = new Rect(Canvas.ActualWidth - w - margin, Canvas.ActualHeight - h - margin, w, h);

            var r = t.ScreenRect;
            var vx = (Math.Max(RulerInset, 0) - r.X) / r.Width;
            var vy = (Math.Max(RulerInset, 0) - r.Y) / r.Height;
            var vw = (Canvas.ActualWidth - RulerInset) / r.Width;
            var vh = (Canvas.ActualHeight - RulerInset) / r.Height;
            var x0 = Math.Clamp(vx, 0, 1);
            var y0 = Math.Clamp(vy, 0, 1);
            var x1 = Math.Clamp(vx + vw, 0, 1);
            var y1 = Math.Clamp(vy + vh, 0, 1);
            var viewport = new Rect(thumb.X + x0 * w, thumb.Y + y0 * h, Math.Max(2, (x1 - x0) * w), Math.Max(2, (y1 - y0) * h));
            return (thumb, viewport);
        }

        private void DrawNavigator(SKCanvas canvas, ViewTransform t)
        {
            if (GetNavigatorRects(t) is not { } rects)
                return;
            var (thumb, viewport) = rects;
            var backgroundColor = ThemeColor("DaisyBase200Brush", Color.FromArgb(255, 30, 30, 30));
            var accentColor = ThemeColor("DaisyPrimaryBrush", Color.FromArgb(255, 99, 102, 241));

            using var frame = Fill(Color.FromArgb(235, backgroundColor.R, backgroundColor.G, backgroundColor.B));
            canvas.DrawRoundRect(ToSk(new Rect(thumb.X - 4, thumb.Y - 4, thumb.Width + 8, thumb.Height + 8)), 8, 8, frame);
            canvas.DrawImage(t.Source, new SKRect(0, 0, t.Source.Width, t.Source.Height), ToSk(thumb), MinifySampling);

            // Dim everything outside the visible viewport.
            using var shade = Fill(Color.FromArgb(110, 0, 0, 0));
            canvas.DrawRect(ToSk(new Rect(thumb.X, thumb.Y, thumb.Width, Math.Max(0, viewport.Y - thumb.Y))), shade);
            canvas.DrawRect(ToSk(new Rect(thumb.X, viewport.Bottom, thumb.Width, Math.Max(0, thumb.Bottom - viewport.Bottom))), shade);
            canvas.DrawRect(ToSk(new Rect(thumb.X, viewport.Y, Math.Max(0, viewport.X - thumb.X), viewport.Height)), shade);
            canvas.DrawRect(ToSk(new Rect(viewport.Right, viewport.Y, Math.Max(0, thumb.Right - viewport.Right), viewport.Height)), shade);
            using var outline = Stroke(accentColor, 2);
            canvas.DrawRect(ToSk(viewport), outline);
        }

        private bool IsInNavigator(ViewTransform t, Point p) =>
            GetNavigatorRects(t) is { } rects && rects.Thumb.Contains(p);

        /// <summary>Centers the view on the image point under a navigator position.</summary>
        private void PanFromNavigator(ViewTransform t, Point p)
        {
            if (GetNavigatorRects(t) is not { } rects)
                return;
            if (_zoom == null)
                _zoom = GetEffectiveZoom();
            var nx = Math.Clamp((p.X - rects.Thumb.X) / rects.Thumb.Width, 0, 1);
            var ny = Math.Clamp((p.Y - rects.Thumb.Y) / rects.Thumb.Height, 0, 1);
            var r = t.ScreenRect;
            // Desired: image point (nx, ny) at the center of the view area; pan shifts the image center.
            _pan = new Vector2((float)((0.5 - nx) * r.Width), (float)((0.5 - ny) * r.Height));
            ClampPan();
            UpdateZoomUi();
            Canvas.Invalidate();
        }

        #endregion
    }
}
