using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Flowery.Uno.Photos.Services.Platform
{
    /// <summary>Behavior shared by the OS implementations. Commands go through <see cref="ICommandRunner"/> so tests can fake them.</summary>
    public abstract class PlatformServicesBase(ICommandRunner runner) : IPlatformServices
    {
        protected ICommandRunner Runner { get; } = runner;

        public abstract PlatformFeatures Features { get; }

        public virtual TitleBarMode TitleBarMode => TitleBarMode.System;

        public abstract bool MoveToTrash(string path);

        public abstract void RevealInFileManager(string path);

        public abstract void OpenFolder(string folder);

        public abstract void OpenUri(string uri);

        public virtual Task<IReadOnlyList<OpenWithApp>> GetOpenWithAppsAsync(string path) =>
            Task.FromResult<IReadOnlyList<OpenWithApp>>([]);

        public virtual Task<bool> OpenWithAsync(string path, OpenWithApp? app = null) => Task.FromResult(false);

        public virtual bool SetAsWallpaper(string path) => false;

        public virtual Task<bool> CopyFilesToClipboardAsync(IReadOnlyList<string> paths) => Task.FromResult(false);

        public virtual SystemAccent? GetSystemAccent() => null;

        public virtual IReadOnlyList<DisplayRect> GetDisplays() => [];

        public virtual DisplayGeometry DisplayGeometry => DisplayGeometry.None;

        public Task<bool> CopyBitmapToClipboardAsync(byte[] pngBytes)
        {
            if (pngBytes.Length == 0)
                return Task.FromResult(false);
            try
            {
                // Skia: DataWriter.DetachStream is not implemented on the desktop head; a MemoryStream-backed stream is.
                var stream = new MemoryStream(pngBytes).AsRandomAccessStream();
                var package = new DataPackage();
                package.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
                Clipboard.SetContent(package);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>Uno writes file lists to the clipboard on Linux only; the other heads use their own path or hide the action.</summary>
        protected static async Task<bool> CopyStorageItemsAsync(IReadOnlyList<string> paths)
        {
            try
            {
                List<IStorageItem> files = [];
                foreach (var path in paths)
                    files.Add(await StorageFile.GetFileFromPathAsync(path));

                var package = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
                package.SetStorageItems(files);
                Clipboard.SetContent(package);
                Clipboard.Flush();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
