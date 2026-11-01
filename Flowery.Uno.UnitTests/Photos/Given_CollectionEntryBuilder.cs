using System.Globalization;
using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Services;
using Flowery.Uno.Photos.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_CollectionEntryBuilder
    {
        private string _state = null!;
        private string _root = null!;
        private PhotoLibrary _library = null!;

        [TestInitialize]
        public async Task Create()
        {
            _state = TestImages.TempDirectory("photos-coll-state-");
            _root = TestImages.TempDirectory("photos-coll-root-");
            PhotosSettings.Initialize(_state);
            PhotosSettings.IncludeSubfolders = true;
            PhotosSettings.SaveFolders([_root]);

            TestImages.Write(Path.Combine(_root, "beach.jpg"), 300, 200);
            TestImages.Write(Path.Combine(_root, "trip", "city.webp"), 200, 200);

            _library = new PhotoLibrary();
            _library.Initialize(null);
            await _library.RescanAsync();
        }

        [TestCleanup]
        public void Delete()
        {
            TestImages.DeleteQuietly(_state);
            TestImages.DeleteQuietly(_root);
        }

        [TestMethod]
        public void When_AlbumHasPhotos_EntryShowsCountCoverAndDateRange()
        {
            var beach = _library.FindByPath(Path.Combine(_root, "beach.jpg"))!;
            _library.CreateAlbum("Holiday", [beach]);

            var entries = CollectionEntryBuilder.Build(_library, CollectionEntryKind.Album, null, CultureInfo.InvariantCulture);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Holiday", entries[0].Key);
            Assert.AreEqual(1, entries[0].Count);
            Assert.AreSame(beach, entries[0].Cover);
            Assert.AreEqual(beach.SortDate.ToString("Y", CultureInfo.InvariantCulture), entries[0].Subtitle);
        }

        [TestMethod]
        public void When_AlbumIsEmpty_SubtitleIsTheEmptyHint()
        {
            _library.CreateAlbum("Empty");

            var entries = CollectionEntryBuilder.Build(_library, CollectionEntryKind.Album, null, CultureInfo.InvariantCulture);

            Assert.AreEqual(0, entries[0].Count);
            Assert.IsNull(entries[0].Cover);
            Assert.AreEqual(PhotosLocalization.GetString("Photos_EmptyAlbum"), entries[0].Subtitle);
        }

        [TestMethod]
        public void When_Folders_RootIsFlaggedAndSubfolderListed()
        {
            var entries = CollectionEntryBuilder.Build(_library, CollectionEntryKind.Folder, null, CultureInfo.InvariantCulture);

            Assert.AreEqual(2, entries.Count);
            Assert.IsTrue(entries[0].IsRootFolder);
            Assert.IsFalse(entries[1].IsRootFolder);
            Assert.AreEqual("trip", entries[1].Title);
            Assert.AreEqual(entries[1].Key, entries[1].Subtitle);
        }

        [TestMethod]
        public void When_Searching_TitleAndSubtitleAreMatchedIgnoringCase()
        {
            _library.CreateAlbum("Summer");
            _library.CreateAlbum("Winter");

            var byTitle = CollectionEntryBuilder.Build(_library, CollectionEntryKind.Album, "SUMM", CultureInfo.InvariantCulture);
            var byPath = CollectionEntryBuilder.Build(_library, CollectionEntryKind.Folder, "trip", CultureInfo.InvariantCulture);
            var none = CollectionEntryBuilder.Build(_library, CollectionEntryKind.Album, "zzz", CultureInfo.InvariantCulture);

            CollectionAssert.AreEqual(new[] { "Summer" }, byTitle.Select(e => e.Title).ToArray());
            Assert.AreEqual(1, byPath.Count);
            Assert.AreEqual(0, none.Count);
        }

        [TestMethod]
        public void When_DatesShareAMonth_RangeIsASingleLabel()
        {
            var a = new DateTime(2025, 3, 1);
            var b = new DateTime(2025, 3, 28);
            var c = new DateTime(2026, 1, 5);

            Assert.AreEqual(a.ToString("Y", CultureInfo.InvariantCulture), CollectionEntryBuilder.FormatDateRange(a, b, CultureInfo.InvariantCulture));
            StringAssert.Contains(CollectionEntryBuilder.FormatDateRange(a, c, CultureInfo.InvariantCulture), "\u2013");
        }
    }
}
