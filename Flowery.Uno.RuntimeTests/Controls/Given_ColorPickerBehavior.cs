using System.Threading.Tasks;
using Flowery.Controls.ColorPicker;
using Flowery.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using Colors = Microsoft.UI.Colors;

namespace Flowery.Uno.RuntimeTests.Controls
{
    [TestClass]
    public class Given_ColorPickerBehavior
    {
        [TestMethod]
        public void When_ColorEditor_ColorChanges_ComponentsSync()
        {
            var editor = new DaisyColorEditor();
            var color = Color.FromArgb(128, 10, 20, 30);

            editor.Color = color;

            Assert.AreEqual((byte)10, editor.Red);
            Assert.AreEqual((byte)20, editor.Green);
            Assert.AreEqual((byte)30, editor.Blue);
            Assert.AreEqual((byte)128, editor.Alpha);
            Assert.AreEqual(FloweryColorHelpers.ColorToHex(color, includeAlpha: true), editor.HexValue);
        }

        [TestMethod]
        public void When_ColorEditor_AlphaHidden_HexUsesRgb()
        {
            var editor = new DaisyColorEditor
            {
                ShowAlphaChannel = false,
                Color = Color.FromArgb(255, 10, 20, 30)
            };

            Assert.IsTrue(editor.HexValue.StartsWith("#", System.StringComparison.Ordinal));
            Assert.AreEqual("#0A141E", editor.HexValue);
        }

        [TestMethod]
        public void When_ColorSlider_ChannelChanges_UpdatesRange()
        {
            var slider = new DaisyColorSlider
            {
                Channel = ColorSliderChannel.Hue
            };

            Assert.AreEqual(0, slider.Minimum);
            Assert.AreEqual(359, slider.Maximum);
        }

        [TestMethod]
        public async Task When_ColorWheel_HslChanges_ColorUpdates()
        {
            var wheel = new DaisyColorWheel
            {
                Width = 200,
                Height = 200
            };

            RuntimeTestHelpers.AttachToHost(wheel);
            await RuntimeTestHelpers.EnsureLoadedAsync(wheel);

            var hsl = new HslColor(120, 1, 0.5);
            wheel.HslColor = hsl;

            var expected = hsl.ToRgbColor();
            Assert.AreEqual(expected, wheel.Color);
        }

        [TestMethod]
        public void When_ColorGrid_SelectionChanges_ColorAndIndexSync()
        {
            var palette = ColorCollection.CreateGrayscale(2);
            var grid = new DaisyColorGrid
            {
                Palette = palette,
                ShowCustomColors = false
            };

            grid.Color = palette[1];
            Assert.AreEqual(1, grid.ColorIndex);

            grid.ColorIndex = 0;
            Assert.AreEqual(palette[0], grid.Color);
        }

        [TestMethod]
        public void When_ScreenColorPicker_Constructed_DefaultsHold()
        {
            var picker = new DaisyScreenColorPicker();

            Assert.AreEqual(Colors.Black, picker.Color);
            Assert.IsFalse(picker.IsCapturing);
            Assert.IsNotNull(picker.Content);
        }
    }
}
