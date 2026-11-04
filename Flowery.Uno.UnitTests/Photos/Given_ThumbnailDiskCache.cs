using Flowery.Uno.Photos.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_ThumbnailDiskCache
    {
        private string _dir = null!;
        private string _cacheDir = null!;
        private ThumbnailDiskCache _cache = null!;

        [TestInitialize]
        public void Create()
        {
            _dir = TestImages.TempDirectory("photos-dcache-");
            _cacheDir = Path.Combine(_dir, "thumbs");
            _cache = new ThumbnailDiskCache(_cacheDir);
        }

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        private string Photo(string name = "a.png", int w = 80, int h = 40) =>
            TestImages.Write(Path.Combine(_dir, "src", name), w, h);

        [TestMethod]
        public void When_ThumbnailWasWritten_ItIsReadBackWithTheSameSize()
        {
            var photo = Photo();
            using var thumb = TestImages.Gradient(40, 20);

            _cache.Write(photo, 120, thumb);
            using var read = _cache.TryRead(photo, 120);

            Assert.IsNotNull(read);
            Assert.AreEqual((40, 20), (read.Width, read.Height));
            Assert.AreEqual(SKColorType.Bgra8888, read.ColorType);
            Assert.AreEqual(SKAlphaType.Premul, read.AlphaType);
        }

        [TestMethod]
        public void When_NothingWasWritten_ReadReturnsNull()
        {
            Assert.IsNull(_cache.TryRead(Photo(), 120));
            Assert.IsNull(_cache.TryRead(Path.Combine(_dir, "missing.png"), 120));
        }

        [TestMethod]
        public void When_BucketDiffers_ThereIsNoHit()
        {
            var photo = Photo();
            using var thumb = TestImages.Gradient(40, 20);
            _cache.Write(photo, 120, thumb);

            Assert.IsNull(_cache.TryRead(photo, 200));
        }

        [TestMethod]
        public void When_PhotoChanges_TheOldThumbnailIsNotUsed()
        {
            var photo = Photo();
            using var thumb = TestImages.Gradient(40, 20);
            _cache.Write(photo, 120, thumb);

            File.SetLastWriteTimeUtc(photo, File.GetLastWriteTimeUtc(photo).AddMinutes(5));
            Assert.IsNull(_cache.TryRead(photo, 120), "modification time is part of the key");

            Photo(w: 90, h: 50);
            File.SetLastWriteTimeUtc(photo, File.GetLastWriteTimeUtc(photo).AddMinutes(5));
            Assert.IsNull(_cache.TryRead(photo, 120));
        }

        [TestMethod]
        public void When_CacheFileIsCorrupt_ItIsDroppedAndReadReturnsNull()
        {
            var photo = Photo();
            using var thumb = TestImages.Gradient(40, 20);
            _cache.Write(photo, 120, thumb);
            var file = Directory.GetFiles(_cacheDir, "*.webp").Single();
            File.WriteAllBytes(file, [1, 2, 3, 4]);

            Assert.IsNull(_cache.TryRead(photo, 120));
            Assert.IsFalse(File.Exists(file));
        }

        [TestMethod]
        public void When_DecodeIsNeededOnce_SecondCallComesFromDisk()
        {
            var photo = Photo();
            var decodes = 0;
            SKBitmap? Decode(string path, int bucket)
            {
                decodes++;
                return TestImages.Gradient(30, 15);
            }

            using var first = _cache.GetOrCreate(photo, 120, Decode);
            using var second = _cache.GetOrCreate(photo, 120, Decode);

            Assert.AreEqual(1, decodes);
            Assert.AreEqual((30, 15), (second!.Width, second.Height));
        }

        [TestMethod]
        public void When_DecodeFails_NothingIsCached()
        {
            var photo = Photo();

            Assert.IsNull(_cache.GetOrCreate(photo, 120, (_, _) => null));

            Assert.IsFalse(Directory.Exists(_cacheDir) && Directory.GetFiles(_cacheDir).Length > 0);
        }

        [TestMethod]
        public void When_CacheIsTooBig_TrimDeletesTheOldestFirst()
        {
            using var thumb = TestImages.Gradient(64, 64);
            var photos = Enumerable.Range(0, 4).Select(i => Photo($"p{i}.png")).ToList();
            for (var i = 0; i < photos.Count; i++)
            {
                _cache.Write(photos[i], 120, thumb);
                var file = Directory.GetFiles(_cacheDir, "*.webp").OrderBy(f => File.GetLastWriteTimeUtc(f)).Last();
                File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddHours(-10 + i));
            }
            var size = new FileInfo(Directory.GetFiles(_cacheDir, "*.webp")[0]).Length;

            _cache.Trim(size * 2 + 1);

            Assert.IsNull(_cache.TryRead(photos[0], 120));
            Assert.IsNull(_cache.TryRead(photos[1], 120));
            Assert.IsNotNull(_cache.TryRead(photos[2], 120));
            Assert.IsNotNull(_cache.TryRead(photos[3], 120));
        }

        [TestMethod]
        public void When_Cleared_TheFolderIsGone()
        {
            var photo = Photo();
            using var thumb = TestImages.Gradient(40, 20);
            _cache.Write(photo, 120, thumb);

            _cache.Clear();

            Assert.IsFalse(Directory.Exists(_cacheDir));
        }
    }
}
