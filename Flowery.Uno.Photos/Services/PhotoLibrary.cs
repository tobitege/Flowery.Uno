using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Flowery.Uno.Photos.Models;
using Microsoft.UI.Dispatching;

namespace Flowery.Uno.Photos.Services
{
    public enum PhotoSourceKind
    {
        All,
        Favorites,
        Album,
        Folder
    }

    public enum PhotoTypeFilter
    {
        All,
        Jpeg,
        Png,
        Other
    }

    public enum PhotoOrientationFilter
    {
        Any,
        Landscape,
        Portrait,
        Square
    }

    /// <summary>
    /// Describes which photos the gallery shows.
    /// </summary>
    public sealed record PhotoQuery
    {
        public PhotoSourceKind Source { get; init; } = PhotoSourceKind.All;
        public string? SourceKey { get; init; }
        public string? SearchText { get; init; }
        public PhotoTypeFilter TypeFilter { get; init; } = PhotoTypeFilter.All;
        public PhotoOrientationFilter OrientationFilter { get; init; } = PhotoOrientationFilter.Any;
        public PhotoSortMode Sort { get; init; } = PhotoSortMode.DateTakenNewest;
    }

    /// <summary>
    /// Owns the photo library: source folders, scanning with a metadata cache, favorites, albums and file operations.
    /// All public collections are only mutated on the UI thread.
    /// </summary>
    public sealed class PhotoLibrary
    {
        private static readonly Lazy<PhotoLibrary> _instance = new(() => new PhotoLibrary());
        public static PhotoLibrary Instance => _instance.Value;

        private readonly List<string> _folders = [];
        private readonly List<PhotoAlbum> _albums = [];
        private HashSet<string> _favorites = new(PathRules.Comparer);
        private List<PhotoItem> _items = [];
        private Dictionary<string, PhotoItem> _byPath = new(PathRules.Comparer);
        private readonly List<IDisposable> _watchers = [];
        private readonly IPhotoSource _source;
        private DispatcherQueue? _dispatcher;
        private DispatcherQueueTimer? _rescanTimer;
        private CancellationTokenSource? _scanCts;
        private ConcurrentDictionary<string, MetadataCacheEntry> _cache = new(PathRules.Comparer);

        public PhotoLibrary(IPhotoSource? source = null)
        {
            _source = source ?? new FileSystemPhotoSource();
        }

        public IReadOnlyList<string> Folders => _folders;
        public IReadOnlyList<PhotoItem> Items => _items;
        public IReadOnlyList<PhotoAlbum> Albums => _albums;
        public bool IsScanning { get; private set; }

        public static string SamplesFolder => System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Samples");

        /// <summary>Raised on the UI thread whenever items, folders, favorites or albums change.</summary>
        public event EventHandler? Changed;

        /// <summary>Raised on the UI thread with (processed, total) while scanning.</summary>
        public event EventHandler<(int Done, int Total)>? ScanProgress;

        public event EventHandler<bool>? ScanningChanged;

        private string CachePath => System.IO.Path.Combine(PhotosSettings.StateDirectory, "metadata-cache.json");

        // Skia: the dispatcher is optional so the library can be scanned and queried in unit tests without a UI.
        public void Initialize(DispatcherQueue? dispatcher)
        {
            _dispatcher = dispatcher;
            _favorites = PhotosSettings.LoadFavorites();

            if (PhotosSettings.HasSavedFolders())
            {
                _folders.AddRange(PhotosSettings.LoadFolders().Where(f => !string.IsNullOrWhiteSpace(f)));
            }
            else
            {
                var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (!string.IsNullOrWhiteSpace(pictures) && Directory.Exists(pictures))
                    _folders.Add(pictures);
                PhotosSettings.SaveFolders(_folders);
            }

            foreach (var (name, paths) in PhotosSettings.LoadAlbums())
                _albums.Add(new PhotoAlbum(name, paths));

            LoadCache();
            UpdateWatchers();
        }

        #region Scanning

        public async Task RescanAsync()
        {
            _scanCts?.Cancel();
            var cts = new CancellationTokenSource();
            _scanCts = cts;
            var token = cts.Token;

            SetScanning(true);
            try
            {
                var folders = _folders.ToList();
                var recurse = PhotosSettings.IncludeSubfolders;
                var favorites = new HashSet<string>(_favorites, PathRules.Comparer);
                var result = await Task.Run(() => ScanCore(folders, recurse, favorites, token), token).ConfigureAwait(true);
                if (token.IsCancellationRequested)
                    return;

                _items = result;
                _byPath = result.ToDictionary(i => i.Path, PathRules.Comparer);
                Changed?.Invoke(this, EventArgs.Empty);
                // Skia: the path is read now; the write runs later and must not follow a state folder that changed meanwhile.
                var cachePath = CachePath;
                _ = Task.Run(() => SaveCache(cachePath));
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer scan.
            }
            finally
            {
                if (_scanCts == cts)
                    SetScanning(false);
            }
        }

        private void SetScanning(bool value)
        {
            IsScanning = value;
            ScanningChanged?.Invoke(this, value);
        }

        private List<PhotoItem> ScanCore(List<string> folders, bool recurse, HashSet<string> favorites, CancellationToken token)
        {
            var files = new HashSet<string>(PathRules.Comparer);
            foreach (var folder in folders)
            {
                foreach (var file in _source.EnumerateFiles(folder, recurse, token))
                {
                    if (ImageMetadataReader.IsSupported(file))
                        files.Add(file);
                }
            }

            var list = files.ToList();
            var total = list.Count;
            var done = 0;
            var results = new PhotoItem?[total];
            var lastReport = Environment.TickCount64;

            Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount / 2), CancellationToken = token }, i =>
            {
                results[i] = CreateItem(list[i], favorites);
                var current = Interlocked.Increment(ref done);
                var now = Environment.TickCount64;
                if (now - Interlocked.Read(ref lastReport) > 150)
                {
                    Interlocked.Exchange(ref lastReport, now);
                    _dispatcher?.TryEnqueue(() => ScanProgress?.Invoke(this, (current, total)));
                }
            });

            _dispatcher?.TryEnqueue(() => ScanProgress?.Invoke(this, (total, total)));
            return [.. results.Where(r => r != null).Cast<PhotoItem>()];
        }

        private PhotoItem? CreateItem(string path, HashSet<string> favorites)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                    return null;

                var item = new PhotoItem(path)
                {
                    FileSize = info.Length,
                    DateModified = info.LastWriteTime,
                    IsFavorite = favorites.Contains(path)
                };

                if (_cache.TryGetValue(path, out var cached) && cached.Size == info.Length && cached.Ticks == info.LastWriteTimeUtc.Ticks)
                {
                    item.PixelWidth = cached.Width;
                    item.PixelHeight = cached.Height;
                    item.DateTaken = cached.TakenTicks > 0 ? new DateTime(cached.TakenTicks) : null;
                    item.CameraModel = cached.Camera;
                    return item;
                }

                var probe = ImageMetadataReader.ProbeAsync(path).GetAwaiter().GetResult();
                if (probe is { } p)
                {
                    item.PixelWidth = p.Width;
                    item.PixelHeight = p.Height;
                    item.DateTaken = p.DateTaken;
                    item.CameraModel = p.CameraModel;
                }

                _cache[path] = new MetadataCacheEntry
                {
                    Size = info.Length,
                    Ticks = info.LastWriteTimeUtc.Ticks,
                    Width = item.PixelWidth,
                    Height = item.PixelHeight,
                    TakenTicks = item.DateTaken?.Ticks ?? 0,
                    Camera = item.CameraModel
                };
                return item;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates an item for a file outside the library (e.g. opened from the command line or dropped).
        /// </summary>
        public PhotoItem? GetOrCreateItem(string path)
        {
            if (_byPath.TryGetValue(path, out var existing))
                return existing;
            return CreateItem(path, _favorites);
        }

        public PhotoItem? FindByPath(string path) => _byPath.GetValueOrDefault(path);

        /// <summary>
        /// Re-reads metadata for a file after it changed (e.g. edited and saved) and returns the refreshed item.
        /// </summary>
        public PhotoItem? Refresh(string path)
        {
            _cache.TryRemove(path, out _);
            var fresh = CreateItem(path, _favorites);
            if (fresh == null)
                return null;

            if (_byPath.TryGetValue(path, out var existing))
            {
                existing.FileSize = fresh.FileSize;
                existing.DateModified = fresh.DateModified;
                existing.PixelWidth = fresh.PixelWidth;
                existing.PixelHeight = fresh.PixelHeight;
                existing.Revision++;
                Changed?.Invoke(this, EventArgs.Empty);
                return existing;
            }

            if (IsInLibrary(path))
            {
                _items.Add(fresh);
                _byPath[path] = fresh;
                Changed?.Invoke(this, EventArgs.Empty);
            }
            return fresh;
        }

        public bool IsInLibrary(string path) =>
            _folders.Any(f => PathRules.IsUnder(path, f));

        #endregion

        #region Folders

        public bool AddFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return false;
            folder = PathRules.TrimEnd(System.IO.Path.GetFullPath(folder));
            if (_folders.Any(f => string.Equals(PathRules.TrimEnd(f), folder, PathRules.Comparison)))
                return false;
            _folders.Add(folder);
            PhotosSettings.SaveFolders(_folders);
            UpdateWatchers();
            _ = RescanAsync();
            return true;
        }

        public void RemoveFolder(string folder)
        {
            if (_folders.RemoveAll(f => string.Equals(f, folder, PathRules.Comparison)) == 0)
                return;
            PhotosSettings.SaveFolders(_folders);
            UpdateWatchers();
            _ = RescanAsync();
        }

        /// <summary>
        /// Restores a previous folder configuration (used when the settings dialog is cancelled).
        /// </summary>
        public void RestoreFolders(IReadOnlyList<string> folders, bool includeSubfolders)
        {
            var sameFolders = folders.Count == _folders.Count
                && folders.Zip(_folders).All(p => string.Equals(p.First, p.Second, PathRules.Comparison));
            if (sameFolders && includeSubfolders == PhotosSettings.IncludeSubfolders)
                return;

            _folders.Clear();
            _folders.AddRange(folders);
            PhotosSettings.SaveFolders(_folders);
            PhotosSettings.IncludeSubfolders = includeSubfolders;
            PhotosSettings.SaveOptions();
            UpdateWatchers();
            _ = RescanAsync();
        }

        public List<PhotoFolderInfo> GetFolderSummaries()
        {
            var roots = _folders.Select(PathRules.TrimEnd).ToList();
            return [.. _items
                .GroupBy(i => i.FolderPath, PathRules.Comparer)
                .Select(g =>
                {
                    var cover = g.OrderByDescending(i => i.SortDate).First();
                    var isRoot = roots.Any(r => string.Equals(r, PathRules.TrimEnd(g.Key), PathRules.Comparison));
                    return new PhotoFolderInfo(g.Key, PathRules.FolderName(g.Key), g.Count(), cover, isRoot);
                })
                .OrderByDescending(f => f.IsRoot)
                .ThenBy(f => f.Path, PathRules.Comparer)];
        }

        private void UpdateWatchers()
        {
            foreach (var watcher in _watchers)
                watcher.Dispose();
            _watchers.Clear();

            if (!_source.CanWatch)
                return;

            foreach (var folder in _folders)
            {
                if (_source.Watch(folder, PhotosSettings.IncludeSubfolders, OnSourceChanged) is { } watcher)
                    _watchers.Add(watcher);
            }
        }

        private void OnSourceChanged(PhotoSourceChange change)
        {
            if (!ImageMetadataReader.IsSupported(change.Path) && !change.IsRename)
                return;

            _dispatcher?.TryEnqueue(() =>
            {
                if (_rescanTimer == null && _dispatcher != null)
                {
                    _rescanTimer = _dispatcher.CreateTimer();
                    _rescanTimer.Interval = TimeSpan.FromSeconds(1.5);
                    _rescanTimer.IsRepeating = false;
                    _rescanTimer.Tick += (_, _) => _ = RescanAsync();
                }
                _rescanTimer?.Stop();
                _rescanTimer?.Start();
            });
        }

        public void ApplyIncludeSubfolders(bool include)
        {
            if (PhotosSettings.IncludeSubfolders == include)
                return;
            PhotosSettings.IncludeSubfolders = include;
            PhotosSettings.SaveOptions();
            UpdateWatchers();
            _ = RescanAsync();
        }

        #endregion

        #region Favorites

        public void SetFavorite(IEnumerable<PhotoItem> items, bool favorite)
        {
            foreach (var item in items)
            {
                item.IsFavorite = favorite;
                if (favorite)
                    _favorites.Add(item.Path);
                else
                    _favorites.Remove(item.Path);
            }
            PhotosSettings.SaveFavorites(_favorites);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleFavorite(PhotoItem item) => SetFavorite([item], !item.IsFavorite);

        #endregion

        #region Albums

        public PhotoAlbum? FindAlbum(string? name) =>
            name == null ? null : _albums.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.CurrentCultureIgnoreCase));

        public PhotoAlbum CreateAlbum(string name, IEnumerable<PhotoItem>? items = null)
        {
            var baseName = string.IsNullOrWhiteSpace(name) ? "Album" : name.Trim();
            var unique = baseName;
            var counter = 2;
            while (FindAlbum(unique) != null)
                unique = $"{baseName} ({counter++})";

            var album = new PhotoAlbum(unique);
            if (items != null)
                album.Paths.AddRange(items.Select(i => i.Path).Distinct(PathRules.Comparer));
            _albums.Add(album);
            SaveAlbums();
            return album;
        }

        public int AddToAlbum(PhotoAlbum album, IEnumerable<PhotoItem> items)
        {
            var added = 0;
            foreach (var item in items)
            {
                if (album.Paths.Contains(item.Path, PathRules.Comparer))
                    continue;
                album.Paths.Add(item.Path);
                added++;
            }
            SaveAlbums();
            return added;
        }

        public void RemoveFromAlbum(PhotoAlbum album, IEnumerable<PhotoItem> items)
        {
            var remove = new HashSet<string>(items.Select(i => i.Path), PathRules.Comparer);
            album.Paths.RemoveAll(remove.Contains);
            SaveAlbums();
        }

        public bool RenameAlbum(PhotoAlbum album, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return false;
            var existing = FindAlbum(newName.Trim());
            if (existing != null && existing != album)
                return false;
            album.Name = newName.Trim();
            SaveAlbums();
            return true;
        }

        public void DeleteAlbum(PhotoAlbum album)
        {
            _albums.Remove(album);
            SaveAlbums();
        }

        public IEnumerable<PhotoItem> GetAlbumItems(PhotoAlbum album) =>
            album.Paths.Select(p => _byPath.GetValueOrDefault(p)).Where(i => i != null).Cast<PhotoItem>();

        private void SaveAlbums()
        {
            PhotosSettings.SaveAlbums(_albums.Select(a => (a.Name, (IEnumerable<string>)a.Paths)));
            Changed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region File operations

        /// <summary>
        /// Moves the given photos to the Windows Recycle Bin. Returns the number of deleted files.
        /// </summary>
        public async Task<int> DeleteAsync(IReadOnlyList<PhotoItem> items)
        {
            var paths = items.Select(i => i.Path).ToList();
            var deleted = await Task.Run(() =>
            {
                List<string> ok = [];
                foreach (var path in paths)
                {
                    if (FileOperations.MoveToRecycleBin(path))
                        ok.Add(path);
                }
                return ok;
            }).ConfigureAwait(true);

            if (deleted.Count == 0)
                return 0;

            var set = new HashSet<string>(deleted, PathRules.Comparer);
            _items = [.. _items.Where(i => !set.Contains(i.Path))];
            foreach (var path in deleted)
            {
                _byPath.Remove(path);
                _favorites.Remove(path);
                _cache.TryRemove(path, out _);
            }
            foreach (var album in _albums)
                album.Paths.RemoveAll(set.Contains);

            PhotosSettings.SaveFavorites(_favorites);
            PhotosSettings.SaveAlbums(_albums.Select(a => (a.Name, (IEnumerable<string>)a.Paths)));
            Changed?.Invoke(this, EventArgs.Empty);
            return deleted.Count;
        }

        /// <summary>
        /// Copies external files into the library's import folder ("Pictures\Flowery Photos\Imported yyyy-MM-dd").
        /// </summary>
        public async Task<int> ImportFilesAsync(IEnumerable<string> files)
        {
            var root = _folders.FirstOrDefault(Directory.Exists)
                ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var target = System.IO.Path.Combine(root, "Flowery Photos", "Imported " + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            var list = files.Where(ImageMetadataReader.IsSupported).ToList();
            var imported = await Task.Run(() =>
            {
                var count = 0;
                Directory.CreateDirectory(target);
                foreach (var file in list)
                {
                    try
                    {
                        if (IsInLibrary(file))
                            continue;
                        var dest = FileOperations.GetUniquePath(System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)));
                        File.Copy(file, dest);
                        File.SetLastWriteTime(dest, File.GetLastWriteTime(file));
                        count++;
                    }
                    catch
                    {
                        // Skip files that cannot be copied.
                    }
                }
                return count;
            }).ConfigureAwait(true);

            if (!_folders.Any(f => PathRules.IsUnder(target, f)))
            {
                AddFolder(target);
            }
            else
            {
                await RescanAsync();
            }
            return imported;
        }

        #endregion

        #region Query

        public List<PhotoItem> Query(PhotoQuery query)
        {
            IEnumerable<PhotoItem> source = query.Source switch
            {
                PhotoSourceKind.Favorites => _items.Where(i => i.IsFavorite),
                PhotoSourceKind.Album => FindAlbum(query.SourceKey) is { } album ? GetAlbumItems(album) : [],
                PhotoSourceKind.Folder => _items.Where(i => string.Equals(i.FolderPath, query.SourceKey, PathRules.Comparison)),
                _ => _items
            };

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var terms = query.SearchText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                source = source.Where(i => terms.All(t => Matches(i, t)));
            }

            source = query.TypeFilter switch
            {
                PhotoTypeFilter.Jpeg => source.Where(i => i.Extension is "JPG" or "JPEG" or "JPE" or "JFIF"),
                PhotoTypeFilter.Png => source.Where(i => i.Extension == "PNG"),
                PhotoTypeFilter.Other => source.Where(i => i.Extension is not ("JPG" or "JPEG" or "JPE" or "JFIF" or "PNG")),
                _ => source
            };

            source = query.OrientationFilter switch
            {
                PhotoOrientationFilter.Landscape => source.Where(i => i.AspectRatio > 1.05),
                PhotoOrientationFilter.Portrait => source.Where(i => i.AspectRatio < 0.95),
                PhotoOrientationFilter.Square => source.Where(i => i.AspectRatio is >= 0.95 and <= 1.05),
                _ => source
            };

            // Albums keep their manual order unless a specific sort is requested.
            return query.Sort switch
            {
                PhotoSortMode.DateTakenOldest => [.. source.OrderBy(i => i.SortDate)],
                PhotoSortMode.DateModifiedNewest => [.. source.OrderByDescending(i => i.DateModified)],
                PhotoSortMode.NameAscending => [.. source.OrderBy(i => i.FileName, StringComparer.CurrentCultureIgnoreCase)],
                PhotoSortMode.NameDescending => [.. source.OrderByDescending(i => i.FileName, StringComparer.CurrentCultureIgnoreCase)],
                PhotoSortMode.SizeLargest => [.. source.OrderByDescending(i => i.FileSize)],
                _ => [.. source.OrderByDescending(i => i.SortDate)]
            };
        }

        private static bool Matches(PhotoItem item, string term)
        {
            if (item.FileName.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                return true;
            if (item.FolderPath.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                return true;
            if (item.CameraModel?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true)
                return true;
            if (item.Extension.Equals(term.TrimStart('.'), StringComparison.OrdinalIgnoreCase))
                return true;

            var culture = Localization.PhotosLocalization.CurrentCulture;
            var date = item.SortDate;
            return date.Year.ToString(CultureInfo.InvariantCulture) == term
                || date.ToString("MMMM", culture).Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || date.ToString("MMMM", CultureInfo.InvariantCulture).Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Metadata cache

        private void LoadCache()
        {
            try
            {
                if (!File.Exists(CachePath))
                    return;
                var json = File.ReadAllText(CachePath);
                var data = JsonSerializer.Deserialize(json, PhotosJsonContext.Default.DictionaryStringMetadataCacheEntry);
                if (data != null)
                    _cache = new ConcurrentDictionary<string, MetadataCacheEntry>(data, PathRules.Comparer);
            }
            catch
            {
                _cache = new ConcurrentDictionary<string, MetadataCacheEntry>(PathRules.Comparer);
            }
        }

        private void SaveCache(string cachePath)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(cachePath)!);
                var snapshot = new Dictionary<string, MetadataCacheEntry>(_cache, PathRules.Comparer);
                File.WriteAllText(cachePath, JsonSerializer.Serialize(snapshot, PhotosJsonContext.Default.DictionaryStringMetadataCacheEntry));
            }
            catch
            {
                // Cache is optional.
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
            ThumbnailDiskCache.Default.Clear();
            try
            {
                if (File.Exists(CachePath))
                    File.Delete(CachePath);
            }
            catch
            {
                // Ignore.
            }
        }

        #endregion
    }

    public sealed class MetadataCacheEntry
    {
        public long Size { get; set; }
        public long Ticks { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long TakenTicks { get; set; }
        public string? Camera { get; set; }
    }

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(Dictionary<string, MetadataCacheEntry>))]
    internal partial class PhotosJsonContext : JsonSerializerContext
    {
    }
}
