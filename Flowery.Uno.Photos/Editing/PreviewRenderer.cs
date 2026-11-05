using System.Diagnostics;
using SkiaSharp;

namespace Flowery.Uno.Photos.Editing
{
    /// <summary>What a preview shows. Two equal requests give the same picture.</summary>
    public sealed record PreviewRequest(SKImage Source, EditState State, bool ApplyCrop, double Scale);

    /// <summary>
    /// Renders previews off the UI thread, the newest request first: requests made during a render replace each other.
    /// Skia: Win2D rendered on the GPU inside the draw call; a CPU render takes longer than a frame, so a slider must not wait for it.
    /// </summary>
    public sealed class PreviewRenderer
    {
        private readonly object _gate = new();
        private (PreviewRequest Request, SKImage Source)? _next;
        private bool _running;

        /// <summary>Raised on a worker thread; the handler owns the image.</summary>
        public event Action<PreviewRequest, SKImage>? Rendered;

        /// <summary>Queues a render. Takes ownership of <paramref name="source"/>, a copy of the request's source that outlives it.</summary>
        public void Request(PreviewRequest request, SKImage source)
        {
            lock (_gate)
            {
                _next?.Source.Dispose();
                _next = (request, source);
                if (_running)
                    return;
                _running = true;
            }
            _ = Task.Run(Run);
        }

        private void Run()
        {
            while (true)
            {
                (PreviewRequest Request, SKImage Source) work;
                lock (_gate)
                {
                    if (_next is not { } next)
                    {
                        _running = false;
                        return;
                    }
                    work = next;
                    _next = null;
                }

                try
                {
                    var image = EditRenderer.Render(work.Source, work.Request.State, work.Request.ApplyCrop, work.Request.Scale);
                    Rendered?.Invoke(work.Request, image);
                }
                catch (Exception ex)
                {
                    // The old preview stays; the save path reports its own errors.
                    Debug.WriteLine(ex);
                }
                finally
                {
                    work.Source.Dispose();
                }
            }
        }
    }
}
