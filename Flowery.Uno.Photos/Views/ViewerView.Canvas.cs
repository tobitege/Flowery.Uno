using Flowery.Uno.Photos.Editing;
using Flowery.Uno.Photos.Services;
using Microsoft.UI.Input;
using SkiaSharp;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Viewer canvas: rendering, zoom/pan and the pointer interaction. Skia: SKCanvas instead of Win2D. Crop handles,
    /// markup drawing and the before/after split need the edit renderer and come with the editing phase.
    /// </summary>
    public sealed partial class ViewerView
    {
        private static readonly SKSamplingOptions MinifySampling = new(SKCubicResampler.Mitchell);
        private static readonly SKSamplingOptions MagnifySampling = new(SKFilterMode.Linear);

        private readonly record struct ViewTransform(SKImage Source, Size SourceSize, Size ImageSize, double Scale, Vector2 Offset)
        {
            public Rect ScreenRect => new(Offset.X, Offset.Y, ImageSize.Width * Scale, ImageSize.Height * Scale);
        }

        /// <summary>Zoom in DIPs per full-resolution pixel; null means "fit to window".</summary>
        private double? _zoom;
        private Vector2 _pan;
        private bool _panning;
        private Point _pointerStart;
        private Vector2 _panStart;

        private bool IsCropMode => _panelOpen && _tab == EditTab.Crop && !_slideshow;

        private bool IsMarkupMode => _panelOpen && _tab == EditTab.Markup && !_slideshow;

        private EditState DisplayState => State;

        private double RasterScale => XamlRoot?.RasterizationScale ?? 1.0;

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClampPan();
            UpdateZoomUi();
        }

        #region Transform & zoom

        private bool TryGetTransform(out ViewTransform transform)
        {
            transform = default;
            if (_image == null)
                return false;

            var source = ChooseSource(_image);
            var sourceSize = new Size(source.Width, source.Height);
            var state = DisplayState;
            var imageSize = IsCropMode ? EditRenderer.GetTransformedSize(sourceSize, state) : EditRenderer.GetOutputSize(sourceSize, state);

            var viewW = Canvas.ActualWidth;
            var viewH = Canvas.ActualHeight;
            if (viewW <= 0 || viewH <= 0 || imageSize.Width <= 0 || imageSize.Height <= 0)
                return false;

            var inset = RulerInset;
            var areaW = viewW - inset;
            var areaH = viewH - inset;
            var margin = IsCropMode ? 40 : _slideshow ? 0 : 16;
            var fit = Math.Min((areaW - 2 * margin) / imageSize.Width, (areaH - 2 * margin) / imageSize.Height);
            fit = Math.Max(fit, 0.001);
            var sourceToFull = (double)_image.Width / source.Width;
            var scale = _zoom is { } z && !IsCropMode ? z * sourceToFull : fit;

            var pan = IsCropMode ? Vector2.Zero : _pan;
            var offset = new Vector2(
                (float)(inset + areaW / 2 + pan.X - imageSize.Width * scale / 2),
                (float)(inset + areaH / 2 + pan.Y - imageSize.Height * scale / 2));

            transform = new ViewTransform(source, sourceSize, imageSize, scale, offset);
            return true;
        }

        private SKImage ChooseSource(LoadedImage image)
        {
            if (!image.HasSeparatePreview || _zoom == null)
                return image.Preview;
            var previewScale = _zoom.Value * image.Width / image.Preview.Width * RasterScale;
            return previewScale > 1.05 ? image.Full : image.Preview;
        }

        /// <summary>Current zoom in DIPs per full-resolution pixel (fit resolved).</summary>
        private double GetEffectiveZoom()
        {
            if (_zoom is { } z)
                return z;
            if (!TryGetTransform(out var t) || _image == null)
                return 1;
            return t.Scale * t.Source.Width / _image.Width;
        }

        private void ResetView()
        {
            _zoom = null;
            _pan = Vector2.Zero;
        }

        private void ZoomToFit()
        {
            _zoom = null;
            _pan = Vector2.Zero;
            UpdateZoomUi();
            Canvas.Invalidate();
        }

        private void SetZoomPercent(double percent)
        {
            SetZoom(percent / 100 / RasterScale, null);
        }

        /// <summary>
        /// Zooms one step in or out (wheel notch, +/- keys, zoom buttons); see <see cref="ViewerMath.StepZoomPercent"/>.
        /// </summary>
        private void StepZoom(int direction, Point? anchor = null)
        {
            var current = GetEffectiveZoom() * RasterScale * 100;
            SetZoom(ViewerMath.StepZoomPercent(current, direction) / 100 / RasterScale, anchor);
        }

        /// <summary>
        /// Sets the zoom, keeping the image point under <paramref name="anchor"/> (screen DIPs) stationary.
        /// </summary>
        private void SetZoom(double zoom, Point? anchor)
        {
            if (_image == null || IsCropMode)
                return;

            var before = TryGetTransform(out var t0) ? t0 : (ViewTransform?)null;
            var minZoom = 0.02;
            var maxZoom = 16.0 / RasterScale;
            _zoom = Math.Clamp(zoom, minZoom, maxZoom);

            if (before is { } t && TryGetTransform(out var t1))
            {
                var inset = RulerInset;
                var viewCenter = new Vector2((float)(inset + (Canvas.ActualWidth - inset) / 2), (float)(inset + (Canvas.ActualHeight - inset) / 2));
                var a = anchor is { } p ? new Vector2((float)p.X, (float)p.Y) : viewCenter;
                // Image point (in full-res pixels) under the anchor before zooming.
                var fullPerSourceBefore = (double)_image.Width / t.Source.Width;
                var imagePoint = (a - t.Offset) / (float)(t.Scale / fullPerSourceBefore);
                var newScaleFull = (float)_zoom.Value;
                var newOffset = a - imagePoint * newScaleFull;
                var imageScreen = new Vector2((float)(t1.ImageSize.Width * t1.Scale), (float)(t1.ImageSize.Height * t1.Scale));
                _pan = newOffset + imageScreen / 2 - viewCenter;
            }

            ClampPan();
            UpdateZoomUi();
            Canvas.Invalidate();
        }

        private void ClampPan()
        {
            if (!TryGetTransform(out var t))
                return;
            var imageW = t.ImageSize.Width * t.Scale;
            var imageH = t.ImageSize.Height * t.Scale;
            var areaW = Canvas.ActualWidth - RulerInset;
            var areaH = Canvas.ActualHeight - RulerInset;
            var maxX = Math.Max(0, (imageW - areaW) / 2 + 40);
            var maxY = Math.Max(0, (imageH - areaH) / 2 + 40);
            _pan = new Vector2(
                imageW <= areaW ? 0 : (float)Math.Clamp(_pan.X, -maxX, maxX),
                imageH <= areaH ? 0 : (float)Math.Clamp(_pan.Y, -maxY, maxY));
        }

        private void UpdateZoomUi()
        {
            var percent = GetEffectiveZoom() * RasterScale * 100;
            ZoomPercentButton.Content = _zoom == null
                ? $"{L("Photos_ZoomFitShort")} · {percent:0} %"
                : $"{percent:0} %";
            _syncingZoom = true;
            try
            {
                ZoomRange.Value = Math.Clamp(percent, ZoomRange.Minimum, ZoomRange.Maximum);
            }
            finally
            {
                _syncingZoom = false;
            }
            FitButton.IsEnabled = _zoom != null;
        }

        private void OnZoomRangeChanged()
        {
            if (_syncingZoom || _image == null)
                return;
            // The range may re-report (coerce) the value we just pushed; only react to real user changes.
            if (Math.Abs(ZoomRange.Value - GetEffectiveZoom() * RasterScale * 100) < 0.75)
                return;
            SetZoomPercent(ZoomRange.Value);
        }

        private void FitButton_Click(object sender, RoutedEventArgs e) => ZoomToFit();

        private void ZoomInButton_Click(object sender, RoutedEventArgs e) => StepZoom(1);

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => StepZoom(-1);

        #endregion

        #region Drawing

        private void Canvas_Draw(ViewerCanvas sender, SKCanvas canvas, Size area)
        {
            if (!TryGetTransform(out var t))
                return;

            var sampling = t.Scale < 1 ? MinifySampling : MagnifySampling;

            canvas.Save();
            canvas.Translate(t.Offset.X, t.Offset.Y);
            canvas.Scale((float)t.Scale);
            canvas.DrawImage(t.Source, 0, 0, sampling);
            canvas.Restore();

            if (PhotosSettings.ShowGrid && !_slideshow)
                DrawGridlines(canvas, t);
            DrawNavigator(canvas, t);
            if (RulersVisible)
                DrawRulers(canvas, t);
        }

        #endregion

        #region Pointer interaction

        /// <summary>True if the event started on the photo itself and not on a button laid over it.</summary>
        private static bool IsOnCanvas(RoutedEventArgs e) => e.OriginalSource is ViewerCanvas or CursorGrid;

        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!IsOnCanvas(e))
                return;

            Focus(FocusState.Pointer);
            if (_slideshow)
            {
                StopSlideshow();
                e.Handled = true;
                return;
            }

            if (!TryGetTransform(out var t))
                return;

            var point = e.GetCurrentPoint(Canvas);
            var p = point.Position;
            _pointerStart = p;

            if (point.Properties.IsLeftButtonPressed && IsInNavigator(t, p))
            {
                _draggingNavigator = CanvasHost.CapturePointer(e.Pointer);
                PanFromNavigator(t, p);
                e.Handled = true;
                return;
            }

            if (point.Properties.IsLeftButtonPressed || point.Properties.IsMiddleButtonPressed)
            {
                var r = t.ScreenRect;
                if (r.Width > Canvas.ActualWidth || r.Height > Canvas.ActualHeight)
                {
                    _panning = true;
                    _panStart = _pan;
                    CanvasHost.CapturePointer(e.Pointer);
                    CanvasHost.SetCursor(InputSystemCursorShape.SizeAll);
                    e.Handled = true;
                }
            }
        }

        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!TryGetTransform(out var t))
                return;
            var p = e.GetCurrentPoint(Canvas).Position;
            _lastPointer = p;
            if (RulersVisible)
                Canvas.Invalidate();

            if (_draggingNavigator)
            {
                PanFromNavigator(t, p);
                return;
            }

            if (_panning)
            {
                _pan = _panStart + new Vector2((float)(p.X - _pointerStart.X), (float)(p.Y - _pointerStart.Y));
                ClampPan();
                Canvas.Invalidate();
                return;
            }

            UpdateHoverCursor(t);
        }

        private void UpdateHoverCursor(ViewTransform t)
        {
            var shape = InputSystemCursorShape.Arrow;
            if (t.ScreenRect.Width > Canvas.ActualWidth || t.ScreenRect.Height > Canvas.ActualHeight)
                shape = InputSystemCursorShape.Hand;
            CanvasHost.SetCursor(shape);
        }

        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndPointerInteraction();
            CanvasHost.ReleasePointerCapture(e.Pointer);
        }

        private void Canvas_PointerCaptureLost(object sender, PointerRoutedEventArgs e) => EndPointerInteraction();

        private void EndPointerInteraction()
        {
            _draggingNavigator = false;
            if (_panning)
            {
                _panning = false;
                CanvasHost.SetCursor(InputSystemCursorShape.Hand);
            }
            Canvas.Invalidate();
        }

        private int _wheelZoomDelta;

        private async void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (_slideshow || IsCropMode)
                return;

            var point = e.GetCurrentPoint(Canvas);
            var delta = point.Properties.MouseWheelDelta;
            var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            e.Handled = true;

            if (PhotosSettings.WheelNavigates && !ctrl)
            {
                await NavigateAsync(delta > 0 ? -1 : 1);
                return;
            }

            // One zoom step per wheel notch (120); precision touchpads send smaller deltas that add up.
            _wheelZoomDelta += delta;
            while (Math.Abs(_wheelZoomDelta) >= 120)
            {
                var direction = Math.Sign(_wheelZoomDelta);
                _wheelZoomDelta -= direction * 120;
                StepZoom(direction, point.Position);
            }
        }

        private void Canvas_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (!IsOnCanvas(e) || IsCropMode || IsMarkupMode || _slideshow)
                return;
            if (_zoom == null)
                SetZoom(1 / RasterScale, e.GetPosition(Canvas));
            else
                ZoomToFit();
        }

        #endregion
    }
}
