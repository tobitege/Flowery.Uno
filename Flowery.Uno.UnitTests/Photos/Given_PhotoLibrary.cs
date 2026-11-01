using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_PhotoLibrary
    {
        private string _state = null!;
        private string _root = null!;

        [TestInitialize]
        public void Create()
        {
            _state = TestImages.TempDirectory("photos-lib-state-");
            _root = TestImages.TempDirectory("photos-lib-root-");
            PhotosSettings.Initialize(_state);
            PhotosSettings.IncludeSubfolders = true;
            PhotosSettings.SaveFolders([_root]);
        }

        [TestCleanup]
        public void Delete()
        {
            TestImages.DeleteQuietly(_state);
            TestImages.DeleteQuietly(_root);
        }

        private PhotoLibrary NewLibrary()
        {
            var library = new PhotoLibrary();
            library.Initialize(null);
            return library;
        }

        private async Task<PhotoLibrary> ScannedLibraryAsync()
        {
            TestImages.Write(Path.Combine(_root, "beach.jpg"), 300, 200);
            TestImages.Write(Path.Combine(_root, "tall.png"), 100, 300);
            TestImages.Write(Path.Combine(_root, "trip", "city.webp"), 200, 200);
            File.WriteAllText(Path.Combine(_root, "notes.txt"), "not a photo");

            var library = NewLibrary();
            await library.RescanAsync();
            return library;
        }

        [TestMethod]
        public async Task When_FolderHasPhotos_ScanFindsSupportedFilesOnly()
        {
            var library = await ScannedLibraryAsync();

            CollectionAssert.AreEquivalent(new[] { "beach.jpg", "city.webp", "tall.png" }, library.Items.Select(i => i.FileName).ToArray());
            var beach = library.FindByPath(Path.Combine(_root, "beach.jpg"));
            Assert.AreEqual((300, 200), (beach!.PixelWidth, beach.PixelHeight));
        }

        [TestMethod]
        public async Task When_SubfoldersAreExcluded_ScanSkipsThem()
        {
            PhotosSettings.IncludeSubfolders = false;

            var library = await ScannedLibraryAsync();

            CollectionAssert.AreEquivalent(new[] { "beach.jpg", "tall.png" }, library.Items.Select(i => i.FileName).ToArray());
        }

        [TestMethod]
        public async Task When_ScanFinishes_ChangedFiresOnceWithTheItems()
        {
            TestImages.Write(Path.Combine(_root, "a.jpg"), 10, 10);
            var library = NewLibrary();
            var changed = 0;
            var itemsAtChange = -1;
            library.Changed += (_, _) =>
            {
                changed++;
                itemsAtChange = library.Items.Count;
            };

            await library.RescanAsync();

            Assert.AreEqual(1, changed);
            Assert.AreEqual(1, itemsAtChange);
            Assert.IsFalse(library.IsScanning);
        }

        [TestMethod]
        public async Task When_ScanIsRepeated_MetadataComesFromTheCacheFile()
        {
            await ScannedLibraryAsync();
            var cache = Path.Combine(_state, "metadata-cache.json");
            // The cache is written in the background after the scan.
            var content = string.Empty;
            for (var i = 0; i < 100 && !content.Contains("beach.jpg"); i++)
            {
                await Task.Delay(50);
                content = ReadOrEmpty(cache);
            }
            StringAssert.Contains(content, "beach.jpg");

            var second = NewLibrary();
            await second.RescanAsync();

            Assert.AreEqual(3, second.Items.Count);
        }

        [TestMethod]
        public async Task When_FileIsRewritten_RefreshUpdatesSizeAndBumpsTheRevision()
        {
            var library = await ScannedLibraryAsync();
            var path = Path.Combine(_root, "beach.jpg");
            var item = library.FindByPath(path)!;
            var revision = item.Revision;

            TestImages.Write(path, 640, 480);
            var refreshed = library.Refresh(path);

            Assert.AreSame(item, refreshed);
            Assert.AreEqual((640, 480), (item.PixelWidth, item.PixelHeight));
            Assert.AreEqual(revision + 1, item.Revision);
        }

        [TestMethod]
        public async Task When_FavoriteIsSet_ItIsSavedAndFiltered()
        {
            var library = await ScannedLibraryAsync();
            var beach = library.FindByPath(Path.Combine(_root, "beach.jpg"))!;

            library.ToggleFavorite(beach);

            var favorites = library.Query(new PhotoQuery { Source = PhotoSourceKind.Favorites });
            CollectionAssert.AreEqual(new[] { beach }, favorites);
            Assert.IsTrue(PhotosSettings.LoadFavorites().Contains(beach.Path));

            library.ToggleFavorite(beach);
            Assert.AreEqual(0, library.Query(new PhotoQuery { Source = PhotoSourceKind.Favorites }).Count);
        }

        [TestMethod]
        public async Task When_QueryHasFilters_OnlyMatchingPhotosAreReturned()
        {
            var library = await ScannedLibraryAsync();

            Assert.AreEqual("tall.png", Names(library, new PhotoQuery { TypeFilter = PhotoTypeFilter.Png }).Single());
            Assert.AreEqual("city.webp", Names(library, new PhotoQuery { TypeFilter = PhotoTypeFilter.Other }).Single());
            Assert.AreEqual("tall.png", Names(library, new PhotoQuery { OrientationFilter = PhotoOrientationFilter.Portrait }).Single());
            Assert.AreEqual("city.webp", Names(library, new PhotoQuery { OrientationFilter = PhotoOrientationFilter.Square }).Single());
            Assert.AreEqual("beach.jpg", Names(library, new PhotoQuery { OrientationFilter = PhotoOrientationFilter.Landscape }).Single());
        }

        [TestMethod]
        public async Task When_QueryHasSearchText_AllTermsMustMatch()
        {
            var library = await ScannedLibraryAsync();

            Assert.AreEqual("beach.jpg", Names(library, new PhotoQuery { SearchText = "beach" }).Single());
            Assert.AreEqual("city.webp", Names(library, new PhotoQuery { SearchText = "trip city" }).Single(), "folder name and file name");
            Assert.AreEqual("city.webp", Names(library, new PhotoQuery { SearchText = ".webp" }).Single(), "extension");
            Assert.AreEqual(0, Names(library, new PhotoQuery { SearchText = "beach city" }).Count());
            Assert.AreEqual(3, library.Query(new PhotoQuery { SearchText = DateTime.Now.Year.ToString() }).Count, "year of the file date");
        }

        [TestMethod]
        public async Task When_QueryHasASortMode_ItOrdersTheResult()
        {
            var library = await ScannedLibraryAsync();
            File.SetLastWriteTime(Path.Combine(_root, "beach.jpg"), new DateTime(2020, 1, 1));
            File.SetLastWriteTime(Path.Combine(_root, "tall.png"), new DateTime(2022, 1, 1));
            File.SetLastWriteTime(Path.Combine(_root, "trip", "city.webp"), new DateTime(2021, 1, 1));
            await library.RescanAsync();

            CollectionAssert.AreEqual(new[] { "beach.jpg", "city.webp", "tall.png" }, Names(library, new PhotoQuery { Sort = PhotoSortMode.NameAscending }).ToArray());
            CollectionAssert.AreEqual(new[] { "tall.png", "city.webp", "beach.jpg" }, Names(library, new PhotoQuery { Sort = PhotoSortMode.NameDescending }).ToArray());
            CollectionAssert.AreEqual(new[] { "tall.png", "city.webp", "beach.jpg" }, Names(library, new PhotoQuery { Sort = PhotoSortMode.DateModifiedNewest }).ToArray());
            CollectionAssert.AreEqual(new[] { "beach.jpg", "city.webp", "tall.png" }, Names(library, new PhotoQuery { Sort = PhotoSortMode.DateTakenOldest }).ToArray());
        }

        [TestMethod]
        public async Task When_AlbumNamesCollide_TheyAreMadeUnique()
        {
            var library = await ScannedLibraryAsync();
            var beach = library.FindByPath(Path.Combine(_root, "beach.jpg"))!;

            var first = library.CreateAlbum("Trip", [beach]);
            var second = library.CreateAlbum(" Trip ");

            Assert.AreEqual("Trip", first.Name);
            Assert.AreEqual("Trip (2)", second.Name);
            Assert.AreEqual(1, library.AddToAlbum(second, [beach, beach]));
            Assert.AreEqual(0, library.AddToAlbum(second, [beach]));
        }

        [TestMethod]
        public async Task When_AlbumIsRenamedToAnExistingName_RenameFails()
        {
            var library = await ScannedLibraryAsync();
            var a = library.CreateAlbum("A");
            var b = library.CreateAlbum("B");

            Assert.IsFalse(library.RenameAlbum(b, "a"), "names compare case-insensitively");
            Assert.IsFalse(library.RenameAlbum(b, "  "));
            Assert.IsTrue(library.RenameAlbum(b, "C"));
            Assert.AreEqual("C", b.Name);
            Assert.AreSame(a, library.FindAlbum("A"));
        }

        [TestMethod]
        public async Task When_AlbumsAreSaved_ANewLibraryLoadsThemAndQueriesTheirPhotos()
        {
            var library = await ScannedLibraryAsync();
            var tall = library.FindByPath(Path.Combine(_root, "tall.png"))!;
            var beach = library.FindByPath(Path.Combine(_root, "beach.jpg"))!;
            library.CreateAlbum("Mix", [tall, beach]);

            var reloaded = NewLibrary();
            await reloaded.RescanAsync();

            var album = reloaded.FindAlbum("Mix");
            Assert.IsNotNull(album);
            var items = reloaded.Query(new PhotoQuery { Source = PhotoSourceKind.Album, SourceKey = "Mix", Sort = PhotoSortMode.NameAscending });
            CollectionAssert.AreEqual(new[] { "beach.jpg", "tall.png" }, items.Select(i => i.FileName).ToArray());

            reloaded.RemoveFromAlbum(album, items.Where(i => i.FileName == "tall.png"));
            Assert.AreEqual(1, album.Paths.Count);
            reloaded.DeleteAlbum(album);
            Assert.IsNull(reloaded.FindAlbum("Mix"));
        }

        [TestMethod]
        public async Task When_FolderIsQueried_OnlyItsPhotosAreReturned()
        {
            var library = await ScannedLibraryAsync();

            var items = library.Query(new PhotoQuery { Source = PhotoSourceKind.Folder, SourceKey = Path.Combine(_root, "trip") });

            Assert.AreEqual("city.webp", items.Single().FileName);
        }

        [TestMethod]
        public async Task When_FoldersAreSummarized_RootsComeFirstWithTheNewestAsCover()
        {
            var library = await ScannedLibraryAsync();
            File.SetLastWriteTime(Path.Combine(_root, "beach.jpg"), new DateTime(2020, 1, 1));
            File.SetLastWriteTime(Path.Combine(_root, "tall.png"), new DateTime(2023, 1, 1));
            await library.RescanAsync();

            var summaries = library.GetFolderSummaries();

            Assert.AreEqual(2, summaries.Count);
            Assert.IsTrue(summaries[0].IsRoot);
            Assert.AreEqual(2, summaries[0].Count);
            Assert.AreEqual("tall.png", summaries[0].Cover!.FileName);
            Assert.IsFalse(summaries[1].IsRoot);
            Assert.AreEqual("trip", summaries[1].Name);
        }

        [TestMethod]
        public async Task When_FolderIsAddedAndRemoved_TheListIsSavedAndPathsAreChecked()
        {
            var library = NewLibrary();
            var other = TestImages.TempDirectory("photos-lib-other-");
            try
            {
                Assert.IsFalse(library.AddFolder(_root + Path.DirectorySeparatorChar), "same folder with a trailing separator");
                Assert.IsFalse(library.AddFolder(Path.Combine(other, "missing")));
                Assert.IsTrue(library.AddFolder(other));
                CollectionAssert.AreEqual(new[] { _root, PathRules.TrimEnd(other) }, PhotosSettings.LoadFolders());

                Assert.IsTrue(library.IsInLibrary(Path.Combine(other, "x.jpg")));
                Assert.IsFalse(library.IsInLibrary(other + "2" + Path.DirectorySeparatorChar + "x.jpg"));

                library.RemoveFolder(other);
                CollectionAssert.AreEqual(new[] { _root }, PhotosSettings.LoadFolders());
                await library.RescanAsync();
            }
            finally
            {
                TestImages.DeleteQuietly(other);
            }
        }

        [TestMethod]
        public async Task When_FilesAreImported_TheyAreCopiedIntoADatedFolderOfTheLibrary()
        {
            var library = await ScannedLibraryAsync();
            var outside = TestImages.TempDirectory("photos-lib-outside-");
            try
            {
                var external = TestImages.Write(Path.Combine(outside, "new.jpg"), 80, 60);
                var inside = Path.Combine(_root, "beach.jpg");

                var count = await library.ImportFilesAsync([external, inside, Path.Combine(outside, "readme.txt")]);

                Assert.AreEqual(1, count, "files already in the library and unsupported files are skipped");
                var imported = library.Items.Single(i => i.FileName == "new.jpg");
                StringAssert.StartsWith(imported.FolderPath, Path.Combine(_root, "Flowery Photos", "Imported "));
                Assert.AreEqual(4, library.Items.Count);
            }
            finally
            {
                TestImages.DeleteQuietly(outside);
            }
        }

        [TestMethod]
        public async Task When_ImportedNameExists_ACounterIsAppended()
        {
            var library = await ScannedLibraryAsync();
            var outside = TestImages.TempDirectory("photos-lib-outside-");
            try
            {
                var external = TestImages.Write(Path.Combine(outside, "dup.jpg"), 20, 20);

                await library.ImportFilesAsync([external]);
                await library.ImportFilesAsync([external]);

                CollectionAssert.AreEquivalent(new[] { "dup.jpg", "dup (1).jpg" }, library.Items.Where(i => i.FileName.StartsWith("dup")).Select(i => i.FileName).ToArray());
            }
            finally
            {
                TestImages.DeleteQuietly(outside);
            }
        }

        [TestMethod]
        public async Task When_FileIsOutsideTheLibrary_GetOrCreateItemReadsItsMetadata()
        {
            var library = await ScannedLibraryAsync();
            var outside = TestImages.TempDirectory("photos-lib-outside-");
            try
            {
                var path = TestImages.Write(Path.Combine(outside, "far.png"), 33, 44);

                var item = library.GetOrCreateItem(path);

                Assert.IsNotNull(item);
                Assert.AreEqual((33, 44), (item.PixelWidth, item.PixelHeight));
                Assert.IsNull(library.FindByPath(path), "it is not added to the library");
            }
            finally
            {
                TestImages.DeleteQuietly(outside);
            }
        }

        [TestMethod]
        public async Task When_SubfolderOptionChanges_ItIsPersistedAndRescanned()
        {
            var library = await ScannedLibraryAsync();
            Assert.AreEqual(3, library.Items.Count);

            library.ApplyIncludeSubfolders(false);
            await WaitForAsync(() => library.Items.Count == 2);

            Assert.AreEqual(2, library.Items.Count);
            PhotosSettings.Initialize(_state);
            Assert.IsFalse(PhotosSettings.IncludeSubfolders);
        }

        private static string ReadOrEmpty(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            }
            catch (IOException)
            {
                return string.Empty;
            }
        }

        private static IEnumerable<string> Names(PhotoLibrary library, PhotoQuery query) =>
            library.Query(query).Select(i => i.FileName);

        private static async Task WaitForAsync(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
                await Task.Delay(50);
        }
    }
}
