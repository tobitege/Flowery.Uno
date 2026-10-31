using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_PhotoSource
    {
        private string _state = null!;
        private string _root = null!;

        [TestInitialize]
        public void Create()
        {
            _state = TestImages.TempDirectory("photos-src-state-");
            _root = TestImages.TempDirectory("photos-src-root-");
            PhotosSettings.Initialize(_state);
            PhotosSettings.IncludeSubfolders = true;
        }

        [TestCleanup]
        public void Delete()
        {
            TestImages.DeleteQuietly(_state);
            TestImages.DeleteQuietly(_root);
        }

        [TestMethod]
        public void When_RootIsMissing_NothingIsEnumeratedOrWatched()
        {
            var source = new FileSystemPhotoSource();
            var missing = Path.Combine(_root, "missing");

            Assert.AreEqual(0, source.EnumerateFiles(missing, true, CancellationToken.None).Count());
            Assert.IsNull(source.Watch(missing, true, _ => { }));
        }

        [TestMethod]
        public void When_RecurseIsOff_OnlyTheTopFolderIsEnumerated()
        {
            TestImages.Write(Path.Combine(_root, "a.jpg"), 8, 8);
            TestImages.Write(Path.Combine(_root, "sub", "b.jpg"), 8, 8);
            var source = new FileSystemPhotoSource();

            var flat = source.EnumerateFiles(_root, false, CancellationToken.None).Select(Path.GetFileName).ToList();
            var deep = source.EnumerateFiles(_root, true, CancellationToken.None).Select(Path.GetFileName).ToList();

            CollectionAssert.AreEquivalent(new[] { "a.jpg" }, flat);
            CollectionAssert.AreEquivalent(new[] { "a.jpg", "b.jpg" }, deep);
        }

        [TestMethod]
        public void When_EnumerationIsCancelled_ItStops()
        {
            TestImages.Write(Path.Combine(_root, "a.jpg"), 8, 8);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsExactly<OperationCanceledException>(() =>
                new FileSystemPhotoSource().EnumerateFiles(_root, true, cts.Token).ToList());
        }

        [TestMethod]
        public async Task When_FileIsCreated_WatchReportsIt()
        {
            var source = new FileSystemPhotoSource();
            var seen = new List<PhotoSourceChange>();
            using var watcher = source.Watch(_root, true, change =>
            {
                lock (seen)
                    seen.Add(change);
            });
            Assert.IsNotNull(watcher);

            var path = TestImages.Write(Path.Combine(_root, "new.jpg"), 8, 8);

            for (var i = 0; i < 100; i++)
            {
                lock (seen)
                {
                    if (seen.Any(c => c.Path == path))
                        return;
                }
                await Task.Delay(50);
            }
            Assert.Fail("no change reported for the new file");
        }

        [TestMethod]
        public async Task When_LibraryUsesASource_ItScansThroughItAndWatchesEachFolder()
        {
            var photo = TestImages.Write(Path.Combine(_root, "elsewhere", "x.jpg"), 40, 30);
            var source = new FakePhotoSource { Files = { [_root] = [photo, Path.Combine(_root, "notes.txt")] } };
            PhotosSettings.SaveFolders([_root]);
            var library = new PhotoLibrary(source);

            library.Initialize(null);
            await library.RescanAsync();

            Assert.AreEqual("x.jpg", library.Items.Single().FileName, "unsupported extensions are filtered by the library");
            CollectionAssert.AreEqual(new[] { _root }, source.Watched);
            Assert.AreEqual(1, source.Active);

            library.RemoveFolder(_root);
            Assert.AreEqual(0, source.Active, "the watcher is disposed when the folder goes");
        }

        [TestMethod]
        public async Task When_SourceCannotWatch_LibraryDoesNotAskForWatchers()
        {
            var photo = TestImages.Write(Path.Combine(_root, "x.jpg"), 40, 30);
            var source = new FakePhotoSource { CanWatch = false, Files = { [_root] = [photo] } };
            PhotosSettings.SaveFolders([_root]);
            var library = new PhotoLibrary(source);

            library.Initialize(null);
            await library.RescanAsync();

            Assert.AreEqual(1, library.Items.Count);
            Assert.AreEqual(0, source.Watched.Count);
        }

        private sealed class FakePhotoSource : IPhotoSource
        {
            public bool CanWatch { get; init; } = true;
            public Dictionary<string, List<string>> Files { get; } = [];
            public List<string> Watched { get; } = [];
            public int Active;

            public IEnumerable<string> EnumerateFiles(string root, bool recurse, CancellationToken token) =>
                Files.TryGetValue(root, out var files) ? files : [];

            public IDisposable? Watch(string root, bool recurse, Action<PhotoSourceChange> onChanged)
            {
                Watched.Add(root);
                Active++;
                return new Release(() => Active--);
            }

            private sealed class Release(Action action) : IDisposable
            {
                public void Dispose() => action();
            }
        }
    }
}
