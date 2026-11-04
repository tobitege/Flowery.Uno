using Flowery.Uno.Photos.Models;
using Flowery.Uno.Photos.Services;
using Flowery.Uno.Photos.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_GalleryLayout
    {
        private const double Width = 1000;
        private const double Target = 200;
        private const double Gap = 6;

        private static PhotoItem Photo(int index, int w, int h, DateTime date) =>
            new($"/pics/{index:D3}.jpg") { PixelWidth = w, PixelHeight = h, DateTaken = date, DateModified = date };

        private static List<PhotoItem> Landscapes(int count, DateTime? start = null) =>
            [.. Enumerable.Range(0, count).Select(i => Photo(i, 1500, 1000, (start ?? new DateTime(2024, 5, 1)).AddMinutes(-i)))];

        [TestMethod]
        public void When_NoItemsOrNarrow_NoRows()
        {
            Assert.AreEqual(0, GalleryLayout.Build([], Width, Target, Gap, PhotoGroupMode.None, false).Count);
            Assert.AreEqual(0, GalleryLayout.Build(Landscapes(3), 20, Target, Gap, PhotoGroupMode.None, false).Count);
        }

        [TestMethod]
        public void When_RowIsFull_TilesFillTheWidthExactly()
        {
            var rows = GalleryLayout.Build(Landscapes(20), Width, Target, Gap, PhotoGroupMode.None, false);

            var photoRows = rows.OfType<GalleryPhotoRow>().ToList();
            Assert.IsTrue(photoRows.Count > 1);
            foreach (var row in photoRows.Take(photoRows.Count - 1))
            {
                var total = row.Tiles.Sum(t => t.Width) + Gap * (row.Tiles.Count - 1);
                Assert.AreEqual(Width, total, 0.001);
            }
        }

        [TestMethod]
        public void When_RowsAreBuilt_TopsFollowHeights()
        {
            var rows = GalleryLayout.Build(Landscapes(15), Width, Target, Gap, PhotoGroupMode.None, false);

            Assert.AreEqual(0, rows[0].Top);
            for (var i = 1; i < rows.Count; i++)
                Assert.AreEqual(rows[i - 1].Top + rows[i - 1].Height, rows[i].Top, 0.001);
        }

        [TestMethod]
        public void When_LastRowIsShort_ItDoesNotStretch()
        {
            var rows = GalleryLayout.Build(Landscapes(7), Width, Target, Gap, PhotoGroupMode.None, false);

            var last = (GalleryPhotoRow)rows[^1];
            var total = last.Tiles.Sum(t => t.Width) + Gap * (last.Tiles.Count - 1);
            Assert.IsTrue(total <= Width);
        }

        [TestMethod]
        public void When_SquareTiles_WidthEqualsHeight()
        {
            List<PhotoItem> items = [Photo(0, 3000, 1000, DateTime.Today), Photo(1, 1000, 3000, DateTime.Today)];

            var rows = GalleryLayout.Build(items, Width, Target, Gap, PhotoGroupMode.None, squareTiles: true);

            var row = (GalleryPhotoRow)rows[0];
            Assert.AreEqual(2, row.Tiles.Count);
            foreach (var (_, width) in row.Tiles)
                Assert.AreEqual(row.TileHeight, width);
        }

        [TestMethod]
        public void When_GroupedByMonth_EachMonthGetsAHeader()
        {
            var items = Landscapes(3, new DateTime(2024, 5, 20));
            items.AddRange(Landscapes(2, new DateTime(2024, 3, 10)).Select((p, i) => Photo(10 + i, 1500, 1000, new DateTime(2024, 3, 10 - i))));

            var rows = GalleryLayout.Build(items, Width, Target, Gap, PhotoGroupMode.Month, false);

            var headers = rows.OfType<GalleryHeaderRow>().ToList();
            Assert.AreEqual(2, headers.Count);
            Assert.AreEqual(3, headers[0].Items.Count);
            Assert.AreEqual(2, headers[1].Items.Count);
            Assert.IsInstanceOfType<GalleryHeaderRow>(rows[0]);
            Assert.AreEqual(GalleryLayout.HeaderHeight, headers[0].Height);
        }

        [TestMethod]
        public void When_GroupedByYear_TitleIsTheYear()
        {
            var title = GalleryLayout.FormatGroupTitle(new DateTime(2023, 7, 4), PhotoGroupMode.Year, System.Globalization.CultureInfo.InvariantCulture);

            Assert.AreEqual("2023", title);
        }

        [TestMethod]
        public void When_LongDateIsFormatted_TheWeekdayIsDropped()
        {
            var title = GalleryLayout.FormatLongDate(new DateTime(2026, 10, 3), System.Globalization.CultureInfo.GetCultureInfo("en-US"));

            Assert.AreEqual("October 3, 2026", title);
        }

        [TestMethod]
        public void When_OffsetIsInsideARow_FindRowAtReturnsIt()
        {
            var rows = GalleryLayout.Build(Landscapes(30), Width, Target, Gap, PhotoGroupMode.None, false);

            Assert.AreEqual(0, GalleryLayout.FindRowAt(rows, 0));
            Assert.AreEqual(0, GalleryLayout.FindRowAt(rows, -50));
            Assert.AreEqual(-1, GalleryLayout.FindRowAt([], 10));
            for (var i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(i, GalleryLayout.FindRowAt(rows, rows[i].Top));
                Assert.AreEqual(i, GalleryLayout.FindRowAt(rows, rows[i].Top + rows[i].Height - 0.5));
            }
            Assert.AreEqual(rows.Count - 1, GalleryLayout.FindRowAt(rows, 1_000_000));
        }
    }
}
