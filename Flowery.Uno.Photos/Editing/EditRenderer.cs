using Flowery.Uno.Photos.Services;
using SkiaSharp;
using Windows.Foundation;

namespace Flowery.Uno.Photos.Editing
{
    /// <summary>
    /// Image loading and output geometry for the viewer. Skia: the Win2D effect graph (geometry, adjustments, markup,
    /// final render) arrives with the editing phase; this part is what the read-only viewer needs.
    /// </summary>
    public static class EditRenderer
    {
        public const int PreviewMaxEdge = 2560;

        /// <summary>Longest edge a decoded photo keeps. Larger files are downscaled while decoding (Win2D used the GPU limit).</summary>
        public const int MaxBitmapEdge = 16384;

        private static readonly SKSamplingOptions PreviewResampling = new(SKCubicResampler.Mitchell);

        public static Size GetTransformedSize(Size source, EditState state) =>
            state.Rotation % 2 == 1 ? new Size(source.Height, source.Width) : source;

        public static Size GetOutputSize(Size source, EditState state)
        {
            var t = GetTransformedSize(source, state);
            return new Size(Math.Max(1, Math.Round(t.Width * state.Crop.Width)), Math.Max(1, Math.Round(t.Height * state.Crop.Height)));
        }

        /// <summary>
        /// Decodes a file with its EXIF orientation applied. Bitmaps with an edge above <see cref="MaxBitmapEdge"/> are
        /// downscaled. Throws <see cref="InvalidDataException"/> for files SkiaSharp cannot decode (HEIC, AVIF, TIFF, JXR).
        /// The caller owns the bitmap.
        /// </summary>
        public static SKBitmap LoadOriented(string path)
        {
            int targetHeight;
            using (var codec = SKCodec.Create(path))
            {
                if (codec == null)
                    throw new InvalidDataException("Format not supported");

                var swap = ThumbnailDecoder.SwapsAxes(codec.EncodedOrigin);
                var width = swap ? codec.Info.Height : codec.Info.Width;
                var height = swap ? codec.Info.Width : codec.Info.Height;
                var longest = Math.Max(width, height);
                targetHeight = longest > MaxBitmapEdge
                    ? Math.Max(1, (int)Math.Round((double)height * MaxBitmapEdge / longest))
                    : height;
            }

            return ThumbnailDecoder.Decode(path, targetHeight) ?? throw new InvalidDataException("Format not supported");
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
