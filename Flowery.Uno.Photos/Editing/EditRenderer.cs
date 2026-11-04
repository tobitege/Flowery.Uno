using System.Numerics;
using Flowery.Uno.Photos.Editing.Effects;
using Flowery.Uno.Photos.Services;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;

namespace Flowery.Uno.Photos.Editing
{
    /// <summary>
    /// Renders an <see cref="EditState"/>. All edits are resolution independent (normalized geometry), so the same state
    /// renders the live preview at screen size and the final export at full resolution.
    /// Skia: the Win2D effect graph ran lazily on the GPU; here every render is a CPU raster pass (the canvas of an
    /// SKCanvasElement has no GPU context), so the result is an image the viewer keeps and only draws.
    /// </summary>
    public static class EditRenderer
    {
        public const int PreviewMaxEdge = 2560;

        /// <summary>Longest edge a decoded photo keeps. Larger files are downscaled while decoding (Win2D used the GPU limit).</summary>
        public const int MaxBitmapEdge = 16384;

        private const float StrokeHighlighterAlpha = 110;
        private const int MinBandHeight = 64;

        private static readonly SKSamplingOptions PreviewResampling = new(SKCubicResampler.Mitchell);
        private static readonly SKSamplingOptions LinearSampling = new(SKFilterMode.Linear);
        private static readonly SKSamplingOptions NearestSampling = new(SKFilterMode.Nearest);

        public static Size GetTransformedSize(Size source, EditState state) =>
            state.Rotation % 2 == 1 ? new Size(source.Height, source.Width) : source;

        public static Size GetOutputSize(Size source, EditState state)
        {
            var t = GetTransformedSize(source, state);
            return new Size(Math.Max(1, Math.Round(t.Width * state.Crop.Width)), Math.Max(1, Math.Round(t.Height * state.Crop.Height)));
        }

        /// <summary>
        /// Rotation, flip, straighten (with fill zoom) and optionally crop, as a matrix from source pixels to output pixels.
        /// Skia: replaces the Transform2DEffect of BuildGeometry; the crop is the size of the surface it is drawn on.
        /// </summary>
        public static SKMatrix GetGeometryMatrix(Size sourceSize, EditState state, bool applyCrop)
        {
            var t = GetTransformedSize(sourceSize, state);
            var tw = (float)t.Width;
            var th = (float)t.Height;

            var angle = (float)((state.Rotation % 4) * Math.PI / 2 + state.Straighten * Math.PI / 180);
            var theta = Math.Abs(state.Straighten) * Math.PI / 180;
            var fill = (float)(Math.Cos(theta) + Math.Max(tw / th, th / tw) * Math.Sin(theta));

            var matrix = Matrix3x2.CreateTranslation(-(float)sourceSize.Width / 2, -(float)sourceSize.Height / 2)
                * Matrix3x2.CreateScale(state.FlipH ? -1 : 1, state.FlipV ? -1 : 1)
                * Matrix3x2.CreateRotation(angle)
                * Matrix3x2.CreateScale(fill)
                * Matrix3x2.CreateTranslation(tw / 2, th / 2);

            if (applyCrop && state.Crop != EditState.FullCrop)
                matrix *= Matrix3x2.CreateTranslation(-(float)(state.Crop.X * tw), -(float)(state.Crop.Y * th));

            return new SKMatrix(matrix.M11, matrix.M21, matrix.M31, matrix.M12, matrix.M22, matrix.M32, 0, 0, 1);
        }

        /// <summary>
        /// Renders geometry and adjustments (no strokes) of <paramref name="source"/>. <paramref name="scale"/> is the
        /// size of the result relative to the source, so a preview only pays for the pixels it shows.
        /// The caller owns the image.
        /// </summary>
        public static SKImage Render(SKImage source, EditState state, bool applyCrop = true, double scale = 1, bool includeFilter = true, bool adjust = true)
        {
            var sourceSize = new Size(source.Width, source.Height);
            var size = applyCrop ? GetOutputSize(sourceSize, state) : GetTransformedSize(sourceSize, state);
            var width = Math.Max(1, (int)Math.Round(size.Width * scale));
            var height = Math.Max(1, (int)Math.Round(size.Height * scale));

            var plan = adjust ? AdjustmentPlan.Create(state, includeFilter) : AdjustmentPlan.None;
            var image = DrawGeometry(source, sourceSize, state, applyCrop, width, height, plan.Before);
            try
            {
                return ApplyDetail(image, state, plan);
            }
            finally
            {
                plan.Dispose();
            }
        }

        /// <summary>The size of a preview relative to its source, for a display scale; coarse so that small zoom changes reuse it.</summary>
        public static double GetPreviewScale(double displayScale) =>
            Math.Clamp(Math.Ceiling(displayScale * 20) / 20, 0.05, 1);

        /// <summary>Applies a filter look to a small image for its tile in the panel. The caller owns the bitmap.</summary>
        public static SKBitmap RenderFilterTile(SKImage image, ColorMatrix matrix)
        {
            var bitmap = new SKBitmap(new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
            using var canvas = new SKCanvas(bitmap);
            using var filter = SKColorFilter.CreateColorMatrix(matrix.ToSkiaMatrix());
            using var paint = new SKPaint { ColorFilter = filter };
            canvas.Clear(SKColors.Transparent);
            canvas.DrawImage(image, 0, 0, NearestSampling, paint);
            return bitmap;
        }

        /// <summary>Renders the final edited image at full resolution, strokes included. The caller owns the image.</summary>
        public static SKImage RenderFinal(SKImage source, EditState state)
        {
            var edited = Render(source, state);
            if (state.Strokes.IsEmpty)
                return edited;

            using var owned = edited;
            using var surface = CreateSurface(edited.Width, edited.Height);
            surface.Canvas.DrawImage(edited, 0, 0, NearestSampling);
            var transformed = GetTransformedSize(new Size(source.Width, source.Height), state);
            DrawStrokes(surface.Canvas, state.Strokes, transformed, state.Crop, new Size(edited.Width, edited.Height));
            return surface.Snapshot();
        }

        /// <summary>
        /// Draws markup strokes. <paramref name="imageSize"/> is the size of the drawn image in the current drawing space;
        /// <paramref name="visible"/> is the normalized region of the transformed image that is drawn.
        /// </summary>
        public static void DrawStrokes(SKCanvas canvas, IEnumerable<MarkupStroke> strokes, Size transformedSize, Rect visible, Size imageSize)
        {
            var scaleX = imageSize.Width / Math.Max(visible.Width, 1e-6);
            var scaleY = imageSize.Height / Math.Max(visible.Height, 1e-6);
            var refEdge = Math.Max(transformedSize.Width, transformedSize.Height) * (imageSize.Width / Math.Max(1, transformedSize.Width * visible.Width));

            foreach (var stroke in strokes)
            {
                if (stroke.Points.Length == 0)
                    continue;

                var width = (float)(stroke.Width * refEdge);
                var alpha = stroke.Highlighter ? (byte)Math.Min(StrokeHighlighterAlpha, stroke.Color.A) : stroke.Color.A;
                using var paint = new SKPaint
                {
                    Color = new SKColor(stroke.Color.R, stroke.Color.G, stroke.Color.B, alpha),
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round
                };

                SKPoint Map(Vector2 p) => new((float)((p.X - visible.X) * scaleX), (float)((p.Y - visible.Y) * scaleY));

                if (stroke.Points.Length == 1)
                {
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(Map(stroke.Points[0]), width / 2, paint);
                    continue;
                }

                using var builder = new SKPathBuilder();
                builder.MoveTo(Map(stroke.Points[0]));
                for (var i = 1; i < stroke.Points.Length; i++)
                {
                    // Quadratic smoothing through midpoints.
                    var p0 = Map(stroke.Points[i - 1]);
                    var p1 = Map(stroke.Points[i]);
                    builder.QuadTo(p0, new SKPoint((p0.X + p1.X) / 2, (p0.Y + p1.Y) / 2));
                }
                builder.LineTo(Map(stroke.Points[^1]));
                using var path = builder.Detach();
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = Math.Max(1, width);
                canvas.DrawPath(path, paint);
            }
        }

        /// <summary>The encoder for a file name; everything that is not PNG is written as JPEG.</summary>
        public static SKEncodedImageFormat GetFormatForPath(string path) =>
            System.IO.Path.GetExtension(path).ToLowerInvariant() == ".png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;

        /// <summary>
        /// True if the format can be written back; otherwise edits are saved as JPEG copies.
        /// Skia: SkiaSharp cannot encode BMP, GIF, TIFF and JPEG XR, which Win2D could.
        /// </summary>
        public static bool CanWriteFormat(string path) =>
            System.IO.Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".jpe" or ".jfif" or ".png";

        /// <summary>Encodes for <paramref name="path"/>; <paramref name="quality"/> (0 to 100) applies to JPEG.</summary>
        public static byte[] Encode(SKImage image, string path, int quality)
        {
            using var data = image.Encode(GetFormatForPath(path), quality)
                ?? throw new InvalidOperationException("The image could not be encoded.");
            return data.ToArray();
        }

        private static SKSurface CreateSurface(int width, int height) =>
            SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException($"No surface for {width} x {height}.");

        /// <summary>First pass: geometry plus the color filters that do not need neighbouring pixels.</summary>
        private static SKImage DrawGeometry(SKImage source, Size sourceSize, EditState state, bool applyCrop, int width, int height, SKColorFilter? filter)
        {
            var size = applyCrop ? GetOutputSize(sourceSize, state) : GetTransformedSize(sourceSize, state);
            var scaleX = (float)(width / size.Width);
            var scaleY = (float)(height / size.Height);
            var matrix = GetGeometryMatrix(sourceSize, state, applyCrop);
            var sampling = width < size.Width || state.HasGeometry ? PreviewResampling : LinearSampling;
            return DrawInBands(width, height, canvas =>
            {
                canvas.Scale(scaleX, scaleY);
                canvas.Concat(matrix);
                using var paint = new SKPaint { ColorFilter = filter };
                canvas.DrawImage(source, 0, 0, sampling, paint);
            });
        }

        /// <summary>
        /// Runs <paramref name="draw"/> on horizontal bands of a new image, one per core. Skia: the raster pipeline runs the
        /// shaders on one core; the bands bring back part of the speed of the GPU graph.
        /// </summary>
        private static SKImage DrawInBands(int width, int height, Action<SKCanvas> draw)
        {
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(info);
            bitmap.Erase(SKColors.Transparent);
            var pixels = bitmap.GetPixels();
            var rowBytes = bitmap.RowBytes;
            var bands = Math.Clamp(height / MinBandHeight, 1, Environment.ProcessorCount);

            Parallel.For(0, bands, band =>
            {
                var top = (int)((long)height * band / bands);
                var bottom = (int)((long)height * (band + 1) / bands);
                using var surface = SKSurface.Create(info.WithSize(width, bottom - top), pixels + top * rowBytes, rowBytes)
                    ?? throw new InvalidOperationException($"No surface for {width} x {bottom - top}.");
                surface.Canvas.Translate(0, -top);
                draw(surface.Canvas);
            });

            bitmap.SetImmutable();
            return SKImage.FromBitmap(bitmap);
        }

        /// <summary>Remaining passes. Takes ownership of <paramref name="image"/>.</summary>
        private static SKImage ApplyDetail(SKImage image, EditState s, AdjustmentPlan plan)
        {
            if (plan.NeedsHighlightsShadows)
                image = Replace(image, ApplyHighlightsShadows(image, s, plan.After));

            if (s.Sharpness < 0 && plan.Adjust)
                image = Replace(image, Blur(image, (float)(-s.Sharpness / 100 * Math.Max(image.Width, image.Height) / 400)));

            if (plan.Adjust && (s.Sharpness > 0 || s.Vignette != 0))
                image = Replace(image, ApplySharpenAndVignette(image, s));

            return image;
        }

        private static SKImage Replace(SKImage old, SKImage next)
        {
            old.Dispose();
            return next;
        }

        private static SKImage ApplyHighlightsShadows(SKImage image, EditState s, SKColorFilter? after)
        {
            using var mask = Blur(image, PhotoEffects.MaskBlurSigma);
            using var imageShader = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            using var maskShader = mask.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            using var shader = PhotoEffects.HighlightsShadows(
                imageShader,
                maskShader,
                (float)Math.Clamp(s.Highlights / 100, -1, 1),
                (float)Math.Clamp(s.Shadows / 100, -1, 1),
                (float)Math.Clamp(s.Clarity / 100, -1, 1));

            return DrawInBands(image.Width, image.Height, canvas =>
            {
                using var paint = new SKPaint { Shader = shader, ColorFilter = after, BlendMode = SKBlendMode.Src };
                canvas.DrawRect(0, 0, image.Width, image.Height, paint);
            });
        }

        private static SKImage Blur(SKImage image, float sigma)
        {
            using var blur = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp);
            return DrawInBands(image.Width, image.Height, canvas =>
            {
                using var paint = new SKPaint { ImageFilter = blur };
                canvas.DrawImage(image, 0, 0, NearestSampling, paint);
            });
        }

        private static SKImage ApplySharpenAndVignette(SKImage image, EditState s)
        {
            var shader = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            try
            {
                if (s.Sharpness > 0)
                {
                    var sharpened = PhotoEffects.Sharpen(shader, (float)(s.Sharpness / 100 * 6));
                    shader.Dispose();
                    shader = sharpened;
                }

                if (s.Vignette != 0)
                {
                    var vignette = PhotoEffects.Vignette(
                        shader,
                        image.Width,
                        image.Height,
                        s.Vignette > 0 ? SKColors.Black : SKColors.White,
                        (float)Math.Clamp(Math.Abs(s.Vignette) / 100, 0, 1));
                    shader.Dispose();
                    shader = vignette;
                }

                var final = shader;
                return DrawInBands(image.Width, image.Height, canvas =>
                {
                    using var paint = new SKPaint { Shader = final, BlendMode = SKBlendMode.Src };
                    canvas.DrawRect(0, 0, image.Width, image.Height, paint);
                });
            }
            finally
            {
                shader.Dispose();
            }
        }

        /// <summary>
        /// Color steps of an edit in the order of the Win2D graph: exposure, highlights and shadows, contrast, temperature,
        /// then one color matrix (brightness, saturation, filter, fade). Steps that do not read neighbouring pixels are
        /// folded into the pass around them, so an edit without highlights and shadows needs one pass only.
        /// </summary>
        private sealed class AdjustmentPlan : IDisposable
        {
            public static readonly AdjustmentPlan None = new(false, false, null, null);

            private AdjustmentPlan(bool adjust, bool needsHighlightsShadows, SKColorFilter? before, SKColorFilter? after)
            {
                Adjust = adjust;
                NeedsHighlightsShadows = needsHighlightsShadows;
                Before = before;
                After = after;
            }

            public bool Adjust { get; }

            public bool NeedsHighlightsShadows { get; }

            public SKColorFilter? Before { get; }

            public SKColorFilter? After { get; }

            public static AdjustmentPlan Create(EditState s, bool includeFilter)
            {
                var exposure = s.Exposure != 0 ? PhotoEffects.Exposure((float)(s.Exposure / 100 * 1.5)) : null;
                var rest = Compose(
                    s.Contrast != 0 ? PhotoEffects.Contrast((float)Math.Clamp(s.Contrast / 100 * 0.75, -1, 1)) : null,
                    s.Warmth != 0 || s.Tint != 0
                        ? PhotoEffects.TemperatureTint((float)Math.Clamp(s.Warmth / 100 * 0.8, -1, 1), (float)Math.Clamp(s.Tint / 100 * 0.8, -1, 1))
                        : null);
                rest = Compose(rest, CreateColorMatrix(s, includeFilter));

                return s.Highlights != 0 || s.Shadows != 0 || s.Clarity != 0
                    ? new AdjustmentPlan(true, true, exposure, rest)
                    : new AdjustmentPlan(true, false, Compose(exposure, rest), null);
            }

            public void Dispose()
            {
                Before?.Dispose();
                After?.Dispose();
            }

            /// <summary>Runs <paramref name="first"/>, then <paramref name="second"/>. Takes ownership of both.</summary>
            private static SKColorFilter? Compose(SKColorFilter? first, SKColorFilter? second)
            {
                if (first == null || second == null)
                    return first ?? second;

                var composed = SKColorFilter.CreateCompose(second, first);
                first.Dispose();
                second.Dispose();
                return composed;
            }

            private static SKColorFilter? CreateColorMatrix(EditState s, bool includeFilter)
            {
                // Brightness, saturation, fade and the filter look are folded into one color matrix.
                var matrix = ColorMatrix.Identity;
                var useMatrix = false;
                if (s.Brightness != 0)
                {
                    matrix = matrix.Then(ColorMatrix.Brightness((float)(s.Brightness / 100 * 0.25)));
                    useMatrix = true;
                }
                if (s.Saturation != 0)
                {
                    matrix = matrix.Then(ColorMatrix.Saturation((float)(1 + s.Saturation / 100)));
                    useMatrix = true;
                }
                if (includeFilter && s.Filter != FilterPresets.NoneId && s.FilterIntensity > 0)
                {
                    var preset = FilterPresets.Get(s.Filter);
                    matrix = matrix.Then(ColorMatrix.Identity.Lerp(preset.Matrix, (float)Math.Clamp(s.FilterIntensity / 100, 0, 1)));
                    useMatrix = true;
                }
                if (s.Fade > 0)
                {
                    matrix = matrix.Then(ColorMatrix.Fade((float)(s.Fade / 100)));
                    useMatrix = true;
                }
                return useMatrix ? SKColorFilter.CreateColorMatrix(matrix.ToSkiaMatrix()) : null;
            }
        }

        /// <summary>
        /// Decodes a file with its EXIF orientation applied. Bitmaps with an edge above <see cref="MaxBitmapEdge"/> are
        /// downscaled. Throws <see cref="UnsupportedFormatException"/> for files SkiaSharp cannot decode (HEIC, AVIF, TIFF, JXR).
        /// The caller owns the bitmap.
        /// </summary>
        public static SKBitmap LoadOriented(string path)
        {
            int targetHeight;
            using (var codec = SKCodec.Create(path))
            {
                if (codec == null)
                    throw new UnsupportedFormatException();

                var swap = ThumbnailDecoder.SwapsAxes(codec.EncodedOrigin);
                var width = swap ? codec.Info.Height : codec.Info.Width;
                var height = swap ? codec.Info.Width : codec.Info.Height;
                var longest = Math.Max(width, height);
                targetHeight = longest > MaxBitmapEdge
                    ? Math.Max(1, (int)Math.Round((double)height * MaxBitmapEdge / longest))
                    : height;
            }

            return ThumbnailDecoder.Decode(path, targetHeight) ?? throw new UnsupportedFormatException();
        }

        /// <summary>
        /// Creates a downscaled copy for interactive drawing, or returns the source if it is small enough.
        /// </summary>
        public static SKBitmap CreatePreview(SKBitmap source, int maxEdge = PreviewMaxEdge)
        {
            var longest = Math.Max(source.Width, source.Height);
            if (longest <= maxEdge)
                return source;

            var scale = (double)maxEdge / longest;
            var info = new SKImageInfo(
                Math.Max(1, (int)Math.Round(source.Width * scale)),
                Math.Max(1, (int)Math.Round(source.Height * scale)),
                SKColorType.Bgra8888,
                SKAlphaType.Premul);
            return source.Resize(info, PreviewResampling) ?? source;
        }
    }
}
