using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Color = Windows.UI.Color;
using Flowery.Enums;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_PhotosSettings
    {
        private string _dir = null!;

        [TestInitialize]
        public void Create()
        {
            _dir = TestImages.TempDirectory("photos-settings-");
            PhotosSettings.Initialize(_dir);
        }

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        public void When_OptionsAreSaved_TheyComeBackAfterReload()
        {
            PhotosSettings.TileHeight = 250.5;
            PhotosSettings.SortMode = PhotoSortMode.NameDescending;
            PhotosSettings.GroupMode = PhotoGroupMode.Year;
            PhotosSettings.ShowFileNames = true;
            PhotosSettings.ConfirmDelete = false;
            PhotosSettings.SlideshowSeconds = 7;
            PhotosSettings.JpegQuality = 80;
            PhotosSettings.SquareTiles = true;
            PhotosSettings.CustomColors[2] = Color.FromArgb(255, 1, 2, 3);
            PhotosSettings.SaveOptions();

            PhotosSettings.TileHeight = 200;
            PhotosSettings.SortMode = PhotoSortMode.DateTakenNewest;
            PhotosSettings.ConfirmDelete = true;
            PhotosSettings.CustomColors[2] = Color.FromArgb(255, 255, 255, 255);
            PhotosSettings.Initialize(_dir);

            Assert.AreEqual(250.5, PhotosSettings.TileHeight);
            Assert.AreEqual(PhotoSortMode.NameDescending, PhotosSettings.SortMode);
            Assert.AreEqual(PhotoGroupMode.Year, PhotosSettings.GroupMode);
            Assert.IsTrue(PhotosSettings.ShowFileNames);
            Assert.IsFalse(PhotosSettings.ConfirmDelete);
            Assert.AreEqual(7, PhotosSettings.SlideshowSeconds);
            Assert.AreEqual(80, PhotosSettings.JpegQuality);
            Assert.IsTrue(PhotosSettings.SquareTiles);
            Assert.AreEqual(Color.FromArgb(255, 1, 2, 3), PhotosSettings.CustomColors[2]);
        }

        [TestMethod]
        public void When_SavedValuesAreOutOfRange_TheyAreClamped()
        {
            File.WriteAllLines(Path.Combine(_dir, "photos.options.state"),
            [
                "TileHeight=5000",
                "SlideshowSeconds=0",
                "JpegQuality=1",
                "SortMode=NotASortMode",
                "garbage line"
            ]);

            PhotosSettings.SortMode = PhotoSortMode.SizeLargest;
            PhotosSettings.Initialize(_dir);

            Assert.AreEqual(420, PhotosSettings.TileHeight);
            Assert.AreEqual(1, PhotosSettings.SlideshowSeconds);
            Assert.AreEqual(40, PhotosSettings.JpegQuality);
            Assert.AreEqual(PhotoSortMode.SizeLargest, PhotosSettings.SortMode, "an unknown value keeps the current one");
        }

        [TestMethod]
        public void When_FoldersAreEmptied_TheLibraryIsNotReseeded()
        {
            Assert.IsFalse(PhotosSettings.HasSavedFolders());

            PhotosSettings.SaveFolders([]);

            Assert.IsTrue(PhotosSettings.HasSavedFolders());
            Assert.AreEqual(0, PhotosSettings.LoadFolders().Count);
        }

        [TestMethod]
        public void When_FoldersAreSaved_TheyAreLoadedInOrder()
        {
            PhotosSettings.SaveFolders(["/a/one", "/b/two"]);

            CollectionAssert.AreEqual(new[] { "/a/one", "/b/two" }, PhotosSettings.LoadFolders());
        }

        [TestMethod]
        public void When_AlbumsAreSaved_NamesAndPathsRoundTrip()
        {
            PhotosSettings.SaveAlbums([("Trip\t2024", new[] { "/p/a.jpg", "/p/b.jpg" }), ("Empty", Array.Empty<string>())]);

            var albums = PhotosSettings.LoadAlbums();

            Assert.AreEqual(2, albums.Count);
            Assert.AreEqual("Trip 2024", albums[0].Name, "a tab in the name would break the line format");
            CollectionAssert.AreEqual(new[] { "/p/a.jpg", "/p/b.jpg" }, albums[0].Paths);
            Assert.AreEqual(0, albums[1].Paths.Count);
        }

        [TestMethod]
        public void When_FavoritesAreSaved_PathCaseFollowsTheOs()
        {
            PhotosSettings.SaveFavorites(["/p/A.jpg"]);

            var favorites = PhotosSettings.LoadFavorites();

            Assert.AreEqual(!OperatingSystem.IsLinux(), favorites.Contains("/p/a.jpg"));
            Assert.IsTrue(favorites.Contains("/p/A.jpg"));
        }

        [TestMethod]
        public void When_WindowBoundsAreImplausible_TheyAreIgnored()
        {
            PhotosSettings.SaveWindowBounds(new WindowBounds(10, 20, 1000, 700));
            Assert.AreEqual(new WindowBounds(10, 20, 1000, 700), PhotosSettings.LoadWindowBounds());

            PhotosSettings.SaveWindowBounds(new WindowBounds(0, 0, 50, 700));
            Assert.IsNull(PhotosSettings.LoadWindowBounds());
        }

        [TestMethod]
        [DataRow("Medium", DaisySize.Medium)]
        [DataRow("extralarge", DaisySize.ExtraLarge)]
        public void When_SavedSizeIsATier_ItIsLoaded(string saved, DaisySize expected)
        {
            File.WriteAllText(Path.Combine(_dir, "size.state"), saved);

            Assert.AreEqual(expected, PhotosSettings.LoadGlobalSize());
        }

        [TestMethod]
        [DataRow("99")]
        [DataRow("-1")]
        [DataRow("NotASize")]
        public void When_SavedSizeIsNotATier_ItIsIgnored(string saved)
        {
            File.WriteAllText(Path.Combine(_dir, "size.state"), saved);

            Assert.IsNull(PhotosSettings.LoadGlobalSize());
        }

        [TestMethod]
        public void When_ThemeAndLanguageAreSaved_BlankValuesAreNotWritten()
        {
            PhotosSettings.SaveTheme("Dark");
            PhotosSettings.SaveTheme("  ");
            PhotosSettings.SaveLanguage("de");

            Assert.AreEqual("Dark", PhotosSettings.LoadTheme());
            Assert.AreEqual("de", PhotosSettings.LoadLanguage());
        }
    }
}
