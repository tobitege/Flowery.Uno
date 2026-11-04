using System.IO;
using System.Threading;

namespace Flowery.Uno.Photos.Services
{
    /// <summary>A change reported by <see cref="IPhotoSource.Watch"/>. A rename may be a folder, so it is never filtered by extension.</summary>
    public readonly record struct PhotoSourceChange(string Path, bool IsRename);

    /// <summary>
    /// Where the library finds its photos. The desktop head scans folders on disk; Browser (no folder scan) and Android
    /// (scoped storage) will need a picker or MediaStore based source instead. Skia: not in Flowery.WinUI.Photos, added
    /// so those heads do not need a PhotoLibrary rewrite. Reading, importing and deleting files still go through paths.
    /// </summary>
    public interface IPhotoSource
    {
        /// <summary>False when the source cannot report changes (Browser); the library then only rescans on request.</summary>
        bool CanWatch { get; }

        /// <summary>All files below <paramref name="root"/>. A missing or unreadable root yields nothing.</summary>
        IEnumerable<string> EnumerateFiles(string root, bool recurse, CancellationToken token);

        /// <summary>Reports changes below <paramref name="root"/> until the result is disposed. Null when watching is not possible.</summary>
        IDisposable? Watch(string root, bool recurse, Action<PhotoSourceChange> onChanged);
    }

    /// <summary>Folders on a local file system, watched with <see cref="FileSystemWatcher"/>.</summary>
    public sealed class FileSystemPhotoSource : IPhotoSource
    {
        public bool CanWatch => true;

        public IEnumerable<string> EnumerateFiles(string root, bool recurse, CancellationToken token)
        {
            if (!Directory.Exists(root))
                yield break;

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = recurse,
                IgnoreInaccessible = true,
                AttributesToSkip = System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System
            };

            IEnumerator<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", options).GetEnumerator();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                yield break;
            }

            using (files)
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        if (!files.MoveNext())
                            yield break;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        // Skip unreadable folders.
                        yield break;
                    }
                    yield return files.Current;
                }
            }
        }

        public IDisposable? Watch(string root, bool recurse, Action<PhotoSourceChange> onChanged)
        {
            if (!Directory.Exists(root))
                return null;

            try
            {
                var watcher = new FileSystemWatcher(root)
                {
                    IncludeSubdirectories = recurse,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
                };
                watcher.Created += (_, e) => onChanged(new PhotoSourceChange(e.FullPath, false));
                watcher.Deleted += (_, e) => onChanged(new PhotoSourceChange(e.FullPath, false));
                watcher.Changed += (_, e) => onChanged(new PhotoSourceChange(e.FullPath, false));
                watcher.Renamed += (_, e) => onChanged(new PhotoSourceChange(e.FullPath, true));
                watcher.EnableRaisingEvents = true;
                return watcher;
            }
            catch
            {
                // Watching is best-effort.
                return null;
            }
        }
    }
}
