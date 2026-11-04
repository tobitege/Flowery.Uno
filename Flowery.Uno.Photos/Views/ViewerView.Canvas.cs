using Flowery.Uno.Photos.Editing;
using Flowery.Uno.Photos.Services;
using Microsoft.UI.Input;
using SkiaSharp;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Viewer canvas: rendering, zoom/pan, before/after split, crop handles and markup drawing. Skia: SKCanvas instead of
    /// Win2D. Geometry and strokes are drawn directly; color edits come from <see cref="PreviewRenderer"/> and appear
    /// when the render is ready, so sliders do not wait for it.
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
        private bool _compareSplit;
        private double _split = 0.5;
        private bool _draggingSplit;
        private CropHandle _cropHandle;
        private Rect _cropStart;
        private Rect? _liveCrop;
        private double? _cropAspect;
        private List<Vector2>? _currentStroke;
        private Color _penColor = Color.FromArgb(255, 239, 68, 68);
        private Color _currentStrokeColor;
        private double _penSize = 6;
        private bool _highlighter;
        private readonly PreviewRenderer _previewRenderer = new();
        private SKImage? _edited;
        private PreviewRequest? _editedRequest;
        private PreviewRequest? _pendingRequest;

        private bool IsCropMode => _panelOpen && _tab == EditTab.Crop && !_slideshow;

        private bool IsMarkupMode => _panelOpen && _tab == EditTab.Markup && !_slideshow;

        private EditState DisplayState => _liveCrop is { } crop ? State with { Crop = crop } : State;

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
            // Snap to whole device pixels: a fractional offset blends neighbouring pixels and makes 100 % look soft.
            var raster = RasterScale;
            var offset = new Vector2(
                (float)(Math.Round((inset + areaW / 2 + pan.X - imageSize.Width * scale / 2) * raster) / raster),
                (float)(Math.Round((inset + areaH / 2 + pan.Y - imageSize.Height * scale / 2) * raster) / raster));

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
            _liveCrop = null;
            _currentStroke = null;
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

            var state = DisplayState;
            var cropMode = IsCropMode;
            var sampling = t.Scale < 1 ? MinifySampling : MagnifySampling;

            canvas.Save();
            canvas.Translate(t.Offset.X, t.Offset.Y);
            canvas.Scale((float)t.Scale);

            if (_compareSplit && !cropMode)
            {
                var splitX = (float)(t.ImageSize.Width * _split);
                canvas.Save();
                canvas.ClipRect(new SKRect(0, 0, splitX, (float)t.ImageSize.Height));
                DrawGeometry(canvas, t, state, sampling);
                canvas.Restore();
                canvas.Save();
                canvas.ClipRect(new SKRect(splitX, 0, (float)t.ImageSize.Width, (float)t.ImageSize.Height));
                DrawEdited(canvas, t, state, sampling);
                DrawAllStrokes(canvas, t, state, cropMode);
                canvas.Restore();
            }
            else
            {
                DrawEdited(canvas, t, state, sampling);
                DrawAllStrokes(canvas, t, state, cropMode);
            }

            canvas.Restore();

            if (PhotosSettings.ShowGrid && !cropMode && !_slideshow)
                DrawGridlines(canvas, t);
            if (cropMode)
                DrawCropOverlay(canvas, t, state.Crop);
            if (_compareSplit && !cropMode)
                DrawSplitOverlay(canvas, t);
            DrawNavigator(canvas, t);
            if (RulersVisible)
                DrawRulers(canvas, t);
        }

        /// <summary>Draws the image without color edits; the canvas is in image space.</summary>
        private void DrawGeometry(SKCanvas canvas, ViewTransform t, EditState state, SKSamplingOptions sampling)
        {
            canvas.Save();
            canvas.ClipRect(new SKRect(0, 0, (float)t.ImageSize.Width, (float)t.ImageSize.Height));
            canvas.Concat(EditRenderer.GetGeometryMatrix(t.SourceSize, state, applyCrop: !IsCropMode));
            canvas.DrawImage(t.Source, 0, 0, sampling);
            canvas.Restore();
        }

        /// <summary>Draws the image with its edits, or the closest picture at hand while the render runs.</summary>
        private void DrawEdited(SKCanvas canvas, ViewTransform t, EditState state, SKSamplingOptions sampling)
        {
            if (!state.HasColorEdits)
            {
                DrawGeometry(canvas, t, state, sampling);
                return;
            }

            RequestPreview(t, state);
            if (_edited is { } edited && CanStandIn(edited, t, state))
                canvas.DrawImage(edited, new SKRect(0, 0, (float)t.ImageSize.Width, (float)t.ImageSize.Height), sampling);
            else
                DrawGeometry(canvas, t, state, sampling);
        }

        /// <summary>An older render may stay on screen while the next one runs, unless it would show the photo turned or stretched.</summary>
        private bool CanStandIn(SKImage edited, ViewTransform t, EditState state)
        {
            var shown = _editedRequest?.State;
            var aspect = t.ImageSize.Width / t.ImageSize.Height;
            return shown != null
                && shown.Rotation == state.Rotation && shown.FlipH == state.FlipH && shown.FlipV == state.FlipV
                && Math.Abs((double)edited.Width / edited.Height / aspect - 1) < 0.02;
        }

        private void RequestPreview(ViewTransform t, EditState state)
        {
            if (_image == null)
                return;

            var applyCrop = !IsCropMode;
            // Strokes are drawn on top, and without the crop its rectangle changes nothing in the picture.
            var picture = state with { Strokes = [], Crop = applyCrop ? state.Crop : EditState.FullCrop };
            var request = new PreviewRequest(t.Source, picture, applyCrop, EditRenderer.GetPreviewScale(t.Scale * RasterScale));
            if (request == _editedRequest || request == _pendingRequest)
                return;

            _pendingRequest = request;
            _previewRenderer.Request(request, _image.Share(t.Source));
        }

        private void OnPreviewRendered(PreviewRequest request, SKImage image)
        {
            if (!DispatcherQueue.TryEnqueue(() => ShowPreview(request, image)))
                image.Dispose();
        }

        private void ShowPreview(PreviewRequest request, SKImage image)
        {
            // The photo may have changed while the render ran.
            if (_image == null || !(ReferenceEquals(request.Source, _image.Preview) || ReferenceEquals(request.Source, _image.Full)))
            {
                image.Dispose();
                return;
            }

            _edited?.Dispose();
            _edited = image;
            _editedRequest = request;
            if (request == _pendingRequest)
                _pendingRequest = null;
            Canvas.Invalidate();
        }

        private void ClearPreview()
        {
            _edited?.Dispose();
            _edited = null;
            _editedRequest = null;
            _pendingRequest = null;
        }

        private void DrawAllStrokes(SKCanvas canvas, ViewTransform t, EditState state, bool cropMode)
        {
            var strokes = state.Strokes;
            if (_currentStroke is { Count: > 0 } current)
            {
                strokes = strokes.Add(new MarkupStroke(_currentStrokeColor, (float)(_penSize / 1000), _highlighter, [.. current]));
            }
            if (strokes.IsEmpty)
                return;

            var transformed = EditRenderer.GetTransformedSize(t.SourceSize, state);
            var visible = cropMode ? EditState.FullCrop : state.Crop;
            canvas.Save();
            canvas.ClipRect(new SKRect(0, 0, (float)t.ImageSize.Width, (float)t.ImageSize.Height));
            EditRenderer.DrawStrokes(canvas, strokes, transformed, visible, t.ImageSize);
            canvas.Restore();
        }

        private static void DrawCropOverlay(SKCanvas canvas, ViewTransform t, Rect crop)
        {
            var r = t.ScreenRect;
            var c = new Rect(r.X + crop.X * r.Width, r.Y + crop.Y * r.Height, crop.Width * r.Width, crop.Height * r.Height);
            using var shade = Fill(Color.FromArgb(150, 0, 0, 0));

            canvas.DrawRect(ToSk(new Rect(r.X, r.Y, r.Width, Math.Max(0, c.Y - r.Y))), shade);
            canvas.DrawRect(ToSk(new Rect(r.X, c.Bottom, r.Width, Math.Max(0, r.Bottom - c.Bottom))), shade);
            canvas.DrawRect(ToSk(new Rect(r.X, c.Y, Math.Max(0, c.X - r.X), c.Height)), shade);
            canvas.DrawRect(ToSk(new Rect(c.Right, c.Y, Math.Max(0, r.Right - c.Right), c.Height)), shade);

            using var white = Fill(Color.FromArgb(255, 255, 255, 255));
            using var outline = Stroke(Color.FromArgb(255, 255, 255, 255), 1.5f);
            using var grid = Stroke(Color.FromArgb(90, 255, 255, 255), 1);
            for (var i = 1; i < 3; i++)
            {
                var x = (float)(c.X + c.Width * i / 3);
                var y = (float)(c.Y + c.Height * i / 3);
                canvas.DrawLine(x, (float)c.Y, x, (float)c.Bottom, grid);
                canvas.DrawLine((float)c.X, y, (float)c.Right, y, grid);
            }
            canvas.DrawRect(ToSk(c), outline);

            const float len = 20;
            const float thick = 4;
            var l = (float)c.X;
            var tp = (float)c.Y;
            var rt = (float)c.Right;
            var b = (float)c.Bottom;
            void Bar(float x, float y, float w, float h) => canvas.DrawRect(x, y, w, h, white);
            // Corners
            Bar(l - thick / 2, tp - thick / 2, len, thick);
            Bar(l - thick / 2, tp - thick / 2, thick, len);
            Bar(rt - len + thick / 2, tp - thick / 2, len, thick);
            Bar(rt - thick / 2, tp - thick / 2, thick, len);
            Bar(l - thick / 2, b - thick / 2, len, thick);
            Bar(l - thick / 2, b - len + thick / 2, thick, len);
            Bar(rt - len + thick / 2, b - thick / 2, len, thick);
            Bar(rt - thick / 2, b - len + thick / 2, thick, len);
            // Edge midpoints
            var mx = (l + rt) / 2;
            var my = (tp + b) / 2;
            Bar(mx - len / 2, tp - thick / 2, len, thick);
            Bar(mx - len / 2, b - thick / 2, len, thick);
            Bar(l - thick / 2, my - len / 2, thick, len);
            Bar(rt - thick / 2, my - len / 2, thick, len);
        }

        private void DrawSplitOverlay(SKCanvas canvas, ViewTransform t)
        {
            var r = t.ScreenRect;
            var x = (float)(r.X + r.Width * _split);
            var top = (float)Math.Max(r.Y, 0);
            var bottom = (float)Math.Min(r.Bottom, Canvas.ActualHeight);
            using var line = Stroke(Color.FromArgb(220, 255, 255, 255), 2);
            using var knob = Fill(Color.FromArgb(230, 255, 255, 255));
            using var arrow = Stroke(Color.FromArgb(255, 30, 30, 30), 2);
            canvas.DrawLine(x, top, x, bottom, line);
            var cy = (top + bottom) / 2;
            canvas.DrawCircle(x, cy, 16, knob);
            canvas.DrawLine(x - 6, cy - 5, x - 10, cy, arrow);
            canvas.DrawLine(x - 10, cy, x - 6, cy + 5, arrow);
            canvas.DrawLine(x + 6, cy - 5, x + 10, cy, arrow);
            canvas.DrawLine(x + 10, cy, x + 6, cy + 5, arrow);

            using var font = new SKFont(SKTypeface.Default, 13) { Embolden = true };
            DrawLabel(canvas, L("Photos_Before"), new Vector2(x - 12, top + 12), font, rightAligned: true);
            DrawLabel(canvas, L("Photos_After"), new Vector2(x + 12, top + 12), font, rightAligned: false);
        }

        private static void DrawLabel(SKCanvas canvas, string text, Vector2 position, SKFont font, bool rightAligned)
        {
            var w = font.MeasureText(text);
            var h = font.Size * 1.3f;
            var x = rightAligned ? position.X - w - 16 : position.X;
            using var background = Fill(Color.FromArgb(160, 0, 0, 0));
            using var foreground = Fill(Color.FromArgb(255, 255, 255, 255));
            canvas.DrawRoundRect(new SKRect(x, position.Y, x + w + 16, position.Y + h + 8), 10, 10, background);
            canvas.DrawText(text, x + 8, position.Y + 4 + font.Size, SKTextAlign.Left, font, foreground);
        }

        private void ToggleCompare()
        {
            _compareSplit = !_compareSplit;
            _split = 0.5;
            // Comparing only makes sense with edits at hand: show the edit panel alongside the split.
            if (_compareSplit && !_panelOpen && !_slideshow)
                SetPanelOpen(true, _lastEditTab);
            CompareButton.Variant = _compareSplit ? DaisyButtonVariant.Primary : DaisyButtonVariant.Ghost;
            CompareButton.ButtonStyle = _compareSplit ? DaisyButtonStyle.Soft : DaisyButtonStyle.Default;
            Canvas.Invalidate();
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

            if ((point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed) && IsEyedropperActive)
            {
                // Left click picks the primary color, right click the secondary color.
                if (SampleColor(t, p) is { } sampled)
                {
                    if (point.Properties.IsRightButtonPressed)
                        SetSecondaryColor(sampled);
                    else
                        SetPenColor(sampled);
                    SetEyedropper(false);
                }
                e.Handled = true;
                return;
            }

            if (point.Properties.IsLeftButtonPressed && IsInNavigator(t, p))
            {
                _draggingNavigator = CanvasHost.CapturePointer(e.Pointer);
                PanFromNavigator(t, p);
                e.Handled = true;
                return;
            }

            if (point.Properties.IsLeftButtonPressed && IsCropMode)
            {
                _cropHandle = CropMath.HitTest(t.ScreenRect, State.Crop, p);
                if (_cropHandle != CropHandle.None)
                {
                    _cropStart = State.Crop;
                    _liveCrop = State.Crop;
                    CanvasHost.CapturePointer(e.Pointer);
                    e.Handled = true;
                }
                return;
            }

            if ((point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed) && IsMarkupMode)
            {
                if (ScreenToTransformedNormalized(t, p) is { } n)
                {
                    // Like Paint Shop Pro: the right mouse button draws with the secondary color.
                    _currentStrokeColor = point.Properties.IsRightButtonPressed ? _secondaryColor : _penColor;
                    _currentStroke = [n];
                    CanvasHost.CapturePointer(e.Pointer);
                    Canvas.Invalidate();
                    e.Handled = true;
                }
                return;
            }

            if (point.Properties.IsLeftButtonPressed && _compareSplit)
            {
                var splitX = t.ScreenRect.X + t.ScreenRect.Width * _split;
                if (Math.Abs(p.X - splitX) < 20)
                {
                    _draggingSplit = true;
                    CanvasHost.CapturePointer(e.Pointer);
                    e.Handled = true;
                    return;
                }
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

            if (_cropHandle != CropHandle.None)
            {
                var dx = (p.X - _pointerStart.X) / t.ScreenRect.Width;
                var dy = (p.Y - _pointerStart.Y) / t.ScreenRect.Height;
                _liveCrop = CropMath.Resize(_cropStart, _cropHandle, dx, dy, t.ImageSize, _cropAspect);
                Canvas.Invalidate();
                return;
            }

            if (_currentStroke != null)
            {
                if (ScreenToTransformedNormalized(t, p, clamp: true) is { } n)
                {
                    var last = _currentStroke[^1];
                    var minDistance = 1.5 / Math.Max(t.ScreenRect.Width, 1);
                    if (Vector2.Distance(last, n) > minDistance)
                    {
                        _currentStroke.Add(n);
                        Canvas.Invalidate();
                    }
                }
                return;
            }

            if (_draggingSplit)
            {
                _split = Math.Clamp((p.X - t.ScreenRect.X) / t.ScreenRect.Width, 0.02, 0.98);
                Canvas.Invalidate();
                return;
            }

            if (_panning)
            {
                _pan = _panStart + new Vector2((float)(p.X - _pointerStart.X), (float)(p.Y - _pointerStart.Y));
                ClampPan();
                Canvas.Invalidate();
                return;
            }

            UpdateHoverCursor(t, p);
        }

        private void UpdateHoverCursor(ViewTransform t, Point p)
        {
            var shape = InputSystemCursorShape.Arrow;
            if (IsCropMode)
            {
                shape = CropMath.HitTest(t.ScreenRect, State.Crop, p) switch
                {
                    CropHandle.TopLeft or CropHandle.BottomRight => InputSystemCursorShape.SizeNorthwestSoutheast,
                    CropHandle.TopRight or CropHandle.BottomLeft => InputSystemCursorShape.SizeNortheastSouthwest,
                    CropHandle.Left or CropHandle.Right => InputSystemCursorShape.SizeWestEast,
                    CropHandle.Top or CropHandle.Bottom => InputSystemCursorShape.SizeNorthSouth,
                    CropHandle.Move => InputSystemCursorShape.SizeAll,
                    _ => InputSystemCursorShape.Arrow
                };
            }
            else if (IsMarkupMode)
            {
                shape = InputSystemCursorShape.Cross;
            }
            else if (_compareSplit && Math.Abs(p.X - (t.ScreenRect.X + t.ScreenRect.Width * _split)) < 20)
            {
                shape = InputSystemCursorShape.SizeWestEast;
            }
            else if (t.ScreenRect.Width > Canvas.ActualWidth || t.ScreenRect.Height > Canvas.ActualHeight)
            {
                shape = InputSystemCursorShape.Hand;
            }
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
            if (_cropHandle != CropHandle.None)
            {
                _cropHandle = CropHandle.None;
                if (_liveCrop is { } crop)
                {
                    _liveCrop = null;
                    ApplyState(State with { Crop = crop });
                    _history.Commit();
                }
            }

            if (_currentStroke is { Count: > 0 } stroke)
            {
                _currentStroke = null;
                ApplyState(State with
                {
                    Strokes = State.Strokes.Add(new MarkupStroke(_currentStrokeColor, (float)(_penSize / 1000), _highlighter, [.. stroke]))
                });
                AddRecentColor(_currentStrokeColor);
            }
            _currentStroke = null;

            _draggingSplit = false;
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

        private Vector2? ScreenToTransformedNormalized(ViewTransform t, Point p, bool clamp = false)
        {
            var r = t.ScreenRect;
            var nx = (p.X - r.X) / r.Width;
            var ny = (p.Y - r.Y) / r.Height;
            if (!clamp && (nx < 0 || ny < 0 || nx > 1 || ny > 1))
                return null;
            nx = Math.Clamp(nx, 0, 1);
            ny = Math.Clamp(ny, 0, 1);

            // Displayed image is the cropped output (outside crop mode); map back to the uncropped transformed space.
            var crop = IsCropMode ? EditState.FullCrop : DisplayState.Crop;
            return new Vector2((float)(crop.X + nx * crop.Width), (float)(crop.Y + ny * crop.Height));
        }

        /// <summary>
        /// Largest centered crop with the given output aspect inside the current crop.
        /// </summary>
        private Rect CreateAspectCrop(double? aspect)
        {
            if (aspect is not { } a || _image == null)
                return State.Crop;
            var transformed = EditRenderer.GetTransformedSize(new Size(_image.Width, _image.Height), State);
            return CropMath.CreateAspectCrop(State.Crop, a, transformed);
        }

        #endregion
    }
}
