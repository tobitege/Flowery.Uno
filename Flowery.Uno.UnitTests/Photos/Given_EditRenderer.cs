using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Flowery.Uno.Photos.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;

namespace Flowery.Uno.UnitTests.Photos
{
    [TestClass]
    public class Given_EditRenderer
    {
        private static readonly SKColor Red = new(220, 30, 30);
        private static readonly SKColor Green = new(30, 200, 30);
        private static readonly SKColor Blue = new(30, 30, 220);
        private static readonly SKColor Yellow = new(230, 220, 30);

        /// <summary>Four colored quadrants: red top left, green top right, blue bottom left, yellow bottom right.</summary>
        private static SKImage Quadrants(int width = 80, int height = 40)
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
            using var paint = new SKPaint();
            void Fill(SKColor c, float x, float y)
            {
                paint.Color = c;
                surface.Canvas.DrawRect(x, y, width / 2f, height / 2f, paint);
            }
            Fill(Red, 0, 0);
            Fill(Green, width / 2f, 0);
            Fill(Blue, 0, height / 2f);
            Fill(Yellow, width / 2f, height / 2f);
            return surface.Snapshot();
        }

        private static SKImage Flat(byte grey, int width = 64, int height = 64)
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
            surface.Canvas.Clear(new SKColor(grey, grey, grey));
            return surface.Snapshot();
        }

        private static SKColor Pixel(SKImage image, int x, int y)
        {
            using var bitmap = SKBitmap.FromImage(image);
            return bitmap.GetPixel(x, y);
        }

        private static void AssertNear(SKColor expected, SKColor actual, int tolerance = 6)
        {
            var near = Math.Abs(expected.Red - actual.Red) <= tolerance
                && Math.Abs(expected.Green - actual.Green) <= tolerance
                && Math.Abs(expected.Blue - actual.Blue) <= tolerance;
            Assert.IsTrue(near, $"expected {expected}, got {actual}");
        }

        private static double Luma(SKColor c) => 0.2126 * c.Red + 0.7152 * c.Green + 0.0722 * c.Blue;

        [TestMethod]
        public void When_NothingIsEdited_PixelsStayTheSame()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original);

            Assert.AreEqual((80, 40), (result.Width, result.Height));
            AssertNear(Red, Pixel(result, 10, 10), 0);
            AssertNear(Yellow, Pixel(result, 70, 30), 0);
        }

        [TestMethod]
        public void When_RotatedClockwise_TopLeftMovesToTopRight()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { Rotation = 1 });

            Assert.AreEqual((40, 80), (result.Width, result.Height));
            AssertNear(Red, Pixel(result, 30, 10));
            AssertNear(Blue, Pixel(result, 10, 10));
            AssertNear(Yellow, Pixel(result, 10, 70));
        }

        [TestMethod]
        public void When_FlippedHorizontally_LeftAndRightSwap()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { FlipH = true });

            AssertNear(Green, Pixel(result, 10, 10));
            AssertNear(Red, Pixel(result, 70, 10));
        }

        [TestMethod]
        public void When_FlippedVertically_TopAndBottomSwap()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { FlipV = true });

            AssertNear(Blue, Pixel(result, 10, 10));
            AssertNear(Yellow, Pixel(result, 70, 10));
        }

        [TestMethod]
        public void When_Cropped_OutputIsTheSelectedRegion()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { Crop = new Rect(0.5, 0.5, 0.5, 0.5) });

            Assert.AreEqual((40, 20), (result.Width, result.Height));
            AssertNear(Yellow, Pixel(result, 20, 10));
        }

        [TestMethod]
        public void When_CropIsIgnored_OutputKeepsTheTransformedSize()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { Crop = new Rect(0.5, 0.5, 0.5, 0.5) }, applyCrop: false);

            Assert.AreEqual((80, 40), (result.Width, result.Height));
            AssertNear(Red, Pixel(result, 10, 10));
        }

        [TestMethod]
        public void When_Straightened_TheCornersHaveNoGaps()
        {
            using var source = Flat(128, 80, 40);

            using var result = EditRenderer.Render(source, EditState.Original with { Straighten = 12 });

            Assert.AreEqual((80, 40), (result.Width, result.Height));
            foreach (var (x, y) in new[] { (0, 0), (79, 0), (0, 39), (79, 39) })
                Assert.AreEqual((byte)255, Pixel(result, x, y).Alpha, $"corner {x},{y}");
        }

        [TestMethod]
        public void When_ScaleIsGiven_ResultHasThatSize()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { Exposure = 20 }, scale: 0.5);

            Assert.AreEqual((40, 20), (result.Width, result.Height));
        }

        [TestMethod]
        [DataRow(40.0, true)]
        [DataRow(-40.0, false)]
        public void When_ExposureChanges_GreyMovesTheRightWay(double exposure, bool brighter)
        {
            using var source = Flat(100);

            using var result = EditRenderer.Render(source, EditState.Original with { Exposure = exposure });

            // 2^(exposure / 100 * 1.5).
            var expected = (byte)Math.Round(100 * Math.Pow(2, exposure / 100 * 1.5));
            AssertNear(new SKColor(expected, expected, expected), Pixel(result, 32, 32), 2);
            Assert.AreEqual(brighter, Pixel(result, 32, 32).Red > 100);
        }

        [TestMethod]
        public void When_BrightnessIsRaised_GreyGetsBrighter()
        {
            using var source = Flat(100);

            using var result = EditRenderer.Render(source, EditState.Original with { Brightness = 50 });

            Assert.IsTrue(Pixel(result, 32, 32).Red > 110);
        }

        [TestMethod]
        public void When_ContrastIsRaised_DarkGetsDarkerAndBrightGetsBrighter()
        {
            using var dark = Flat(70);
            using var bright = Flat(190);
            var state = EditState.Original with { Contrast = 50 };

            using var darkResult = EditRenderer.Render(dark, state);
            using var brightResult = EditRenderer.Render(bright, state);

            Assert.IsTrue(Pixel(darkResult, 32, 32).Red < 70);
            Assert.IsTrue(Pixel(brightResult, 32, 32).Red > 190);
        }

        [TestMethod]
        public void When_WarmthIsRaised_RedGrowsAgainstBlue()
        {
            using var source = Flat(128);

            using var result = EditRenderer.Render(source, EditState.Original with { Warmth = 60 });

            var pixel = Pixel(result, 32, 32);
            Assert.IsTrue(pixel.Red > pixel.Blue + 20, pixel.ToString());
        }

        [TestMethod]
        public void When_SaturationIsRemoved_ColorsBecomeGrey()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { Saturation = -100 });

            var pixel = Pixel(result, 10, 10);
            Assert.AreEqual(pixel.Red, pixel.Green, 2);
            Assert.AreEqual(pixel.Green, pixel.Blue, 2);
        }

        [TestMethod]
        public void When_ShadowsAreRaised_DarkGetsBrighter()
        {
            using var source = Flat(40, 400, 400);

            using var result = EditRenderer.Render(source, EditState.Original with { Shadows = 80 });

            Assert.IsTrue(Pixel(result, 200, 200).Red > 50);
        }

        [TestMethod]
        public void When_HighlightsAreLowered_BrightGetsDarker()
        {
            using var source = Flat(230, 400, 400);

            using var result = EditRenderer.Render(source, EditState.Original with { Highlights = -80 });

            Assert.IsTrue(Pixel(result, 200, 200).Red < 200);
        }

        [TestMethod]
        public void When_ShadowsAndContrastAreCombined_TheColorStepsStillRun()
        {
            using var source = Flat(128, 400, 400);

            using var result = EditRenderer.Render(source, EditState.Original with { Shadows = 10, Saturation = -100, Warmth = 60 });

            // The color filters run after the highlights and shadows pass: saturation last removes the warmth.
            var pixel = Pixel(result, 200, 200);
            Assert.AreEqual(pixel.Red, pixel.Blue, 3);
        }

        [TestMethod]
        public void When_SharpnessIsRaised_EdgesGainContrast()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { Sharpness = 80, Saturation = -100 });
            using var plain = EditRenderer.Render(source, EditState.Original with { Saturation = -100 });

            var before = Math.Abs(Luma(Pixel(plain, 39, 10)) - Luma(Pixel(plain, 40, 10)));
            var after = Math.Abs(Luma(Pixel(result, 39, 10)) - Luma(Pixel(result, 40, 10)));
            Assert.IsTrue(after > before, $"{before} -> {after}");
        }

        [TestMethod]
        public void When_SharpnessIsNegative_EdgesAreBlurred()
        {
            using var source = Quadrants(400, 200);

            using var result = EditRenderer.Render(source, EditState.Original with { Sharpness = -100 });

            var left = Pixel(result, 199, 50);
            Assert.IsTrue(left.Green > Red.Green + 10, left.ToString());
        }

        [TestMethod]
        [DataRow(70.0, true)]
        [DataRow(-70.0, false)]
        public void When_VignetteIsSet_CornersChangeAndTheCenterStays(double vignette, bool darker)
        {
            using var source = Flat(128, 200, 200);

            using var result = EditRenderer.Render(source, EditState.Original with { Vignette = vignette });

            Assert.AreEqual(darker, Pixel(result, 1, 1).Red < 100, Pixel(result, 1, 1).ToString());
            Assert.AreEqual(!darker, Pixel(result, 1, 1).Red > 160, Pixel(result, 1, 1).ToString());
            AssertNear(new SKColor(128, 128, 128), Pixel(result, 100, 100), 4);
        }

        [TestMethod]
        public void When_EveryFilterIsApplied_OnlyNoneLeavesThePixels()
        {
            using var source = Quadrants();

            foreach (var preset in FilterPresets.All)
            {
                using var result = EditRenderer.Render(source, EditState.Original with { Filter = preset.Id });

                var same = Pixel(result, 10, 10) == Red && Pixel(result, 70, 30) == Yellow;
                Assert.AreEqual(preset.Id == FilterPresets.NoneId, same, preset.Id);
            }
        }

        /// <summary>Output of Win2D 1.3.2 for these edits, measured with the same mapping of the sliders.</summary>
        [TestMethod]
        [DataRow(64, nameof(EditState.Contrast), 100.0, 46, 46, 46, 3)]
        [DataRow(192, nameof(EditState.Contrast), 100.0, 210, 210, 210, 3)]
        [DataRow(64, nameof(EditState.Contrast), -100.0, 82, 82, 82, 3)]
        [DataRow(128, nameof(EditState.Warmth), 100.0, 169, 128, 81, 2)]
        [DataRow(128, nameof(EditState.Warmth), -100.0, 74, 128, 255, 2)]
        [DataRow(128, nameof(EditState.Tint), 100.0, 106, 128, 109, 2)]
        [DataRow(128, nameof(EditState.Tint), -100.0, 153, 128, 150, 2)]
        [DataRow(64, nameof(EditState.Shadows), 100.0, 81, 81, 81, 8)]
        [DataRow(64, nameof(EditState.Shadows), -100.0, 41, 41, 41, 8)]
                public void When_ComparedToWin2D_TheCurvesMatch(int grey, string property, double value, int red, int green, int blue, int tolerance)
        {
            using var source = Flat((byte)grey, 64, 64);
            var state = property switch
            {
                nameof(EditState.Contrast) => EditState.Original with { Contrast = value },
                nameof(EditState.Warmth) => EditState.Original with { Warmth = value },
                nameof(EditState.Tint) => EditState.Original with { Tint = value },
                nameof(EditState.Shadows) => EditState.Original with { Shadows = value },
                _ => EditState.Original with { Highlights = value }
            };

            using var result = EditRenderer.Render(source, state);

            AssertNear(new SKColor((byte)red, (byte)green, (byte)blue), Pixel(result, 32, 32), tolerance);
        }

        /// <summary>
        /// Win2D 1.3.2 highlights on flat colors. Saturated colors change much less than a grey of the same luminance, and
        /// white stays white while light grey darkens.
        /// </summary>
        [TestMethod]
        [DataRow(128, 128, 128, -100.0, 50, 50, 50, 3)]
        [DataRow(128, 128, 128, 100.0, 184, 184, 184, 3)]
        [DataRow(248, 248, 248, -100.0, 204, 204, 204, 4)]
        [DataRow(255, 255, 255, -100.0, 255, 255, 255, 2)]
        // Two full channels and one empty one are the weakest corner of the fit (the earlier curve was 85 off here).
        [DataRow(204, 0, 204, -100.0, 197, 0, 197, 14)]
        [DataRow(204, 0, 204, -50.0, 199, 0, 199, 8)]
        [DataRow(153, 0, 153, 100.0, 166, 0, 166, 14)]
        [DataRow(255, 255, 0, -50.0, 205, 206, 0, 8)]
        [DataRow(255, 0, 0, -100.0, 250, 0, 0, 8)]
        public void When_HighlightsAreSet_ColorsFollowWin2D(int r, int g, int b, double highlights, int red, int green, int blue, int tolerance)
        {
            using var surface = SKSurface.Create(new SKImageInfo(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul));
            surface.Canvas.Clear(new SKColor((byte)r, (byte)g, (byte)b));
            using var source = surface.Snapshot();

            using var result = EditRenderer.Render(source, EditState.Original with { Highlights = highlights });

            AssertNear(new SKColor((byte)red, (byte)green, (byte)blue), Pixel(result, 32, 32), tolerance);
        }

        /// <summary>
        /// Win2D 1.3.2 vignette on a white photo: the band is measured from each edge in units of the short side, so a wide
        /// photo darkens its long edges like its short ones, and corners blend the two distances. The steep bands near the
        /// corners are the least exact part of the fit.
        /// </summary>
        [TestMethod]
        [DataRow(500, 500, 30.0, 0, 250, 50, 4)]
        [DataRow(500, 500, 30.0, 50, 250, 242, 4)]
        [DataRow(500, 500, 30.0, 25, 25, 49, 6)]
        [DataRow(500, 500, 100.0, 0, 250, 124, 4)]
        [DataRow(500, 500, 100.0, 0, 0, 4, 6)]
        [DataRow(500, 500, 100.0, 50, 250, 212, 4)]
        [DataRow(500, 500, 100.0, 25, 25, 41, 6)]
        [DataRow(500, 500, 100.0, 250, 250, 255, 4)]
        [DataRow(1200, 400, 100.0, 600, 0, 124, 4)]
        [DataRow(1200, 400, 100.0, 60, 200, 242, 4)]
        [DataRow(1200, 400, 30.0, 60, 200, 255, 4)]
        public void When_VignetteIsSet_TheEdgesFollowWin2D(int width, int height, double vignette, int x, int y, int grey, int tolerance)
        {
            using var source = Flat(255, width, height);

            using var result = EditRenderer.Render(source, EditState.Original with { Vignette = vignette });

            AssertNear(new SKColor((byte)grey, (byte)grey, (byte)grey), Pixel(result, x, y), tolerance);
        }

        [TestMethod]
        public void When_BlurredInBands_TheBandEdgesDoNotShow()
        {
            using var source = Flat(128, 300, 400);

            using var result = EditRenderer.Render(source, EditState.Original with { Sharpness = -100, Shadows = 20 });

            using var bitmap = SKBitmap.FromImage(result);
            var first = bitmap.GetPixel(150, 0);
            for (var y = 0; y < 400; y++)
                AssertNear(first, bitmap.GetPixel(150, y), 1);
        }

        [TestMethod]
        public void When_ClarityIsRaised_EdgesGainContrastAndFlatPartsStay()
        {
            using var source = Quadrants(200, 100);

            using var result = EditRenderer.Render(source, EditState.Original with { Clarity = 100 });

            AssertNear(Red, Pixel(result, 20, 20), 1);
            Assert.IsTrue(Pixel(result, 99, 20).Green < Red.Green, Pixel(result, 99, 20).ToString());
        }

        [TestMethod]
        public void When_VignetteIsApplied_LongSidesDarkenLikeShortSides()
        {
            using var source = Flat(128, 400, 200);

            using var result = EditRenderer.Render(source, EditState.Original with { Vignette = 60 });

            // Win2D: a rounded rectangle measured from the edges, not a circle.
            AssertNear(new SKColor(128, 128, 128), Pixel(result, 200, 100), 1);
            Assert.AreEqual(Pixel(result, 200, 2).Red, Pixel(result, 2, 100).Red, 3);
        }

        [TestMethod]
        public void When_FilterIsExcluded_ThePixelsStay()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { Filter = FilterPresets.All[1].Id }, includeFilter: false);

            AssertNear(Red, Pixel(result, 10, 10), 0);
        }

        [TestMethod]
        public void When_FilterIntensityIsZero_ThePixelsStay()
        {
            using var source = Quadrants();

            using var result = EditRenderer.Render(source, EditState.Original with { Filter = FilterPresets.All[1].Id, FilterIntensity = 0 });

            AssertNear(Red, Pixel(result, 10, 10), 0);
        }

        [TestMethod]
        public void When_ColorMatrixIsConverted_OffsetsAndChannelsLandInTheSkiaLayout()
        {
            var matrix = ColorMatrix.Identity.Then(ColorMatrix.Sepia).ToSkiaMatrix();

            // Sepia: red output takes 0.393 of red, 0.769 of green and 0.189 of blue.
            Assert.AreEqual(0.393f, matrix[0], 1e-4f);
            Assert.AreEqual(0.769f, matrix[1], 1e-4f);
            Assert.AreEqual(0.189f, matrix[2], 1e-4f);
            Assert.AreEqual(1f, matrix[18], 1e-4f);
        }

        [TestMethod]
        public void When_FinalImageHasStrokes_TheyAreDrawnOnIt()
        {
            using var source = Flat(128, 100, 100);
            var stroke = new MarkupStroke(Color.FromArgb(255, 255, 0, 0), 0.1f, false, [new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f)]);

            using var result = EditRenderer.RenderFinal(source, EditState.Original with { Strokes = ImmutableList.Create(stroke) });

            AssertNear(new SKColor(255, 0, 0), Pixel(result, 50, 50));
            AssertNear(new SKColor(128, 128, 128), Pixel(result, 50, 10));
        }

        [TestMethod]
        public void When_StrokeIsAHighlighter_ThePhotoShowsThrough()
        {
            using var source = Flat(128, 100, 100);
            var stroke = new MarkupStroke(Color.FromArgb(255, 255, 0, 0), 0.1f, true, [new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f)]);

            using var result = EditRenderer.RenderFinal(source, EditState.Original with { Strokes = ImmutableList.Create(stroke) });

            var pixel = Pixel(result, 50, 50);
            Assert.IsTrue(pixel.Green > 50 && pixel.Red > pixel.Green, pixel.ToString());
        }

        [TestMethod]
        public void When_StrokeLiesInTheCroppedPart_ItFollowsTheCrop()
        {
            using var source = Flat(128, 100, 100);
            var stroke = new MarkupStroke(Color.FromArgb(255, 255, 0, 0), 0.05f, false, [new Vector2(0.75f, 0.75f)]);
            var state = EditState.Original with { Crop = new Rect(0.5, 0.5, 0.5, 0.5), Strokes = ImmutableList.Create(stroke) };

            using var result = EditRenderer.RenderFinal(source, state);

            AssertNear(new SKColor(255, 0, 0), Pixel(result, 25, 25));
        }

        [TestMethod]
        [DataRow("a.jpg", true)]
        [DataRow("a.JPEG", true)]
        [DataRow("a.png", true)]
        [DataRow("a.gif", false)]
        [DataRow("a.bmp", false)]
        [DataRow("a.webp", true)]
        public void When_FormatIsChecked_OnlyEncodableFormatsCanBeReplaced(string path, bool expected)
        {
            Assert.AreEqual(expected, EditRenderer.CanWriteFormat(path));
        }

        [TestMethod]
        public void When_Encoded_TheBytesMatchTheExtension()
        {
            using var source = Quadrants();

            var png = EditRenderer.Encode(source, "a.png", 90);
            var jpeg = EditRenderer.Encode(source, "a.jpg", 90);
            var webp = EditRenderer.Encode(source, "a.webp", 90);

            Assert.AreEqual(0x89, png[0]);
            Assert.AreEqual(0xFF, jpeg[0]);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(webp, 0, 4));
            Assert.AreEqual("WEBP", System.Text.Encoding.ASCII.GetString(webp, 8, 4));
        }

        [TestMethod]
        public void When_ManyStatesRenderInParallel_NothingCrashes()
        {
            using var source = Quadrants(400, 200);

            Parallel.For(0, 16, i =>
            {
                using var result = EditRenderer.Render(source, EditState.Original with { Exposure = i, Shadows = 10, Sharpness = 20, Vignette = 30 });
                Assert.AreEqual(400, result.Width);
            });
        }

        /// <summary>Not a limit, only the timing the plan asks for: set PHOTOS_TIMING to a file path to get it.</summary>
        [TestMethod]
        public void When_TimingIsRequested_Report24Megapixels()
        {
            if (Environment.GetEnvironmentVariable("PHOTOS_TIMING") is not { Length: > 0 } report)
                return;

            using var bitmap = TestImages.Gradient(6000, 4000);
            bitmap.SetImmutable();
            using var full = SKImage.FromBitmap(bitmap);
            using var preview2560 = EditRenderer.Render(full, EditState.Original, scale: 2560.0 / 6000);
            var all = EditState.Original with
            {
                Exposure = 20, Contrast = 20, Highlights = -20, Shadows = 20, Clarity = 20, Warmth = 10,
                Saturation = 10, Sharpness = 30, Vignette = 30, Filter = FilterPresets.All[1].Id
            };
            var color = EditState.Original with { Exposure = 20, Contrast = 20, Warmth = 10, Saturation = 10 };

            void Time(string name, SKImage image, EditState state, double scale)
            {
                EditRenderer.Render(image, state, scale: scale).Dispose();
                var watch = Stopwatch.StartNew();
                using var result = EditRenderer.Render(image, state, scale: scale);
                File.AppendAllText(report, $"{name}: {watch.ElapsedMilliseconds} ms ({result.Width} x {result.Height})\n");
            }

            Time("full all", full, all, 1);
            Time("full color", full, color, 1);
            Time("preview2560 all", preview2560, all, 1);
            Time("preview2560 color", preview2560, color, 1);
            Time("screen1600 all", preview2560, all, 1600.0 / 2560);
            Time("screen1600 color", preview2560, color, 1600.0 / 2560);
            Time("screen1000 all", preview2560, all, 1000.0 / 2560);
            var watchSave = Stopwatch.StartNew();
            using var final = EditRenderer.RenderFinal(full, all);
            var bytes = EditRenderer.Encode(final, "a.jpg", 92);
            File.AppendAllText(report, $"save jpeg: {watchSave.ElapsedMilliseconds} ms, {bytes.Length / 1024} KB\n");
        }
    }
}
