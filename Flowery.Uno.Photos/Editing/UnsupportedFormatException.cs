namespace Flowery.Uno.Photos.Editing
{
    /// <summary>The file is an image format SkiaSharp cannot decode (HEIC, AVIF, TIFF, JXR); the viewer shows a localized text.</summary>
    public sealed class UnsupportedFormatException() : Exception("Format not supported");
}
