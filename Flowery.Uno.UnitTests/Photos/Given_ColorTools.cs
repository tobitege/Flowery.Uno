using Flowery.Uno.Photos.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Windows.UI;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_ColorTools
    {
        private string _dir = null!;

        [TestInitialize]
        public void Create() => _dir = TestImages.TempDirectory("photos-colors-");

        [TestCleanup]
        public void Delete() => TestImages.DeleteQuietly(_dir);

        [TestMethod]
        [DataRow((byte)255, (byte)0, (byte)0)]
        [DataRow((byte)12, (byte)200, (byte)99)]
        [DataRow((byte)128, (byte)128, (byte)128)]
        public void When_ColorGoesThroughHsl_ItComesBack(byte r, byte g, byte b)
        {
            var (h, s, l) = ColorTools.ToHsl(Color.FromArgb(255, r, g, b));

            var back = ColorTools.FromHsl(h, s, l);

            Assert.AreEqual((r, g, b), (back.R, back.G, back.B));
        }

        [TestMethod]
        public void When_HueRotatesHalfACircle_RedBecomesCyan()
        {
            var result = ColorTools.RotateHue(Color.FromArgb(255, 255, 0, 0), 180);

            Assert.AreEqual(((byte)0, (byte)255, (byte)255), (result.R, result.G, result.B));
        }

        [TestMethod]
        public void When_Inverted_ChannelsFlipAndAlphaStays()
        {
            var result = ColorTools.Invert(Color.FromArgb(100, 10, 20, 30));

            Assert.AreEqual(((byte)100, (byte)245, (byte)235, (byte)225), (result.A, result.R, result.G, result.B));
        }

        [TestMethod]
        [DataRow(ColorTools.Harmony.Complementary)]
        [DataRow(ColorTools.Harmony.Triad)]
        [DataRow(ColorTools.Harmony.Analogous)]
        [DataRow(ColorTools.Harmony.Monochrome)]
        public void When_HarmonyIsCreated_ItHasNineColors(ColorTools.Harmony harmony)
        {
            var colors = ColorTools.CreateHarmony(Color.FromArgb(255, 200, 60, 40), harmony);

            Assert.HasCount(9, colors);
        }

        [TestMethod]
        public void When_ColorsAreExtracted_TheLargestAreaComesFirst()
        {
            using var surface = SKSurface.Create(new SKImageInfo(200, 200));
            surface.Canvas.Clear(new SKColor(200, 20, 20));
            using var paint = new SKPaint { Color = new SKColor(20, 20, 200) };
            surface.Canvas.DrawRect(0, 0, 200, 40, paint);
            using var image = surface.Snapshot();

            var colors = ColorTools.ExtractDominantColors(image, 3);

            Assert.HasCount(3, colors);
            Assert.IsTrue(colors[0].R > 150 && colors[0].B < 60, colors[0].ToString());
        }

        [TestMethod]
        public void When_PaletteIsWrittenAndRead_TheColorsComeBack()
        {
            var path = Path.Combine(_dir, "a.pal");
            Color[] colors = [Color.FromArgb(255, 1, 2, 3), Color.FromArgb(255, 250, 128, 0)];

            ColorTools.WriteJascPalette(path, colors);

            CollectionAssert.AreEqual(colors, ColorTools.ReadPalette(path));
            Assert.StartsWith("JASC-PAL\r\n0100\r\n2\r\n", File.ReadAllText(path));
        }

        [TestMethod]
        public void When_GimpPaletteIsRead_HeaderAndCommentsAreSkipped()
        {
            var path = Path.Combine(_dir, "a.gpl");
            File.WriteAllLines(path, ["GIMP Palette", "Name: test", "Columns: 4", "# comment", "10 20 30\tDark", "  255 255 255 White", "not a color"]);

            var colors = ColorTools.ReadPalette(path);

            CollectionAssert.AreEqual(new[] { Color.FromArgb(255, 10, 20, 30), Color.FromArgb(255, 255, 255, 255) }, colors);
        }
    }
}
