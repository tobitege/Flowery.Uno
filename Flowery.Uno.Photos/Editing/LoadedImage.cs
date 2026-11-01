using SkiaSharp;

namespace Flowery.Uno.Photos.Editing
{
    /// <summary>
    /// A photo decoded for the viewer: the full-resolution pixels and a smaller preview for interactive drawing.
    /// Skia: replaces the pair of Win2D CanvasBitmaps. Owns the bitmaps and the images made from them.
    /// </summary>
    public sealed class LoadedImage : IDisposable
    {
        private LoadedImage(SKBitmap full, SKBitmap preview)
        {
            FullBitmap = full;
            PreviewBitmap = preview;
            full.SetImmutable();
            Full = SKImage.FromBitmap(full);
            if (ReferenceEquals(full, preview))
            {
                Preview = Full;
            }
            else
            {
                preview.SetImmutable();
                Preview = SKImage.FromBitmap(preview);
            }
        }

        public SKBitmap FullBitmap { get; }

        public SKBitmap PreviewBitmap { get; }

        public SKImage Full { get; }

        public SKImage Preview { get; }

        /// <summary>Pixel width of the full-resolution image (orientation applied).</summary>
        public int Width => FullBitmap.Width;

        public int Height => FullBitmap.Height;

        public bool HasSeparatePreview => !ReferenceEquals(Full, Preview);

        /// <summary>Decodes <paramref name="path"/> with its EXIF orientation applied. Throws <see cref="InvalidDataException"/> if it cannot be decoded.</summary>
        public static LoadedImage Load(string path)
        {
            var full = EditRenderer.LoadOriented(path);
            try
            {
                return new LoadedImage(full, EditRenderer.CreatePreview(full));
            }
            catch
            {
                full.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (HasSeparatePreview)
            {
                Preview.Dispose();
                PreviewBitmap.Dispose();
            }
            Full.Dispose();
            FullBitmap.Dispose();
        }
    }
}
